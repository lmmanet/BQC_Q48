using BQJX.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.Common;
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

        //Task ConcentrationTask;

        #region Private Members

        private static ILogger logger = new MyLogger(typeof(CapperFour));

        private readonly ICarrierTwo _carrier;

        private readonly IConcentration _concentration;

        private readonly ICapperFive _capperFive;

        private readonly ICapperThree _capperThree;


        private readonly static object _lockObj = new object();


        #endregion

        #region Construtors

        public CapperFour(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess, IConcentration concentration, ICarrierTwo carrier, ICapperFive capperFive,ICapperThree capperThree) : base(io, motion, globalStatus, dataAccess, logger)
        {
            this._concentration = concentration;
            this._carrier = carrier;
            this._capperFive = capperFive;
            this._capperThree = capperThree;

            _axisY = 19;
            _axisC1 = 20;
            _axisC2 = 21;
            _axisZ = 28;
            _holding = 44;
            _claw = 45;

            _holdingCloseSensor = 45;  //I0.5
            _holdingOpenSensor = 46;   //I0.6
            _capperSensor = 47;        //I0.7
            _xOffset = 60;    //拧盖X偏移量
            _posData = _dataAccess.GetCapperPosData(4);

        }

        public override void UpdatePosData()
        {
            _posData = _dataAccess.GetCapperPosData(4);
        }

        public override CapperInfo GetCapperInfo()
        {
            var cpInfo = base.GetCapperInfo();
            cpInfo.CapperId = 4;
            cpInfo.CapperName = "ICapperFour";
            cpInfo.CapperOffDistance = -1.5;
            cpInfo.CapperOnTorque = 30;
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
            s1: var result = await CapperOn(30, 40, gs).ConfigureAwait(false);
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
            s1: var result = await CapperOff(gs, -1.5).ConfigureAwait(false);

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
        /// 农残浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool DoConcentrationOne(Sample sample,IGlobalStatus gs)
        {
            try
            {
                lock (_lockObj)
                {
                    bool result;

                    //开始移取浓缩液 浓缩
                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Concentration))
                    {
                        //搬运到浓缩
                        if (sample.MainStep == 16 && !_globalStatus.IsStopped)
                        {
                            result = _carrier.GetSelingFromCapperFourToConcentration(sample, gs);
                            if (!result)
                            {
                                throw new Exception($"西林瓶{sample.Id}搬运到浓缩失败!");
                            }
                            sample.MainStep++;
                        }

                        //开始浓缩
                        if (sample.MainStep == 17 && !_globalStatus.IsStopped)
                        {
                            result = _concentration.DoConcentration(sample, gs);
                            if (!result)
                            {
                                return false;
                            }
                            sample.MainStep++;
                        }

                        //称重 判断结果  继续浓缩？
                        if (sample.MainStep == 18 && !_globalStatus.IsStopped)
                        {
                            result = _carrier.GetSelingFromConcentrationToWeight(sample, 1, sample.TechParams.Add_Mark_B, gs);
                            if (!result)
                            {
                                return false;
                            }
                            sample.MainStep++;
                        }
                    }
                    else
                    {
                        sample.MainStep = 19;
                    }


                    //复溶 取样
                    if (sample.MainStep >= 19 && !_globalStatus.IsStopped)
                    {
                        result = RedissolveAndGetSampleToBottle(sample, gs);
                        if (!result)
                        {
                            return false;
                        }
                        return true;
                    }

                    //完成
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
        /// 兽残浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool DoConcentrationTwo(Sample sample,IGlobalStatus gs)
        {
            try
            {
                lock (_lockObj)
                {
                    bool result;

                    //搬运到浓缩
                    if (sample.MainStep == 16 && !_globalStatus.IsStopped)
                    {
                        result = _carrier.GetSelingFromCapperFourToConcentration(sample, gs);
                        if (!result)
                        {
                            throw new Exception($"西林瓶{sample.Id}搬运到浓缩失败!");
                        }
                        sample.MainStep++;
                    }

                    //开始浓缩
                    if (sample.MainStep == 17 && !_globalStatus.IsStopped)
                    {
                        result = _concentration.DoConcentration(sample, gs);
                        if (!result)
                        {
                            return false;
                        }
                        sample.MainStep++;
                    }


                    //称重 判断结果  继续浓缩？
                    if (sample.MainStep == 18 && !_globalStatus.IsStopped)
                    {
                        result = _carrier.GetSelingFromConcentrationToWeight(sample, 0,0, gs);
                        if (!result)
                        {
                            return false;
                        }
                        sample.MainStep++;
                    }

                    //取样 并 搬回空管
                    if (sample.MainStep >= 19 && !_globalStatus.IsStopped)
                    {
                        result = RedissolveAndGetSampleToBottle(sample, gs,false);
                        if (!result)
                        {
                            return false;
                        }
                        return true;
                    }

                    //完成
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 农残移液  从净化管 ==》 西林瓶  从净化管==》小瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">1:浓缩移液 2:提取样品</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool DoPipettingOne(Sample sample, int var, IGlobalStatus gs)
        {
            bool result;

            if (var == 1 && !_globalStatus.IsStopped)
            {
                //拧盖4  搬运搬运西林瓶到拧盖4  =》拆盖 ==》称重
                if (sample.MainStep == 8 && !_globalStatus.IsStopped)
                {
                    //内部已经加锁
                    result = GetSeilingAndWeight(sample, gs).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("搬运西林到称重失败!");
                    }
                    sample.MainStep = 9;
                }

                //移液
                lock (_lockObj)
                {
                    //内部子步骤已经完成
                    if (sample.MainStep == 9 && !_globalStatus.IsStopped)
                    {
                        result = _capperThree.DoPipetting(sample, DoPipettingOneSub, gs);
                        if (!result)
                        {
                            throw new Exception("从净化管移液到西林瓶失败!");
                        }
                        sample.MainStep = 13;
                        return true;
                    }
                }

            }

            else if (var == 2 && !_globalStatus.IsStopped)
            {
                if (sample.MainStep == 8 && !_globalStatus.IsStopped)
                {
                    sample.MainStep = 20;
                }

                //准备小瓶
                if (sample.MainStep == 20 && !_globalStatus.IsStopped)
                {
                    //拧盖5  搬运小瓶到拧盖5
                    //搬运小瓶到拧盖5 第一组
                    if (sample.SubStep == 0 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1InShelf))
                        {
                            result = _carrier.GetBottleFromMaterialToCapperFive_One(sample, gs);
                            if (!result)
                            {
                                throw new Exception($"搬运{sample.Id}样品小瓶到拧盖5失败!");
                            }
                            sample.SubStep++;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    //拆盖 第一组
                    if (sample.SubStep == 1 && !_globalStatus.IsStopped)
                    {
                        result = _capperFive.CapperOff(sample, gs);
                        if (!result)
                        {
                            throw new Exception($"样品小瓶{sample.Id}拆盖失败!");
                        }
                        sample.SubStep = 0;
                        sample.MainStep++;
                    }
                }

                //移液
                if (sample.MainStep == 21 && !_globalStatus.IsStopped)
                {
                    result = _capperThree.DoPipetting(sample, _capperFive.DoPipettingFromCapperThreeToBottle, gs);
                    if (!result)
                    {
                        throw new Exception($"净化管{sample.Id}提取样品液 失败!");
                    }
                    sample.MainStep = 30;//完成
                    sample.SubStep = 0;
                    return true;
                }
            }

            

            return false;

        }
        private bool DoPipettingOneSub(Sample sample,IGlobalStatus  gs)
        {
            //净化管到西林瓶
            return _carrier.DoPipettingTwo(sample, 1, gs);
        }


        /// <summary>
        /// 获取西林瓶重量
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">加标1</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public async Task<bool> GetSeilingAndWeight(Sample sample, IGlobalStatus gs)
        {
            await Task.Delay(100).ConfigureAwait(false);
            try
            {
                lock (_lockObj)
                {
                    bool result;
                    if (sample.SeilingStep == 0 && !_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖Y轴移动到接驳位出错!");
                        }
                        sample.SeilingStep++;
                    }


                    //搬运西林瓶到拧盖4
                    if (sample.SeilingStep == 1 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInShelf))
                        {
                            result = _carrier.GetSelingFromMaterialToCapperFour(sample, gs);
                            if (!result)
                            {
                                throw new Exception($"西林瓶{sample.Id}搬运到拧盖4 失败!");
                            }
                        }
                        sample.SeilingStep++;
                    }

                    if (sample.SeilingStep == 2 && !_globalStatus.IsStopped)
                    {
                        if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingUnCapped))
                        {
                            //拆盖
                            result = CapperOffAsync(sample, gs).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"西林瓶{sample.Id}拆盖失败!");
                            }
                            SampleStatusHelper.SetBitOn(sample, SampleStatus.IsSelingUnCapped);
                        }
                        sample.SeilingStep++;

                        if (_unCapFalt == true)
                        {
                            throw new Exception("拧盖4检测有盖,请确认拆盖成功后继续程序!");
                        }
                    }

                    if (sample.SeilingStep == 3 && !_globalStatus.IsStopped)
                    {
                        if ((sample.SeilingWeight1 == 0 || sample.SeilingWeight2 == 0) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                        {
                            //搬运到称重称重  内部已经把步骤置零
                            result = _carrier.GetSelingFromCapperFourToWeightAndBack(sample, 2, sample.TechParams.Add_Mark_A, gs);
                            if (!result)
                            {
                                throw new Exception($"西林瓶{sample.Id}称重失败!");
                            }
                        }
                        return true;
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
                _globalStatus.PauseProgram();
                return false;
            }



        }

        #endregion

        #region Protected Methods

        protected override double GetCapperSensorCoordinate()
        {
            return _posData.CapperPos + 48;
        }

        #endregion

        #region Private Methods


        /// <summary>
        /// 复溶并提取样品液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <param name="isExtractToSample">是否提取到小瓶</param>
        /// <returns></returns>
        private bool RedissolveAndGetSampleToBottle(Sample sample,IGlobalStatus gs,bool isExtractToSample = true)
        {
            bool result;
            //复溶
            if (sample.MainStep == 19 && !_globalStatus.IsStopped)
            {
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInConcentration) && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Redissolve)
                    && !sample.ConcentrationFailure && !sample.ConcentrationFailure2)
                {
                    result = _concentration.Redissolve(sample, gs);
                    if (!result)
                    {
                        throw new Exception($"样品{sample.Id}复溶 失败!");
                    }
                }
                sample.MainStep++;
            }

            //从浓缩搬运到拧盖4
            if (sample.MainStep == 20 && !_globalStatus.IsStopped)
            {
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInConcentration))
                {
                    result = _carrier.GetSelingFromConcentrationToCapperFour(sample, gs);
                    if (!result)
                    {
                        throw new Exception($"西林瓶{sample.Id}搬运到拧盖4 失败!");
                    }
                    sample.MainStep++;
                }
                if (!isExtractToSample) //无需提取到小瓶
                {
                    sample.MainStep = 22;
                }
            }

            //提取样品液
            if (sample.MainStep == 21 && !_globalStatus.IsStopped)
            {
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper) && !sample.ConcentrationFailure && !sample.ConcentrationFailure2)
                {
                    result = _capperFive.DoPipettingFromCapperFourToBottle(sample, gs);
                    if (!result)
                    {
                        throw new Exception($"西林瓶{sample.Id}提取样品液 失败!");
                    }
                }
                sample.MainStep++;
            }


            //下料
            if (sample.MainStep == 22 && !_globalStatus.IsStopped)
            {
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                {
                    result = CapperOnAndGetSeilingBack(sample, gs);
                    if (!result)
                    {
                        throw new Exception("搬运空管到西林瓶架失败!");
                    }
                }
                sample.MainStep++;
            }

            if (sample.MainStep == 23)
            {
                sample.SubStep = 0;
                sample.MainStep = 30;  //完成
                return true;
            }

            throw new Exception("复溶状态错误!");
         
        }

        /// <summary>
        /// 装盖搬运空管到西林瓶架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        private bool CapperOnAndGetSeilingBack(Sample sample,IGlobalStatus gs)
        {
            bool result;
            //装盖
            if (SampleStatusHelper.BitIsOn(sample,SampleStatus.IsSelingUnCapped) && !_globalStatus.IsStopped)
            {
                result = CapperOnAsync(sample, gs).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception($"西林瓶{sample.Id}装盖失败!");
                }
                SampleStatusHelper.ResetBit(sample, SampleStatus.IsSelingUnCapped);
            }

            //下料
            if (SampleStatusHelper.BitIsOn(sample,SampleStatus.IsSelingInCapper)&&!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingUnCapped) && !_globalStatus.IsStopped)
            {
                result = _carrier.GetSelingFromCapperFourToMaterial(sample, gs);
                if (!result)
                {
                    throw new Exception($"西林瓶{sample.Id}搬运到西林瓶架失败!");
                }
            }

            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInShelf))
            {
                return true;
            }

            if (!_globalStatus.IsStopped)
            {
                throw new Exception("样品复溶 提取样品 搬回空管失败 状态错误!");
            }
            return false;
        }



        #endregion

    }
}
