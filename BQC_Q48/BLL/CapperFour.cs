using BQJX.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class CapperFour : CapperBase, ICapperFour
    {

        Task ConcentrationTask;

        #region Private Members

        private static ILogger logger = new MyLogger(typeof(CapperFour));

        private readonly ICarrierTwo _carrier;

        private readonly IConcentration _concentration;

        private readonly ICapperFive _capperFive;

        private readonly static object _lockObj = new object();

        private double _capperoff = -1.5; //拆盖偏移

        #endregion

        #region Construtors

        public CapperFour(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess, IConcentration concentration, ICarrierTwo carrier, ICapperFive capperFive) : base(io, motion, globalStatus, dataAccess, logger)
        {
            this._concentration = concentration;
            this._carrier = carrier;
            this._capperFive = capperFive;

            _axisY = 19;
            _axisC1 = 20;
            _axisC2 = 21;
            _axisZ = 28;
            _holding = 44;
            _claw = 45;

            _holdingCloseSensor = 45;  //I0.5
            _holdingOpenSensor = 46;   //I0.6

            _xOffset = 60;    //拧盖X偏移量
            _capperTorque = 30;  //拧盖力度
            _capperTimeout = 40;  //拧盖超时时间 S 
            _posData = _dataAccess.GetCapperPosData(4);

        }

        #endregion


        #region Public Methods


        public Task StartConcentration(Sample sample,CancellationTokenSource cts)
        {
            var ret = GlobalCache.Instance.ConcentrationList.Contains(sample);
            if (!ret)
            {
                GlobalCache.Instance.ConcentrationList.Add(sample);
            }

            if (ConcentrationTask != null)
            {
                if (!ConcentrationTask.IsCompleted)
                {
                    return ConcentrationTask;
                }
            }


            ConcentrationTask = Task.Run(() =>
            {
                while (GlobalCache.Instance.ConcentrationList.Count>0 && !_globalStatus.IsStopped)
                {
                    var itemSample = GlobalCache.Instance.ConcentrationList[0];

                    if (TechStatusHelper.BitIsOn(itemSample.TechParams,TechStatus.ExtractPurify))
                    {
                        _logger?.Info($"开始浓缩 -- {itemSample.Id}");
                    }

                    if (!TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractPurify)&& !TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractSupernate3)
                    && TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractSample)
                    )
                    {
                        _logger?.Info($"开始提取样品液 -- {itemSample.Id}");
                    }

                    if (TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractSupernate3))
                    {
                        _logger?.Info($"开始提取萃取液并浓缩 -- {itemSample.Id}");
                    }

                    GlobalCache.Instance.ConcentrationList.Remove(itemSample);
                }
             

            });

            return ConcentrationTask;
        }


        /// <summary>
        /// 农残浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool DoConcentrationOne(Sample sample,CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    bool result;

                    //搬运西林瓶到拧盖4 并称重
                    result = GetSeilingAndWeight11(sample,null, cts);
                    if (!result)
                    {
                        throw new Exception($"西林瓶{sample.Id}搬运到拧盖4 失败!");
                    }

                    //开始移取浓缩液 浓缩
                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Concentration))
                    {
                        result = _carrier.DoPipettingTwo(sample,1, cts);
                        if (!result)
                        {
                            throw new Exception($"西林瓶{sample.Id}移取待浓缩液失败!");
                        }

                        //搬运到浓缩
                        result = _carrier.GetSelingFromCapperFourToConcentration(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"西林瓶{sample.Id}搬运到浓缩失败!");
                        }

                        //开始浓缩
                        result = _concentration.DoConcentration(sample, cts);
                        if (!result)
                        {
                            return false;
                        }

                        //称重 判断结果  继续浓缩？
                        result = _carrier.GetSelingFromConcentrationToWeight(sample, AddMark_A, cts);
                        if (!result)
                        {
                            return false;
                        }

                    }

                    //加标


                    //取样
                    result = RedissolveAndGetSampleToBottle(sample, cts);
                    if (!result)
                    {
                        return false;
                    }

                    //完成
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

        /// <summary>
        /// 兽残浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool DoConcentrationTwo(Sample sample,CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    bool result;

                    //搬运西林瓶到拧盖4 并称重
                    result = GetSeilingAndWeight11(sample,AddMark_B, cts);
                    if (!result)
                    {
                        throw new Exception($"西林瓶{sample.Id}搬运到拧盖4 失败!");
                    }

                    //开始移取浓缩液 浓缩
                    result = _carrier.DoPipettingTwo(sample,2, cts);
                    if (!result)
                    {
                        throw new Exception($"西林瓶{sample.Id}移取待浓缩液失败!");
                    }

                    //搬运到浓缩
                    result = _carrier.GetSelingFromCapperFourToConcentration(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"西林瓶{sample.Id}搬运到浓缩失败!");
                    }

                    //开始浓缩
                    result = _concentration.DoConcentration(sample, cts);
                    if (!result)
                    {
                        return false;
                    }

                    //称重 判断结果  继续浓缩？
                    result = _carrier.GetSelingFromConcentrationToWeight(sample, null, cts);
                    if (!result)
                    {
                        return false;
                    }

                    //加标


                    //取样 并 搬回空管
                    result = RedissolveAndGetSampleToBottle(sample, cts);
                    if (!result)
                    {
                        return false;
                    }
         
                    //完成
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从净化管移液到小瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromPurify(Sample sample, CancellationTokenSource cts)
        {
            return _capperFive.DoPipettingFromCapperThreeToBottle(sample, cts);
        }


        /// <summary>
        /// 获取西林瓶重量 农残
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSeilingAndWeight(Sample sample, CancellationTokenSource cts)
        {

            try
            {
                lock (_lockObj)
                {
                    bool result;
                    //搬运西林瓶到拧盖4
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInShelf))
                    {
                        result = _carrier.GetSelingFromMaterialToCapperFour(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"西林瓶{sample.Id}搬运到拧盖4 失败!");
                        }
                    }

                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingUnCapped))
                    {
                        //拆盖
                        result = CapperOff(cts, _capperoff).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"西林瓶{sample.Id}拆盖失败!");
                        }
                    }

                    if ((sample.SeilingWeight1 == 0 || sample.SeilingWeight2 == 0) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                    {
                        //搬运到称重称重
                        result = _carrier.GetSelingFromCapperFourToWeightAndBack(sample, null, cts);
                        if (!result)
                        {
                            throw new Exception($"西林瓶{sample.Id}称重失败!");
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {

                throw;
            }



        }


        #endregion


        #region Private Methods

        /// <summary>
        /// 获取西林瓶重量
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSeilingAndWeight11(Sample sample,Func<Sample,CancellationTokenSource,bool> func,CancellationTokenSource cts)
        {
         
            try
            {
                lock (_lockObj)
                {
                    bool result;
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖Y轴移动到接驳位出错!");
                    }

                    //搬运西林瓶到拧盖4
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInShelf))
                    {
                        result = _carrier.GetSelingFromMaterialToCapperFour(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"西林瓶{sample.Id}搬运到拧盖4 失败!");
                        }
                    }

                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingUnCapped))
                    {
                        //拆盖
                        result = CapperOff(cts, _capperoff).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"西林瓶{sample.Id}拆盖失败!");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsSelingUnCapped);
                    }

                    if ((sample.SeilingWeight1 == 0 || sample.SeilingWeight2 == 0) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                    {
                        //搬运到称重称重
                        result = _carrier.GetSelingFromCapperFourToWeightAndBack(sample, func, cts);
                        if (!result)
                        {
                            throw new Exception($"西林瓶{sample.Id}称重失败!");
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
          

           
        }

        /// <summary>
        /// 复溶并提取样品液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool RedissolveAndGetSampleToBottle(Sample sample,CancellationTokenSource cts)
        {
            bool result;
            //复溶
            if (SampleStatusHelper.BitIsOn(sample,SampleStatus.IsSelingInConcentration) && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Redissolve))
            {
                result = _concentration.Redissolve(sample, cts);
                if (!result)
                {
                    throw new Exception($"样品{sample.Id}复溶 失败!");
                }
            }

            //从浓缩搬运到拧盖4
            if (SampleStatusHelper.BitIsOn(sample,SampleStatus.IsSelingInConcentration))
            {
                result = _carrier.GetSelingFromConcentrationToCapperFour(sample, cts);
                if (!result)
                {
                    throw new Exception($"西林瓶{sample.Id}搬运到拧盖4 失败!");
                }
            }

            //提取样品液
            if (SampleStatusHelper.BitIsOn(sample,SampleStatus.IsSelingInCapper))
            {
                result = _capperFive.DoPipettingFromCapperFourToBottle(sample, cts);
                if (!result)
                {
                    throw new Exception($"西林瓶{sample.Id}提取样品液 失败!");
                }
            }

            //下料
            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper) && !TechStatusHelper.BitIsOn(sample.TechParams,TechStatus.ExtractSample))
            {
                result = CapperOnAndGetSeilingBack(sample, cts);
                if (!result)
                {
                    throw new Exception("搬运空管到西林瓶架失败!");
                }
            }

            return true;
        }

        /// <summary>
        /// 装盖搬运空管到西林瓶架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool CapperOnAndGetSeilingBack(Sample sample,CancellationTokenSource cts)
        {
            bool result;
            //装盖
            if (SampleStatusHelper.BitIsOn(sample,SampleStatus.IsSelingUnCapped))
            {
                result = CapperOn(_capperTorque, 40, cts).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception($"西林瓶{sample.Id}装盖失败!");
                }
                SampleStatusHelper.ResetBit(sample, SampleStatus.IsSelingUnCapped);
            }

            //下料
            if (SampleStatusHelper.BitIsOn(sample,SampleStatus.IsSelingInCapper)&&!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingUnCapped))
            {
                result = _carrier.GetSelingFromCapperFourToMaterial(sample, cts);
                if (!result)
                {
                    throw new Exception($"西林瓶{sample.Id}搬运到西林瓶架失败!");
                }
            }

            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInShelf))
            {
                return true;
            }

            throw new Exception("样品复溶 提取样品 搬回空管失败 状态错误!");
        }

        /// <summary>
        /// 农残加标
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool AddMark_A(Sample sample, CancellationTokenSource cts)
        {
            return _carrier.AddMarkFromSourceToWeight(sample, 1, cts);
        }

        /// <summary>
        /// 兽残加标
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool AddMark_B(Sample sample,CancellationTokenSource cts)
        {
            return _carrier.AddMarkFromSourceToWeight(sample, 2, cts);
        }








        #endregion

    }
}
