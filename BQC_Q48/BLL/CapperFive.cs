using BQJX.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
using Q_Platform.Logger;
using System.Threading;
using System;
using System.Threading.Tasks;
using Q_Platform.Common;

namespace Q_Platform.BLL
{
    public class CapperFive : CapperBase ,ICapperFive
    {

        #region Private Members

        private static ILogger logger = new MyLogger(typeof(CapperFive));

        private readonly ICarrierTwo _carrier;

        private readonly static object _lockObj = new object();

        //private double _capperoff = -2; //拆盖偏移

        #endregion

        #region Construtors

        public CapperFive(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess,ICarrierTwo carrier) : base(io, motion, globalStatus, dataAccess, logger)
        {
            this._carrier = carrier;
            _axisY = 22;
            _axisC1 = 23;
            _axisC2 = 24;
            _axisZ = 27;
            _holding = 47;
            _claw = 48;

            _holdingCloseSensor = 48;  //I1.0
            _holdingOpenSensor = 49;   //I1.1
            _capperSensor = 50;        //I1.2
            _posData = _dataAccess.GetCapperPosData(5);

        }

        public override void UpdatePosData()
        {
            _posData = _dataAccess.GetCapperPosData(5);
        }

        public override CapperInfo GetCapperInfo()
        {
            var cpInfo = base.GetCapperInfo();
            cpInfo.CapperId = 5;
            cpInfo.CapperName = "ICapperFive";
            cpInfo.CapperOffDistance = -2;
            cpInfo.CapperOnTorque = 25;
            return cpInfo;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 装盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOnAsync(Sample sample, IGlobalStatus gs)
        {
            //判断样品是否有盖

            s1: var result = await CapperOn(18, 40, gs).ConfigureAwait(false);

            if (!result)
            {
                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                    {
                        Thread.Sleep(1000);
                    }

                    if (!_globalStatus.IsStopped)
                    {
                        goto s1;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 拆盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOffAsync(Sample sample, IGlobalStatus gs)
        {
            //判断样品是否有盖
           s1: var result = await CapperOff(gs, -2.5).ConfigureAwait(false);

            if (!result)
            {
                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                    {
                        Thread.Sleep(1000);
                    }

                    if (!_globalStatus.IsStopped)
                    {
                        goto s1;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 提取净化管  ===> 小瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool DoPipettingFromCapperThreeToBottle(Sample sample,IGlobalStatus gs)
        {
            try
            {
                lock (_lockObj)
                {
                    bool result;

                    result = _carrier.DoPipettingOne(sample, 1, CapOn, CapOff, gs);
                    if (!result)
                    {
                        throw new Exception($"样品小瓶{sample.Id}移液失败!");
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }


        /// <summary>
        /// 提取浓缩样品液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool DoPipettingFromCapperFourToBottle(Sample sample, IGlobalStatus gs)
        {
            try
            {
                if (!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractSample))
                {
                    return true;
                }
                lock (_lockObj)
                {
                    bool result;
                    //准备小瓶
                    if (sample.BottleStep == 0 && !_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖Y轴到接驳位出错!");
                        }
                        sample.BottleStep++;
                    }

                    //搬运小瓶到拧盖5 第一组
                    if (sample.BottleStep == 1 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1InShelf))
                        {
                            result = _carrier.GetBottleFromMaterialToCapperFive_One(sample, gs);
                            if (!result)
                            {
                                throw new Exception($"搬运{sample.Id}样品小瓶到拧盖5失败!");
                            }
                            sample.BottleStep++;
                        }
                        else
                        {
                           throw new Exception("步骤状态错误!");
                        }
                    }

                    //拆盖 第一组
                    if (sample.BottleStep == 2 && !_globalStatus.IsStopped)
                    {
                        if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1ExtractDone))
                        {
                            if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1UnCapped))
                            {
                                result = CapperOffAsync(sample, gs).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"样品小瓶{sample.Id}拆盖失败!");
                                }
                                SampleStatusHelper.SetBitOn(sample, SampleStatus.IsBottle1UnCapped);
                            }
                        }
                        sample.BottleStep++;
                    }

                    //两次移液
                    if (sample.BottleStep == 3 && !_globalStatus.IsStopped)
                    {
                        if (_unCapFalt == true)
                        {
                            throw new Exception("拧盖5检测有盖,请确认拆盖成功后继续程序!");
                        }

                        result = _carrier.DoPipettingOne(sample, 2, CapOn, CapOff, gs);
                        if (!result)
                        {
                            throw new Exception();
                        }
                        sample.BottleStep++;
                    }

                    if (sample.BottleStep == 4)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1InShelf)
                            && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2InShelf))
                        {
                            sample.BottleStep = 0;
                            return true;
                        }
                    }

