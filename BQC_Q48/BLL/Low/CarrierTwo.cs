using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
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

   

        #region Private Members

        private readonly static object _lockObj = new object();

        private string _currentMethodName = string.Empty;

        private CarrierTwoPosData _posData;

        private ICarrierTwoDataAccess _dataAccess;

        private readonly IWeight _weight;

        private readonly IIoDevice _io;

        private readonly ushort _axisSyring = 15;//注射器

        private readonly ushort _weithtId = 3;

        private readonly ushort _zCylinderCtr = 51;//Q1.3

        private readonly ushort _zCylinderUpSensor = 51;//I1.3


        #endregion

        #region Construtors
        public CarrierTwo(IEtherCATMotion motion, IIoDevice io, IEPG26 claw, IGlobalStatus globalStatus, ICarrierTwoDataAccess dataAccess, IWeight weight) : base(motion, claw, globalStatus, logger)
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

            _posData = _dataAccess.GetPosData();
        }

        public override void UpdatePosData()
        {
            _posData = _dataAccess.GetPosData();
        }

        #endregion

        #region Public Methods

        public override async Task<bool> GoHome(CancellationTokenSource cts)
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

            return await base.GoHome(cts);
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
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSelingFromMaterialToCapperFour(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从西林瓶架搬运{sampleId}西林瓶到拧盖4");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInShelf))
                    {
                        if (sample.SeilingStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromMaterialToCapperFour((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从西林瓶架搬运{sampleId}西林瓶到拧盖4失败！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            sample.SeilingStatus = 1;
                        }
                        if (sample.SeilingStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromMaterialToCapperFour((ushort)(2 * sampleId), cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从西林瓶架搬运{sampleId}西林瓶到拧盖4失败,SampleStatus-{sample.Status}");
                }
            } 
            catch (OccupyMethodException)
            {
               return GetSelingFromMaterialToCapperFour(sample, cts);
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
        /// 从拧盖4搬运西林瓶到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSelingFromCapperFourToMaterial(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从拧盖4搬运{sampleId}西林瓶到西林瓶架");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                    {
                        if (sample.SeilingStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromCapperFourToMaterial((ushort)(2 * sampleId - 1),null,null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖4搬运{sampleId}西林瓶到西林瓶架 失败！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            sample.SeilingStatus = 1;
                        }
                        if (sample.SeilingStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromCapperFourToMaterial((ushort)(2 * sampleId),null,null, cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从拧盖4搬运{sampleId}西林瓶到西林瓶架 失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSelingFromCapperFourToMaterial(sample, cts);
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
        /// 从拧盖4搬运西林瓶到浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSelingFromCapperFourToConcentration(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从拧盖4搬运{sampleId}西林瓶到浓缩");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                    {
                        if (sample.SeilingStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromCapperFourToConcentration((ushort)(2 * sampleId - 1), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖4搬运{sampleId}西林瓶到浓缩 失败！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            sample.SeilingStatus = 1;
                        }
                        if (sample.SeilingStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromCapperFourToConcentration((ushort)(2 * sampleId), null, null, cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从从拧盖4搬运{sampleId}西林瓶到浓缩 失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSelingFromCapperFourToConcentration(sample, cts);
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
        /// 从浓缩搬运西林瓶到拧盖4
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSelingFromConcentrationToCapperFour(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从浓缩搬运{sampleId}西林瓶到拧盖4");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInConcentration))
                    {
                        if (sample.SeilingStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromConcentrationToCapperFour((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从浓缩搬运{sampleId}西林瓶到拧盖4 失败！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            sample.SeilingStatus = 1;
                        }
                        if (sample.SeilingStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSeilingFromConcentrationToCapperFour((ushort)(2 * sampleId), cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从浓缩搬运{sampleId}西林瓶到拧盖4 失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSelingFromConcentrationToCapperFour(sample, cts);
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
        /// 从拧盖4搬运西林瓶到称重 并搬运回
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSelingFromCapperFourToWeightAndBack(Sample sample,int var ,double volume, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从拧盖4搬运{sampleId}西林瓶到称重");

                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                    {
                        if (sample.SeilingStatus == 0 && sample.SeilingWeight1 == 0 && !_globalStatus.IsStopped)
                        {
                            //搬运到称重
                            result = GetSeilingFromCapperFourToWeight((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖4搬运{sampleId}西林瓶到称台 失败！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            //读取称台值
                            sample.SeilingWeight1 = ReadWeight();

                            if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddMark1) && var != 0)
                            {
                                result = AddMarkFromSourceToWeight(var, volume, cts);
                                if (result)
                                {
                                    throw new Exception("浓缩前加标失败!");
                                }

                            }
                            //搬运回
                            result = GetSeilingFromWeightToCapperFour((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从称台搬运{sampleId}西林瓶到拧盖4 失败！ SeilingStatus-{sample.SeilingStatus}");
                            }

                            sample.SeilingStatus = 1;
                        }

                        if (sample.SeilingStatus == 1 && sample.SeilingWeight2 == 0 && !_globalStatus.IsStopped)
                        {
                            //搬运到称重
                            result = GetSeilingFromCapperFourToWeight((ushort)(2 * sampleId), cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖4搬运{sampleId}西林瓶到称台 失败！ SeilingStatus-{sample.SeilingStatus}");
                            }
                            //读取称台值
                            sample.SeilingWeight2 = ReadWeight();

                            if (TechStatusHelper.BitIsOn(sample.TechParams,TechStatus.AddMark1) && var != 0)
                            {
                                result = AddMarkFromSourceToWeight(var,volume,cts);
                                if (result)
                                {
                                    throw new Exception("浓缩前加标失败!");
                                }
                                
                            }

                            //搬运回
                            result = GetSeilingFromWeightToCapperFour((ushort)(2 * sampleId), cts);
                            if (!result)
                            {
                                throw new Exception($"从称台搬运{sampleId}西林瓶到拧盖4 失败！ SeilingStatus-{sample.SeilingStatus}");
                            }

                            sample.SeilingStatus = 0;
                        }
                    }

                    //试管在拧盖3   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInCapper))
                    {
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从拧盖4搬运{sampleId}西林瓶到称重 失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSelingFromCapperFourToWeightAndBack(sample, var,volume, cts);
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
        /// 从浓缩搬运西林瓶到称重 并搬运回
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">加标种类1~4 0：不加</param>
        /// <param name="volume">加标量</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSelingFromConcentrationToWeight(Sample sample, int var, double volume, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从浓缩搬运{sampleId}西林瓶到称重");

                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingInConcentration))
                    {
                        if (sample.SeilingStatus == 0 && !_globalStatus.IsStopped)
                        {
                            //搬运到称重
                            if (sample.SubStep == 0 && !_globalStatus.IsStopped)
                            {
                                result = GetSeilingFromConcentrationToWeight((ushort)(2 * sampleId - 1), cts);
                                if (!result)
                                {
                                    throw new Exception($"从拧盖4搬运{sampleId}西林瓶到称台 失败！ SeilingStatus-{sample.SeilingStatus}");
                                }
                                sample.SubStep++;
                            }

                            if (sample.SubStep == 1 && !_globalStatus.IsStopped)
                            {  //读取称台值
                                var weitht = ReadWeight();
                                if (weitht <= sample.SeilingWeight1 + 0.5)
                                {
                                    _logger?.Debug($"称台数据 空瓶：{sample.SeilingWeight1} 浓缩后：{weitht}");
                                    //判断是否加标
                                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddMark2) && var != 0)
                                    {
                                        result = AddMarkFromSourceToWeight(var,volume, cts);
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
                                result = GetSeilingFromWeightToConcentration((ushort)(2 * sampleId - 1), cts);
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
                                result = GetSeilingFromConcentrationToWeight((ushort)(2 * sampleId), cts);
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
                                if (weitht <= sample.SeilingWeight2 + 0.5)
                                {
                                    _logger?.Debug($"称台数据 空瓶：{sample.SeilingWeight2} 浓缩后：{weitht}");
                                    //判断是否加标
                                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddMark2) && var != 0)
                                    {
                                        result = AddMarkFromSourceToWeight(var, volume, cts);
                                        if (!result)
                                        {
                                            throw new Exception($"{sampleId}西林瓶加标失败");
                                        }
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
                            if (sample.SubStep == 5 && !_globalStatus.IsStopped)
                            {
                                result = GetSeilingFromWeightToConcentration((ushort)(2 * sampleId), cts);
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
                                _currentMethodName = string.Empty;
                                return true;
                            }
                        }
                    }
                    throw new Exception($"从浓缩搬运{sampleId}西林瓶到称重 失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSelingFromConcentrationToWeight(sample, var,volume, cts);
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

        //========================================进样小瓶=========================================================//

        /// <summary>
        /// 从小瓶架搬运小瓶到拧盖5
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isFirst">是否是第一个样品</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetBottleFromMaterialToCapperFive_One(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            ushort num = (ushort)(2 * sampleId -1); 
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从小瓶瓶架搬运{sampleId}小瓶到拧盖5");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsBottle1InShelf))
                    {
                        if (sample.BottleStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = Get_GC_BottleFromMaterialToCapperFive(num, cts);
                            if (!result)
                            {
                                throw new Exception($"从气质小瓶架搬运{sampleId}小瓶到拧盖5 失败! BottleStatus-{sample.BottleStatus}");
                            }
                            sample.BottleStatus = 1;
                        }
                        if (sample.BottleStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = Get_LC_BottleFromMaterialToCapperFive(num, cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从小瓶瓶架搬运{sampleId}小瓶到拧盖5 失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetBottleFromMaterialToCapperFive_One(sample, cts);
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
        /// 从小瓶架搬运小瓶到拧盖5
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isFirst">是否是第一个样品</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetBottleFromMaterialToCapperFive_Two(Sample sample, CancellationTokenSource cts)
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
                        result = Get_GC_BottleFromMaterialToCapperFive(num, cts);
                        if (!result)
                        {
                            throw new Exception($"从气质小瓶架搬运{sampleId}小瓶到拧盖5 失败! BottleStatus-{sample.BottleStatus}");
                        }
                        sample.BottleStatus = 1;
                    }
                    if (sample.BottleStatus == 1 && !_globalStatus.IsStopped)
                    {
                        result = Get_LC_BottleFromMaterialToCapperFive(num, cts);
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
        /// 从拧盖5搬运小瓶到小瓶架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isFirst">是否是第一个样品</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetBottleFromCapperFiveToMaterial_One(Sample sample, CancellationTokenSource cts)
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
                        result = Get_GC_BottleFromCapperFiveToMaterial(num, cts);
                        if (!result)
                        {
                            throw new Exception($"从拧盖5搬运{sampleId}小瓶到气质小瓶架 失败! BottleStatus-{sample.BottleStatus}");
                        }
                        sample.BottleStatus = 1;
                    }
                    if (sample.BottleStatus == 1 && !_globalStatus.IsStopped)
                    {
                        result = Get_LC_BottleFromCapperFiveToMaterial(num, cts);
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
        /// 从拧盖5搬运小瓶到小瓶架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isFirst">是否是第一个样品</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetBottleFromCapperFiveToMaterial_Two(Sample sample, CancellationTokenSource cts)
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
                        result = Get_GC_BottleFromCapperFiveToMaterial(num, cts);
                        if (!result)
                        {
                            throw new Exception($"从拧盖5搬运{sampleId}小瓶到气质小瓶架 失败! BottleStatus-{sample.BottleStatus}");
                        }
                        sample.BottleStatus = 1;
                    }
                    if (sample.BottleStatus == 1 && !_globalStatus.IsStopped)
                    {
                        result = Get_LC_BottleFromCapperFiveToMaterial(num, cts);
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
                if (cts?.IsCancellationRequested == true)
                {
                    _logger?.Error(ex.Message);
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
        /// <param name="var">1:  2:</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool DoPipettingOne(Sample sample, int var,Func<Sample,int,CancellationTokenSource,bool> capperOn, Func<Sample, int,CancellationTokenSource, bool> capperOff, CancellationTokenSource cts)
        {
            double volume = sample.TechParams.ExtractSampleVolume;  //提取样品溶液量
            int tech_i = 1;
            if (var == 2)
            {
                tech_i = 2;
            }
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger.Info($"提取样品液{ sample.Id}移液-{volume}ml");
                    if (sample.PipettorStep2 == 1 && !_globalStatus.IsStopped)
                    {
                        //取枪头
                        var result = base.GetNeedleAsync(GetTip1Coordinate(2 * sample.Id - 1), cts).GetAwaiter().GetResult();
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
                        var result = DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id -1 , tech_i), GetPipettorTargetCoordinate(1, tech_i), volume, cts).GetAwaiter().GetResult();
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
                        var result = DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id -1, tech_i), GetPipettorTargetCoordinate(2, tech_i), volume, cts).GetAwaiter().GetResult();
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
                        var result = base.PutNeedleAsync(GetTip1Coordinate(2 * sample.Id -1 ), cts,30).GetAwaiter().GetResult();
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
                        var result = capperOn(sample,1,cts);
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
                        var result = GetBottleFromCapperFiveToMaterial_One(sample, cts);
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
                        var result = GetBottleFromMaterialToCapperFive_Two(sample, cts);
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
                        var result = capperOff(sample,2, cts);
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
                        var result = base.GetNeedleAsync(GetTip1Coordinate(2 * sample.Id ), cts).GetAwaiter().GetResult();
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
                        var result = DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id ,tech_i), GetPipettorTargetCoordinate(1, tech_i), volume, cts).GetAwaiter().GetResult();
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
                        var result = DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id , tech_i), GetPipettorTargetCoordinate(2, tech_i), volume, cts).GetAwaiter().GetResult();
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
                        var result = base.PutNeedleAsync(GetTip1Coordinate(2 * sample.Id ), cts, 30).GetAwaiter().GetResult();
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
                        var result = capperOn(sample,2, cts);
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
                        var result = GetBottleFromCapperFiveToMaterial_Two(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"第二组小瓶搬运到试管架失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2 = 1;
                        _currentMethodName = string.Empty;
                        return true;
                      
                    }

                    throw new Exception($"样品{ sample.Id}移液-{volume}ml失败,pipettingStep-{sample.PipettorStep2}");
                }
            }
            catch (OccupyMethodException)
            {
                return DoPipettingOne(sample,var,capperOn, capperOff, cts);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }

        }

        /// <summary>
        /// 第二组移液 浓缩 净化管（2ml） ==》西林瓶   大管==》西林瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">1:净化管（2ml） ==》西林瓶 2:大管==》西林瓶</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool DoPipettingTwo(Sample sample,int var,CancellationTokenSource cts)
        {
            double volume = sample.TechParams.ConcentrationVolume;  //提取样品浓缩量
            int tech_i = 3;
            if (var == 2)
            {
                tech_i = 4;
            }
            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger.Info($"提取浓缩液{ sample.Id}移液-{volume}ml");
                    if (sample.PipettorStep2 == 1 && !_globalStatus.IsStopped)
                    {
                        //取枪头
                        var result = base.GetNeedleAsync(GetTip2Coordinate(2 * sample.Id - 1), cts).GetAwaiter().GetResult();
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
                        var result = DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id - 1, tech_i), GetPipettorTargetCoordinate(1, tech_i), volume, cts).GetAwaiter().GetResult();
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
                        var result = base.PutNeedleAsync(GetTip2Coordinate(2 * sample.Id - 1), cts, 30).GetAwaiter().GetResult();
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
                        var result = base.GetNeedleAsync(GetTip2Coordinate(2 * sample.Id), cts).GetAwaiter().GetResult();
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
                        var result = DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id, tech_i), GetPipettorTargetCoordinate(2, tech_i), volume, cts).GetAwaiter().GetResult();
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
                        var result = base.PutNeedleAsync(GetTip2Coordinate(2 * sample.Id), cts, 30).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第二管放枪头失败,pipettingStep-{sample.PipettorStep2}");
                        }
                        sample.PipettorStep2 = 1;
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"样品{ sample.Id}移液-{volume}ml失败,pipettingStep-{sample.PipettorStep2}");
                }
            }
            catch (OccupyMethodException)
            {
                return DoPipettingTwo(sample, var,cts);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

    
        //========================================加标=========================================================//


        /// <summary>
        /// 从试管架搬运净化管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromMaterialToCapperThree(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"搬运{sampleId}净化管到拧盖3");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToCapperThree((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToCapperThree((ushort)(2 * sampleId), cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}净化管到拧盖3失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromMaterialToCapperThree(sample, cts);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从拧盖3搬运净化管到振荡
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperThreeToVibration(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从拧盖3搬运{sampleId}净化管到振荡");

                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToVibration((ushort)(2 * sampleId),null,null, cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToVibration((ushort)(2 * sampleId -1), null, null, cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从拧盖3搬运{sampleId}净化管到振荡失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromCapperThreeToVibration(sample, cts);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从振荡搬运净化管到哪拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromVibrationToCapperThree(Sample sample,CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从振荡搬运{sampleId}净化管到拧盖3");

                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInVibration))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToCapperThree((ushort)(2 * sampleId -1), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToCapperThree((ushort)(2 * sampleId ), null, null, cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从振荡搬运{sampleId}净化管到拧盖3失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromVibrationToCapperThree(sample, cts);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从振荡搬运净化管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromVibrationToMaterial(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从振荡搬运{sampleId}净化管到试管架");

                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInVibration))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToMaterial((ushort)(2 * sampleId - 1),null,null, cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToMaterial((ushort)(2 * sampleId), null, null, cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从振荡搬运{sampleId}净化管到试管架失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromVibrationToMaterial(sample, cts);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }

        }


        /// <summary>
        /// 搬运试管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleToCapperThree(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"搬运{sampleId}净化管到拧盖3");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToCapperThree((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToCapperThree((ushort)(2 * sampleId), cts);
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
                            result = GetSampleFromVibrationToCapperThree((ushort)(2 * sampleId -1), null, null,cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡2搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToCapperThree((ushort)(2 * sampleId), null, null, cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}净化管到拧盖3失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleToCapperThree(sample, cts);
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"搬运{sampleId}净化管到拧盖3 停止");
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
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperThreeToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从拧盖3搬运{sampleId}净化管到移栽");
                    //净化管在拧盖3
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToTransfer((ushort)(2 * sampleId),null,null, func, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖3搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToTransfer((ushort)(2 * sampleId - 1), null, null, func, cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从拧盖3搬运{sampleId}净化管到移栽失败,SampleStatus-{sample.PurifyStatus}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromCapperThreeToTransfer(sample,func, cts);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从拧盖3取有盖试管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperThreeToMaterial(Sample sample,CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从拧盖3搬运{sampleId}净化管到净化管架");
                    //净化管在拧盖3
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToMaterial((ushort)(2 * sampleId - 1),null,null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖3搬运{sampleId}净化管到净化管架失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToMaterial((ushort)(2 * sampleId ), null, null, cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从拧盖3搬运{sampleId}净化管到净化管架失败,SampleStatus-{sample.PurifyStatus}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromCapperThreeToMaterial(sample, cts);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }


        /// <summary>
        /// 从移栽搬运试管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromTransferToCapperThree(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从移栽搬运{sampleId}净化管到拧盖3");
                    //试管在移栽
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromTransferToCapperThree((ushort)(2 * sampleId), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从移栽搬运{sampleId}净化管到拧盖3失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromTransferToCapperThree((ushort)(2 * sampleId -1), func, cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从移栽搬运{sampleId}净化管到拧盖3失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromTransferToCapperThree(sample,func, cts);
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽搬运{sampleId}净化管到拧盖3 停止");
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
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromMaterialToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"搬运{sampleId}净化管到移栽");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToTransfer((ushort)(2 * sampleId - 1), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToTransfer((ushort)(2 * sampleId), func, cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}净化管到移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromMaterialToTransfer(sample, func, cts);
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"搬运{sampleId}净化管到移栽 停止");
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
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromTransferToMaterial(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从移栽搬运{sampleId}净化管到试管架");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromTransferToMaterial((ushort)(2 * sampleId - 1), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从移栽搬运{sampleId}净化管到试管架失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromTransferToMaterial((ushort)(2 * sampleId), func, cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"从移栽搬运{sampleId}净化管到试管架失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleFromTransferToMaterial(sample,func, cts);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                 throw ex;
            }
        }



        /// <summary>
        /// 搬运试管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;

            Thread.Sleep(300);
            try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(_currentMethodName))
                    {
                        if (_currentMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            throw new OccupyMethodException();
                        }
                    }
                    _currentMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"搬运{sampleId}净化管到移栽");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToTransfer((ushort)(2 * sampleId - 1), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromMaterialToTransfer((ushort)(2 * sampleId), func, cts);
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
                            result = GetSampleFromVibrationToTransfer((ushort)(2 * sampleId - 1), null, null,func, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡2搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromVibrationToTransfer((ushort)(2 * sampleId), null, null,func, cts);
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
                            result = GetSampleFromCapperThreeToTransfer((ushort)(2 * sampleId - 1), null, null, func, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖3搬运{sampleId}净化管到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.PurifyStatus = 1;
                        }
                        if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleFromCapperThreeToTransfer((ushort)(2 * sampleId), null, null, func, cts);
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
                        _currentMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}净化管到移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (OccupyMethodException)
            {
                return GetSampleToTransfer(sample, func, cts);
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"搬运{sampleId}净化管到移栽 停止");
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromMaterialToCapperThree(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            var result = base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromMaterialToVibration(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            var result = base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromMaterialToTransfer(ushort num, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            //旋转到指定位置
            var result1 = func.Invoke(num, cts);

            //取料
            var result = base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
            result = base.PutTubeAsync(GetTransferCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperThreeToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }
     
            //取料
            result = base.GetTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
            result = base.PutTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperThreeToVibration(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }
          
            //取料
            result =  base.GetTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
            result = base.PutTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperThreeToTransfer(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            //旋转到指定位置
            var result1 = func.Invoke(num, cts);

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            result =  base.GetTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //Z轴上升
            CheckAxisZInSafePos(cts).GetAwaiter().GetResult();
            while (_globalStatus.IsPause)
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
            result = base.PutTubeAsync(GetTransferCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromVibrationToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }
           
            //取料
            result = base.GetTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
            result = base.PutTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromVibrationToTransfer(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            //旋转到指定位置
            var result1 = func.Invoke(num, cts);

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            result = base.GetTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
            result = base.PutTubeAsync(GetTransferCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        protected bool GetSampleFromVibrationToCapperThree(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }
     
            //取料
            result =  base.GetTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
            result = base.PutTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromTransferToMaterial(ushort num, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //旋转到指定位置
            var result1 = func.Invoke(num, cts);
            if (!result1.GetAwaiter().GetResult())
            {
                throw new Exception("移栽移动到指定位失败!");
            }
   
            //取料
            var result =  base.GetTubeAsync(GetTransferCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
        
            //放料
            result = base.PutTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromTransferToCapperThree(ushort num, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //旋转到指定位置
            var result1 = func.Invoke(num,cts);
            if (!result1.GetAwaiter().GetResult())
            {
                throw new Exception("移栽移动到指定位失败!");
            }

            //取料
            var result = base.GetTubeAsync(GetTransferCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromMaterialToCapperFour(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            var result =  base.GetTubeAsync(GetSeilingCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromCapperFourToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            var result = base.GetTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetSeilingCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromCapperFourToConcentration(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            var result = base.GetTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetConcentrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromCapperFourToWeight(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            var result =  base.GetTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetWeightCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromConcentrationToCapperFour(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            var result =  base.GetTubeAsync(GetConcentrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result =  base.PutTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromConcentrationToWeight(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;


            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            //取料
            var result = base.GetTubeAsync(GetConcentrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result =  base.PutTubeAsync(GetWeightCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromWeightToCapperFour(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            var result =  base.GetTubeAsync(GetWeightCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result =  base.PutTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromWeightToConcentration(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            var result = base.GetTubeAsync(GetWeightCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result =  base.PutTubeAsync(GetConcentrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool Get_GC_BottleFromMaterialToCapperFive(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 180;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            var result = base.GetTubeAsync(Get_GC_BottleCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetCapperFiveCoordinate(1), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool Get_GC_BottleFromCapperFiveToMaterial(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 180;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            var result =  base.GetTubeAsync(GetCapperFiveCoordinate(1), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result =  base.PutTubeAsync(Get_GC_BottleCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool Get_LC_BottleFromMaterialToCapperFive(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 180;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            var result = base.GetTubeAsync(Get_LC_BottleCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(GetCapperFiveCoordinate(2), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool Get_LC_BottleFromCapperFiveToMaterial(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 180;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            var result = base.GetTubeAsync(GetCapperFiveCoordinate(2), clawOpenByte, cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //放料
            result = base.PutTubeAsync(Get_LC_BottleCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
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
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool AddMarkFromSourceToWeight(int var,double volume, CancellationTokenSource cts)
        {
            try
            {
                Z_Cylinder_Up();
                //Xy移动到取液位
                var result = CarrierMoveToSafePos(GetAddMarkSourceCoordinate(var),cts).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception("XY轴移动到加标取液位 失败!");
                }
                while (_globalStatus.IsPause)
                {
                    Thread.Sleep(2000);
                }
                Z_Cylinder_Down();
                //吸液
                result = _motion.P2pMoveWithCheckDone(_axisSyring, volume, _syringVel, _globalStatus).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception($"注射器吸液失败-{volume}ul");
                }
                //上升
                Z_Cylinder_Up();
                while (_globalStatus.IsPause)
                {
                    Thread.Sleep(2000);
                }
                //xy移动到吐液位
                result = CarrierMoveToSafePos(GetAddMarkTargetCoordinate(), cts).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception("XY轴移动到加标吐液位 失败!");
                }
                Z_Cylinder_Down();
                while (_globalStatus.IsPause)
                {
                    Thread.Sleep(2000);
                }
                //吐液
                result = _motion.P2pMoveWithCheckDone(_axisSyring, 0, _syringVel, _globalStatus).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception($"注射器吐液失败");
                }
                while (_globalStatus.IsPause)
                {
                    Thread.Sleep(2000);
                }
                //上升
                Z_Cylinder_Up();

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        } 

        /// <summary>
        /// 注射器清洗
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool WashSyring(CancellationTokenSource cts)
        {
            try
            {
                Z_Cylinder_Up();
                //Xy移动到取液位
                var result = CarrierMoveToSafePos(GetSyringWashCoordinate(), cts).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception("XY轴移动到加标取液位 失败!");
                }
                Z_Cylinder_Down();
                while (_globalStatus.IsPause)
                {
                    Thread.Sleep(2000);
                }
                //吸液
                result = _motion.P2pMoveWithCheckDone(_axisSyring, 50, _syringVel, _globalStatus).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception($"注射器清洗吸液失败-{50}ul");
                }
                while (_globalStatus.IsPause)
                {
                    Thread.Sleep(2000);
                }
                //吐液
                result = _motion.P2pMoveWithCheckDone(_axisSyring, 0, _syringVel, _globalStatus).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception($"注射器吐液失败");
                }
                while (_globalStatus.IsPause)
                {
                    Thread.Sleep(2000);
                }
                //上升
                Z_Cylinder_Up();

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        #endregion

        #region Protected Methods 移液部分

        protected override async Task<bool> DoPipettingAsync(double[] sourcePos, double[] targetPos, double volume, CancellationTokenSource cts)
        {
            //移液1ml
            if (volume <= 1 && !_globalStatus.IsStopped)
            {
                return await base.DoPipettingAsync(sourcePos, targetPos, volume, cts).ConfigureAwait(false);
            }

            //移液1~2ml
            if (volume > 1 && volume <= 2)
            {
                if (_pipStep == 0 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 1 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, volume - 1, cts).ConfigureAwait(false);
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
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 1 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 2 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, volume - 2, cts).ConfigureAwait(false);
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
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 1 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 2 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 3 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, volume - 3, cts).ConfigureAwait(false);
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
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 1 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 2 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 3 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 4 && !_globalStatus.IsStopped)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, volume - 4, cts).ConfigureAwait(false);
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
            _logger?.Debug($"Z_Cylinder_Up-{checkSensor}");
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
            _logger?.Debug($"Z_Cylinder_Down-{checkSensor}");
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
        /// <returns></returns>
        private double[] GetConcentrationCoordinate(int tubeId)
        {
            double x = _posData.ConcentrationPos[0];
            double y = _posData.ConcentrationPos[1];
            double z = _posData.ConcentrationPos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 45, y, z };
            }
            return new double[] { x, y, z };
        }

        /// <summary>
        /// 获取称重位置坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetWeightCoordinate(int tubeId)
        {
            return _posData.WeightPos;
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
         
            return base.GetCoordinate(tubeId , 8, 12, -10, 20, xyz);
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
            return base.GetCoordinate(tubeId, 8, 12, -10, 20, xyz);
        }

        /// <summary>
        /// 获取移液取液坐标
        /// </summary>
        /// <param name="sampleId">单双数</param>
        /// <param name="tech_i">1:净化管（2ml）==》小瓶 2:西林瓶  ==》 小瓶 3:净化管（2ml） ==》西林瓶 4:大管==》西林瓶 </param>
        /// <returns></returns>
        private double[] GetPipettorSourceCoordinate(int sampleId ,int tech_i)
        {
            //净化管（2ml）==》小瓶  浓缩西林瓶  ==》 小瓶  净化管（2ml） ==》西林瓶    大管==》西林瓶  
            double[] poss = new double[3];
           
            switch (tech_i)
            {
                case 1:
                case 3:  //净化管（2ml）  拧盖3处
                    Array.Copy(_posData.PipettingSourcePos,poss,3);
                    break;
                case 2:  //西林瓶
                    Array.Copy(_posData.PipettingTargetPos2, poss, 3);
                    break;
                case 4:  //移栽大管
                    Array.Copy(_posData.PipettingSourcePos2, poss, 3);
                    break;
                default:
                    throw new InvalidOperationException($"移液工艺错误 err:{tech_i}");
            }

            int i = (sampleId - 1) % 2;

            if (i == 1)//单数
            {
                if (tech_i == 4)
                {
                    return new double[] { poss[0] + 45, poss[1], poss[2] };
                }
                else
                {
                    return new double[] { poss[0] + 60, poss[1], poss[2] };
                }
            }
            else
            {
                return poss;
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
                    poss = new double[] { _posData.PipettingTargetPos2[0], _posData.PipettingTargetPos2[1], _posData.PipettingTargetPos2[2] -35 };
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


        #endregion


        private double ReadWeight()
        {
            //等待称台稳定
            bool isStatic = false;
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(10);
            while (!isStatic)
            {
                isStatic = _weight.ReadIsStatic(_weithtId).GetAwaiter().GetResult();
                Thread.Sleep(500);
                if (DateTime.Now > end)
                {
                    throw new Exception("等待称台稳定超时 10S");
                }
            }
            //读取称台值
            return _weight.ReadWeight(_weithtId).GetAwaiter().GetResult();
        }


        /// <summary>
        /// 移液器规避位置
        /// </summary>
        /// <returns></returns>
        protected override double[] GetPipettingSafePos()
        {
            return null;
            //return new double[] { 330, 0 };
        }

    }
}
