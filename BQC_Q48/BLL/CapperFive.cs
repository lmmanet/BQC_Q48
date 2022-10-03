using BQJX.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
using Q_Platform.Logger;
using System.Threading;
using System;
using System.Threading.Tasks;

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

            //_xOffset = 60;    //拧盖X偏移量
            _posData = _dataAccess.GetCapperPosData(5);

        }

        public override void UpdatePosData()
        {
            _posData = _dataAccess.GetCapperPosData(5);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 装盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOnAsync(Sample sample, CancellationTokenSource cts)
        {
            //判断样品是否有盖

            var result = await CapperOn(30, 40, cts).ConfigureAwait(false);

            if (!result)
            {
                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause)
                    {
                        Thread.Sleep(2000);
                    }

                    if (!_globalStatus.IsStopped)
                    {
                        return await CapperOnAsync(sample, cts);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 拆盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOffAsync(Sample sample, CancellationTokenSource cts)
        {
            //判断样品是否有盖
            var result = await CapperOff(cts, -2).ConfigureAwait(false);

            if (!result)
            {
                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause)
                    {
                        Thread.Sleep(2000);
                    }

                    if (!_globalStatus.IsStopped)
                    {
                        return await CapperOffAsync(sample, cts);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 提取净化管样品液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool DoPipettingFromCapperThreeToBottle(Sample sample,CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    bool result;

                    if (TechStatusHelper.BitIsOn(sample.TechParams,TechStatus.ExtractSample))
                    {
                        result = DoPipetting(sample, 1, cts);
                        if (!result)
                        {
                            throw new Exception();
                        }
                        TechStatusHelper.ResetBit(sample.TechParams, TechStatus.ExtractSample);
                    }

                    if (!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractSample))
                    {
                        return true;
                    }
                    throw new Exception("提取净化样品液失败!");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    _logger?.Error(ex.Message) ;
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
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool DoPipettingFromCapperFourToBottle(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    bool result;

                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractSample))
                    {
                        result = DoPipetting(sample, 2, cts);
                        if (!result)
                        {
                            throw new Exception();
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    _logger?.Error(ex.Message);
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 从净化管提取样品液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">1:净化管 ==》小瓶  2:西林瓶==》小瓶</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool DoPipetting(Sample sample, int var, CancellationTokenSource cts)
        {
            bool result;
            lock (_lockObj)
            {
                if (sample.SubStep ==0 && !_globalStatus.IsStopped)
                {
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖Y轴到接驳位出错!");
                    }
                    sample.SubStep++;
                }

                //搬运小瓶到拧盖5 第一组
                if (sample.SubStep == 1 && !_globalStatus.IsStopped)
                {
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1InShelf))
                    {
                        result = _carrier.GetBottleFromMaterialToCapperFive_One(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"搬运{sample.Id}样品小瓶到拧盖5失败!");
                        }
                    }
                    sample.SubStep++;
                }

                //拆盖 第一组
                if (sample.SubStep == 2 && !_globalStatus.IsStopped)
                {
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1ExtractDone))
                    {
                        if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1UnCapped))
                        {
                            result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"样品小瓶{sample.Id}拆盖失败!");
                            }
                            SampleStatusHelper.SetBitOn(sample, SampleStatus.IsBottle1UnCapped);
                        }
                    }
                    sample.SubStep++;
                }

                //两次移液
                if (sample.SubStep == 3 && !_globalStatus.IsStopped)
                {
                    result = _carrier.DoPipettingOne(sample, var, GetNextBottle, cts);
                    if (!result)
                    {
                        throw new Exception($"样品小瓶{sample.Id}移液失败!");
                    }
                    sample.SubStep++;
                }


                //装盖
                if (sample.SubStep == 4 && !_globalStatus.IsStopped)
                {
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2UnCapped))
                    {
                        result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"样品小瓶{sample.Id}拆盖失败!");
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsBottle2UnCapped);
                    }
                    sample.SubStep++;
                }


                //搬运小瓶到小瓶架
                if (sample.SubStep == 5 && !_globalStatus.IsStopped)
                {
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2InCapper))
                    {
                        result = _carrier.GetBottleFromCapperFiveToMaterial_Two(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"搬运{sample.Id}样品小瓶到小瓶架失败!");
                        }
                    }
                    sample.SubStep++;
                }

                if (sample.SubStep == 6 )
                {
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2InShelf))
                    {
                        sample.SubStep = 0;
                        return true;
                    }
                }
            
                return false;


            }

        }

        /// <summary>
        /// 取下第一组  上料第二组
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetNextBottle(Sample sample,CancellationTokenSource cts)
        {
            bool result;
            //装盖
            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1UnCapped))
            {
                result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception($"样品小瓶{sample.Id}拆盖失败!");
                }
                SampleStatusHelper.ResetBit(sample, SampleStatus.IsBottle1UnCapped);
            }

            //搬运小瓶到小瓶架
            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1InCapper))
            {
                result = _carrier.GetBottleFromCapperFiveToMaterial_One(sample, cts);
                if (!result)
                {
                    throw new Exception($"搬运{sample.Id}样品小瓶到小瓶架失败!");
                }
            }

            //搬运小瓶到拧盖5 第2组
            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2InShelf))
            {
                result = _carrier.GetBottleFromMaterialToCapperFive_Two(sample, cts);
                if (!result)
                {
                    throw new Exception($"搬运{sample.Id}样品小瓶到拧盖5失败!");
                }
            }

            //拆盖
            if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2UnCapped))
            {
                result = CapperOffAsync(sample,cts).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception($"样品小瓶{sample.Id}拆盖失败!");
                }
                SampleStatusHelper.SetBitOn(sample, SampleStatus.IsBottle2UnCapped);
            }

            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2UnCapped)&& SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1InShelf))
            {
                return true;
            }


            return false;

        }


        #endregion



    }
}