                    return false;

                }
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 拆盖  拧盖4调用
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool CapperOff(Sample sample, IGlobalStatus gs)
        {
            if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1UnCapped))
            {
                //内部已有拆盖暂停判断
                var result = CapperOffAsync(sample, gs).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception($"样品小瓶{sample.Id}拆盖失败!");
                }
                SampleStatusHelper.SetBitOn(sample, SampleStatus.IsBottle1UnCapped);
            }
            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1UnCapped))
            {
                if (_unCapFalt == true)
                {
                    throw new Exception("拧盖5检测有盖,请确认拆盖成功后继续程序!");
                }
                return true;
            }
            return false;
        }


        #endregion

        #region Private Methods

        /// <summary>
        /// 从净化管提取样品液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">1:净化管 ==》小瓶  2:西林瓶==》小瓶</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        private bool DoPipetting(Sample sample, int var, IGlobalStatus gs)
        {
            bool result;
            lock (_lockObj)
            {
                if (sample.BottleStep == 0 && !_globalStatus.IsStopped)
                {
                    result = MovePutGetPos(gs).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖Y轴到接驳位出错!");
                    }
                    sample.BottleStep++;
                }

                //搬运小瓶到拧盖5 第一组
                if (sample.BottleStep == 1 && !_globalStatus.IsStopped)
                {
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1InShelf))
                    {
                        result = _carrier.GetBottleFromMaterialToCapperFive_One(sample, gs);
                        if (!result)
                        {
                            throw new Exception($"搬运{sample.Id}样品小瓶到拧盖5失败!");
                        }
                        sample.BottleStep++;
                    }
                    else
                    {
                        return false;
                    }
                }

                //拆盖 第一组
                if (sample.BottleStep == 2 && !_globalStatus.IsStopped)
                {
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1ExtractDone))
                    {
                        if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1UnCapped))
                        {
                            result = CapperOffAsync(sample, gs).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"样品小瓶{sample.Id}拆盖失败!");
                            }
                            SampleStatusHelper.SetBitOn(sample, SampleStatus.IsBottle1UnCapped);
                        }
                    }
                    sample.BottleStep++;
                }

                //两次移液
                if (sample.BottleStep == 3 && !_globalStatus.IsStopped)
                {
                    sample.BottleStep++;
                    if (_unCapFalt == true)
                    {
                        throw new Exception("拧盖5检测有盖,请确认拆盖成功后继续程序!");
                    }
                }

                if (sample.BottleStep == 4 )
                {
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1InShelf) 
                        && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2InShelf))
                    {
                        sample.BottleStep = 0;
                        return true;
                    }
                }
            
                return false;


            }

        }

        /// <summary>
        /// 装盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">1:第一组</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        private bool CapOn(Sample sample,int var ,IGlobalStatus gs)
        {
            bool result;
            //装盖
            if (var == 1 && !_globalStatus.IsStopped)
            {
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1UnCapped))
                {
                    result = CapperOnAsync(sample, gs).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception($"样品小瓶{sample.Id}拆盖失败!");
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsBottle1UnCapped);
                    return true;
                }
               
            }
            else if (var == 2 && !_globalStatus.IsStopped)
            {
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2UnCapped))
                {
                    result = CapperOnAsync(sample, gs).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception($"样品小瓶{sample.Id}拆盖失败!");
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsBottle2UnCapped);
                    return true;
                }

            }
            return false;

        }    
        
        /// <summary>
        /// 第二组拆盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        private bool CapOff(Sample sample,int var,IGlobalStatus gs)
        {
            bool result;

            //拆盖
            if (var ==1 && !_globalStatus.IsStopped)
            {
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1UnCapped))
                {
                    result = CapperOffAsync(sample, gs).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception($"样品小瓶{sample.Id}拆盖失败!");
                    }
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsBottle1UnCapped);
                    if (_unCapFalt == true)
                    {
                        throw new Exception("拧盖5检测有盖,请确认拆盖成功后继续程序!");
                    }
                    return true;
                }
               
            }
            else if(var == 2 && !_globalStatus.IsStopped)
            {
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2UnCapped))
                {
                    result = CapperOffAsync(sample, gs).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception($"样品小瓶{sample.Id}拆盖失败!");
                    }
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsBottle2UnCapped);
                    if (_unCapFalt == true)
                    {
                        throw new Exception("拧盖5检测有盖,请确认拆盖成功后继续程序!");
                    }
                    return true;
                }
             
            }

            return false;

        }


        #endregion



    }
}
