using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.Common;
using Q_Platform.DAL;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class CarrierTwo : CarrierBase , ICarrierTwo
    {
        private static ILogger logger = new MyLogger(typeof(CarrierTwo));

        private int _pipStep;    //移液步骤

        private readonly ISyringTwo _syring;

        #region Private Members

        private readonly static object _lockObj = new object();

        private CarrierTwoPosData _posData;

        private ICarrierTwoDataAccess _dataAccess;

        private readonly IWeight _weight;

        private readonly IIoDevice _io;

        private readonly ushort _axisSyring = 15;//注射器

        private readonly ushort _weithtId = 3;

        private readonly ushort _zCylinderCtr = 51;//Q1.3

        private readonly ushort _zCylinderUpSensor = 51;//I1.3

        private readonly ushort _drainPump = 57;    //排液泵 Q2.1

        private readonly double _markSyringVel = 20;

        private readonly double _markObsorbVel = 3;

        #endregion

        #region Construtors
        public CarrierTwo(IEtherCATMotion motion, IIoDevice io, IEPG26 claw, IGlobalStatus globalStatus, ICarrierTwoDataAccess dataAccess, IWeight weight,ISyringTwo syring) : base(motion, claw, globalStatus, logger)
        {
            _axisX = 9;
            _axisY = 10;
            _axisZ1 = 11;
            _axisZ2 = 12;
            _axisP = 14;
            _clawSlaveId = 3;
            _putOffNeedle = -0.5;
            this._dataAccess = dataAccess;
            this._weight = weight;
            this._io = io;
            this._syring = syring;
            _posData = _dataAccess.GetPosData();
        }

        public override void UpdatePosData()
        {
            _posData = _dataAccess.GetPosData();
        }

        public override CarrierInfo GetCarrierInfo()
        {
            var result = base.GetCarrierInfo();
            result.CarrierName = "ICarrierTwo";
            result.CarrierId = 2;
            return result;
        }

        #endregion

        #region Public Methods

        public override async Task<bool> GoHome(IGlobalStatus gs)
        {
            _pipStep = 0;
            if (!_motion.IsServeOn(_axisSyring))
            {
                _motion.ServoOn(_axisSyring);
            }
      
            var result = await _motion.GohomeWithCheckDone(_axisSyring, 21, _globalStatus).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }

            //排空残留
            _io.WriteBit_DO_Delay_Reverse(_drainPump, 20);

            return await base.GoHome(gs);
        }

        public override bool StopMove()
        {
            _motion.StopMove(_axisSyring);
            return base.StopMove();
        }

        //========================================西林瓶========================================================//

        /// <summary>
        /// 从西林瓶架搬运西林瓶到拧盖4
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSelingFromMaterialToCapperFour(Sample sample, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从西林瓶架搬运{sampleId}西林瓶到拧盖4");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInShelf))
                    {
                        if (sample.SeilingStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromMaterialToCapperFour((ushort)(2 * sampleId - 1), gs);
                            if (!result)
                            {
                                throw new Exception($"从西林瓶架搬运{sampleId}西林瓶到拧盖4失败！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            sample.SeilingStatus = 1;
                        }
                        if (sample.SeilingStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromMaterialToCapperFour((ushort)(2 * sampleId), gs);
                            if (!result)
                            {
                                throw new Exception($"从西林瓶架搬运{sampleId}西林瓶到拧盖4！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            sample.SeilingStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsSelingInShelf);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsSelingInCapper);
                    }

                    //西林瓶在拧盖   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从西林瓶架搬运{sampleId}西林瓶到拧盖4失败,SampleStatus-{sample.Status}");
                }
            } 
            catch (OccupyMethodException)
            {
               return GetSelingFromMaterialToCapperFour(sample, gs);
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
        /// 从拧盖4搬运西林瓶到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSelingFromCapperFourToMaterial(Sample sample, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从拧盖4搬运{sampleId}西林瓶到西林瓶架");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                    {
                        if (sample.SeilingStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromCapperFourToMaterial((ushort)(2 * sampleId - 1),null,null, gs);
                            if (!result)
                            {
                                throw new Exception($"从拧盖4搬运{sampleId}西林瓶到西林瓶架 失败！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            sample.SeilingStatus = 1;
                        }
                        if (sample.SeilingStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromCapperFourToMaterial((ushort)(2 * sampleId),null,null, gs);
                            if (!result)
                            {
                                throw new Exception($"从拧盖4搬运{sampleId}西林瓶到西林瓶架 失败！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            sample.SeilingStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsSelingInCapper);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsSelingInShelf);
                    }

                    //西林瓶在架子
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInShelf))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从拧盖4搬运{sampleId}西林瓶到西林瓶架 失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSelingFromCapperFourToMaterial(sample, gs);
            }
            catch (Exception ex)
            {
                if(_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从拧盖4搬运西林瓶到浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSelingFromCapperFourToConcentration(Sample sample, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从拧盖4搬运{sampleId}西林瓶到浓缩");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                    {
                        if (sample.SeilingStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromCapperFourToConcentration((ushort)(2 * sampleId - 1), null, null, gs);
                            if (!result)
                            {
                                throw new Exception($"从拧盖4搬运{sampleId}西林瓶到浓缩 失败！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            sample.SeilingStatus = 1;
                        }
                        if (sample.SeilingStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromCapperFourToConcentration((ushort)(2 * sampleId), null, null, gs);
                            if (!result)
                            {
                                throw new Exception($"从拧盖4搬运{sampleId}西林瓶到浓缩 失败！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            sample.SeilingStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsSelingInCapper);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsSelingInConcentration);
                    }

                    //在浓缩
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInConcentration))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从从拧盖4搬运{sampleId}西林瓶到浓缩 失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSelingFromCapperFourToConcentration(sample, gs);
            }
            catch (Exception ex)
            {
               if(_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从浓缩搬运西林瓶到拧盖4
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSelingFromConcentrationToCapperFour(Sample sample, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从浓缩搬运{sampleId}西林瓶到拧盖4");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInConcentration))
                    {
                        if (sample.SeilingStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromConcentrationToCapperFour((ushort)(2 * sampleId - 1), gs);
                            if (!result)
                            {
                                throw new Exception($"从浓缩搬运{sampleId}西林瓶到拧盖4 失败！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            sample.SeilingStatus = 1;
                        }
                        if (sample.SeilingStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromConcentrationToCapperFour((ushort)(2 * sampleId), gs);
                            if (!result)
                            {
                                throw new Exception($"从浓缩搬运{sampleId}西林瓶到拧盖4 失败！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            sample.SeilingStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsSelingInConcentration);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsSelingInCapper);
                    }

                    //试管在拧盖3   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从浓缩搬运{sampleId}西林瓶到拧盖4 失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSelingFromConcentrationToCapperFour(sample, gs);
            }
            catch (Exception ex)
            {
               if(_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从拧盖4搬运西林瓶到称重 并搬运回
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSelingFromCapperFourToWeightAndBack(Sample sample,int var ,double volume, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从拧盖4搬运{sampleId}西林瓶到称重");

                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                    {
                        if (sample.SeilingStatus == 0 && sample.SeilingWeight1 == 0 && !_globalStatus.IsStopped)
                        {
                            //搬运到称重
                            if (sample.SeilingStep == 3 && !_globalStatus.IsStopped)
                            {
                                result = WeightClear();
                                if (!result)
                                {
                                    throw new Exception("称台清零出错!");
                                }

                                result = GetSeilingFromCapperFourToWeight((ushort)(2 * sampleId - 1), gs);
                                if (!result)
                                {
                                    throw new Exception($"从拧盖4搬运{sampleId}西林瓶到称台 失败！ SeilingStatus-{sample.SeilingStatus}");
                                }
                                sample.SeilingStep++;
                            }

                            //读取称台值
                            if (sample.SeilingStep == 4 && !_globalStatus.IsStopped)
                            {
                                sample.SeilingWeight1 = ReadWeight();
                                sample.SeilingStep++;
                            }
                         
                            if (sample.SeilingStep == 5 && !_globalStatus.IsStopped)
                            {
                                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddMark1) && var != 0)
                                {
                                    result = AddMarkFromSourceToWeight(var, volume, gs);
                                    if (!result)
                                    {
                                        throw new Exception("浓缩前加标失败!");
                                    }
                                }
                                sample.SeilingStep++;
                            }
                            //搬运回
                            if (sample.SeilingStep == 6 && !_globalStatus.IsStopped)
                            {
                                result = GetSeilingFromWeightToCapperFour((ushort)(2 * sampleId - 1), gs);
                                if (!result)
                                {
                                    throw new Exception($"从称台搬运{sampleId}西林瓶到拧盖4 失败！ SeilingStatus-{sample.SeilingStatus}");
                                }
                                sample.SeilingStep++;
                            }
                            sample.SeilingStatus = 1;
                        }

                        if (sample.SeilingStatus == 1 && sample.SeilingWeight2 == 0 && !_globalStatus.IsStopped)
                        {
                            //搬运到称重
                            if (sample.SeilingStep == 7 && !_globalStatus.IsStopped)
                            {
                                result = WeightClear();
                                if (!result)
                                {
                                    throw new Exception("称台清零出错!");
                                }

                                result = GetSeilingFromCapperFourToWeight((ushort)(2 * sampleId), gs);
                                if (!result)
                                {
                                    throw new Exception($"从拧盖4搬运{sampleId}西林瓶到称台 失败！ SeilingStatus-{sample.SeilingStatus}");
                                }
                                sample.SeilingStep++;
                            }

                            //读取称台值
                            if (sample.SeilingStep == 8 && !_globalStatus.IsStopped)
                            {
                                sample.SeilingWeight2 = ReadWeight();
                                sample.SeilingStep++;
                            }

                            if (sample.SeilingStep == 9 && !_globalStatus.IsStopped)
                            {
                                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddMark1) && var != 0)
                                {
                                    result = AddMarkFromSourceToWeight(var, volume, gs);
                                    if (!result)
                                    {
                                        throw new Exception("浓缩前加标失败!");
                                    }
                                }
                                sample.SeilingStep++;
                            }

                            //搬运回
                            if (sample.SeilingStep == 10 && !_globalStatus.IsStopped)
                            {
                                result = GetSeilingFromWeightToCapperFour((ushort)(2 * sampleId), gs);
                                if (!result)
                                {
                                    throw new Exception($"从称台搬运{sampleId}西林瓶到拧盖4 失败！ SeilingStatus-{sample.SeilingStatus}");
                                }
                                sample.SeilingStatus = 0;
                                sample.SeilingStep++;
                            }
                        }
                    }

                    //试管在拧盖3   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        sample.SeilingStep = 0;
                        return true;
                    }
                    throw new Exception($"从拧盖4搬运{sampleId}西林瓶到称重 失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSelingFromCapperFourToWeightAndBack(sample, var,volume, gs);
            }
            catch (Exception ex)
            {
               if(_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从浓缩搬运西林瓶到称重 并搬运回
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">加标种类1~4 0：不加</param>
        /// <param name="volume">加标量</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSelingFromConcentrationToWeight(Sample sample, int var, double volume, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从浓缩搬运{sampleId}西林瓶到称重");

                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInConcentration))
                    {
                        if (sample.SeilingStatus == 0 && !_globalStatus.IsStopped)
                        {
                            //搬运到称重
                            if (sample.SubStep == 0 && !_globalStatus.IsStopped)
                            {
                                result = WeightClear();
                                if (!result)
                                {
                                    throw new Exception("称台清零出错!");
                                }

                                result = GetSeilingFromConcentrationToWeight((ushort)(2 * sampleId - 1), gs);
                                if (!result)
                                {
                                    throw new Exception($"从拧盖4搬运{sampleId}西林瓶到称台 失败！ SeilingStatus-{sample.SeilingStatus}");
                                }
                                sample.SubStep++;
                            }

                            if (sample.SubStep == 1 && !_globalStatus.IsStopped)
                            {  //读取称台值
                                var weitht = ReadWeight();
                                if (weitht <= sample.SeilingWeight1 + 0.5 && !sample.ConcentrationFailure)
                                {
                                    //_logger?.Debug($"称台数据 空瓶：{sample.SeilingWeight1} 浓缩后：{weitht}");
                                    //判断是否加标
                                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddMark2) && var != 0)
                                    {
                                        result = AddMarkFromSourceToWeight(var,volume, gs);
                                        if (!result)
                                        {
                                            throw new Exception($"{sampleId}西林瓶加标失败");
                                        }
                                        //TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddMark1); 下一步复位
                                    }
                                }
                                else
                                {
                                    _logger?.Debug($"浓缩失败 称台数据 空瓶：{sample.SeilingWeight1} 浓缩后：{weitht}");
                                    sample.ConcentrationFailure = true;
                                }
                                sample.SubStep++;
                            }

                            //搬运回
                            if (sample.SubStep ==2 && !_globalStatus.IsStopped)
                            {
                                result = GetSeilingFromWeightToConcentration((ushort)(2 * sampleId - 1), gs);
                                if (!result)
                                {
                                    throw new Exception($"从称台搬运{sampleId}西林瓶到浓缩 失败！ SeilingStatus-{sample.SeilingStatus}");
                                }
                                sample.SubStep++;
                            }
                          
                            sample.SeilingStatus = 1;
                        }

                        if (sample.SeilingStatus == 1 && !_globalStatus.IsStopped)
                        {
                            //搬运到称重
                            if (sample.SubStep == 3 && !_globalStatus.IsStopped)
                            {
                                result = WeightClear();
                                if (!result)
                                {
                                    throw new Exception("称台清零出错!");
                                }

                                result = GetSeilingFromConcentrationToWeight((ushort)(2 * sampleId), gs);
                                if (!result)
                                {
                                    throw new Exception($"从拧盖4搬运{sampleId}西林瓶到称台 失败！ SeilingStatus-{sample.SeilingStatus}");
                                }
                                sample.SubStep++;
                            }

                            //读取称台值
                            if (sample.SubStep == 4 && !_globalStatus.IsStopped)
                            {
                                var weitht = ReadWeight();
                                if (weitht <= sample.SeilingWeight2 + 0.5 && !sample.ConcentrationFailure2)
                                {
                                    //_logger?.Debug($"称台数据 空瓶：{sample.SeilingWeight2} 浓缩后：{weitht}");
                                    //判断是否加标
                                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddMark2) && var != 0)
                                    {
                                        result = AddMarkFromSourceToWeight(var, volume, gs);
                                        if (!result)
                                        {
                                            throw new Exception($"{sampleId}西林瓶加标失败");
                                        }
                                    }
                                }
                                else
                                {
                                    _logger?.Debug($"浓缩失败 称台数据 空瓶：{sample.SeilingWeight1} 浓缩后：{weitht}");
                                    sample.ConcentrationFailure2 = true;
                                }
                                sample.SubStep++;
                            }


                            //搬运回
                            if (sample.SubStep == 5 && !_globalStatus.IsStopped)
                            {
                                result = GetSeilingFromWeightToConcentration((ushort)(2 * sampleId), gs);
                                if (!result)
                                {
                                    throw new Exception($"从称台搬运{sampleId}西林瓶到浓缩 失败！ SeilingStatus-{sample.SeilingStatus}");
                                }
                                sample.SubStep++;
                            }
                            if (sample.SubStep == 6)
                            {
                                sample.SubStep = 0;
                                sample.SeilingStatus = 0;
                                GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                                return true;
                            }
                        }
                    }
                    throw new Exception($"从浓缩搬运{sampleId}西林瓶到称重 失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSelingFromConcentrationToWeight(sample, var,volume, gs);
            }
            catch (Exception ex)
            {
               if(_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        //========================================进样小瓶=========================================================//

        /// <summary>
        /// 从小瓶架搬运小瓶到拧盖5
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isFirst">是否是第一个样品</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetBottleFromMaterialToCapperFive_One(Sample sample, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;
            ushort num = (ushort)(2 * sampleId -1); 
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从小瓶瓶架搬运{sampleId}小瓶到拧盖5");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1InShelf))
                    {
                        if (sample.BottleStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = Get_GC_BottleFromMaterialToCapperFive(num, gs);
                            if (!result)
                            {
                                throw new Exception($"从气质小瓶架搬运{sampleId}小瓶到拧盖5 失败! BottleStatus-{sample.BottleStatus}");
                            }
                            sample.BottleStatus = 1;
                        }
                        if (sample.BottleStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = Get_LC_BottleFromMaterialToCapperFive(num, gs);
                            if (!result)
                            {
                                throw new Exception($"从液质小瓶架搬运{sampleId}小瓶到拧盖5 失败! BottleStatus-{sample.BottleStatus}");
                            }
                            sample.BottleStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsBottle1InShelf);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsBottle1InCapper);
                    }

                    //小瓶瓶在拧盖   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1InCapper))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从小瓶瓶架搬运{sampleId}小瓶到拧盖5 失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetBottleFromMaterialToCapperFive_One(sample, gs);
            }
            catch (Exception ex)
            {
               if(_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从小瓶架搬运小瓶到拧盖5
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isFirst">是否是第一个样品</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        private bool GetBottleFromMaterialToCapperFive_Two(Sample sample, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;
            ushort num = (ushort)(2 * sampleId);
         
            try
            {
                _logger?.Info($"从小瓶瓶架搬运{sampleId}小瓶到拧盖5");
                //试管在净化试管架
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2InShelf))
                {
                    if (sample.BottleStatus == 0 && !_globalStatus.IsStopped)
                    {
                        result = Get_GC_BottleFromMaterialToCapperFive(num, gs);
                        if (!result)
                        {
                            throw new Exception($"从气质小瓶架搬运{sampleId}小瓶到拧盖5 失败! BottleStatus-{sample.BottleStatus}");
                        }
                        sample.BottleStatus = 1;
                    }
                    if (sample.BottleStatus == 1 && !_globalStatus.IsStopped)
                    {
                        result = Get_LC_BottleFromMaterialToCapperFive(num, gs);
                        if (!result)
                        {
                            throw new Exception($"从液质小瓶架搬运{sampleId}小瓶到拧盖5 失败! BottleStatus-{sample.BottleStatus}");
                        }
                        sample.BottleStatus = 0;
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsBottle2InShelf);
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsBottle2InCapper);
                }

                //小瓶瓶在拧盖   
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2InCapper))
                {
                    return true;
                }
                throw new Exception($"从小瓶瓶架搬运{sampleId}小瓶到拧盖5 失败,SampleStatus-{sample.Status}");
            }
            catch (Exception ex)
            {
               if(_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从拧盖5搬运小瓶到小瓶架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isFirst">是否是第一个样品</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        private bool GetBottleFromCapperFiveToMaterial_One(Sample sample, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;
            ushort num = (ushort)(2 * sampleId -1);
         
            try
            {
                _logger?.Info($"从拧盖5搬运{sampleId}小瓶到气质小瓶架");
                //试管在净化试管架
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1InCapper))
                {
                    if (sample.BottleStatus == 0 && !_globalStatus.IsStopped)
                    {
                        result = Get_GC_BottleFromCapperFiveToMaterial(num, gs);
                        if (!result)
                        {
                            throw new Exception($"从拧盖5搬运{sampleId}小瓶到气质小瓶架 失败! BottleStatus-{sample.BottleStatus}");
                        }
                        sample.BottleStatus = 1;
                    }
                    if (sample.BottleStatus == 1 && !_globalStatus.IsStopped)
                    {
                        result = Get_LC_BottleFromCapperFiveToMaterial(num, gs);
                        if (!result)
                        {
                            throw new Exception($"从液质小瓶架搬运{sampleId}小瓶到拧盖5 失败! BottleStatus-{sample.BottleStatus}");
                        }
                        sample.BottleStatus = 0;
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsBottle1InCapper);
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsBottle1InShelf);
                }

                //小瓶瓶在架子
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1InShelf))
                {
                    return true;
                }
                throw new Exception($"从拧盖5搬运{sampleId}小瓶到气质小瓶架 失败,SampleStatus-{sample.Status}");
            }
            catch (Exception ex)
            {
               if(_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从拧盖5搬运小瓶到小瓶架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isFirst">是否是第一个样品</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        private bool GetBottleFromCapperFiveToMaterial_Two(Sample sample, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;
            ushort num = (ushort)(2 * sampleId);
            Thread.Sleep(300);
            try
            {
                _logger?.Info($"从拧盖5搬运{sampleId}小瓶到气质小瓶架");
                //试管在净化试管架
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2InCapper))
                {
                    if (sample.BottleStatus == 0 && !_globalStatus.IsStopped)
                    {
                        result = Get_GC_BottleFromCapperFiveToMaterial(num, gs);
                        if (!result)
                        {
                            throw new Exception($"从拧盖5搬运{sampleId}小瓶到气质小瓶架 失败! BottleStatus-{sample.BottleStatus}");
                        }
                        sample.BottleStatus = 1;
                    }
                    if (sample.BottleStatus == 1 && !_globalStatus.IsStopped)
                    {
                        result = Get_LC_BottleFromCapperFiveToMaterial(num, gs);
                        if (!result)
                        {
                            throw new Exception($"从液质小瓶架搬运{sampleId}小瓶到拧盖5 失败! BottleStatus-{sample.BottleStatus}");
                        }
                        sample.BottleStatus = 0;
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsBottle2InCapper);
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsBottle2InShelf);
                }

                //小瓶瓶在架子
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle2InShelf))
                {
                    return true;
                }
                throw new Exception($"从拧盖5搬运{sampleId}小瓶到气质小瓶架 失败,SampleStatus-{sample.Status}");
            }
            catch (Exception ex)
            {
               if(_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        //========================================移液=========================================================//

        /// <summary>
        /// 第一组移液  从净化管到小瓶  从西林瓶到小瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">1:净化管到小瓶  2:西林瓶到小瓶</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool DoPipettingOne(Sample sample, int var,Func<Sample,int,IGlobalStatus,bool> capperOn, Func<Sample, int,IGlobalStatus, bool> capperOff, IGlobalStatus gs)
        {
            double volume = sample.TechParams.ExtractSampleVolume;  //提取样品溶液量 ExtractSampleVolume
            int tech_i = 1;
            double deep = 1;
            double liquidHigh = sample.TechParams.ExtractDeepOffset[1]; // 农残 净化到小瓶
            double[] safePos = GetPipettingSafePos();
            if (var == 2)
            {
                tech_i = 2;
                liquidHigh = sample.TechParams.ExtractDeepOffset[3];  //农残西林到小  兽药西林到小瓶
                safePos = null;
            }
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger.Info($"提取样品液{ sample.Id}移液-{volume}ml");
                    if (sample.PipettorStep2 == 1 && !_globalStatus.IsStopped)
                    {
                        //取枪头
                        var result = base.GetNeedleAsync(GetTip1Coordinate(2 * sample.Id - 1), gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第一管取枪头失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }

                    //第一个样品到气质小瓶
                    if (sample.PipettorStep2 == 2 && !_globalStatus.IsStopped)
                    {
                        //移液
                        var result = DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id -1 , tech_i,liquidHigh), GetPipettorTargetCoordinate(1, tech_i), volume, deep, 0.05, safePos, true, gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第一管移液失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }

                    //第一个样品到液质小瓶
                    if (sample.PipettorStep2 == 3 && !_globalStatus.IsStopped)
                    {  
                        //移液
                        var result = DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id -1, tech_i, liquidHigh), GetPipettorTargetCoordinate(2, tech_i), volume, deep, 0.05, safePos, true, gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第一管移液失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }
                    //推枪头
                    if (sample.PipettorStep2 == 4 && !_globalStatus.IsStopped)
                    { 
                        //退枪头
                        var result = base.PutNeedleAsync(GetTip1Coordinate(2 * sample.Id -1 ), gs,20).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第一管放枪头失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsBottle1ExtractDone);
                    }

                    //小瓶装盖
                    if (sample.PipettorStep2 == 5 && !_globalStatus.IsStopped)
                    {
                        //装盖 
                        var result = capperOn(sample,1,gs);
                        if (!result)
                        {
                            throw new Exception($"小瓶装盖失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }

                    //搬运小瓶到试管架  下料
                    if (sample.PipettorStep2 == 6 && !_globalStatus.IsStopped)
                    {
                        //搬运下料 
                        var result = GetBottleFromCapperFiveToMaterial_One(sample, gs);
                        if (!result)
                        {
                            throw new Exception($"第一组小瓶搬运到试管架失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }

                    //搬运第二组小瓶到拧盖5  上料
                    if (sample.PipettorStep2 == 7 && !_globalStatus.IsStopped)
                    {
                        //搬运上料
                        var result = GetBottleFromMaterialToCapperFive_Two(sample, gs);
                        if (!result)
                        {
                            throw new Exception($"第二组小瓶搬运到拧盖5失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }

                    //第二组小瓶拆盖
                    if (sample.PipettorStep2 == 8 && !_globalStatus.IsStopped)
                    {
                        // 拆盖 
                        var result = capperOff(sample,2, gs);
                        if (!result)
                        {
                            throw new Exception($"第二组小瓶装盖失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }

                    //第二个样品移液
                    if (sample.PipettorStep2 == 9 && !_globalStatus.IsStopped)
                    {
                        //取枪头
                        var result = base.GetNeedleAsync(GetTip1Coordinate(2 * sample.Id ), gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第二管取枪头失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }
                    //第2个样品到气质小瓶
                    if (sample.PipettorStep2 == 10 && !_globalStatus.IsStopped)
                    {
                        //移液
                        var result = DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id ,tech_i, liquidHigh), GetPipettorTargetCoordinate(1, tech_i), volume, deep, 0.05, safePos, true, gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第二管移液失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }
                    //第2个样品到液质小瓶
                    if (sample.PipettorStep2 == 11 && !_globalStatus.IsStopped)
                    {
                        //移液
                        var result = DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id , tech_i, liquidHigh), GetPipettorTargetCoordinate(2, tech_i), volume, deep, 0.05, safePos, true, gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第二管移液失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }
                    //第二组移液推枪头
                    if (sample.PipettorStep2 == 12 && !_globalStatus.IsStopped)
                    {
                        //退枪头
                        var result = base.PutNeedleAsync(GetTip1Coordinate(2 * sample.Id ), gs, 20).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第二管放枪头失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsBottle2ExtractDone);
                        sample.PipettorStep2++;
                    }

                    //小瓶装盖
                    if (sample.PipettorStep2 == 13 && !_globalStatus.IsStopped)
                    {
                        //装盖 
                        var result = capperOn(sample,2, gs);
                        if (!result)
                        {
                            throw new Exception($"第二组小瓶装盖失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }

                    //第二组小瓶搬运到试管架
                    if (sample.PipettorStep2 == 14 && !_globalStatus.IsStopped)
                    {
                        //搬运下料 
                        var result = GetBottleFromCapperFiveToMaterial_Two(sample, gs);
                        if (!result)
                        {
                            throw new Exception($"第二组小瓶搬运到试管架失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2 = 1;
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                      
                    }

                    throw new Exception($"样品{ sample.Id}移液-{volume}ml失败,pipettingStep-{sample.PipettorStep2}");
                }
            }
            catch (OccupyMethodException)
            {
                return DoPipettingOne(sample,var,capperOn, capperOff, gs);
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
        /// 第二组移液 浓缩 净化管（2ml） ==》西林瓶   大管==》西林瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">1:净化管（2ml） ==》西林瓶 2:大管==》西林瓶</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool DoPipettingTwo(Sample sample,int var,IGlobalStatus gs)
        {
            double volume = sample.TechParams.ConcentrationVolume;  //提取样品浓缩量
            int tech_i = 3;
            double deep = 2; //液面深度
            double liquidHigh = sample.TechParams.ExtractDeepOffset[3];  //净化管到西林瓶 农残
            double[] safePos = GetPipettingSafePos();
            if (var == 2)
            {
                tech_i = 4;
                liquidHigh = sample.TechParams.ExtractDeepOffset[3];  //萃取到西林 兽药
            }
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger.Info($"提取浓缩液{ sample.Id}移液-{volume}ml");
                    if (sample.PipettorStep2 == 1 && !_globalStatus.IsStopped)
                    {
                        //取枪头
                        var result = base.GetNeedleAsync(GetTip2Coordinate(2 * sample.Id - 1), gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第一管取枪头失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }

                    //第一个样品到西林瓶1
                    if (sample.PipettorStep2 == 2 && !_globalStatus.IsStopped)
                    {
                        //移液
                        var result = DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id - 1, tech_i, liquidHigh), GetPipettorTargetCoordinate(1, tech_i), volume, deep, 0.05, safePos, true, gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第一管移液失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }

                    //推枪头
                    if (sample.PipettorStep2 == 3 && !_globalStatus.IsStopped)
                    {
                        //退枪头
                        var result = base.PutNeedleAsync(GetTip2Coordinate(2 * sample.Id - 1), gs, 20).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第一管放枪头失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }

                    //第二个样品移液
                    if (sample.PipettorStep2 == 4 && !_globalStatus.IsStopped)
                    {
                        //取枪头
                        var result = base.GetNeedleAsync(GetTip2Coordinate(2 * sample.Id), gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第二管取枪头失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }

                    //第2个样品到西林瓶2
                    if (sample.PipettorStep2 == 5 && !_globalStatus.IsStopped)
                    {
                        //移液
                        var result = DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id, tech_i, liquidHigh), GetPipettorTargetCoordinate(2, tech_i), volume, deep, 0.05, safePos,true, gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第二管移液失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2++;
                    }

                    //推枪头
                    if (sample.PipettorStep2 == 6 && !_globalStatus.IsStopped)
                    {
                        //退枪头
                        var result = base.PutNeedleAsync(GetTip2Coordinate(2 * sample.Id), gs, 20).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第二管放枪头失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2 = 1;
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"样品{ sample.Id}移液-{volume}ml失败,pipettingStep-{sample.PipettorStep2}");
                }
            }
            catch (OccupyMethodException)
            {
                return DoPipettingTwo(sample, var,gs);
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                throw ex;
            }

        }

    
        //========================================加标=========================================================//


        /// <summary>
        /// 从试管架搬运净化管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSampleFromMaterialToCapperThree(Sample sample, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"搬运{sampleId}净化管到拧盖3");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToCapperThree((ushort)(2 * sampleId - 1), gs);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToCapperThree((ushort)(2 * sampleId), gs);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInShelf);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInCapper);
                    }

                    //试管在拧盖3   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}净化管到拧盖3失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromMaterialToCapperThree(sample, gs);
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
        /// 从拧盖3搬运净化管到振荡
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperThreeToVibration(Sample sample, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从拧盖3搬运{sampleId}净化管到振荡");

                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToVibration((ushort)(2 * sampleId),null,null, gs);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToVibration((ushort)(2 * sampleId -1), null, null, gs);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInCapper);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInVibration);
                    }

                    //试管在振荡
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInVibration))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从拧盖3搬运{sampleId}净化管到振荡失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromCapperThreeToVibration(sample, gs);
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
        /// 从振荡搬运净化管到哪拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSampleFromVibrationToCapperThree(Sample sample,IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从振荡搬运{sampleId}净化管到拧盖3");

                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInVibration))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToCapperThree((ushort)(2 * sampleId -1), null, null, gs);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToCapperThree((ushort)(2 * sampleId ), null, null, gs);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInVibration);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInCapper);
                    }

                    //试管在振荡
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从振荡搬运{sampleId}净化管到拧盖3失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromVibrationToCapperThree(sample, gs);
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
        /// 从振荡搬运净化管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSampleFromVibrationToMaterial(Sample sample, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从振荡搬运{sampleId}净化管到试管架");

                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInVibration))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToMaterial((ushort)(2 * sampleId - 1),null,null, gs);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToMaterial((ushort)(2 * sampleId), null, null, gs);
                            if (!result)
                            {
                                throw new Exception($"从振荡搬运{sampleId}净化管到试管架失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInVibration);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInShelf);
                    }

                    //试管在试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从振荡搬运{sampleId}净化管到试管架失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromVibrationToMaterial(sample, gs);
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
        /// 搬运试管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSampleToCapperThree(Sample sample, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"搬运{sampleId}净化管到拧盖3");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToCapperThree((ushort)(2 * sampleId - 1), gs);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToCapperThree((ushort)(2 * sampleId), gs);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInShelf);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInCapper);
                    }

                    //试管在振荡
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInVibration))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToCapperThree((ushort)(2 * sampleId -1), null, null,gs);
                            if (!result)
                            {
                                throw new Exception($"从振荡2搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToCapperThree((ushort)(2 * sampleId), null, null, gs);
                            if (!result)
                            {
                                throw new Exception($"从振荡2" +
                                    $"搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInVibration);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInCapper);
                    }

                    //试管在移栽
                  
                    //试管在拧盖3   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}净化管到拧盖3失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleToCapperThree(sample, gs);
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
        /// 从拧盖3搬运试管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperThreeToTransfer(Sample sample, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从拧盖3搬运{sampleId}净化管到移栽");
                    //净化管在拧盖3
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToTransfer((ushort)(2 * sampleId),null,null, func, gs);
                            if (!result)
                            {
                                throw new Exception($"从拧盖3搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToTransfer((ushort)(2 * sampleId - 1), null, null, func, gs);
                            if (!result)
                            {
                                throw new Exception($"从拧盖3搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInCapper);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInTransfer);
                    }

                    //试管在移栽
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从拧盖3搬运{sampleId}净化管到移栽失败,SampleStatus-{sample.PurifyStatus}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromCapperThreeToTransfer(sample,func, gs);
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
        /// 从拧盖3取有盖试管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperThreeToMaterial(Sample sample,IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从拧盖3搬运{sampleId}净化管到净化管架");
                    //净化管在拧盖3
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToMaterial((ushort)(2 * sampleId - 1),null,null, gs);
                            if (!result)
                            {
                                throw new Exception($"从拧盖3搬运{sampleId}净化管到净化管架失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToMaterial((ushort)(2 * sampleId ), null, null, gs);
                            if (!result)
                            {
                                throw new Exception($"从拧盖3搬运{sampleId}净化管到净化管架失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInCapper);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInShelf);
                    }

                    //试管在移栽
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从拧盖3搬运{sampleId}净化管到净化管架失败,SampleStatus-{sample.PurifyStatus}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromCapperThreeToMaterial(sample, gs);
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
        /// 从移栽搬运试管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSampleFromTransferToCapperThree(Sample sample, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从移栽搬运{sampleId}净化管到拧盖3");
                    //试管在移栽
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromTransferToCapperThree((ushort)(2 * sampleId), func, gs);
                            if (!result)
                            {
                                throw new Exception($"从移栽搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromTransferToCapperThree((ushort)(2 * sampleId -1), func, gs);
                            if (!result)
                            {
                                throw new Exception($"从移栽搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInTransfer);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInCapper);
                    }

                    //试管在拧盖3   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从移栽搬运{sampleId}净化管到拧盖3失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromTransferToCapperThree(sample,func, gs);
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
        /// 从试管架搬运试管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSampleFromMaterialToTransfer(Sample sample, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"搬运{sampleId}净化管到移栽");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToTransfer((ushort)(2 * sampleId - 1), func, gs);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToTransfer((ushort)(2 * sampleId), func, gs);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInShelf);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInTransfer);
                    }

                    //试管在移栽
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}净化管到移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromMaterialToTransfer(sample, func, gs);
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
        /// 从移栽取下试管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSampleFromTransferToMaterial(Sample sample, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从移栽搬运{sampleId}净化管到试管架");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromTransferToMaterial((ushort)(2 * sampleId - 1), func, gs);
                            if (!result)
                            {
                                throw new Exception($"从移栽搬运{sampleId}净化管到试管架失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromTransferToMaterial((ushort)(2 * sampleId), func, gs);
                            if (!result)
                            {
                                throw new Exception($"从移栽搬运{sampleId}净化管到试管架失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInTransfer);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInShelf);
                    }

                    //试管在移栽
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从移栽搬运{sampleId}净化管到试管架失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromTransferToMaterial(sample,func, gs);
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
        /// 搬运试管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetSampleToTransfer(Sample sample, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"搬运{sampleId}净化管到移栽");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToTransfer((ushort)(2 * sampleId - 1), func, gs);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToTransfer((ushort)(2 * sampleId), func, gs);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInShelf);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInTransfer);
                    }

                    //试管在振荡
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInVibration))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToTransfer((ushort)(2 * sampleId - 1), null, null,func, gs);
                            if (!result)
                            {
                                throw new Exception($"从振荡2搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToTransfer((ushort)(2 * sampleId), null, null,func, gs);
                            if (!result)
                            {
                                throw new Exception($"从振荡2搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInVibration);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInTransfer);
                    }

                    //试管在拧盖3   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToTransfer((ushort)(2 * sampleId - 1), null, null, func, gs);
                            if (!result)
                            {
                                throw new Exception($"从拧盖3搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToTransfer((ushort)(2 * sampleId), null, null, func, gs);
                            if (!result)
                            {
                                throw new Exception($"从拧盖3搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInCapper);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInTransfer);
                    }

                    //试管在移栽
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                    {
                        GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}净化管到移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleToTransfer(sample, func, gs);
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped||_globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 清洗加标注射器
        /// </summary>
        /// <returns></returns>
        public bool WashSyring(IGlobalStatus gs)
        {
           s0: try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CarrierTwoMethodName))
                    {
                        if (GlobalCache.Instance.CarrierTwoMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            goto s0;
                        }
                    }
                    GlobalCache.Instance.CarrierTwoMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"清洗加标注射器");

                    //加入清水
                    var result2 = _syring.AddSolve(1, 2, gs);
                    //气缸上升
                    Z_Cylinder_Up();
                    //移动到清洗位
                    var result = CarrierMoveToSafePos(GetSyringWashCoordinate(),gs);

                    if (!result.GetAwaiter().GetResult())
                    {
                        _io.WriteBit_DO_Delay_Reverse(_drainPump, 20);
                        throw new Exception("移动到清洗位失败!");
                    }  
                    
                    //气缸下降
                    Z_Cylinder_Down();

                    //判断注射器加液完成
                    if (!result2.GetAwaiter().GetResult())
                    {
                        Z_Cylinder_Up();
                        throw new Exception("注射器加水失败!");
                    }
                    //注射器抽打
                    _motion.P2pMoveWithCheckDone(_axisSyring, 50, _syringVel, _globalStatus).GetAwaiter().GetResult();
                    _motion.P2pMoveWithCheckDone(_axisSyring, 0, _syringVel, _globalStatus).GetAwaiter().GetResult();
                    _motion.P2pMoveWithCheckDone(_axisSyring, 50, _syringVel, _globalStatus).GetAwaiter().GetResult();
                    _motion.P2pMoveWithCheckDone(_axisSyring, 0, _syringVel, _globalStatus).GetAwaiter().GetResult();

                    //气缸上升抬起
                    Z_Cylinder_Up();
                    //排空
                    _io.WriteBit_DO_Delay_Reverse(_drainPump, 20);

                    //完成清洗
                    GlobalCache.Instance.CarrierTwoMethodName = string.Empty;
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


        #endregion

        #region Protected Methods  搬运部分

        /// <summary>
        /// 从试管架到拧盖3
        /// </summary>
        /// <param name="num"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSampleFromMaterialToCapperThree(ushort num, IGlobalStatus gs)
        {
            byte clawOpenByte = 0;

            //if (gs?.IsCancellationRequested == true)
            //{
            //    throw new TaskCanceledException($"触发停止 gs:{gs.IsCancellationRequested}");
            //}

            //取料
            var result = base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从试管架到振荡
        /// </summary>
        /// <param name="num"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSampleFromMaterialToVibration(ushort num, IGlobalStatus gs)
        {
            byte clawOpenByte = 0;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料
            var result = base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从试管架到移栽
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func">移栽旋转指定角度</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSampleFromMaterialToTransfer(ushort num, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs)
        {
            byte clawOpenByte = 0;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }
            //旋转到指定位置
            var result1 = func.Invoke(num, gs);

            //取料
            var result = base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //旋转到指定位置  判断

            if (!result1.GetAwaiter().GetResult())
            {
                throw new Exception("移栽移动到指定位失败!");
            }
    
            //放料
            result = base.PutTubeAsync(GetTransferCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从拧盖3到试管架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperThreeToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, IGlobalStatus gs)
        {
            byte clawOpenByte = 0;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }
     
            //取料
            result = base.GetTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }
      
            //放料
            result = base.PutTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从拧盖3到振荡
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperThreeToVibration(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, IGlobalStatus gs)
        {
            byte clawOpenByte = 0;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }
          
            //取料
            result =  base.GetTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }
          
            //放料
            result = base.PutTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从拧盖3到移栽
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="func3">移栽旋转指定角度</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperThreeToTransfer(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs)
        {
            byte clawOpenByte = 0;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }
            //旋转到指定位置
            var result1 = func.Invoke(num, gs);

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            result =  base.GetTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //Z轴上升
            CheckAxisZInSafePos(gs).GetAwaiter().GetResult();
            while (_globalStatus.IsPause && !_globalStatus.IsStopped)
            {
                Thread.Sleep(2000);
            }
            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }
      
            //旋转到指定位置  判断
            if (!result1.GetAwaiter().GetResult())
            {
                throw new Exception("移栽移动到指定位失败!");
            }
          
            //放料
            result = base.PutTubeAsync(GetTransferCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从振荡到试管架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSampleFromVibrationToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, IGlobalStatus gs)
        {
            byte clawOpenByte = 0;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }
           
            //取料
            result = base.GetTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            } 
      
            //放料
            result = base.PutTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从振荡到移栽
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="func3">移栽旋转指定角度</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSampleFromVibrationToTransfer(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs)
        {
            byte clawOpenByte = 0;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }
            //旋转到指定位置
            var result1 = func.Invoke(num, gs);

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            result = base.GetTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }
                   
            //旋转到指定位置 判断
            if (!result1.GetAwaiter().GetResult())
            {
                throw new Exception("移栽移动到指定位失败!");
            }
           
            //放料
            result = base.PutTubeAsync(GetTransferCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从振荡到拧盖3
        /// </summary>
        /// <returns></returns>
        protected bool GetSampleFromVibrationToCapperThree(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, IGlobalStatus gs)
        {
            byte clawOpenByte = 0;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }
     
            //取料
            result =  base.GetTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }
 
            //放料
            result = base.PutTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从移栽到试管架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func">移栽旋转指定角度</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSampleFromTransferToMaterial(ushort num, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs)
        {
            byte clawOpenByte = 0;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //旋转到指定位置
            var result1 = func.Invoke(num, gs);
            if (!result1.GetAwaiter().GetResult())
            {
                throw new Exception("移栽移动到指定位失败!");
            }
   
            //取料
            var result =  base.GetTubeAsync(GetTransferCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
        
            //放料
            result = base.PutTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从移栽到拆盖3
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func">移栽旋转指定角度</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSampleFromTransferToCapperThree(ushort num, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs)
        {
            byte clawOpenByte = 0;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //旋转到指定位置
            var result1 = func.Invoke(num,gs);
            if (!result1.GetAwaiter().GetResult())
            {
                throw new Exception("移栽移动到指定位失败!");
            }

            //取料
            var result = base.GetTubeAsync(GetTransferCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        //===============================================================================================================================================//

        /// <summary>
        /// 从西林瓶架到拧盖4
        /// </summary>
        /// <param name="num"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSeilingFromMaterialToCapperFour(ushort num, IGlobalStatus gs)
        {
            byte clawOpenByte = 10;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料
            var result =  base.GetTubeAsync(GetSeilingCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从拧盖4到西林瓶架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSeilingFromCapperFourToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, IGlobalStatus gs)
        {
            byte clawOpenByte = 10;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料
            var result = base.GetTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetSeilingCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从拧盖4到浓缩
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSeilingFromCapperFourToConcentration(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, IGlobalStatus gs)
        {
            byte clawOpenByte = 10;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料
            var result = base.GetTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetConcentrationCoordinate(num,false), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从拧盖4到称重
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSeilingFromCapperFourToWeight(ushort num, IGlobalStatus gs)
        {
            byte clawOpenByte = 10;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料
            var result =  base.GetTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetWeightCoordinate(num,false), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从浓缩到拧盖4
        /// </summary>
        /// <param name="num"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSeilingFromConcentrationToCapperFour(ushort num, IGlobalStatus gs)
        {
            byte clawOpenByte = 10;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料
            var result =  base.GetTubeAsync(GetConcentrationCoordinate(num,true), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result =  base.PutTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从浓缩到称重
        /// </summary>
        /// <param name="num"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSeilingFromConcentrationToWeight(ushort num, IGlobalStatus gs)
        {
            byte clawOpenByte = 10;


            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }
            //取料
            var result = base.GetTubeAsync(GetConcentrationCoordinate(num,true), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result =  base.PutTubeAsync(GetWeightCoordinate(num,false), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从称重到拧盖4
        /// </summary>
        /// <param name="num"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSeilingFromWeightToCapperFour(ushort num, IGlobalStatus gs)
        {
            byte clawOpenByte = 10;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料
            var result =  base.GetTubeAsync(GetWeightCoordinate(num,true), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result =  base.PutTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从称重到浓缩
        /// </summary>
        /// <param name="num"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool GetSeilingFromWeightToConcentration(ushort num, IGlobalStatus gs)
        {
            byte clawOpenByte = 10;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料
            var result = base.GetTubeAsync(GetWeightCoordinate(num,true), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result =  base.PutTubeAsync(GetConcentrationCoordinate(num,false), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从气质小瓶架到拧盖5
        /// </summary>
        /// <param name="num"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool Get_GC_BottleFromMaterialToCapperFive(ushort num, IGlobalStatus gs)
        {
            byte clawOpenByte = 180;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料
            var result = base.GetTubeAsync(Get_GC_BottleCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetCapperFiveCoordinate(1), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从拧盖5到气质小瓶架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool Get_GC_BottleFromCapperFiveToMaterial(ushort num, IGlobalStatus gs)
        {
            byte clawOpenByte = 180;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料
            var result =  base.GetTubeAsync(GetCapperFiveCoordinate(1), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result =  base.PutTubeAsync(Get_GC_BottleCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从液质小瓶架到拧盖5
        /// </summary>
        /// <param name="num"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool Get_LC_BottleFromMaterialToCapperFive(ushort num, IGlobalStatus gs)
        {
            byte clawOpenByte = 180;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料
            var result = base.GetTubeAsync(Get_LC_BottleCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetCapperFiveCoordinate(2), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从拧盖5到气质液质小瓶架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool Get_LC_BottleFromCapperFiveToMaterial(ushort num, IGlobalStatus gs)
        {
            byte clawOpenByte = 180;

            if (gs?.IsStopped == true || gs?.IsEmgStop == true || gs?.IsPause == true)
            {
                throw new TaskCanceledException($"触发停止");
            }

            //取料
            var result = base.GetTubeAsync(GetCapperFiveCoordinate(2), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(Get_LC_BottleCoordinate(num), clawOpenByte, gs).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Protected Methods 加标部分

        /// <summary>
        /// 从取加标液位到称重位
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">加标种类1~4</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected bool AddMarkFromSourceToWeight(int var,double volume, IGlobalStatus gs)
        {
            try
            {
                Z_Cylinder_Up();
                //Xy移动到取液位
                var result = CarrierMoveToSafePos(GetAddMarkSourceCoordinate(var),gs).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception("XY轴移动到加标取液位 失败!");
                }
                while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                {
                    Thread.Sleep(2000);
                }
                Z_Cylinder_Down();
                //吸液
                result = _motion.P2pMoveWithCheckDone(_axisSyring, volume, _markObsorbVel, _globalStatus).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception($"注射器吸液失败-{volume}ul");
                }
                Thread.Sleep(1000);
                //上升
                Z_Cylinder_Up();
                while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                {
                    Thread.Sleep(2000);
                }
                //xy移动到吐液位
                result = CarrierMoveToSafePos(GetAddMarkTargetCoordinate(), gs).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception("XY轴移动到加标吐液位 失败!");
                }
                Z_Cylinder_Down();
                while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                {
                    Thread.Sleep(2000);
                }
                //吐液
                result = _motion.P2pMoveWithCheckDone(_axisSyring, 0, _markSyringVel, _globalStatus).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception($"注射器吐液失败");
                }
                while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                {
                    Thread.Sleep(2000);
                }
                //上升
                Z_Cylinder_Up();

                return true;
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

        #endregion

        #region Protected Methods 移液部分

        /// <summary>
        /// 开始移液器
        /// </summary>
        /// <param name="sourcePos">移液取液位</param>
        /// <param name="targetPos">移液吐液位</param>
        /// <param name="volume"></param>
        /// <param name="deep">移液液位深度</param>
        /// <param name="airColumn">吸取空气柱</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected override async Task<bool> DoPipettingAsync(double[] sourcePos, double[] targetPos, double volume,double deep,double airColumn,double[] safePos, bool isNeedGoSafePosObsorb, IGlobalStatus gs)
        {
            //移液1ml
            if (volume <= 1 && !_globalStatus.IsStopped)
            {
                return await base.DoPipettingAsync(sourcePos, targetPos, volume, deep, airColumn, safePos,false, gs).ConfigureAwait(false);
            }

            //移液1~2ml
            if (volume > 1 && volume <= 2)
            {
                if (_pipStep == 0 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, deep, airColumn, safePos, false, gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 1 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, volume - 1, deep, airColumn, safePos,true, gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep = 0;
                    return true;
                }
            }

            //移液2~3ml
            if (volume > 2 && volume <= 3)
            {
                if (_pipStep == 0 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, deep, airColumn, safePos, false,gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 1 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, deep, airColumn, safePos,true, gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 2 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, volume - 2, deep, airColumn, safePos,true, gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep = 0;
                    return true;
                }
            }

            //移液3~4ml
            if (volume > 3 && volume <= 4)
            {
                if (_pipStep == 0 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, deep, airColumn, safePos,false, gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 1 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, deep, airColumn, safePos,true, gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 2 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, deep, airColumn, safePos,true, gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 3 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, volume - 3, deep, airColumn, safePos,true, gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep = 0;
                    return true;
                }
            }

            //移液4~5ml
            if (volume > 4 && volume <= 5)
            {
                if (_pipStep == 0 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, deep, airColumn, safePos,false, gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 1 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, deep, airColumn, safePos,true, gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 2 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, deep, airColumn, safePos,true, gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 3 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, deep, airColumn, safePos,true, gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 4 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, volume - 4, deep, airColumn, safePos, true,gs).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep = 0;
                    return true;
                }
            }

            throw new InvalidOperationException($"移液参数超出范围{volume}");
        }

        /// <summary>
        /// Z气缸上
        /// </summary>
        /// <param name="checkSensor"></param>
        private void Z_Cylinder_Up(bool checkSensor = true)
        {
            //_logger?.Debug($"Z_Cylinder_Up-{checkSensor}");
            var result = _io.WriteBit_DO(_zCylinderCtr, false);
            if (!result)
            {
                throw new Exception("Z1_Cylinder_Up Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }

            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_zCylinderUpSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 12)
                {
                    throw new TimeoutException("Z_Cylinder_Up 超时");
                }
            } while (!result);
        }

        /// <summary>
        /// Z气缸下
        /// </summary>
        /// <param name="checkSensor"></param>
        private void Z_Cylinder_Down(bool checkSensor = false)
        {
            //_logger?.Debug($"Z_Cylinder_Down-{checkSensor}");
            var result = _io.WriteBit_DO(_zCylinderCtr, true);
            if (!result)
            {
                throw new Exception("Z_Cylinder_Down Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(2000);
                return;
            }

            //int temp = 0;
            //do
            //{
            //    result = _io.ReadBit_DI(_zCylinderLow);
            //    Thread.Sleep(500);
            //    temp++;
            //    if (temp > 12)
            //    {
            //        throw new TimeoutException("Z_Cylinder_Down 超时");
            //    }
            //} while (!result);
        }

        /// <summary>
        /// 移液器规避位置
        /// </summary>
        /// <returns></returns>
        protected override double[] GetPipettingSafePos()
        {
            return new double[] { 378, 118 };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 获取15ml试管位置
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] GetSampleTubeCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.PurifyTubePos1;
            if (tubeId > 48)
            {
                xyz = _posData.PurifyTubePos2;
            }

            //计算偏移
            int id = (tubeId - 1) % 48;

            //计算结果

            return base.GetCoordinate(id + 1, 12, 4, -32, 32, xyz);

        }

        /// <summary>
        /// 获取拧盖3坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetCapperThreeCoordinatte(int tubeId)
        {
            double x = _posData.CapperThreePos[0];
            double y = _posData.CapperThreePos[1];
            double z = _posData.CapperThreePos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 60, y, z };
            }
            return new double[] { x, y, z };
        }    
      
        /// <summary>
        /// 获取振荡坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetVibrationCoordinatte(int tubeId)
        {
            double x = _posData.VibrationTwoPos[0];
            double y = _posData.VibrationTwoPos[1];
            double z = _posData.VibrationTwoPos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 60, y, z };
            }
            return new double[] { x, y, z };
        }

        /// <summary>
        /// 获取移栽坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetTransferCoordinatte(int tubeId)
        {
            double x = _posData.TransferRightPos[0];
            double y = _posData.TransferRightPos[1];
            double z = _posData.TransferRightPos[2];

            int i = (tubeId - 1) % 2;
            //if (i == 1)  //单数
            //{
            //    return new double[] { x, y + 50, z };
            //}
            return new double[] { x, y, z };
        }

        //=======================================================================//

        /// <summary>
        /// 获取拧盖4坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetCapperFourCoordinate(int tubeId)
        {
            double x = _posData.CapperFourPos[0];
            double y = _posData.CapperFourPos[1];
            double z = _posData.CapperFourPos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 60, y, z };
            }
            return new double[] { x, y, z };
        }

        /// <summary>
        /// 获取西林瓶坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] GetSeilingCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.SeilingPos1;
            if (tubeId > 48)
            {
                xyz = _posData.SeilingPos2;
            }

            //计算偏移
            int id = (tubeId - 1) % 48;

            //计算结果

            return base.GetCoordinate(id + 1, 12, 4, -32, 32, xyz);
        }

        /// <summary>
        /// 获取浓缩位置坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <param name="isGetPos">是否为取料位</param>
        /// <returns></returns>
        private double[] GetConcentrationCoordinate(int tubeId,bool isGetPos)
        {
            double x = _posData.ConcentrationPos[0];
            double y = _posData.ConcentrationPos[1];
            double z = _posData.ConcentrationPos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                if (!isGetPos)
                {
                    return new double[] { x + 45, y, z - 3 };
                }
                return new double[] { x + 45, y, z };
            }
            else
            {
                if (!isGetPos)
                {
                    return new double[] { x, y, z - 3 };
                }
                return new double[] { x, y, z };
            }
          
        }

        /// <summary>
        /// 获取称重位置坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <param name="isGetPos">是否为取料位</param>
        /// <returns></returns>
        private double[] GetWeightCoordinate(int tubeId,bool isGetPos)
        {
            if (isGetPos)
            {
               return new double[] { _posData.WeightPos[0], _posData.WeightPos[1], _posData.WeightPos[2] };
            }
            else
            {
               return new double[] { _posData.WeightPos[0], _posData.WeightPos[1], _posData.WeightPos[2] - 3 };
            }
        }



        /// <summary>
        /// 获取气质小瓶坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] Get_GC_BottleCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.BottlePos1;
            if (tubeId > 48)
            {
                xyz = _posData.BottlePos2;
            }

            //计算偏移
            int id = (tubeId - 1) % 48;

            //计算结果

            return base.GetCoordinate(id + 1, 12, 4, -16, 15, xyz);
        }

        /// <summary>
        /// 获取液质小瓶坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] Get_LC_BottleCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.BottlePos3;
            if (tubeId > 48)
            {
                xyz = _posData.BottlePos4;
            }

            //计算偏移
            int id = (tubeId - 1) % 48;

            //计算结果

            return base.GetCoordinate(id + 1, 12, 4, -16, 15, xyz);
        }

        /// <summary>
        /// 获取拧盖5坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetCapperFiveCoordinate(int tubeId)
        {
            double x = _posData.CapperFivePos[0];
            double y = _posData.CapperFivePos[1];
            double z = _posData.CapperFivePos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 60, y, z };
            }
            return new double[] { x, y, z };
        }


        /// <summary>
        /// 获取Tip1头坐标
        /// </summary>
        /// <param name="tubeId">1-48</param>
        /// <returns></returns>
        private double[] GetTip1Coordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.NeedlePos1;
         
            return base.GetCoordinate(tubeId , 8, 12, -20, 20, xyz);
        }

        /// <summary>
        /// 获取Tip2头坐标 兽药专用
        /// </summary>
        /// <param name="tubeId">1-48</param>
        /// <returns></returns>
        private double[] GetTip2Coordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.NeedlePos2;
            return base.GetCoordinate(tubeId, 8, 12, -20, 20, xyz);
        }

        /// <summary>
        /// 获取移液取液坐标
        /// </summary>
        /// <param name="sampleId">单双数</param>
        /// <param name="tech_i">1:净化管（2ml）==》小瓶 2:西林瓶  ==》 小瓶 3:净化管（2ml） ==》西林瓶 4:大管==》西林瓶 </param>
        /// <returns></returns>
        private double[] GetPipettorSourceCoordinate(int sampleId ,int tech_i,double offset)
        {
            //净化管（2ml）==》小瓶  浓缩西林瓶  ==》 小瓶  净化管（2ml） ==》西林瓶    大管==》西林瓶  
            double[] poss = new double[3];
           
            switch (tech_i)
            {
                case 1:
                case 3:  //净化管（2ml）  拧盖3处
                    Array.Copy(_posData.PipettingSourcePos,poss,3);//净化管取液
                    break;
                case 2:  //西林瓶
                    Array.Copy(_posData.PipettingSourcePos1, poss, 3);//西林瓶取液
                    break;
                case 4:  //移栽大管
                    Array.Copy(_posData.PipettingSourcePos2, poss, 3);//移栽萃取管取液
                    break;
                default:
                    throw new InvalidOperationException($"移液工艺错误 err:{tech_i}");
            }

            int i = (sampleId - 1) % 2;

            if (i == 1)//单数
            {
                if (tech_i == 4)
                {
                    return new double[] { poss[0] + 45, poss[1], poss[2] + offset };
                }
                else
                {
                    return new double[] { poss[0] + 60, poss[1], poss[2] + offset };
                }
            }
            else
            {
                return new double[] { poss[0], poss[1], poss[2] + offset };
            }


        }

        /// <summary>
        /// 获取移液吐液坐标
        /// </summary>
        /// <param name="sampleId"></param>
        /// <param name="tech_i"></param>
        /// <returns></returns>
        private double[] GetPipettorTargetCoordinate(int sampleId,int tech_i)
        {
            //净化管（2ml）==》小瓶  浓缩西林瓶  ==》 小瓶  净化管（2ml） ==》西林瓶    大管==》西林瓶  
            double[] poss = new double[3];
            switch (tech_i)
            {
                case 1:
                case 2: //小瓶（2ml）  拧盖5处
                    poss= _posData.PipettingTargetPos1;
                    break;
                case 3: //西林瓶 拧盖4处
                case 4: //西林瓶 拧盖4处
                    poss = new double[] { _posData.PipettingTargetPos2[0], _posData.PipettingTargetPos2[1], _posData.PipettingTargetPos2[2] + 20 };
                    break;
                default:
                    throw new InvalidOperationException($"移液工艺错误 err:{tech_i}");
            }

            int i = (sampleId - 1) % 2;

            if (i == 1)//单数
            {
                return new double[] { poss[0] + 60, poss[1], poss[2] };
            }
            else
            {
                return poss;
            }
        }

        /// <summary>
        /// 获取加标取液位
        /// </summary>
        /// <param name="var">1~4</param>
        /// <returns></returns>
        private double[] GetAddMarkSourceCoordinate(int var)
        {
            double[] poss = _posData.SyringSourcePos;
            switch (var)
            {
                case 1:
                    return new double[] { poss[0], poss[1] };              
                case 2:
                    return new double[] { poss[0] +50, poss[1] };             
                case 3:
                    return new double[] { poss[0], poss[1]+50 };     
                case 4:
                    return new double[] { poss[0]+50, poss[1]+50 };
                default:
                    return null;
            }

        }

        /// <summary>
        /// 获取加标目标位
        /// </summary>
        /// <returns></returns>
        private double[] GetAddMarkTargetCoordinate()
        {
            return _posData.SyringTargePos;
        }

        /// <summary>
        /// 获取注射器清洗位置
        /// </summary>
        /// <returns></returns>
        private double[] GetSyringWashCoordinate()
        {
            return _posData.SyringWashPos;
        }

        /// <summary>
        /// 读取重量值
        /// </summary>
        /// <returns></returns>
        private double ReadWeight()
        {
            //等待称台稳定
            bool isStatic = false;
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(3);
            while (!isStatic)
            {
                isStatic = _weight.ReadIsStatic(_weithtId).GetAwaiter().GetResult();
                Thread.Sleep(500);
                if (DateTime.Now > end)
                {
                    //throw new Exception("等待称台稳定超时 10S");
                    break;
                }
            }
            //读取称台值
            return _weight.ReadWeight(_weithtId).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 称台清零
        /// </summary>
        /// <returns></returns>
        private bool WeightClear()
        {
            return true;
         //  return _weight.Clear(_weithtId).GetAwaiter().GetResult();
        }

        #endregion




    }
}
