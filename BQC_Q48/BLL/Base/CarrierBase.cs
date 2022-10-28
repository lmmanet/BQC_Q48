using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public abstract class CarrierBase
    {

        #region 

        /// <summary>
        /// 移液器是否有枪头
        /// </summary>
        protected bool _isGotNeedle;

        /// <summary>
        /// 移液器中有液体
        /// </summary>
        protected bool _syringHaveLiquid;

        /// <summary>
        /// 推枪头位置
        /// </summary>

        protected double _putOffNeedle;

        /// <summary>
        /// 允许Z轴下降
        /// </summary>
        protected bool _isCanZ_Down = true;

        #endregion

        #region Private Members

        protected readonly IEtherCATMotion _motion;

        protected readonly ILogger _logger;

        protected readonly IEPG26 _claw;

        protected readonly IGlobalStatus _globalStatus;

        #endregion

        #region Variable

        protected double _xyMoveVel = 500;
        protected double _zMoveDownVel = 200;
        protected double _zMoveUpVel = 600;
        protected double _zPipettorMoveUpVel = 100;
        protected double _absorbVel = 2;
        protected double _syringVel = 3;

        protected ushort _axisX;
        protected ushort _axisY;
        protected ushort _axisZ1;
        protected ushort _axisZ2;
        protected ushort _axisP;  //移液轴
        protected ushort _clawSlaveId;

        protected int _pipettingStep = 1;

        #endregion

        #region Constructors

        public CarrierBase(IEtherCATMotion motion, IEPG26 claw,IGlobalStatus globalStatus,ILogger logger)
        {
            this._motion = motion;
            this._claw = claw;
            this._logger = logger;
            this._globalStatus = globalStatus;
            _globalStatus.StopProgramEventArgs += StopMove;
            _globalStatus.PauseProgramEventArgs += StopMove;
        }


        public virtual CarrierInfo GetCarrierInfo()
        {
            var result = new CarrierInfo()
            {
                AxisX = _axisX,
                AxisY = _axisY,
                AxisZ1 = _axisZ1,
                AxisZ2 = _axisZ2,
                AxisF = _axisP,
                ClawId =_clawSlaveId,
                PutOffNeedle = _putOffNeedle,
                AbsorbVel = _absorbVel,
                SyringVel = _syringVel
            };
            return result;
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
                //禁用夹爪
                var result = await _claw.Disable(_clawSlaveId).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("禁用手爪失败！");
                }
                //使能夹爪
                result = await _claw.Enable(_clawSlaveId).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("使能手爪失败！");
                }

                //Z1轴使能
                if (!_motion.IsServeOn(_axisZ1))
                {
                    _motion.ServoOn(_axisZ1);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
                //Z2轴使能
                if (!_motion.IsServeOn(_axisZ2))
                {
                    _motion.ServoOn(_axisZ2);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
                //Z1 Z2回零
                var ret1 =  _motion.P2pMoveWithCheckDone(_axisZ1, 0, 50, _globalStatus).ConfigureAwait(false);
                var ret2 =  _motion.P2pMoveWithCheckDone(_axisZ2, 0, 50, _globalStatus).ConfigureAwait(false);

                if (!await ret1 || ! await ret2)
                {
                    throw new Exception("Z轴回零失败!");
                }

                //移液器轴回零
                if (!_motion.IsServeOn(_axisP))
                {
                    _motion.ServoOn(_axisP);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
                result = await SyringHome(gs).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("注射器回零失败！");
                }

                //Y轴回零
                if (!_motion.IsServeOn(_axisY))
                {
                    _motion.ServoOn(_axisY);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
                result = await _motion.P2pMoveWithCheckDone(_axisY, 0, 50, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Y轴回零失败!");
                }
                //X轴回零
                if (!_motion.IsServeOn(_axisX))
                {
                    _motion.ServoOn(_axisX);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
                result = await _motion.P2pMoveWithCheckDone(_axisX, 0, 50, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("X轴回零失败!");
                }
                //手爪打开
                result = await OpenClaw(80).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪打开失败！");
                }
                return true;
            }
            catch (Exception ex)
            {
                if (_globalStatus?.IsStopped == true)
                {
                    return false;
                }
                _logger?.Error(ex.Message);
                return false;
            }
         
          

        }

        /// <summary>
        /// 停止模块运动
        /// </summary>
        /// <returns></returns>
        public virtual bool StopMove()
        {
            _motion.StopMove(_axisX);
            _motion.StopMove(_axisY);
            _motion.StopMove(_axisZ1);
            _motion.StopMove(_axisZ2);
            _motion.StopMove(_axisP);
            return true;
        }

        /// <summary>
        /// 更写入的点位数据
        /// </summary>
        public abstract void UpdatePosData();

        #endregion

        #region Protected Methods

        /// <summary>
        /// 取试管
        /// </summary>
        /// <param name="pos">目标点位</param>
        /// <param name="clawOpenByte">手爪打开位</param>
        /// <param name="func">取料辅助动作</param>
        /// <param name="gs"></param>
        /// <param name="clawCloseByte">手爪关闭位置</param>
        /// <returns></returns>
        protected async Task<bool> GetTubeAsync(double[] pos,byte clawOpenByte, IGlobalStatus gs,byte clawCloseByte = 255)
        {
            try
            {
                _logger?.Debug($"GetTubeAsync-{clawOpenByte}-{clawCloseByte}");
                //判断手爪是否抓取物件 在指定打开位置
               s1: var result = await CarrierMoveToSafePos(pos, gs).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }
                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                    }
                    throw new Exception("XY移动到目标位置出错!");
                }

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

                //移动到取料点位
                s2: result = await CarrierMoveTo(pos, null,gs).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s2;
                        }
                    }
                    throw new Exception("移动到取料点出错");
                }

                //手爪夹紧
                int temp =0;
              attemp:  try
                {
                    while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                    {
                        Thread.Sleep(1000);
                    }
                    result = await CloseClaw(clawCloseByte).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception("手爪夹紧出错");
                    }
                }
                catch (Exception ex)
                {
                    temp++;
                    if (temp <3)
                    {
                        await OpenClaw(clawOpenByte).ConfigureAwait(false);
                        Thread.Sleep(500);
                        goto attemp;
                    }
                    _logger?.Error(ex.Message);
                    throw ex;
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
        /// 放试管
        /// </summary>
        /// <param name="clawOpenByte">手爪打开位</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected async Task<bool> PutTubeAsync(double[] pos, byte clawOpenByte, IGlobalStatus gs)
        {
            try
            {
                _logger?.Debug($"PutTubeAsync-{clawOpenByte}");
                //如果手爪打开 判定放料完成
                if (await _claw.ClawGetchStatus(_clawSlaveId) == 3)
                {
                    return true;
                }

                //移动到放料点位
                s1: var result = await CarrierMoveTo(pos, ClawIsGetchPiece, gs).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                    }
                    throw new Exception("移动到放料位出错");
                }
             
                //手爪松开
                result = await OpenClaw(clawOpenByte).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪打开出错");
                }

                //移动到安全位置
                //判断Z轴是否在原点
                await CheckAxisZInSafePos(gs).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped ||_globalStatus.IsPause)
                {
                    return false;
                }

                _logger.Warn(ex.Message);
                return false;
            }
           

        }

        /// <summary>
        /// 取枪头
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        protected async Task<bool> GetNeedleAsync(double[] pos, IGlobalStatus gs)
        {
            try
            {
                _logger?.Debug($"GetNeedleAsync");
                //判断移液器是否有枪头
                if (_isGotNeedle)
                {
                    return true;
                }

                //判断Z1 Z2是否在原位
                await CheckAxisZInSafePos(gs).ConfigureAwait(false);
              
                //XY轴移动到目标位置
                ushort[] axes = new ushort[2] { _axisX, _axisY };
                double[] posArray = new double[2] { pos[0], pos[1] };
                s1: var result = await _motion.InterPolation_2D_lineWithCheckDone(axes, posArray, _xyMoveVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                    }
                    throw new Exception($"GetNeedleAsync XY移动到指定位置失败");
                }
                //移液器移动到0位
                s2: result = await _motion.GohomeWithCheckDone(_axisP, 21, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s2;
                        }
                    }
                    throw new Exception($"GetNeedleAsync 移液器回零失败");
                }
              
                //Z2轴下降到指定位置
                s3: result = await _motion.P2pMoveWithCheckDone(_axisZ2, pos[2], _zMoveDownVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s3;
                        }
                    }
                    throw new Exception($"GetNeedleAsync Z2轴移动到指定位置失败");
                }

                _isGotNeedle = true;
                Thread.Sleep(1000);

                //Z2轴上升到安全位置
                s4: result = await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _zMoveUpVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s4;
                        }
                    }
                    throw new Exception($"GetNeedleAsync Z2轴移动到安全位置失败");
                }

                return result;
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped ||_globalStatus.IsPause)
                {
                    return false;
                }

                _logger.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 放枪头
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected async Task<bool> PutNeedleAsync(double[] pos, IGlobalStatus gs,double putOffset = 80)
        {
            try
            {
                _logger?.Debug($"PutNeedleAsync");
                //判断移液器是否有枪头
                if (!_isGotNeedle)
                {
                    return true;
                }

                //判断Z1 Z2是否在原位
                await CheckAxisZInSafePos(gs).ConfigureAwait(false);

                //XY轴移动到目标位置
                ushort[] axes = new ushort[2] { _axisX, _axisY };
                double[] posArray = new double[2] { pos[0], pos[1] };
               s1: var result = await _motion.InterPolation_2D_lineWithCheckDone(axes, posArray, _xyMoveVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                        else
                        {
                            throw new Exception($"PutNeedleAsync XY移动到指定位置失败");
                        }

                    }
                    else
                    {
                        throw new Exception($"PutNeedleAsync XY移动到指定位置失败");
                    }
                  
                }
                
                //Z2轴下降到指定位置
               s2: result = await _motion.P2pMoveWithCheckDone(_axisZ2, pos[2] - putOffset, _zMoveDownVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s2;
                        }
                        else
                        {
                            throw new Exception($"PutNeedleAsync Z2轴移动到指定位置失败");
                        }

                    }
                    else
                    {
                       throw new Exception($"PutNeedleAsync Z2轴移动到指定位置失败");
                    }
                   
                }
            
                //移液器移动到推枪头位
               s3: result = await _motion.P2pMoveWithCheckDone(_axisP, _putOffNeedle, _absorbVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s3;
                        }
                        else
                        {
                            throw new Exception("移液器推枪头失败");
                        }

                    }
                    else
                    {
                        throw new Exception("移液器推枪头失败");
                    }
                   
                }

                _isGotNeedle = false;
                Thread.Sleep(100);

                //移液器回零
                SyringHome(gs);
                while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                {
                    Thread.Sleep(2000);
                }
                //Z2轴上升到安全位置
               s4: result = await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _zMoveUpVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s4;
                        }
                        else
                        {
                            throw new Exception($"GetNeedleAsync Z2轴移动到安全位置失败");
                        }

                    }
                    else
                    {
                        throw new Exception($"GetNeedleAsync Z2轴移动到安全位置失败");
                    }

                   
                }

                return result;
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped ||_globalStatus.IsPause)
                {
                    return false;
                }

                _logger.Warn(ex.Message);
               return false;
            }
           
        }

        /// <summary>
        /// 开始移液器
        /// </summary>
        /// <param name="sourcePos">移液取液位</param>
        /// <param name="targetPos">移液吐液位</param>
        /// <param name="volume"></param>
        /// <param name="deep">移液液位深度</param>
        /// <param name="airColumn">吸取空气柱</param>
        /// <param name="safePos">避空位</param>
        /// <param name="isNeedGoSafePosObsorb">吸液前是否需要移动到避空位</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected virtual async Task<bool> DoPipettingAsync(double[] sourcePos, double[] targetPos, double volume, double deep, double airColumn,double[] safePos,bool isNeedGoSafePosObsorb, IGlobalStatus gs)
        {
            try
            {
                _logger?.Debug($"DoPipettingAsync-{volume}");
                ushort[] axes = new ushort[2] { _axisX, _axisY };
                double[] sourcePosArray = new double[2] { sourcePos[0], sourcePos[1] };
                double[] targetPosArray = new double[2] { targetPos[0], targetPos[1] };

                //判断Z1 Z2是否在原位
                await CheckAxisZInSafePos(gs).ConfigureAwait(false);

                //判断是否已经吸取溶液
                if (_syringHaveLiquid)
                {
                    goto syringOut;
                }

                #region 去吸取溶液

                //避位
                if (safePos != null && isNeedGoSafePosObsorb)
                {
                s0: var ret = await _motion.InterPolation_2D_lineWithCheckDone(axes, safePos, _xyMoveVel, _globalStatus).ConfigureAwait(false);
                    if (!ret)
                    {
                        if (_globalStatus.IsPause)
                        {
                            while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                            {
                                Thread.Sleep(1000);
                            }

                            if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                            {
                                goto s0;
                            }
                            else
                            {
                                throw new Exception($"DoPipettingAsync XY移动到移液规避位置失败");
                            }

                        }
                        else
                        {
                            throw new Exception($"DoPipettingAsync XY移动到移液规避位置失败");
                        }

                    }

                }

            //XY轴移动到取液位置
            s1: var result = await _motion.InterPolation_2D_lineWithCheckDone(axes, sourcePosArray, _xyMoveVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                        else
                        {
                            throw new Exception($"DoPipettingAsync XY移动到取液位置失败");
                        }

                    }
                    else
                    {
                        throw new Exception($"DoPipettingAsync XY移动到取液位置失败");
                    }
                   
                }

                //Z2轴下降到取位置
                s2: result = await _motion.P2pMoveWithCheckDone(_axisZ2, sourcePos[2], _zMoveDownVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s2;
                        }
                        else
                        {
                            throw new Exception($"DoPipettingAsync Z2轴移动到取液位置失败");
                        }

                    }
                    else
                    {
                        throw new Exception($"DoPipettingAsync Z2轴移动到取液位置失败");
                    }
                   
                }

                //开始吸液
                ushort[] axes2 = new ushort[2] { _axisZ2, _axisP };
                double[] targetPosArray2 = new double[2] { sourcePos[2] +deep, volume };

            s3: result = await _motion.InterPolation_2D_lineWithCheckDone(axes2, targetPosArray2, _absorbVel*50, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s3;
                        }
                        else
                        {
                            throw new Exception($"DoPipettingAsync 移液器吸液失败");
                        }

                    }
                    else
                    {
                        throw new Exception($"DoPipettingAsync 移液器吸液失败");
                    }
                   
                }

                Thread.Sleep(500);
                _syringHaveLiquid = true;

                //Z2轴上升到安全位置
               s4: result = await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _zPipettorMoveUpVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s4;
                        }
                        else
                        {
                            throw new Exception($"DoPipettingAsync Z2轴移动到安全位置失败");
                        }

                    }
                    else
                    {
                        throw new Exception($"DoPipettingAsync Z2轴移动到安全位置失败");
                    }
                 
                }

                //吸取空气柱
                _motion.P2pMoveWithCheckDone(_axisP, volume + airColumn, _syringVel, _globalStatus).ConfigureAwait(false);
            //s5: result = await _motion.P2pMoveWithCheckDone(_axisP, volume + airColumn, _syringVel, _globalStatus).ConfigureAwait(false);
            //if (!result)
            //{
            //    if (_globalStatus.IsPause)
            //    {
            //        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
            //        {
            //            Thread.Sleep(1000);
            //        }

            //        if (!_globalStatus.IsStopped)
            //        {
            //            goto s5;
            //        }
            //        else
            //        {
            //            throw new Exception($"DoPipettingAsync 移液器吸空气柱失败");
            //        }

            //    }
            //    else
            //    {
            //        throw new Exception($"DoPipettingAsync 移液器吸空气柱失败");
            //    }

            //}
            #endregion


            syringOut:

                #region 去目标位置吐液

                //避位
                if (safePos != null)
                {
                   s6: result = await _motion.InterPolation_2D_lineWithCheckDone(axes, safePos, _xyMoveVel, _globalStatus).ConfigureAwait(false);
                    if (!result)
                    {
                        if (_globalStatus.IsPause)
                        {
                            while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                            {
                                Thread.Sleep(1000);
                            }

                            if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                            {
                                goto s6;
                            }
                            else
                            {
                                throw new Exception($"DoPipettingAsync XY移动到移液规避位置失败");
                            }

                        }
                        else
                        {
                            throw new Exception($"DoPipettingAsync XY移动到移液规避位置失败");
                        }
                       
                    }

                }

                //XY轴移动到吐液位置
               s7: result = await _motion.InterPolation_2D_lineWithCheckDone(axes, targetPosArray, _xyMoveVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s7;
                        }
                        else
                        {
                            throw new Exception($"DoPipettingAsync XY移动到吐液位置失败");
                        }

                    }
                    else
                    {
                        throw new Exception($"DoPipettingAsync XY移动到吐液位置失败");
                    }
                 
                }
             

                //Z2轴下降到取位置
               s8: result = await _motion.P2pMoveWithCheckDone(_axisZ2, targetPos[2], _zMoveDownVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s8;
                        }
                        else
                        {
                            throw new Exception($"DoPipettingAsync Z2轴移动到吐液位置失败");
                        }

                    }
                    else
                    {
                        throw new Exception($"DoPipettingAsync Z2轴移动到吐液位置失败");
                    }
                   
                }

                //开始吐液
               s9: result = await _motion.P2pMoveWithCheckDone(_axisP, 0, _syringVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s9;
                        }
                        else
                        {
                            throw new Exception($"DoPipettingAsync 移液器吐液失败");
                        }

                    }
                    else
                    {
                        throw new Exception($"DoPipettingAsync 移液器吐液失败");
                    }
                  
                }
                //吸取空气 
               s10: result = await _motion.P2pMoveWithCheckDone(_axisP, 0.5, _syringVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s10;
                        }
                        else
                        {
                            throw new Exception($"DoPipettingAsync 移液器吸空气失败");
                        }

                    }
                    else
                    {
                        throw new Exception($"DoPipettingAsync 移液器吸空气失败");
                    }
                   
                }

                //再吐液
               s11: result = await _motion.P2pMoveWithCheckDone(_axisP, 0, _syringVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s11;
                        }
                        else
                        {
                            throw new Exception($"DoPipettingAsync 移液器吐液失败");
                        }

                    }
                    else
                    {
                        throw new Exception($"DoPipettingAsync 移液器吐液失败");
                    }
                   
                }
                _syringHaveLiquid = false;

                #endregion

                ////Z2轴上升到安全位置   报错
                //_motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, gs);

                return result;
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped ||_globalStatus.IsPause)
                {
                    return false;
                }

                _logger.Warn(ex.Message);
                return false;
            }
          
        }

        /// <summary>
        /// 检查Z轴是否在安全位置
        /// </summary>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected async Task<bool> CheckAxisZInSafePos(IGlobalStatus gs)
        {
            _logger?.Debug($"CheckAxisZInSafePos");
            if (!AxisIsInSafePos(_axisZ1))
            {
               s1: var result = await _motion.P2pMoveWithCheckDone(_axisZ1, 0, _zMoveUpVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                        else
                        {
                            throw new Exception($"Z1轴移动到安全位置失败!");
                        }
                    }
                    else
                    {
                        throw new Exception($"Z1轴移动到安全位置失败!");
                    }
                }

            }

            if (!AxisIsInSafePos(_axisZ2))
            {
                s2: var result = await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _zMoveUpVel, _globalStatus).ConfigureAwait(false);
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
                        else
                        {
                            throw new Exception($"Z2轴移动到安全位置失败!");
                        }
                    }
                    else
                    {
                        throw new Exception($"Z2轴移动到安全位置失败!");
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 打开手爪到指定位置
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> OpenClaw(byte openPos)
        {
            _logger?.Debug($"OpenClaw-{openPos}");
            var result = await _claw.SendCommand(_clawSlaveId, openPos, 255, 255).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception("手爪打开失败！");
            }
            int status = 0;
            Thread.Sleep(500);
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(10);
            do
            {
                status = _claw.ClawGetchStatus(_clawSlaveId).GetAwaiter().GetResult();
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
                    throw new TimeoutException("提取手爪动作超时！");
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
            int attemp = 0;
            _logger?.Debug($"CloseClaw-{closePos}-attemp-{attemp}");
           s1: var result = await _claw.SendCommand(_clawSlaveId, closePos, 255, 255).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Error("_claw.SendCommand err!");
                throw new Exception("手爪抓取物件失败！");
            }
            int status = 0;
            Thread.Sleep(500);
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(10);
            do
            {
                status = _claw.ClawGetchStatus(_clawSlaveId).GetAwaiter().GetResult();
                if (status == 2)
                {
                    return true;
                }
                if (status == 3)
                {
                    if (attemp<5)
                    {
                        attemp++;
                        goto s1;
                    }
                    throw new Exception("手爪未抓取到物料！");
                }
                Thread.Sleep(300);
                if (DateTime.Now > end)
                {
                    throw new TimeoutException("手爪动作超时！");
                }

            } while (status == 0);
            throw new Exception($"手爪状态错误 err{status}");
        }

        /// <summary>
        /// 移液器回零
        /// </summary>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected async Task<bool> SyringHome(IGlobalStatus gs)
        {
            _logger?.Debug($"SyringHome");
            var result = await _motion.GohomeWithCheckDone(_axisP, 21, _globalStatus).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception($"PutNeedleAsync 移液器回零失败");
            }
            return true;
        }

        /// <summary>
        /// 搬运XYZ移动到坐标点
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected async Task<bool> CarrierMoveTo(double[] coordinate, Func<bool,int,Task<bool>> func,IGlobalStatus gs)
        {
            _logger?.Debug($"CarrierMoveTo");
            try
            {
                double x = coordinate[0];
                double y = coordinate[1];
                double z = coordinate[2];
                ushort[] axisXY = new ushort[2] { _axisX, _axisY };
                double[] targetArr = new double[2] { x, y };

                //判断Z轴是否在原点
                await CheckAxisZInSafePos(gs).ConfigureAwait(false);

                //XY轴移动到取料位置
               s1: var ret = await _motion.InterPolation_2D_lineWithCheckDone(axisXY, targetArr, _xyMoveVel, _globalStatus).ConfigureAwait(false);
                if (!ret)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                        else
                        {
                            throw new Exception("XY轴移动到指定位出错");
                        }

                    }
                    else
                    {
                        throw new Exception("XY轴移动到指定位出错");
                    }
              
                }

                if (func != null)
                {
                    var funResult = await func.Invoke(true,5).ConfigureAwait(false);
                    if (!funResult)
                    {
                        throw new Exception("手爪上无试管");
                    }
                }

                //Z轴下降到取料位置
               s2: var result = await _motion.P2pMoveWithCheckDone(_axisZ1, z, _zMoveDownVel, _globalStatus).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s2;
                        }
                        else
                        {
                            throw new Exception("Z1轴下降到指定位置失败");
                        }

                    }
                    else
                    {
                        throw new Exception("Z1轴下降到指定位置失败");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped ||_globalStatus.IsPause)
                {
                    return false;
                }

                _logger.Warn(ex.Message);
                return false;
            }
          
        }

        /// <summary>
        /// 搬运移动到安全位
        /// </summary>
        /// <param name="gs"></param>
        /// <returns></returns>
        protected async Task<bool> CarrierMoveToSafePos(double[] coordinate, IGlobalStatus gs)
        {
            try
            {
                _logger?.Debug($"CarrierMoveToSafePos");
                double x = coordinate[0];
                double y = coordinate[1];
                ushort[] axisXY = new ushort[2] { _axisX, _axisY };
                double[] targetArr = new double[2] { x, y };

                //判断Z轴是否在原点
                await CheckAxisZInSafePos(gs).ConfigureAwait(false);

                //XY轴移动到取料位置
              s1:  var ret = await _motion.InterPolation_2D_lineWithCheckDone(axisXY, targetArr, _xyMoveVel, _globalStatus).ConfigureAwait(false);
                if (!ret)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                        else
                        {
                            throw new Exception("移动到安全位置失败!");
                        }

                    }
                    else
                    {
                        throw new Exception("移动到安全位置失败!");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped ||_globalStatus.IsPause)
                {
                    return false;
                }

                _logger.Warn(ex.Message);
                return false;
            }
           
        }

        /// <summary>
        /// 计算坐标
        /// </summary>
        /// <param name="tubeId">试管Id 从1开始</param>
        /// <param name="rows">总行数</param>
        /// <param name="columns">总列数</param>
        /// <param name="xOffset">X偏移量（正负）</param>
        /// <param name="yOffset">Y偏移量（正负）</param>
        /// <param name="refCoordinate">初始坐标</param>
        /// <returns></returns>
        protected double[] GetCoordinate(int tubeId,int rows,int columns,double xOffset,double yOffset,double[] refCoordinate)
        {
            double[] resultDouble = new double[refCoordinate.Length];
            //获取参考点坐标
            Array.Copy(refCoordinate, resultDouble, refCoordinate.Length);
          
            //计算偏移
            int id = (tubeId - 1) % (rows * columns);
            int xIndex = id / rows;
            int yIndex = id % rows;
            //计算结果
            resultDouble[0] += xOffset * xIndex;      //X计算偏移
            resultDouble[1] += yOffset * yIndex;   //Y计算偏移

            return resultDouble;
        }

        /// <summary>
        /// 判断轴是否在安全位置
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        protected bool AxisIsInSafePos(ushort axisNo)
        {
            var currentPos = _motion.GetCurrentPos(axisNo);
            return Math.Round(currentPos, 1) == 0;
        }

        /// <summary>
        /// 手爪是否抓取到物件
        /// </summary>
        /// <returns>true:手爪上有物件 false:手爪上无物件</returns>
        protected async Task<bool> ClawIsGetchPiece(bool isCheck,int timeout = 5)
        {
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(timeout);
        s1: var status = await _claw.ClawGetchStatus(_clawSlaveId).ConfigureAwait(false);
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
        /// 移液器避位
        /// </summary>
        /// <returns></returns>
        protected abstract double[] GetPipettingSafePos();

        #endregion

   
    }
}
