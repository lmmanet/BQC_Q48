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
    public class CentrifugalCarrier : ICentrifugalCarrier
    {
        //运行参数
        private Task _pipttorTask;            //移液提取 （包括离心搬运 拧盖2 拧盖3）
        private Task _concentrationTask;
        

        #region Private Members
        private readonly static object _lockObj = new object();
        private readonly static object _lockCapperTwo = new object();

        private readonly IEtherCATMotion _motion;
        private readonly IIoDevice _io;
        private readonly ILS_Motion _stepMotion;
        private readonly IEPG26 _claw;
        private readonly ILogger _logger;
        private readonly ICentrifugalCarrierPosDataAccess _dataAccess;
        private readonly ICapperTwo _capperTwo;
        private readonly ICapperThree _capperThree;
        private readonly ICapperFour _capperFour;
        private readonly IGlobalStatus _globalStatus;

        private readonly IVibrationOne _vibrationOne;

        private readonly ICarrierOne _carrierOne;
        private readonly ICarrierTwo _carrierTwo;

        private CentrifugalCarrierPosData _posData;

        #endregion

        #region Variants

        private ushort _axisZ = 7;               //搬运Z轴
        private ushort _axisX = 14;              //搬运X轴
        private ushort _axisC = 15;              //搬运旋转轴

        private ushort _y_Ctr = 2;               //Y气缸控制
        private ushort _y_HP = 3;                //Y气缸缩回感应
        private ushort _y_WP = 2;                //Y气缸伸出感应

        private ushort _clawId = 2;              //电爪485地址
        private double _stepMoveVel = 80;        //步进电机移动速度
        private double _sevorMoveVel = 80;       //伺服电机移动速度
        #endregion

        #region Properties

        public bool IsPipttorTaskDone
        {
            get
            {
                if (_pipttorTask != null)
                {
                    return _pipttorTask.IsCompleted;
                }
                else
                {
                    return true;
                }
            }
        }

        public bool IsConcentrationTaskDone
        {
            get
            {
                if (_concentrationTask != null)
                {
                    return _concentrationTask.IsCompleted;
                }
                else
                {
                    return true;
                }
            }
        }


        #endregion

        #region Construtors

        public CentrifugalCarrier(IEtherCATMotion motion, IIoDevice io, ILS_Motion stepMotion, IEPG26 claw, ICentrifugalCarrierPosDataAccess dataAccess, 
            ICapperTwo capperTwo,ICapperThree capperThree, ICapperFour capperFour, IGlobalStatus globalStatus, IVibrationOne vibrationOne,
            ICarrierOne carrierOne,ICarrierTwo carrierTwo)
        {
            this._motion = motion;
            this._io = io;
            this._stepMotion = stepMotion;
            this._dataAccess = dataAccess;
            this._claw = claw;

            this._capperTwo = capperTwo;
            this._capperThree = capperThree;
            this._capperFour = capperFour;
            this._vibrationOne = vibrationOne;
            this._carrierOne = carrierOne;
            this._carrierTwo = carrierTwo;


            this._logger = new MyLogger(typeof(CentrifugalCarrier));
            this._globalStatus = globalStatus;
            _globalStatus.StopProgramEventArgs += StopMove;
            _globalStatus.PauseProgramEventArgs += StopMove;

            _posData = _dataAccess.GetPosData();
        }

        public void UpdatePosData()
        {
            _posData = _dataAccess.GetPosData();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 模块回零
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GoHome(CancellationTokenSource cts)
        {
            _logger?.Info("离心机搬运回零");
            try
            {
                //失能夹爪
                var result = await _claw.Disable(_clawId).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //使能夹爪
                result = await _claw.Enable(_clawId).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //Z轴回零
                result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, 10, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //X C轴回零
                await _stepMotion.ServoOn(_axisX).ConfigureAwait(false);
                await _stepMotion.ServoOn(_axisC).ConfigureAwait(false);
                var ret1 = _stepMotion.GoHomeWithCheckDone(_axisX, cts).ConfigureAwait(false);
                var ret2 = _stepMotion.GoHomeWithCheckDone(_axisC, cts).ConfigureAwait(false);
                if (!ret1.GetAwaiter().GetResult() || !ret2.GetAwaiter().GetResult())
                {
                    return false;
                }
                //气缸回零
                Y_Cylinder_Put();

                if (GlobalCache.Instance.TubeInCentrifugal.Count >0)
                {
                    GlobalCache.Instance.TubeInCentrifugal.ForEach(t => _logger?.Warn($"离心机工位-{t}-存在试管"));
                    return false;
                }

                return true;
            }
            //catch (CommunicationException cmex)
            //{
            //    return false;
            //}
            //catch (EtherCATMotionException ecex)
            //{
            //    return false;
            //}
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

        }

        public bool StopMove()
        {
            _motion.StopMove(_axisZ);
            _stepMotion.StopMove(_axisX);
            _stepMotion.StopMove(_axisC);
            return true;
        }

        /// <summary>
        /// 从离心机中清出试管
        /// </summary>
        public void ClearTubeOut()
        {
            var list = new List<ushort>();
            foreach (var item in GlobalCache.Instance.TubeInCentrifugal)
            {
                list.Add(item);
            }
            foreach (var item in list)
            {
                GlobalCache.Instance.TubeInCentrifugal.Remove(item);
            }
        }

        //================================================================离心机搬运部分=====================================================================//

        /// <summary>
        /// 农残一次离心从冰浴取料   或兽药一次离心从试管架取料
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="GoStation"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromColdToCentrifugal(Sample sample, Func<ushort, Task<bool>> GoStation, CancellationTokenSource cts)
        {
          s0:  try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CentrifugalCarrierMethodName))
                    {
                        if (GlobalCache.Instance.CentrifugalCarrierMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            goto s0;
                        }
                    }
                    GlobalCache.Instance.CentrifugalCarrierMethodName = MethodBase.GetCurrentMethod().Name;

                    bool result;
                    //移栽上料
                    if (sample.CenCarrierStep == 0 && !_globalStatus.IsStopped)
                    {
                        //大管农残一次离心  
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCold) && !TechStatusHelper.BitIsOn(sample.TechParams,TechStatus.ExtractSupernate2))
                        {
                            result = GetSampleToTransfer(sample, cts);
                            if (!result)
                            {
                                return false;
                            }
                            sample.CenCarrierStep++;
                        }
                        //大管兽药一次离心
                        else if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf) && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractSupernate2))
                        {
                            result = GetSampleToTransfer(sample, cts);
                            if (!result)
                            {
                                return false;
                            }
                            sample.CenCarrierStep++;
                        }
                        //抛出异常

                    }

                    //放试管进离心机
                    if (sample.CenCarrierStep == 1 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                        {
                            result = GetTubeInCentrifugal(sample, GoStation, 1, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCentrifugal))
                        {
                            sample.CenCarrierStep++;
                            GlobalCache.Instance.CentrifugalCarrierMethodName = string.Empty;
                            return true;
                        }
                        
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                _globalStatus.PauseProgram();
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        public bool GetSampleFromCentrifugalToMaterial(Sample sample, Func<ushort, Task<bool>> GoStation, CancellationTokenSource cts)
        {
          s0:  try
            {
                lock(_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CentrifugalCarrierMethodName))
                    {
                        if (GlobalCache.Instance.CentrifugalCarrierMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            goto s0;
                        }
                    }
                    GlobalCache.Instance.CentrifugalCarrierMethodName = MethodBase.GetCurrentMethod().Name;

                    bool result;

                    //取出试管
                    if (sample.CenCarrierStep == 2 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCentrifugal))
                        {
                            result = GetTubeOutCentrifugal(sample, GoStation, 1, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample.CenCarrierStep++;
                    }

                    ///移栽下料
                    if (sample.CenCarrierStep == 3 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                        {
                            result = GetSampleFromTransferToMarterial(sample, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample.CenCarrierStep = 0;
                        GlobalCache.Instance.CentrifugalCarrierMethodName = string.Empty;
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                _globalStatus.PauseProgram();
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        public bool GetPolishFromMaterialToCentrifugal(Sample sample, Func<ushort, Task<bool>> GoStation, CancellationTokenSource cts)
        {
           s0: try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CentrifugalCarrierMethodName))
                    {
                        if (GlobalCache.Instance.CentrifugalCarrierMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            goto s0;
                        }
                    }
                    GlobalCache.Instance.CentrifugalCarrierMethodName = MethodBase.GetCurrentMethod().Name;

                    bool result;
                    //移栽上料
                    if (sample.CenCarrierStep == 0 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInShelf))
                        {
                            result = GetPolishFromMaterialToTransfer(sample, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample.CenCarrierStep++;
                    }

                    //放试管进离心机
                    if (sample.CenCarrierStep == 1 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInTransfer))
                        {
                            result = GetTubeInCentrifugal(sample, GoStation, 3, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample.CenCarrierStep++;
                        GlobalCache.Instance.CentrifugalCarrierMethodName = string.Empty;
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                _globalStatus.PauseProgram();
                _logger?.Warn(ex.Message);
                return false;
            }
        }       
        
        public bool GetPolishFroCentrifugaToShelf(Sample sample, Func<ushort, Task<bool>> GoStation, CancellationTokenSource cts)
        {
            s0: try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CentrifugalCarrierMethodName))
                    {
                        if (GlobalCache.Instance.CentrifugalCarrierMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            goto s0;
                        }
                    }
                    GlobalCache.Instance.CentrifugalCarrierMethodName = MethodBase.GetCurrentMethod().Name;

                    bool result;
                    //取出试管
                    if (sample.CenCarrierStep == 2 && !_globalStatus.IsStopped)     //&& !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Centrifugal3)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCentrifugal))
                        {
                            result = GetTubeOutCentrifugal(sample, GoStation, 3, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample.CenCarrierStep++;
                    }

                    //移栽下料
                    if (sample.CenCarrierStep == 3 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInTransfer))
                        {
                            result = GetPolishFromTransferToMarterial(sample, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample.CenCarrierStep = 0;
                        GlobalCache.Instance.CentrifugalCarrierMethodName = string.Empty;
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                _globalStatus.PauseProgram();
                if (!_globalStatus.IsStopped)
                {
                    _logger?.Warn(ex.Message);
                }
                return false;
            }
        }

        public bool GetPurifyFromMaterialToCentrifugal(Sample sample, Func<ushort, Task<bool>> GoStation, CancellationTokenSource cts)
        {
            s0: try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CentrifugalCarrierMethodName))
                    {
                        if (GlobalCache.Instance.CentrifugalCarrierMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            goto s0;
                        }
                    }
                    GlobalCache.Instance.CentrifugalCarrierMethodName = MethodBase.GetCurrentMethod().Name;

                    bool result;
                    //搬运2从净化管架上料
                    if (sample.CenCarrierStep == 0 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                        {
                            result = GetPurifyFromMaterialToTransfer(sample, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample.CenCarrierStep++;
                    }

                    //放试管进离心机
                    if (sample.CenCarrierStep == 1 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                        {
                            result = GetTubeInCentrifugal(sample, GoStation, 2, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample.CenCarrierStep++;
                        GlobalCache.Instance.CentrifugalCarrierMethodName = string.Empty;
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                _globalStatus.PauseProgram();
                if (!_globalStatus.IsStopped)
                {
                    _logger?.Warn(ex.Message);
                }
                return false;
            }
        } 
        
        public bool GetPurifyFromCentrifugalToMaterial(Sample sample, Func<ushort, Task<bool>> GoStation, CancellationTokenSource cts)
        {
            s0: try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CentrifugalCarrierMethodName))
                    {
                        if (GlobalCache.Instance.CentrifugalCarrierMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            goto s0;
                        }
                    }
                    GlobalCache.Instance.CentrifugalCarrierMethodName = MethodBase.GetCurrentMethod().Name;

                    bool result;
                    //取出试管
                    if (sample.CenCarrierStep == 2 && !_globalStatus.IsStopped ) 
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCentrifugal))
                        {
                            result = GetTubeOutCentrifugal(sample, GoStation, 2, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample.CenCarrierStep++;
                    }

                    //移栽下料
                    if (sample.CenCarrierStep == 3 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                        {
                            result = GetPurifyFromTransferToMarterial(sample, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample.CenCarrierStep = 0;
                        GlobalCache.Instance.CentrifugalCarrierMethodName = string.Empty;
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                _globalStatus.PauseProgram();
                if (!_globalStatus.IsStopped)
                {
                    _logger?.Warn(ex.Message);
                }
                return false;
            }
        }

        public bool GetBigAndSmallToCentrifugal(Sample sample1, Sample sample2, int var, Func<ushort, Task<bool>> GoStation, CancellationTokenSource cts)
        {
            s0: try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CentrifugalCarrierMethodName))
                    {
                        if (GlobalCache.Instance.CentrifugalCarrierMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            goto s0;
                        }
                    }
                    GlobalCache.Instance.CentrifugalCarrierMethodName = MethodBase.GetCurrentMethod().Name;

                    bool result;

                    //搬运净化管到移栽
                    if (sample2.SubStep == 0 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample2, SampleStatus.IsPurfyInShelf))
                        {
                            result = GetPurifyFromMaterialToTransfer(sample2, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample2.SubStep++;
                    }

                    //搬运样品大管到移栽
                    if (var == 1 && !_globalStatus.IsStopped)
                    {
                        //移栽上料  两种情况
                        if (sample1.SubStep == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetSampleToTransfer(sample1, cts);
                            if (!result)
                            {
                                return false;
                            }
                            sample1.SubStep++;
                        }

                    }
                    else if (var == 2 && !_globalStatus.IsStopped)
                    {
                        //移栽上料  两种情况
                        if (sample1.SubStep == 0 && !_globalStatus.IsStopped)
                        {
                            result = GetPolishFromMaterialToTransfer(sample1, cts);
                            if (!result)
                            {
                                return false;
                            }
                            sample1.SubStep++;
                        }
                    }

                    //小管到离心机
                    if (sample2.SubStep == 1 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample2, SampleStatus.IsPurfyInTransfer))
                        {
                            result = GetTubeInCentrifugal(sample2, GoStation, 2, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample2.SubStep++;
                    }

                    //大管到离心机
                    if (var ==1 && !_globalStatus.IsStopped)
                    {
                        if (!_globalStatus.IsStopped && sample1.SubStep == 1)
                        {
                            if (SampleStatusHelper.BitIsOn(sample1, SampleStatus.IsInTransfer))
                            {
                                //样品管
                                result = GetTubeInCentrifugal(sample1, GoStation, 1, cts);
                                if (!result)
                                {
                                    return false;
                                }
                            }
                            sample1.SubStep++;
                        }
                    }
                    else if (var == 2 && !_globalStatus.IsStopped)
                    {
                        if ( !_globalStatus.IsStopped && sample1.SubStep == 1)
                        {
                            //萃取管
                            if (SampleStatusHelper.BitIsOn(sample1, SampleStatus.IsPolishInTransfer))
                            {
                                result = GetTubeInCentrifugal(sample1, GoStation, 3, cts);
                                if (!result)
                                {
                                    return false;
                                }
                            }
                            sample1.SubStep++;
                        }
                    }

                    //判断大小管放进了离心机
                    if (sample1.SubStep == 2 && sample2.SubStep == 2)
                    {
                        GlobalCache.Instance.CentrifugalCarrierMethodName = string.Empty;
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                _globalStatus.PauseProgram();
                if (!_globalStatus.IsStopped)
                {
                    _logger?.Warn(ex.Message);
                }
                return false;
            }
        }

        public bool GetBigAndSmallToToMarterial(Sample sample1, Sample sample2, int var, Func<ushort, Task<bool>> GoStation, CancellationTokenSource cts)
        {
           s0: try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CentrifugalCarrierMethodName))
                    {
                        if (GlobalCache.Instance.CentrifugalCarrierMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            goto s0;
                        }
                    }
                    GlobalCache.Instance.CentrifugalCarrierMethodName = MethodBase.GetCurrentMethod().Name;

                    bool result;

                    //从离心机取出净化管
                    if (sample2.SubStep == 3 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample2, SampleStatus.IsPurfyInCentrifugal))
                        {
                            result = GetTubeOutCentrifugal(sample2, GoStation, 2, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample2.SubStep++;
                    }

                    //从离心机取出大管
                    if (var == 1 && !_globalStatus.IsStopped)
                    {
                        if (sample1.SubStep == 3 && !_globalStatus.IsStopped)
                        {
                            if (SampleStatusHelper.BitIsOn(sample1, SampleStatus.IsInCentrifugal))
                            {
                                //样品管
                                result = GetTubeOutCentrifugal(sample1, GoStation, 1, cts);
                                if (!result)
                                {
                                    return false;
                                }
                            }
                            sample1.SubStep++;
                        }
                    }
                    else if (var ==2 && !_globalStatus.IsStopped)
                    {
                        if (sample1.SubStep == 3 && !_globalStatus.IsStopped)
                        {
                            if (SampleStatusHelper.BitIsOn(sample1, SampleStatus.IsPolishInCentrifugal))
                            {
                                //萃取管
                                result = GetTubeOutCentrifugal(sample1, GoStation, 3, cts);
                                if (!result)
                                {
                                    return false;
                                }
                            }
                            sample1.SubStep++;
                        }
                    }

                    //净化管放到试管架
                    if (!_globalStatus.IsStopped && sample2.SubStep == 4)
                    {
                        if (SampleStatusHelper.BitIsOn(sample2, SampleStatus.IsPurfyInTransfer))
                        {
                            result = GetPurifyFromTransferToMarterial(sample2, cts);
                            if (!result)
                            {
                                return false;
                            }
                        }
                        sample2.SubStep++;
                    }

                    //大管放入到试管架
                    if (var == 1 && !_globalStatus.IsStopped)
                    {
                        if (!_globalStatus.IsStopped&& sample1.SubStep == 4)
                        {
                            if (SampleStatusHelper.BitIsOn(sample1, SampleStatus.IsInTransfer))
                            {
                                //样品管
                                result = GetSampleFromTransferToMarterial(sample1, cts);
                                if (!result)
                                {
                                    return false;
                                }
                            }
                            sample1.SubStep++;
                        }
                    }
                    else if (var == 2 && !_globalStatus.IsStopped)
                    {
                        if (!_globalStatus.IsStopped && sample1.SubStep == 4)
                        {
                            if (SampleStatusHelper.BitIsOn(sample1, SampleStatus.IsPolishInTransfer))
                            {
                                //萃取管
                                result = GetPolishFromTransferToMarterial(sample1, cts);
                                if (!result)
                                {
                                    return false;
                                }
                            }
                            sample1.SubStep++;
                        }

                    }

                    //判断大小管放到了试管架
                    if (sample1.SubStep == 5 && sample2.SubStep == 5)
                    {
                        GlobalCache.Instance.CentrifugalCarrierMethodName = string.Empty;
                        return true;
                    }

                    return false;

                }
            }
            catch (Exception ex)
            {
                _globalStatus.PauseProgram();
                if (!_globalStatus.IsStopped)
                {
                    _logger?.Warn(ex.Message);
                }
                return false;
            }
        }

        /// <summary>
        /// 搬运试管到离心机
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="varTube">1:样品管 2:净化小管 3:萃取管</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetTubeInCentrifugal(Sample sample, Func<ushort, Task<bool>> func, int varTube, CancellationTokenSource cts)
        {
            byte clawOpen = 110;
            ushort pos1 = 2, pos2 = 4;
            int isInTransfer = 0, isInCentrigugal = 0;
            bool isBig =false;
            ushort sampleId = sample.Id;

            //判断大小管

            if (varTube == 1)
            {   //样品大管
                isBig = true;
                clawOpen = 0;
                pos1 = 1;
                pos2 = 3;
                isInTransfer = 8;
                isInCentrigugal = 9;
            }
            else if(varTube == 2)
            {   //净化小管
                isBig = false ;
                clawOpen = 110;
                pos1 = 2;
                pos2 = 4;
                isInTransfer = 16;
                isInCentrigugal = 17;
            }
            else if(varTube == 3)
            {   //萃取大管
                isBig = true;
                clawOpen = 0;
                pos1 = 1;
                pos2 = 3;
                isInTransfer = 46;
                isInCentrigugal = 47;
            }

          s0:  try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CentrifugalCarrierMethodName))
                    {
                        if (GlobalCache.Instance.CentrifugalCarrierMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            goto s0;
                        }
                    }
                    GlobalCache.Instance.CentrifugalCarrierMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"搬运{sampleId}样品到离心机");
                    if (SampleStatusHelper.BitIsOn(sample, isInTransfer))
                    {

                        if (varTube == 1)
                        {
                            if (sample.SampleTubeStatus == 0 && !_globalStatus.IsStopped)
                            {
                                //移栽上取料
                                var result = GetTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId - 1), isBig), clawOpen, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"从离心移栽取{sampleId}样品失败！ SampleTubeStatus-{sample.SampleTubeStatus}");
                                }

                                //离心机放料
                                result = PutTubeAtCentrifugal(pos1, func, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"放{sampleId}样品到离心机失败！ SampleTubeStatus-{sample.SampleTubeStatus}");
                                }
                                sample.SampleTubeStatus = 1;
                            }

                            if (sample.SampleTubeStatus == 1 && !_globalStatus.IsStopped)
                            {
                                //移栽上取料
                                var result = GetTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId), isBig), clawOpen, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"从离心移栽取{sampleId}样品失败！ SampleTubeStatus-{sample.SampleTubeStatus}");
                                }

                                //离心机放料
                                result = PutTubeAtCentrifugal(pos2, func, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"放{sampleId}样品到离心机失败！ SampleTubeStatus-{sample.SampleTubeStatus}");
                                }
                                sample.SampleTubeStatus = 0;
                            }

                            SampleStatusHelper.ResetBit(sample, isInTransfer);
                            SampleStatusHelper.SetBitOn(sample, isInCentrigugal);
                        }

                        else if  (varTube == 2)
                        {
                            if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                            {
                                //移栽上取料
                                var result = GetTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId - 1), isBig), clawOpen, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"从离心移栽取{sampleId}样品失败！ PurifyStatus-{sample.PurifyStatus}");
                                }

                                //离心机放料
                                result = PutTubeAtCentrifugal(pos1, func, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"放{sampleId}样品到离心机失败！ PurifyStatus-{sample.PurifyStatus}");
                                }
                                sample.PurifyStatus = 1;
                            }

                            if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                            {
                                //移栽上取料
                                var result = GetTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId), isBig), clawOpen, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"从离心移栽取{sampleId}样品失败！ PurifyStatus-{sample.PurifyStatus}");
                                }

                                //离心机放料
                                result = PutTubeAtCentrifugal(pos2, func, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"放{sampleId}样品到离心机失败！ PurifyStatus-{sample.PurifyStatus}");
                                }
                                sample.PurifyStatus = 0;
                            }

                            SampleStatusHelper.ResetBit(sample, isInTransfer);
                            SampleStatusHelper.SetBitOn(sample, isInCentrigugal);
                        }

                        else if (varTube == 3)
                        {
                            if (sample.PolishStatus == 0 && !_globalStatus.IsStopped)
                            {
                                //移栽上取料
                                var result = GetTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId - 1), isBig), clawOpen, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"从离心移栽取{sampleId}样品失败！ PolishStatus-{sample.PolishStatus}");
                                }

                                //离心机放料
                                result = PutTubeAtCentrifugal(pos1, func, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"放{sampleId}样品到离心机失败！ PolishStatus-{sample.PolishStatus}");
                                }
                                sample.PolishStatus = 1;
                            }

                            if (sample.PolishStatus == 1 && !_globalStatus.IsStopped)
                            {
                                //移栽上取料
                                var result = GetTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId), isBig), clawOpen, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"从离心移栽取{sampleId}样品失败！ PolishStatus-{sample.PolishStatus}");
                                }

                                //离心机放料
                                result = PutTubeAtCentrifugal(pos2, func, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"放{sampleId}样品到离心机失败！ PolishStatus-{sample.PolishStatus}");
                                }
                                sample.PolishStatus = 0;
                            }
                            SampleStatusHelper.ResetBit(sample, isInTransfer);
                            SampleStatusHelper.SetBitOn(sample, isInCentrigugal);
                        }

                    }

                    if (SampleStatusHelper.BitIsOn(sample, isInCentrigugal))
                    {
                        GlobalCache.Instance.CentrifugalCarrierMethodName = string.Empty;
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}样品到离心机失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _globalStatus.PauseProgram();
                if (!_globalStatus.IsStopped)
                {
                    _logger?.Warn(ex.Message);
                }
                return false;
            }
        }

        /// <summary>
        /// 从离心机搬运试管出来
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="isBig"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetTubeOutCentrifugal(Sample sample, Func<ushort, Task<bool>> func, int varTube , CancellationTokenSource cts)
        {
            byte clawOpen = 110;
            bool isBig =false;
            ushort pos1 = 2, pos2 = 4;
            int isInTransfer = 0, isInCentrigugal = 0;
            ushort sampleId = sample.Id;

            //判断大小管
            if (varTube == 1)
            {   //样品大管
                isBig = true;
                clawOpen = 0;
                pos1 = 1;
                pos2 = 3;
                isInTransfer = 8;
                isInCentrigugal = 9;
            }
            else if (varTube == 2)
            {   //净化小管
                isBig = false;
                clawOpen = 110;
                pos1 = 2;
                pos2 = 4;
                isInTransfer = 16;
                isInCentrigugal = 17;
            }
            else if (varTube == 3)
            {   //萃取大管
                isBig = true;
                clawOpen = 0;
                pos1 = 1;
                pos2 = 3;
                isInTransfer = 46;
                isInCentrigugal = 47;
            }

           s0: try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CentrifugalCarrierMethodName))
                    {
                        if (GlobalCache.Instance.CentrifugalCarrierMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            goto s0;
                        }
                    }
                    GlobalCache.Instance.CentrifugalCarrierMethodName = MethodBase.GetCurrentMethod().Name;

                    _logger?.Info($"从离心机搬运{sampleId}样品到移栽");
                    if (SampleStatusHelper.BitIsOn(sample, isInCentrigugal))
                    {
                        if (varTube == 1)
                        {
                            if (sample.SampleTubeStatus == 0 && !_globalStatus.IsStopped)
                            {
                                //离心机取料
                                var result = GetTubeAtCentrifugal(pos1, func, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"从离心机取{sampleId}样品失败！ SampleTubeStatus-{sample.SampleTubeStatus}");
                                }

                                //移栽放料
                                result = PutTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId - 1), isBig), clawOpen, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"放{sampleId}样品到移栽失败！ SampleTubeStatus-{sample.SampleTubeStatus}");
                                }
                                sample.SampleTubeStatus = 1;
                            }

                            if (sample.SampleTubeStatus == 1 && !_globalStatus.IsStopped)
                            {
                                //离心机取料
                                var result = GetTubeAtCentrifugal(pos2, func, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"从离心机取{sampleId}样品失败！ SampleTubeStatus-{sample.SampleTubeStatus}");
                                }

                                //移栽放料
                                result = PutTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId), isBig), clawOpen, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"放{sampleId}样品到移栽失败！ SampleTubeStatus-{sample.SampleTubeStatus}");
                                }
                                sample.SampleTubeStatus = 0;
                            }
                            SampleStatusHelper.ResetBit(sample, isInCentrigugal);
                            SampleStatusHelper.SetBitOn(sample, isInTransfer);
                        }

                        else if (varTube ==2)
                        {
                            if (sample.PurifyStatus == 0 && !_globalStatus.IsStopped)
                            {
                                //离心机取料
                                var result = GetTubeAtCentrifugal(pos1, func, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"从离心机取{sampleId}样品失败！ PurifyStatus-{sample.PurifyStatus}");
                                }

                                //移栽放料
                                result = PutTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId - 1), isBig), clawOpen, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"放{sampleId}样品到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                                }
                                sample.PurifyStatus = 1;
                            }

                            if (sample.PurifyStatus == 1 && !_globalStatus.IsStopped)
                            {
                                //离心机取料
                                var result = GetTubeAtCentrifugal(pos2, func, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"从离心机取{sampleId}样品失败！ PurifyStatus-{sample.PurifyStatus}");
                                }

                                //移栽放料
                                result = PutTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId), isBig), clawOpen, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"放{sampleId}样品到移栽失败！ PurifyStatus-{sample.PurifyStatus}");
                                }
                                sample.PurifyStatus = 0;
                            }
                            SampleStatusHelper.ResetBit(sample, isInCentrigugal);
                            SampleStatusHelper.SetBitOn(sample, isInTransfer);
                        }

                        else if (varTube ==3)
                        {
                            if (sample.PolishStatus == 0 && !_globalStatus.IsStopped)
                            {
                                //离心机取料
                                var result = GetTubeAtCentrifugal(pos1, func, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"从离心机取{sampleId}样品失败！ PolishStatus-{sample.PolishStatus}");
                                }

                                //移栽放料
                                result = PutTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId - 1), isBig), clawOpen, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"放{sampleId}样品到移栽失败！ PolishStatus-{sample.PolishStatus}");
                                }
                                sample.PolishStatus = 1;
                            }

                            if (sample.PolishStatus == 1 && !_globalStatus.IsStopped)
                            {
                                //离心机取料
                                var result = GetTubeAtCentrifugal(pos2, func, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"从离心机取{sampleId}样品失败！ PolishStatus-{sample.PolishStatus}");
                                }

                                //移栽放料
                                result = PutTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId), isBig), clawOpen, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"放{sampleId}样品到移栽失败！ PolishStatus-{sample.PolishStatus}");
                                }
                                sample.PolishStatus = 0;
                            }
                            SampleStatusHelper.ResetBit(sample, isInCentrigugal);
                            SampleStatusHelper.SetBitOn(sample, isInTransfer);
                        }

                    }

                    if (SampleStatusHelper.BitIsOn(sample, isInTransfer))
                    {
                        GlobalCache.Instance.CentrifugalCarrierMethodName = string.Empty;
                        return true;
                    }

                    throw new Exception($"从离心机搬运{sampleId}样品到移栽失败,SampleStatus-{sample.Status}");

                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从离心机搬运{sampleId}样品到移栽 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

        }


        //================================================================搬运1 离心机调用部分=====================================================================//
       
        /// <summary>
        /// 搬运样品试管到移栽  两种情况从冰浴或者试管架1
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetSampleToTransfer(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                _logger?.Info($"取{sample.Id}样品到移栽");

                //在冰浴情况
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCold) && !_globalStatus.IsStopped)
                {
                    var result = _carrierOne.GetSampleFromColdToTransfer(sample, TransferMoveLeftPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"从冰浴取{ sample.Id }样品到移栽 失败");
                    }
                    //SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCold);
                    //SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                }
                //在试管架情况
                else if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf) && !_globalStatus.IsStopped)
                {
                    var result = _carrierOne.GetSampleFromMaterialToTransfer(sample, TransferMoveLeftPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"从试管架取{ sample.Id }样品到移栽 失败");
                    }
                }

                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"取{sample.Id}样品到移栽 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

            
        }

        /// <summary>
        /// 从试管架搬运萃取管到移栽 两种情况 在试管架 在冰浴
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetPolishFromMaterialToTransfer(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                _logger?.Info($"取{sample.Id}样品到移栽");

                //在试管架情况
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInShelf) && !_globalStatus.IsStopped)
                {
                    var result = _carrierOne.GetPolishFromMaterialToTransfer(sample, TransferMoveLeftPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"取{ sample.Id }样品到移栽 失败");
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishInShelf);
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishInTransfer);
                }
                //在冰浴情况
                else if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCold) && !_globalStatus.IsStopped)
                {
                    var result = _carrierOne.GetPolishFromMaterialToTransfer(sample, TransferMoveLeftPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"取{ sample.Id }样品到移栽 失败");
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishInShelf);
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishInTransfer);
                }

                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInTransfer))
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"取{sample.Id}样品到移栽 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

            
        }

        /// <summary>
        /// 从试管架搬运净化管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetPurifyFromMaterialToTransfer(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                _logger?.Info($"取{sample.Id}净化管到移栽");

                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf) && !_globalStatus.IsStopped)
                {
                    var result = _carrierTwo.GetSampleFromMaterialToTransfer(sample, TransferMoveRightPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"取{ sample.Id }净化管到移栽 失败");
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInShelf);
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInTransfer);
                }

                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"取{sample.Id}净化管到移栽 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

        }

        /// <summary>
        /// 从移栽取离心完成后的试管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isBig"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetSampleFromTransferToMarterial(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                _logger?.Info($"从移栽取出{sample.Id}样品管");

                //取出大管
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer) && !_globalStatus.IsStopped)
                {
                    var result = _carrierOne.GetSampleFromTransferToMaterial(sample, TransferMoveLeftPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"从移栽取出{sample.Id}样品管 失败");
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsInTransfer);
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInShelf);
                }

                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽取出{sample.Id}样品管 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从移栽取离心完成后的萃取管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isBig"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetPolishFromTransferToMarterial(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                _logger?.Info($"从移栽取出{sample.Id}萃取管");

                //取出大管
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInTransfer) && !_globalStatus.IsStopped)
                {
                    var result = _carrierOne.GetPolishFromTransferToMaterial(sample, TransferMoveLeftPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"从移栽取出{sample.Id}萃取管 失败");
                    }

                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishInTransfer);
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishInShelf);
                }

                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInShelf))
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽取出{sample.Id}萃取管 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从移栽取离心完成后的试管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isBig"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetPurifyFromTransferToMarterial(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                _logger?.Info($"从移栽取出{sample.Id}净化管");

                //取出净化小管
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer) && !_globalStatus.IsStopped)
                {
                    var result = _carrierTwo.GetSampleFromTransferToMaterial(sample, TransferMoveRightPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"从移栽取出{sample.Id}净化管 失败");
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInTransfer);
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInShelf);
                }

                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped)
                {
                    _logger?.Info($"从移栽取出{sample.Id}样品管 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        //================================================================拧盖3 移栽配合部分=====================================================================//

        public bool GetSampleFromTransferToMarterialPiperttor(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从移栽取出{sample.Id}样品净化管");

                    //取出净化管
                    var result = _capperThree.GetSampleFromTransferToCapperThree(sample, TransferMoveRightPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"从移栽取出{sample.Id}样品净化管 失败");
                    }

                    result = _capperThree.GetSampleFromCapperThreeToVibration(sample, cts);

                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsInTransfer);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽取出{sample.Id}样品净化管 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }


        //================================================================移液部分=====================================================================//

        public void AddSampleToPipettingList(Sample sample, string actionCallBack)
        {
            //判断去重
            if (sample != null)
            {
                sample.ActionCallBack = actionCallBack;
                var dic1 = GlobalCache.Instance.PipettorDic;
                if (!dic1.Contains(sample))
                {
                    dic1.Add(sample);
                }
            }
        }

        public void AddSampleToConcentrationList(Sample sample)
        {
            if (sample != null)
            {
                var ret = GlobalCache.Instance.ConcentrationList.Contains(sample);
                if (!ret)
                {
                    GlobalCache.Instance.ConcentrationList.Add(sample);
                }
            }
        }

        /// <summary>
        /// 移液 提取上清液 或者 提取萃取液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="actionCallBack"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public Task StartPipetting(CancellationTokenSource cts)
        {
            if (_pipttorTask != null)
            {
                if (!_pipttorTask.IsCompleted)
                {
                    return _pipttorTask;
                }
            }

            _pipttorTask = Task.Run(() =>
            {
                while (cts?.IsCancellationRequested != true)
                {
                    var pDic = GlobalCache.Instance.PipettorDic;

                    if (pDic.Count<=0)
                    {
                        break;
                    }

                    var itemSample1 = pDic[0];

                    try
                    {   //==========================不能加锁  锁在子方法内部=============================//
                        
                        //是否提取上清液  ==》净化管                      从样品离心管50ml到 ===>  净化管15ml
                        if (itemSample1.MainStep == 5 && !_globalStatus.IsStopped)
                        {
                            if (TechStatusHelper.BitIsOn(itemSample1.TechParams, TechStatus.ExtractSupernate))
                            {
                                // 移液大管到净化管
                                var result = DoPipettingOne(itemSample1, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception("DoCentrifugal1 err");
                                }

                                itemSample1.MainStep = 7;

                                //触发后续动作   振荡净化 提取样品液  振荡涡旋
                                MethodHelper.ExcuteMethod(itemSample1, cts);
                                //样品和任务从列表移除
                                pDic.Remove(itemSample1);
                            }
                            else
                            {
                                throw new Exception("移液工艺状态错误!");
                            }
                        }

                        //提取净化管 净化液   ==》  兽药处理工艺          从净化管15ml到 ===> 萃取管50ml
                        else if (TechStatusHelper.BitIsOn(itemSample1.TechParams, TechStatus.ExtractSupernate2) && itemSample1.MainStep == 8)//&& itemSample1.MainStep == 8
                        {
                            if (!_globalStatus.IsStopped && itemSample1.MainStep == 8)
                            {
                                //净化管到大管
                                var result = DoPipettingTwo(itemSample1, cts);   //净化管到大管移液
                                if (!result)
                                {
                                    throw new Exception("DoCentrifugal2 err");
                                }
                                itemSample1.MainStep++; //振荡1涡旋1
                            }

                            //触发后续动作   振荡净化 提取样品液  振荡涡旋
                            MethodHelper.ExcuteMethod(itemSample1, cts);
                            //样品和任务从列表移除
                            pDic.Remove(itemSample1);
                        }
                        else
                        {
                            throw new Exception("移液步骤号错误!");
                        }
                        
                   
                    }
                    catch (Exception ex)
                    {
                        _globalStatus.PauseProgram();
                        _logger?.Warn(ex.Message);
                        return;
                    }
                   
                }
            });

            return _pipttorTask;

        }

        /// <summary>
        /// 开始浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public Task StartConcentration(CancellationTokenSource cts)
        {
            if (_concentrationTask != null)
            {
                if (!_concentrationTask.IsCompleted)
                {
                    return _concentrationTask;
                }
            }

            _concentrationTask = Task.Run(() =>
            {
                try
                {
                    while (GlobalCache.Instance.ConcentrationList.Count > 0 && !_globalStatus.IsStopped)
                    {
                        var itemSample = GlobalCache.Instance.ConcentrationList[0];
                        bool result;
                        //提取萃取液   ==》浓缩西林瓶
                        if (TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractSupernate3) && itemSample.MainStep >= 11)
                        {
                            //拧盖4  搬运搬运西林瓶到拧盖4  =》拆盖 ==》称重
                            var result1 = _capperFour.GetSeilingAndWeight(itemSample, cts);
                           
                            //加入锁 移液过程
                          s0:  lock (_lockObj) 
                            {
                                if (!string.IsNullOrEmpty(GlobalCache.Instance.CentrifugalCarrierMethodName))
                                {
                                    if (GlobalCache.Instance.CentrifugalCarrierMethodName != MethodBase.GetCurrentMethod().Name)
                                    {
                                        goto s0;
                                    }
                                }
                                GlobalCache.Instance.CentrifugalCarrierMethodName = MethodBase.GetCurrentMethod().Name;


                                //占用拧盖2
                                lock (_lockCapperTwo)
                                {
                                    //搬运已经离心的萃取管到拧盖2 ==》拆盖 ==>到移栽
                                    if (itemSample.MainStep == 11 && !_globalStatus.IsStopped)
                                    {
                                        result = _capperTwo.GetPolishFromMaterialToTransfer(itemSample, TransferMoveLeftPutGetPos, cts);
                                        if (!result)
                                        {
                                            throw new Exception("搬运萃取管到移栽失败!");
                                        }
                                        itemSample.MainStep++;
                                    }

                                    //移栽移动到右侧
                                    if (itemSample.MainStep == 12 && !_globalStatus.IsStopped)
                                    {
                                        result = TransferMoveRightPipettorPos(cts).GetAwaiter().GetResult();
                                        if (!result)
                                        {
                                            throw new Exception("移栽移动到右侧移液位失败!");
                                        }
                                        itemSample.MainStep++;
                                    }
                                 
                                    //判断西林瓶到位
                                    if (!result1.GetAwaiter().GetResult())
                                    {
                                        throw new Exception("搬运西林到称重失败!");
                                    }

                                    //拧盖4 搬运2开始移液 == >移栽移动到右侧   
                                    if (itemSample.MainStep == 13 && !_globalStatus.IsStopped)
                                    {
                                        result = _carrierTwo.DoPipettingTwo(itemSample, 2, cts);//兽残移液
                                        if (!result)
                                        {
                                            throw new Exception("从萃取管移取上清液失败!");
                                        }
                                        itemSample.MainStep++;
                                    }

                                    //从移栽搬运无盖萃取管到拧盖2
                                    if (itemSample.MainStep == 14 && !_globalStatus.IsStopped)
                                    {
                                        result = _capperTwo.GetPolishFromTransferToCapperTwo(itemSample, TransferMoveLeftPutGetPos, cts);
                                        if (!result)
                                        {
                                            throw new Exception("搬运萃取管到拧盖2失败!");
                                        }
                                        itemSample.MainStep++;
                                    }

                                    //拧盖2装盖
                                    if (itemSample.MainStep == 15 && !_globalStatus.IsStopped)
                                    {
                                        result = _capperTwo.GetPolishFromCapperTwoToMaterial(itemSample, cts);
                                        if (!result)
                                        {
                                            throw new Exception("从拧盖2搬运萃取管到试管架失败!");
                                        }
                                        itemSample.MainStep++;
                                        GlobalCache.Instance.CentrifugalCarrierMethodName = string.Empty;
                                    }
                                  
                                }

                            }

                            //移液后的  浓缩步骤
                            if (itemSample.MainStep >= 16 && itemSample.MainStep < 30 && !_globalStatus.IsStopped)
                            {
                                result = _capperFour.DoConcentrationTwo(itemSample, cts);//兽残浓缩
                                if (!result)
                                {
                                    throw new Exception("浓缩失败!");
                                }

                            }

                            if (itemSample.MainStep == 30)//完成
                            {
                                //样品和任务从列表移除
                                GlobalCache.Instance.ConcentrationList.Remove(itemSample);
                            }
                        }

                        //提取净化管 净化液   ==》有/无浓缩
                        else if (!TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractSupernate2) && itemSample.MainStep >= 8 )
                        {
                            //不占用移栽   占用拧盖3
                            if (!TechStatusHelper.BitIsOn(itemSample.TechParams,TechStatus.Concentration))
                            {
                                if (itemSample.MainStep >= 8 && itemSample.MainStep < 30 &&!_globalStatus.IsStopped)
                                {
                                    result = _capperFour.DoPipettingOne(itemSample, 2, cts); //直接提取样品液
                                    if (!result)
                                    {
                                        throw new Exception("提取样品液失败!");
                                    }
                                }
                                if (itemSample.MainStep == 30)//完成
                                {
                                    //样品和任务从列表移除
                                    GlobalCache.Instance.ConcentrationList.Remove(itemSample);
                                }
                            }
                            else
                            {
            
                                //从净化管移液到西林瓶
                                if (itemSample.MainStep >= 8 && itemSample.MainStep < 16 && !_globalStatus.IsStopped)
                                {
                                    //内部步骤 8 ~ 13
                                    result = _capperFour.DoPipettingOne(itemSample,1, cts);//农残移液浓缩
                                    if (!result)
                                    {
                                        throw new Exception("从净化管移取上清液失败!");
                                    }
                                    itemSample.MainStep = 16;
                                }

                                //开始浓缩
                                if (itemSample.MainStep >= 16 && itemSample.MainStep < 30 && !_globalStatus.IsStopped)
                                {
                                    result = _capperFour.DoConcentrationOne(itemSample, cts);
                                    if (!result)
                                    {
                                        throw new Exception("浓缩失败!");
                                    }
                                }

                                if (itemSample.MainStep == 30)//完成
                                {
                                    //样品和任务从列表移除
                                    GlobalCache.Instance.ConcentrationList.Remove(itemSample);
                                }
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    _globalStatus.PauseProgram();
                    _logger?.Warn(ex.Message); 
                    return;
                }
        
            });
            return _concentrationTask;   
        }

        /// <summary>
        /// 移液大管到净化管
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private async Task<bool> DoPipettingOne(Sample sample,CancellationTokenSource cts)
        {
            try
            {
                _logger?.Info($"{sample.Id}样品开始移上清液-Volume-{sample.TechParams.ExtractVolume}ml");
                bool result;
                //搬运净化管到拧盖3 ==> 拆盖 ==>搬运无盖试管到移栽
                if (sample.SubStep <= 10 && !_globalStatus.IsStopped)
                {
                    result = _capperThree.GetSampleFromCapperThreeToTransfer(sample, TransferMoveRightPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"{sample.Id}样品取净化管移液 失败");
                    }
                    sample.SubStep++;
                }

               s0: lock (_lockObj)   //锁定移栽占用部分
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CentrifugalCarrierMethodName))
                    {
                        if (GlobalCache.Instance.CentrifugalCarrierMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            goto s0;
                        }
                    }
                    GlobalCache.Instance.CentrifugalCarrierMethodName = MethodBase.GetCurrentMethod().Name;


                    //搬运到移栽
                    if (sample.SubStep == 11 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                        {
                            result = _carrierTwo.GetSampleFromCapperThreeToTransfer(sample, TransferMoveRightPutGetPos, cts);
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品移液管搬运到移栽 失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.SubStep++;
                        }
                    }

                    //移栽移动到移液位
                    if (sample.SubStep == 12 && !_globalStatus.IsStopped)
                    {
                        //移栽移动到移液位
                        result = TransferMoveLeftPipettorPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"移栽移动到左侧 移液 失败");
                        }
                        sample.SubStep++;
                    }

                    //搬运样品管到拧盖2 ==> 拆盖 
                    if (sample.SubStep == 13 && !_globalStatus.IsStopped)
                    {
                        result = _capperTwo.GetSampleFromMaterialToCapperTwo(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"搬运{sample.Id}样品离心管移液 失败");
                        }
                        sample.SubStep++;
                    }

                    //拧盖2 ==》搬运1开始移液
                    if (sample.SubStep == 14 && !_globalStatus.IsStopped)
                    {
                        result = _carrierOne.DoPipetting(sample, true, cts);
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品管取上清液 失败");
                        }
                        sample.SubStep++;
                    }

                    //搬运净化管到拧盖3
                    if (sample.SubStep == 15 && !_globalStatus.IsStopped)
                    {
                        result = _capperThree.GetSampleFromTransferToCapperThree(sample, TransferMoveRightPutGetPos, cts);
                        if (!result)
                        {
                            throw new Exception($"搬运{sample.Id}净化管到拧盖3 失败");
                        }
                        sample.SubStep++;
                        GlobalCache.Instance.CentrifugalCarrierMethodName = string.Empty;
                    }
                }

                //=============================================================================================//
                //拧盖2 回收试管
                Task<bool> res1 = null;
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperTwo))
                {
                    res1 = _capperTwo.GetSampleFromCapperTwoToMaterial(sample, cts); //包括搬运到试管架
                }
               
                
                //搬运空试管到拧盖3
                if (sample.SubStep >= 16 && !_globalStatus.IsStopped && sample.SubStep < 22)  //16 ~22
                {
                    var result1 = _capperThree.GetSampleFromCapperThreeToVibration(sample, cts);
                    if (!result1)
                    {
                        throw new Exception($"搬运{sample.Id}净化管到试管架失败");
                    }
                    sample.SubStep++;
                }

                //拧盖2 回收试管   判断完成
                if (res1 != null)
                {
                    if (!await res1)
                    {
                        throw new Exception($"从拧盖2取{sample.Id}样品管到试管架1 失败");
                    }
                }
                

                if (sample.SubStep == 23 && !_globalStatus.IsStopped)
                {
                    sample.SubStep = 0;
                    return true;
                }

                throw new Exception($"步骤错误step:{sample.SubStep}");
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped)
                {
                    _logger?.Info($"{sample.Id}样品开始移液-Volume-{sample.TechParams.ExtractVolume}ml 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

        }

        /// <summary>
        /// 净化管移液到大管 
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool DoPipettingTwo(Sample sample,CancellationTokenSource cts)
        {
            //净化管到大管
            s0: try
            {
                lock (_lockObj)
                {
                    if (!string.IsNullOrEmpty(GlobalCache.Instance.CentrifugalCarrierMethodName))
                    {
                        if (GlobalCache.Instance.CentrifugalCarrierMethodName != MethodBase.GetCurrentMethod().Name)
                        {
                            goto s0;
                        }
                    }
                    GlobalCache.Instance.CentrifugalCarrierMethodName = MethodBase.GetCurrentMethod().Name;


                    _logger?.Info($"{sample.Id}净化管开始移液-Volume-{sample.TechParams.ExtractVolume}ml");
                    bool result;

                    //搬运净化管到拧盖3 ==> 拆盖 ==>搬运无盖试管到移栽
                    if (sample.SubStep <= 11 && !_globalStatus.IsStopped)
                    {
                        result = _capperThree.GetSampleFromCapperThreeToTransferWithoutVibration(sample, TransferMoveRightPutGetPos, cts);
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品取净化管移液 失败");
                        }
                        sample.SubStep++;
                    }

                    //移栽移动到左侧移液位置
                    Task<bool> task1 = null;
                    if (sample.SubStep == 12 && !_globalStatus.IsStopped)
                    {
                        //移栽移动到左侧移液位置
                        task1 = TransferMoveLeftPipettorPos(cts);
                    }

                    //搬运萃取管到拧盖2 ==> 拆盖  等待移液
                    if (sample.SubStep == 12 && !_globalStatus.IsStopped)
                    {
                        result = _capperTwo.GetPolishFromMaterialToCapperTwo(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"搬运{sample.Id}样品萃取管到拧盖2 失败");
                        }
                        sample.SubStep++;
                    }

                    //移栽移动到左侧移液位置  检测是否到位
                    if (task1 != null)
                    {
                        if (!task1.GetAwaiter().GetResult())
                        {
                            throw new Exception("移动到左侧移液位失败!");
                        }
                    }

                    //==》搬运1开始移液
                    if (sample.SubStep == 13 && !_globalStatus.IsStopped)
                    {
                        result = _carrierOne.DoPipetting(sample, false, cts);
                        if (!result)
                        {
                            throw new Exception("提取净化液失败!");
                        }
                        sample.SubStep++;
                    }

                    //下料  ==》装盖2 ===》搬运到冰浴 加入到振荡列表？？
                    if (sample.SubStep == 14 && !_globalStatus.IsStopped)
                    {
                        result = _capperTwo.GetPolishFromCapperTwoToMaterial(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"从移栽搬运{sample.Id}萃取管到拧盖2 失败");
                        }
                        sample.SubStep++;
                    }

                    //搬运空试管到拧盖3
                    if (sample.SubStep == 15 && !_globalStatus.IsStopped)
                    {
                        var result1 = _capperThree.GetSampleFromTransferToCapperThree(sample, TransferMoveRightPutGetPos, cts);
                        if (!result1)
                        {
                            throw new Exception($"搬运{sample.Id}净化管到拧盖3 失败");
                        }
                        sample.SubStep++;
                    }

                    //拧盖3装盖  ==>  //拧盖3搬运下料
                    if (sample.SubStep >= 16 && sample.SubStep < 19 && !_globalStatus.IsStopped)
                    {
                        var result1 = _capperThree.GetSampleFromCapperThreeToMaterial(sample,cts);//到19步
                        if (!result1)
                        {
                            throw new Exception($"搬运{sample.Id}净化管到试管架 失败");
                        }
                        sample.SubStep = 0;
                        GlobalCache.Instance.CentrifugalCarrierMethodName = string.Empty;
                        return true;
                    }

                    throw new Exception("提取酯化上清液步骤非法!");
                }
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped)
                {
                    _logger?.Info($"{sample.Id}样品开始移液-Volume-{sample.TechParams.ExtractVolume}ml 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// 离心移栽移动到左侧上下料位(传入到搬运1)
        /// </summary>
        /// <param name="num"></param>
        /// <param name="isBig"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> TransferMoveLeftPutGetPos(ushort num, CancellationTokenSource cts)
        {
            try
            {
                double[] poss1 = GetLeftCoordinate(num, true);
                //X C轴回零
                s1: var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, poss1[0], _stepMoveVel, cts).ConfigureAwait(false);


                var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, poss1[1], _stepMoveVel, cts).ConfigureAwait(false);
                if (!await result1)
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
                    throw new Exception("移栽X轴移动到目标位失败!");
                }

                if (!await result2)
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
                    throw new Exception("移栽旋转轴移动到目标位失败!");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message);
                return false;
            }
        
        }

        /// <summary>
        /// 离心移栽移动左侧移液位，旋转角度固定
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> TransferMoveLeftPipettorPos(CancellationTokenSource cts)
        {
            try
            {
                double[] poss1 = GetLeftCoordinate(1, false); //小管的位置
                                                              //X C轴回零
                s1: var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, poss1[0], _stepMoveVel, cts).ConfigureAwait(false);

                var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, poss1[1], _stepMoveVel, cts).ConfigureAwait(false);
                if (!await result1)
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
                    throw new Exception("移栽X轴移动到目标位失败!");
                }

                if (!await result2)
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
                    throw new Exception("移栽旋转轴移动到目标位失败!");
                }
                return true;
            }
            catch (Exception ex)
            {         
                _logger.Warn(ex.Message);
                return false;
            }
           
        }

        /// <summary>
        /// 离心移栽移动到右侧上下料位（传入到搬运2）
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> TransferMoveRightPutGetPos(ushort num, CancellationTokenSource cts)
        {
            try
            {
                double[] poss1 = GetRightCoordinate(num, false);
                //X C轴回零
               s1: var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, poss1[0], _stepMoveVel, cts).ConfigureAwait(false);

                var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, poss1[1], _stepMoveVel, cts).ConfigureAwait(false);

                if (!await result1)
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
                    throw new Exception("移栽X轴移动到目标位失败!");
                }

                if (!await result2)
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
                    throw new Exception("移栽旋转轴移动到目标位失败!");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message);
                return false;
            }
         
        }

        /// <summary>
        /// 离心移栽移动右侧侧移液位，旋转角度固定
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> TransferMoveRightPipettorPos(CancellationTokenSource cts)
        {
            try
            {
                double[] poss1 = GetRightCoordinate(1, true);
                //X C轴回零
                s1: var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, poss1[0], _stepMoveVel, cts).ConfigureAwait(false);

                var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, poss1[1], _stepMoveVel, cts).ConfigureAwait(false);

                if (!await result1)
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
                    throw new Exception("移栽X轴移动到目标位失败!");
                }

                if (!await result2)
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
                    throw new Exception("移栽旋转轴移动到目标位失败!");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message);
                return false;
            }
   
        }

        /// <summary>
        /// 离心机取料
        /// </summary>
        /// <param name="func">离心机旋转</param>
        /// <returns></returns>
        protected async Task<bool> GetTubeAtCentrifugal(ushort pos, Func<ushort, Task<bool>> func, CancellationTokenSource cts, byte clawCloseByte = 255, byte clawOpenByte = 0)
        {
            _logger.Debug($"GetTubeAtCentrifugal-{pos},clawOpenByte-{clawOpenByte}");
            try
            {
                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece(false))
                {
                    //打开手爪到指定位置
                    var ret = await OpenClaw(clawOpenByte).ConfigureAwait(false);
                    if (!ret)
                    {
                        throw new Exception("手爪打开出错");
                    }
                }
                else //手爪有抓取物件
                {
                    return true;
                }

                //离心机转到指定位置
               s1: var result3 = func(pos).ConfigureAwait(false);

                //Y轴移动到离心机位
                Y_Cylinder_Put();

                //判断离心机旋转到指定位1 //打开离心机门
                if (!await result3)
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
                    throw new Exception("离心机转到指定位出错");
                }

                //Z轴下降到取料位
               s2: var result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZCentrifugalCoordinate(true), _sevorMoveVel, _globalStatus).ConfigureAwait(false);
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
                            goto s2;
                        }
                    }
                    throw new Exception("Z轴到取料位出错！");
                }

                //手爪夹紧
                result = await CloseClaw(clawCloseByte).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪夹紧出错");
                }

                _motion.P2pMoveWithCheckDone(_axisZ,0, _sevorMoveVel, _globalStatus).ConfigureAwait(false);

                //存在试管移除列表
                if (GlobalCache.Instance.TubeInCentrifugal.Contains(pos))
                {
                    GlobalCache.Instance.TubeInCentrifugal.Remove(pos);
                }
               

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 离心机放料
        /// </summary>
        /// <param name="clawOpenByte">手爪打开位置</param>
        /// <param name="pos">离心机旋转位</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> PutTubeAtCentrifugal(ushort pos, Func<ushort, Task<bool>> func, CancellationTokenSource cts, byte clawOpenByte = 0)
        {
            _logger.Debug($"PutTubeAtCentrifugal-{pos},clawOpenByte-{clawOpenByte}");
            try
            {
                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                //如果手爪打开 判定放料完成
                if (await _claw.ClawGetchStatus(_clawId) == 1)
                {
                    return true;

                }


                //离心机转到指定位置
               s1: var result3 = func(pos).ConfigureAwait(false);

                //Y轴移动到离心机位
                Y_Cylinder_Put();

                //判断离心机旋转到指定位1 //打开离心机门
                if (!await result3)
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
                    throw new Exception("离心机转到指定位出错");
                }

                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece(true))
                {
                    throw new Exception("手爪上无试管");
                }
                //Z轴下降到放料位
               s2: var result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZCentrifugalCoordinate(false), _sevorMoveVel, _globalStatus).ConfigureAwait(false);
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
                            goto s2;
                        }
                    }
                    throw new Exception("Z轴到放料位出错！");
                }

                GlobalCache.Instance.TubeInCentrifugal.Add(pos);

                //手爪打开
                result = await OpenClaw(clawOpenByte).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪打开出错！");
                }

                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 在移栽上取料
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="clawOpenByte"></param>
        /// <param name="cts"></param>
        /// <param name="clawCloseByte"></param>
        /// <returns></returns>
        protected async Task<bool> GetTubeAtTransfer(double[] pos, byte clawOpenByte, CancellationTokenSource cts, byte clawCloseByte = 255)
        {
            _logger.Debug($"GetTubeAtTransfer-{pos},clawOpenByte-{clawOpenByte}");
            try
            {
                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece(false))
                {
                    //打开手爪到指定位置
                    var ret = await OpenClaw(clawOpenByte).ConfigureAwait(false);
                    if (!ret)
                    {
                        throw new Exception("手爪打开出错");
                    }
                }
                else //手爪有抓取物件
                {
                    return true;
                }

                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                //C轴回零
                if (pos[1] == 0)
                {
                s0: var ret1 = await _stepMotion.GoHomeWithCheckDone(_axisC, cts).ConfigureAwait(false);
                    if (!ret1)
                    {
                        if (_globalStatus.IsPause)
                        {
                            while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                            {
                                Thread.Sleep(1000);
                            }
                            if (!_globalStatus.IsStopped)
                            {
                                goto s0;
                            }
                        }
                        throw new Exception("旋转轴回零失败!");
                    }
                }
          

            //X轴移动到位置1  //C轴旋转到大小试管位   //离心机旋转到指定位1 
            s1: var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, pos[0], _stepMoveVel, cts).ConfigureAwait(false);
                var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, pos[1], _stepMoveVel, cts).ConfigureAwait(false);
                //Y气缸移动到位
                Y_Cylinder_Get();

                //Z轴下降抓取第一支试管
                if (!await result1 || !await result2)
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
                    throw new Exception("离心移栽运动出错");
                }
               s2: var result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZTransferCoordinate(true), _sevorMoveVel, _globalStatus).ConfigureAwait(false);
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
                            goto s2;
                        }
                    }
                    throw new Exception("Z轴运动到取料位出错");
                }

                //手爪夹紧
                result = await CloseClaw(clawCloseByte).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪夹紧出错");
                }

                return true;

            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message);
                return false;
            }

        }

        /// <summary>
        /// 在移栽上放料放料
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="clawOpenByte"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> PutTubeAtTransfer(double[] pos, byte clawOpenByte, CancellationTokenSource cts)
        {
            _logger.Debug($"PutTubeAtTransfer-{pos},clawOpenByte-{clawOpenByte}");
            try
            {
                //如果手爪打开 判定放料完成
                if (await _claw.ClawGetchStatus(_clawId) == 1)
                {
                    return true;

                }

                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece(true))
                {
                    throw new Exception("手爪上无试管");
                }

                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                //C轴回零
                if (pos[1] == 0)
                {
                s0: var ret1 = await _stepMotion.GoHomeWithCheckDone(_axisC, cts).ConfigureAwait(false);
                    if (!ret1)
                    {
                        if (_globalStatus.IsPause)
                        {
                            while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                            {
                                Thread.Sleep(1000);
                            }
                            if (!_globalStatus.IsStopped)
                            {
                                goto s0;
                            }
                        }
                        throw new Exception("旋转轴回零失败!");
                    }
                }

            //X轴移动到位置1  //C轴旋转到大小试管位   //离心机旋转到指定位1 
            s1: var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, pos[0], _stepMoveVel, cts).ConfigureAwait(false);
                var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, pos[1], _stepMoveVel, cts).ConfigureAwait(false);
                //Y气缸移动到位
                Y_Cylinder_Get();

                //Z轴下降抓取第一支试管
                if (!await result1 || !await result2)
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
                    throw new Exception("离心移栽运动出错");
                }
               s2: var result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZTransferCoordinate(false), _sevorMoveVel, _globalStatus).ConfigureAwait(false);
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
                            goto s2;
                        }
                    }
                    throw new Exception("Z轴运动到放料位出错");
                }

                //手爪松开
                result = await OpenClaw(clawOpenByte).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪打开出错");
                }

                //判断Z轴是否在原点
                if (!await CheckAxisZInSafePos(cts))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message);
                return false;
            }
        }




        /// <summary>
        /// Y气缸取放料位
        /// </summary>
        protected void Y_Cylinder_Get(bool checkSensor = true)
        {
            _logger?.Debug($"Y_Cylinder_Get-{checkSensor}");
            var result = _io.WriteBit_DO(_y_Ctr, true);
            if (!result)
            {
                throw new Exception("Y_Cylinder_Get Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_y_WP);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new TimeoutException("Y气缸到取放料位超时");
                }
            } while (!result);
        }

        /// <summary>
        /// Y气缸离心机位
        /// </summary>
        protected void Y_Cylinder_Put(bool checkSensor = true)
        {
            _logger?.Debug($"Y_Cylinder_Put-{checkSensor}");
            var result = _io.WriteBit_DO(_y_Ctr, false);
            if (!result)
            {
                throw new Exception("Y_Cylinder_Put Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_y_HP);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new TimeoutException("Y气缸到离心机位超时");
                }
            } while (!result);
        }

        /// <summary>
        /// 检查Z轴是否在安全位置
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> CheckAxisZInSafePos(CancellationTokenSource cts)
        {
            if (!AxisIsInSafePos(_axisZ))
            {
                s1: var result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, _globalStatus).ConfigureAwait(false);
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
                    throw new Exception($"Z轴移动到安全位置失败!");
                }
            }
            return true;
        }

        /// <summary>
        /// 手爪是否抓取到物件
        /// </summary>
        /// <returns>true:手爪上有物件 false:手爪上无物件</returns>
        protected async Task<bool> ClawIsGetchPiece(bool isCheck,int timeout = 5)
        {
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(timeout);
        s1: var status = await _claw.ClawGetchStatus(_clawId).ConfigureAwait(false);
            if (isCheck)
            {
                var result = status == 2;
                if (!result)
                {
                    _logger?.Debug($"手爪上无物件 - {status}");
                }
                if (status != 2)
                {
                    if (DateTime.Now < end)
                    {
                        goto s1;
                    }
                }
            }
           
            return status == 2;
        }

        /// <summary>
        /// 打开手爪到指定位置
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> OpenClaw(byte openPos)
        {
            _logger?.Debug($"OpenClaw-{openPos}");
            var result = await _claw.SendCommand(_clawId, openPos, 255, 255).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception("手爪打开失败！");
            }
            int status = 0;
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(10);
            do
            {
                status = _claw.ClawGetchStatus(_clawId).GetAwaiter().GetResult();
                if (status == 3)
                {
                    return true;
                }
                if (status == 1)
                {
                    throw new Exception("手爪打开受阻！");
                }
                Thread.Sleep(300);
                if (DateTime.Now > end)
                {
                    throw new TimeoutException("离心机手爪动作超时！");
                }

            } while (status == 0);

            throw new Exception($"手爪状态错误 err{status}");
        }

        /// <summary>
        /// 关闭手爪
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> CloseClaw(byte closePos)
        {
            _logger?.Debug($"CloseClaw-{closePos}");
            var result = await _claw.SendCommand(_clawId, closePos, 255, 255).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception("手爪抓取物件失败！");
            }
            int status = 0;
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(10);
            do
            {
                status = _claw.ClawGetchStatus(_clawId).GetAwaiter().GetResult();
                if (status == 2)
                {
                    return true;
                }
                if (status == 3)
                {
                    throw new Exception("手爪未抓取到物料！");
                }
                Thread.Sleep(300);
                if (DateTime.Now > end)
                {
                    throw new TimeoutException("离心机手爪动作超时！");
                }

            } while (status == 0);
            throw new Exception($"手爪状态错误 err{status}");
        } 

        #endregion

        #region Private Methods


        /// <summary>
        /// 判断轴是否在安全位置
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        private bool AxisIsInSafePos(ushort axisNo)
        {
            var currentPos = _motion.GetCurrentPos(axisNo);
            return Math.Round(currentPos, 1) == 0;
        }

        /// <summary>
        /// 获取右侧坐标
        /// </summary>
        /// <param name="num">试管编号</param>
        /// <param name="isBig">是否是大管</param>
        /// <returns></returns>
        private double[] GetRightCoordinate(int num, bool isBig)
        {
            bool b = num % 2 == 0;
            if (isBig)
            {
                if (b)
                {
                    return new double[] { _posData.RightPos, _posData.CRightPos1 };
                }
                return new double[] { _posData.RightPos, _posData.CRightPos2 };
            }
            else
            {
                if (b)
                {
                    return new double[] { _posData.RightPos, _posData.CRightPos3 };
                }
                return new double[] { _posData.RightPos, _posData.CRightPos4 };
            }
        }

        /// <summary>
        /// 获取左侧坐标
        /// </summary>
        /// <param name="num">试管编号</param>
        /// <param name="isBig">是否是大管</param>
        /// <returns></returns>
        private double[] GetLeftCoordinate(int num, bool isBig)
        {
            bool b = num % 2 == 0;
            if (isBig)
            {
                if (!b)
                {
                    return new double[] { _posData.LeftPos, _posData.CLeftPutPos1 };
                }
                return new double[] { _posData.LeftPos, _posData.CLeftPutPos2 };
            }
            else
            {
                if (!b)
                {
                    return new double[] { _posData.LeftPos, _posData.CLeftPutPos3 };
                }
                return new double[] { _posData.LeftPos, _posData.CLeftPutPos4 };
            }
        }

        /// <summary>
        /// 获取中间取料点位
        /// </summary>
        /// <param name="num">试管编号</param>
        /// <param name="isBig">是否是大管</param>
        /// <returns></returns>
        private double[] GetCenterCoordinate(ushort num, bool isBig)
        {
            bool b = num % 2 == 0;
            if (isBig)
            {
                if (!b)
                {
                    return new double[] { _posData.XCentPos1, _posData.CCentPos1 };
                }
                return new double[] { _posData.XCentPos2, _posData.CCentPos1 };
            }
            else
            {
                if (!b)
                {
                    return new double[] { _posData.XCentPos1, _posData.CCentPos2 };
                }
                return new double[] { _posData.XCentPos2, _posData.CCentPos2 };
            }

        }

        /// <summary>
        /// 获取Z取试管坐标
        /// </summary>
        /// <param name="isGet">是否获取取料位/放料位</param>
        /// <returns></returns>
        private double GetZTransferCoordinate(bool isGet = true)
        {
            if (!isGet)
            {
                return _posData.ZGetPos - 3;
            }
            return _posData.ZGetPos;
        }

        /// <summary>
        /// 获取Z离心机放料坐标
        /// </summary>
        /// <param name="isGet">是否获取取料位/放料位</param>
        /// <returns></returns>
        private double GetZCentrifugalCoordinate(bool isGet = true)
        {
            if (!isGet)
            {
                return _posData.ZPutPos - 3;
            }
            return _posData.ZPutPos;
        }

        #endregion





    }
}
