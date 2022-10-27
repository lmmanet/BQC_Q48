using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.Common;
using Q_Platform.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public abstract class CapperBase
    {

        #region Protected Members

        protected readonly IIoDevice _io;

        protected readonly ILogger _logger;

        protected readonly ILS_Motion _motion;

        protected readonly IGlobalStatus _globalStatus;

        protected readonly ICapperPosDataAccess _dataAccess;

        #endregion

        #region Variable

        protected double _yMoveVel = 500;
        protected double _zMoveVel = 300;
        protected double _cMoveVel = 80;

        protected ushort _axisY;
        protected ushort _axisZ;
        protected ushort _axisC1;
        protected ushort _axisC2;
        protected ushort _claw;
        protected ushort _holding;
        protected ushort _holdingOpenSensor;
        protected ushort _holdingCloseSensor;
        protected ushort _capperSensor; //光纤检测开关

        protected double _xOffset;

        //protected bool _haveCapper = false;  //手爪上有盖

        protected CapperPosData _posData;

        #endregion

        #region Constructors

        public CapperBase(IIoDevice io,ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess, ILogger logger)
        {
            this._io = io;
            this._motion = motion;
            this._dataAccess = dataAccess;
            this._logger = logger;
            this._globalStatus = globalStatus;
            _globalStatus.StopProgramEventArgs += StopMove;
            _globalStatus.PauseProgramEventArgs += StopMove;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="gs"></param>
        /// <returns></returns>
        public virtual async Task<bool> GoHome(IGlobalStatus gs)
        {
            try
            {
                //if (gs?.IsCancellationRequested == true)
                //{
                //    throw new TaskCanceledException($"触发停止 gs:{gs.IsCancellationRequested}");
                //}
                _logger?.Info("拧盖回零");
                //手爪打开
                _io.WriteBit_DO(_claw, true);

                //使能Z轴
                await _motion.ServoOn(_axisZ).ConfigureAwait(false);
                //Z轴回零
                var result = await _motion.DM2C_GoHomeWithCheckDone(_axisZ, gs).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //使能Y轴
                await _motion.ServoOn(_axisY).ConfigureAwait(false);
                //Y轴回零
                result = await _motion.GoHomeWithCheckDone(_axisY, gs).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //使能C1轴
                await _motion.ServoOn(_axisC1).ConfigureAwait(false);
                //使能C2轴
                await _motion.ServoOn(_axisC2).ConfigureAwait(false);


                //复位抱夹
                OpenHolding();

                //Y轴移动到上下料位置
                result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _yMoveVel, gs).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
                //回零完成
                return true;
            }
            catch (Exception ex)
            {
                //if (gs?.IsCancellationRequested == true)
                //{
                //    _logger?.Info("拧盖回零 停止");
                //    return false;
                //}
                _logger?.Warn($"拧盖回零 err:{ex.Message}");
                return false;
            }

        }

        /// <summary>
        /// 停止拧盖动作
        /// </summary>
        /// <returns></returns>
        public virtual bool StopMove()
        {
            _motion.StopMove(_axisY);
            _motion.StopMove(_axisZ);
            _motion.StopMove(_axisC1);
            _motion.StopMove(_axisC2);
            return true;
        }

        /// <summary>
        /// 更写入的点位数据
        /// </summary>
        public abstract void UpdatePosData();

        /// <summary>
        /// 获取拧盖信息
        /// </summary>
        /// <returns></returns>
        public virtual CapperInfo GetCapperInfo()
        {
            return new CapperInfo()
            {
                AxisC1 = _axisC1,
                AxisC2 = _axisC2,
                AxisY = _axisY,
                AxisZ = _axisZ,
                ClawCtl = _claw,
                HoldCtl = _holding,
                Hold_HP = _holdingOpenSensor,
                Hold_WP = _holdingCloseSensor
            };
        }


        /// <summary>
        /// 装盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public virtual async Task<bool> CapperOnAsync(Sample sample, IGlobalStatus gs)
        {
            //判断样品是否有盖

            s1: var result = await CapperOn(80, 40, gs).ConfigureAwait(false);

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
        public virtual async Task<bool> CapperOffAsync(Sample sample, IGlobalStatus gs)
        {
            //判断样品是否有盖
            s1: var result = await CapperOff(gs).ConfigureAwait(false);

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

        #endregion

        #region Protected Methods

        /// <summary>
        /// 移动到上下料位
        /// </summary>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected async Task<bool> MovePutGetPos(IGlobalStatus gs)
        {
            try
            {
                //复位抱夹
                OpenHolding();

                //Y轴移动到上下料位置
              s1: var result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _yMoveVel, gs).ConfigureAwait(false);
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
              
                    throw new Exception("Y轴移动到上下料位失败!");
                }
                return true;
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger.Warn(ex.Message);
                return false;
            }
          
        }

        /// <summary>
        /// 装盖
        /// </summary>
        /// <param name="torque"></param>
        /// <returns></returns>
        protected async Task<bool> CapperOn(int torque,int timeout,IGlobalStatus gs)
        {
            try
            {
               // _logger?.Debug($"CapperOn-{torque}-{timeout} haveCapper{_haveCapper}");
                _logger?.Debug($"CapperOn-{torque}-{timeout}");

                //判断手爪是否有盖
                //if (!_haveCapper)
                //{
                //    return true;
                //}

                //抱夹夹紧
                CloseHolding();

                //Y轴移动到拧盖位
                s1: var result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.CapperPos, _yMoveVel, gs).ConfigureAwait(false);
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
                    throw new Exception("Y轴运动出错！");
                }

                //Z轴下降到位
               s2: result = await _motion.P2pMoveWithCheckDone(_axisZ, _posData.CapperPos_Z, _yMoveVel, gs).ConfigureAwait(false);
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
                    throw new Exception("Z轴运动出错！");
                }

                //开始拧紧
               s3: var result1 =  _motion.TorqueMoveWithCheckDone(_axisC1, _cMoveVel, torque, timeout,gs).ConfigureAwait(false);
                var result2 = _motion.TorqueMoveWithCheckDone(_axisC2, _cMoveVel, torque, timeout, gs).ConfigureAwait(false);
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
                            goto s3;
                        }
                    }
                    //拧盖失败异常
                    throw new Exception("拧盖失败！");
                }

                //手爪松开
                OpenClaw();
                //Z轴上升到位
               s4: result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, _zMoveVel, gs).ConfigureAwait(false);
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
                            goto s4;
                        }
                    }
                    throw new Exception("Z轴运动出错！");
                }

                //检测有盖


                //Y轴移动到上下料位
               s5: result = await MovePutGetPos(gs).ConfigureAwait(false);
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
                            goto s5;
                        }
                    }
                    throw new Exception("Y轴运动出错！");
                }

                //_haveCapper = false;
                return true;

            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 拆盖
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> CapperOff(IGlobalStatus gs,double offset = -0.75)
        {
            try
            {
                //_logger?.Debug($"CapperOff haveCapper{_haveCapper}");
                _logger?.Debug($"CapperOff haveCapper");
                //判断手爪是否有盖
                //if (_haveCapper)
                //{
                //    return true;
                //}

                //抱夹夹紧
                CloseHolding();

                //Y轴移动到位
               s1: var result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.CapperPos, _yMoveVel, gs).ConfigureAwait(false);
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
                    throw new Exception("Y轴运动出错！");
                }

                //手爪打开
                OpenClaw();
                //Z轴下降到位
                s2: result = await _motion.P2pMoveWithCheckDone(_axisZ, _posData.CapperPos_Z, _yMoveVel, gs).ConfigureAwait(false);
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
                    throw new Exception("Z轴运动出错！");
                }

                //手爪夹紧
                CloseClaw();
                //开始松开
               s3: var result1 =  _motion.RelativeMoveWithCheckDone(_axisC1, offset, 50, gs).ConfigureAwait(false);
                var result2 =  _motion.RelativeMoveWithCheckDone(_axisC2, offset, 50, gs).ConfigureAwait(false);

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
                            goto s3;
                        }
                    }
                    throw new Exception("拆盖失败！");
                }

                //Z轴上升到位
                s4: result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, _yMoveVel, gs).ConfigureAwait(false);
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
                            goto s4;
                        }
                    }
                    throw new Exception("Z轴运动出错！");
                }

                //检测是否拆盖成功
                result = CheckUnCapper(gs);
                if (!result)
                {
                    return false;
                }

            //Y轴移动到上下料位
            s5: result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _yMoveVel, gs).ConfigureAwait(false);
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
                            goto s5;
                        }
                    }
                    throw new Exception("Y轴运动出错！");
                }
                //抱夹松开
                OpenHolding();
               // _haveCapper = true;
                
                return true;

            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 关闭抱夹
        /// </summary>
        protected virtual void CloseHolding(bool checkSensor = false)
        {
            _logger?.Debug($"OpenHolding-{checkSensor}");
            //抱夹夹紧
            var result =  _io.WriteBit_DO(_holding, true);
            if (!result)
            {
                throw new Exception("CloseHolding Err!");
            }
            if (!checkSensor)
            {
                Thread.Sleep(500);
                return ;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_holdingCloseSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new TimeoutException("CloseHolding 超时");
                }
            } while (!result);

        }

        /// <summary>
        /// 打开抱夹
        /// </summary>
        protected virtual void OpenHolding(bool checkSensor = false)
        {
            _logger?.Debug($"OpenHolding-{checkSensor}");
            //抱夹释放
            var result = _io.WriteBit_DO(_holding, false);
            if (!result)
            {
                throw new Exception("OpenHolding Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }

            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_holdingOpenSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new TimeoutException("OpenHolding超时");
                }
            } while (!result);

        }

        /// <summary>
        /// 关闭手爪
        /// </summary>
        protected virtual void CloseClaw()
        {
            _logger?.Debug($"CloseClaw");
            //手爪夹紧
            var result = _io.WriteBit_DO(_claw, false);
            Thread.Sleep(500);
            if (!result)
            {
                throw new Exception("CloseClaw Err!");
            }
        }

        /// <summary>
        /// 打开手爪
        /// </summary>
        protected virtual void OpenClaw()
        {
            _logger?.Debug($"CloseClaw");
            //手爪松开
            var result = _io.WriteBit_DO(_claw, true);
            Thread.Sleep(500);
            if (!result)
            {
                throw new Exception("OpenClaw Err!");
            }
        }

        /// <summary>
        /// 检查有盖
        /// </summary>
        /// <returns></returns>
        protected bool CheckHaveCapper(IGlobalStatus gs)
        {
            var result = _motion.P2pMoveWithCheckDone(_axisY, GetCapperSensorCoordinate(), _yMoveVel, gs).GetAwaiter().GetResult();
            if (!result)
            {
                throw new Exception("Y轴运动出错！");
            }
            if (_io.ReadBit_DI(_capperSensor))
            {
                return true;
            }
            throw new Exception("检测无盖！");
        }

        /// <summary>
        /// 检查无盖
        /// </summary>
        /// <returns></returns>
        protected bool CheckUnCapper(IGlobalStatus gs)
        {
            var result = _motion.P2pMoveWithCheckDone(_axisY, GetCapperSensorCoordinate(), _yMoveVel, gs).GetAwaiter().GetResult();
            if (!result)
            {
                throw new Exception("Y轴运动出错！");
            }
            if (!_io.ReadBit_DI(_capperSensor))
            {
                return true;
            }
            throw new Exception("检测有盖！");
        }

        /// <summary>
        /// 获取检测盖子传感器坐标
        /// </summary>
        /// <returns></returns>
        protected double GetCapperSensorCoordinate()
        {
            return _posData.CapperPos + 46;
        }


        #endregion

        



    }
}
