using BQJX.Common.Interface;
using BQJX.Core.Interface;
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


        #endregion

        #region Private Members

        protected readonly IEtherCATMotion _motion;

        protected readonly IIoDevice _io;

        protected readonly ILogger _logger;

        protected readonly IEPG26 _claw;

        protected readonly IGlobalStatus _globalStauts;

        #endregion

        #region Variable

        protected double _moveVel = 50;
        protected double _absorbVel = 2;
        protected double _syringVel = 5;

        protected ushort _axisX;
        protected ushort _axisY;
        protected ushort _axisZ1;
        protected ushort _axisZ2;
        protected ushort _axisP;  //移液轴
        protected ushort _clawSlaveId;

        #endregion

        #region Constructors

        public CarrierBase(IEtherCATMotion motion, IEPG26 claw,IGlobalStatus globalStatus, ILogger logger)
        {
            this._motion = motion;
            this._claw = claw;
            this._logger = logger;
            this._globalStauts = globalStatus;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public virtual async Task<bool> GoHome(CancellationTokenSource cts)
        {
            try
            {
                //使能夹爪
                var result = await _claw.Enable(_clawSlaveId).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪打开失败！");
                }
                //手爪打开
                result = await OpenClaw(80).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪打开失败！");
                }

                //判断Z轴是否在原点  Z轴回零
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                //移液器轴回零
                if (!_motion.IsServeOn(_axisP))
                {
                    _motion.ServoOn(_axisP);
                    await Task.Delay(1000).ConfigureAwait(false);
                }
                result = await SyringHome(cts).ConfigureAwait(false);
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
                result = await _motion.P2pMoveWithCheckDone(_axisY, 0, 30, cts).ConfigureAwait(false);
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
                result = await _motion.P2pMoveWithCheckDone(_axisX, 0, 30, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("X轴回零失败!");
                }

                return true;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    return false;
                }
                _logger?.Error(ex.Message);
                return false;
            }
         
          

        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// 取试管
        /// </summary>
        /// <param name="pos">目标点位</param>
        /// <param name="clawOpenByte">手爪打开位</param>
        /// <param name="func">取料辅助动作</param>
        /// <param name="cts"></param>
        /// <param name="clawCloseByte">手爪关闭位置</param>
        /// <returns></returns>
        protected async Task<bool> GetTubeAsync(double[] pos,byte clawOpenByte, CancellationTokenSource cts,byte clawCloseByte = 255)
        {
            try
            {
                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece())
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
                var result = await CarrierMoveTo(pos, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("移动到取料点出错");
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
                await OpenClaw(clawOpenByte).ConfigureAwait(false);
                await _motion.P2pMoveWithCheckDone(_axisZ1, 0, _moveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }

        }

        /// <summary>
        /// 放试管
        /// </summary>
        /// <param name="clawOpenByte">手爪打开位</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> PutTubeAsync(double[] pos, byte clawOpenByte, CancellationTokenSource cts)
        {
            try
            {
                //如果手爪打开 判定放料完成
                if (await _claw.ClawGetchStatus(_clawSlaveId) == 1)
                {
                    return true;
                }

                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece())
                {
                    throw new Exception("手爪上无试管");
                }

                //移动到放料点位
                var result = await CarrierMoveTo(pos, cts).ConfigureAwait(false);
                if (!result)
                {
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
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                await OpenClaw(clawOpenByte).ConfigureAwait(false);
                await _motion.P2pMoveWithCheckDone(_axisZ1, 0, _moveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }
           

        }

        /// <summary>
        /// 取枪头
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        protected async Task<bool> GetNeedleAsync(double[] pos, CancellationTokenSource cts)
        {
            try
            {
                //判断移液器是否有枪头
                if (_isGotNeedle)
                {
                    return true;
                }

                //判断Z1 Z2是否在原位
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                //XY轴移动到目标位置
                ushort[] axes = new ushort[2] { _axisX, _axisY };
                double[] posArray = new double[2] { pos[0], pos[1] };
                var result = await _motion.InterPolation_2D_lineWithCheckDone(axes, posArray, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"GetNeedleAsync XY移动到指定位置失败");
                }

                //移液器移动到0位
                result = await _motion.GohomeWithCheckDone(_axisP, 21, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"GetNeedleAsync 移液器回零失败");
                }

                //Z2轴下降到指定位置
                result = await _motion.P2pMoveWithCheckDone(_axisZ2, pos[2], _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"GetNeedleAsync Z2轴移动到指定位置失败");
                }

                _isGotNeedle = true;
                Thread.Sleep(1000);

                //Z2轴上升到安全位置
                result = await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"GetNeedleAsync Z2轴移动到安全位置失败");
                }

                return result;
            }
            catch (Exception ex)
            {
                await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }
        }

        /// <summary>
        /// 放枪头
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> PutNeedleAsync(double[] pos, CancellationTokenSource cts)
        {
            try
            {
                //判断移液器是否有枪头
                if (!_isGotNeedle)
                {
                    return true;
                }

                //判断Z1 Z2是否在原位
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                //XY轴移动到目标位置
                ushort[] axes = new ushort[2] { _axisX, _axisY };
                double[] posArray = new double[2] { pos[0], pos[1] };
                var result = await _motion.InterPolation_2D_lineWithCheckDone(axes, posArray, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"PutNeedleAsync XY移动到指定位置失败");
                }

                //Z2轴下降到指定位置
                result = await _motion.P2pMoveWithCheckDone(_axisZ2, pos[2] -80, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"PutNeedleAsync Z2轴移动到指定位置失败");
                }

                //移液器移动到推枪头位
                result = await _motion.P2pMoveWithCheckDone(_axisP, _putOffNeedle, _absorbVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("移液器推枪头失败");
                }

                _isGotNeedle = false;
                Thread.Sleep(100);

                //移液器回零
                SyringHome(cts);

                //Z2轴上升到安全位置
                result = await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"GetNeedleAsync Z2轴移动到安全位置失败");
                }

                return result;
            }
            catch (Exception ex)
            {
                await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }
           
        }

        /// <summary>
        /// 开始移液器
        /// </summary>
        /// <param name="num"></param>
        /// <param name="volume"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected virtual async Task<bool> DoPipettingAsync(double[] sourcePos, double[] targetPos,double volume, CancellationTokenSource cts)
        {
            try   
            {
                ushort[] axes = new ushort[2] { _axisX, _axisY };
                double[] sourcePosArray = new double[2] { sourcePos[0], sourcePos[1] };
                double[] targetPosArray = new double[2] { targetPos[0], targetPos[1] };

                //判断Z1 Z2是否在原位
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                //判断是否已经吸取溶液
                if (_syringHaveLiquid)
                {
                    goto syringOut;
                }

                #region 去吸取溶液

                //XY轴移动到取液位置
                var result = await _motion.InterPolation_2D_lineWithCheckDone(axes, sourcePosArray, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"DoPipettingAsync XY移动到取液位置失败");
                }

                //Z2轴下降到取位置
                result = await _motion.P2pMoveWithCheckDone(_axisZ2, sourcePos[2], _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"DoPipettingAsync Z2轴移动到取液位置失败");
                }

                //开始吸液
                result = await _motion.P2pMoveWithCheckDone(_axisP, volume, _absorbVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"DoPipettingAsync 移液器吸液失败");
                }

                Thread.Sleep(500);
                _syringHaveLiquid = true;

                //Z2轴上升到安全位置
                result = await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"DoPipettingAsync Z2轴移动到安全位置失败");
                }

                //吸取空气柱
                result = await _motion.P2pMoveWithCheckDone(_axisP, volume + 0.1, _syringVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"DoPipettingAsync 移液器吸空气柱失败");
                }
            #endregion


            syringOut:

                #region 去目标位置吐液

                //避位
                //XY轴移动到吐液位置
                double[] poses = GetPipettingSafePos();
                result = await _motion.InterPolation_2D_lineWithCheckDone(axes, poses, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"DoPipettingAsync XY移动到移液规避位置失败");
                }

                //XY轴移动到吐液位置
                result = await _motion.InterPolation_2D_lineWithCheckDone(axes, targetPosArray, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"DoPipettingAsync XY移动到吐液位置失败");
                }

                //Z2轴下降到取位置
                result = await _motion.P2pMoveWithCheckDone(_axisZ2, targetPos[2], _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"DoPipettingAsync Z2轴移动到吐液位置失败");
                }

                //开始吐液
                result = await _motion.P2pMoveWithCheckDone(_axisP, 0, _syringVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"DoPipettingAsync 移液器吐液失败");
                }
                //吸取空气 
                result = await _motion.P2pMoveWithCheckDone(_axisP, 0.5, _syringVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"DoPipettingAsync 移液器吸空气失败");
                }

                //再吐液
                result = await _motion.P2pMoveWithCheckDone(_axisP, 0, _syringVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"DoPipettingAsync 移液器吐液失败");
                }
                _syringHaveLiquid = false;

                #endregion

                ////Z2轴上升到安全位置   报错
                //_motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, cts);

                return result;
            }
            catch (Exception ex)
            {
                await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }
          
        }

        /// <summary>
        /// 检查Z轴是否在安全位置
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> CheckAxisZInSafePos(CancellationTokenSource cts)
        {
            if (!AxisIsInSafePos(_axisZ1))
            {
                var result = await _motion.P2pMoveWithCheckDone(_axisZ1, 0, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"Z1轴移动到安全位置失败!");
                }

            }
            if (!AxisIsInSafePos(_axisZ2))
            {
                var result = await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"Z2轴移动到安全位置失败!");
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
            var result = await _claw.SendCommand(_clawSlaveId, openPos, 255, 255).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception("手爪打开失败！");
            }
            int status = 0;
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
            var result = await _claw.SendCommand(_clawSlaveId, closePos, 255, 255).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception("手爪抓取物件失败！");
            }
            int status = 0;
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

        /// <summary>
        /// 移液器回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> SyringHome(CancellationTokenSource cts)
        {
            var result = await _motion.GohomeWithCheckDone(_axisP, 21, cts).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception($"PutNeedleAsync 移液器回零失败");
            }
            return true;
        }

        /// <summary>
        /// 移液器移动到指定位置
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> PipettorMoveTo(double[] coordinate, CancellationTokenSource cts)
        {
            try
            {
                double x = coordinate[0];
                double y = coordinate[1];
                double z = coordinate[2];
                ushort[] axisXY = new ushort[2] { _axisX, _axisY };
                double[] targetArr = new double[2] { x, y };

                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                //XY轴移动到取料位置
                var ret = await _motion.InterPolation_2D_lineWithCheckDone(axisXY, targetArr, _moveVel, cts).ConfigureAwait(false);
                if (!ret)
                {
                    throw new Exception("XY轴移动到指定位出错");
                }

                //Z轴下降到取料位置
                var result = await _motion.P2pMoveWithCheckDone(_axisZ2, z, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Z2轴下降到指定位置失败");
                }
                return true;
            }
            catch (Exception ex)
            {
                await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }

        }

        /// <summary>
        /// 搬运XYZ移动到坐标点
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> CarrierMoveTo(double[] coordinate, CancellationTokenSource cts)
        {
            try
            {
                double x = coordinate[0];
                double y = coordinate[1];
                double z = coordinate[2];
                ushort[] axisXY = new ushort[2] { _axisX, _axisY };
                double[] targetArr = new double[2] { x, y };

                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                //XY轴移动到取料位置
                var ret = await _motion.InterPolation_2D_lineWithCheckDone(axisXY, targetArr, _moveVel, cts).ConfigureAwait(false);
                if (!ret)
                {
                    throw new Exception("XY轴移动到指定位出错");
                }

                //Z轴下降到取料位置
                var result = await _motion.P2pMoveWithCheckDone(_axisZ1, z, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Z1轴下降到指定位置失败");
                }

                return true;
            }
            catch (Exception ex)
            {
                await _motion.P2pMoveWithCheckDone(_axisZ1, 0, _moveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }
          
        }



        /// <summary>
        /// 搬运移动到安全位
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> CarrierMoveToSafePos(double[] coordinate, CancellationTokenSource cts)
        {
            double x = coordinate[0];
            double y = coordinate[1];
            ushort[] axisXY = new ushort[2] { _axisX, _axisY };
            double[] targetArr = new double[2] { x, y };

            //判断Z轴是否在原点
            await CheckAxisZInSafePos(cts).ConfigureAwait(false);

            //XY轴移动到取料位置
            var ret = await _motion.InterPolation_2D_lineWithCheckDone(axisXY, targetArr, _moveVel, cts).ConfigureAwait(false);
            if (!ret)
            {
                return false;
            }

            return true;
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
        protected async Task<bool> ClawIsGetchPiece()
        {
            var status = await _claw.ClawGetchStatus(_clawSlaveId).ConfigureAwait(false);
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
