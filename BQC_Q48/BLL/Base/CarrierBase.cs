using BQJX.Common.Interface;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        bool _isGotNeedle;

        /// <summary>
        /// 移液器中有液体
        /// </summary>
        bool _syringHaveLiquid;


        /// <summary>
        /// 推枪头位置
        /// </summary>

        protected double _putOffNeedle = 0;

        #endregion

        #region Private Members

        private readonly IEtherCATMotion _motion;

        private readonly IIoDevice _io;

        private readonly ILogger _logger;

        private readonly IEPG26 _claw;

        private readonly IGlobalStatus _globalStauts;

        #endregion

        #region Variable

        protected double _moveVel = 10;
        protected double _absorbVel = 10;
        protected double _syringVel = 10;


        protected ushort _axisX;
        protected ushort _axisY;
        protected ushort _axisZ1;
        protected ushort _axisZ2;
        protected ushort _axisP;
        protected ushort _clawSlaveId;

        /// <summary>
        /// 手爪半张开状态
        /// </summary>
        protected byte _clawOpenPos = 80;

        

        #endregion

        #region Constructors

        public CarrierBase(IEtherCATMotion motion, IIoDevice io, IEPG26 claw,IGlobalStatus globalStatus, ILogger logger)
        {
            this._motion = motion;
            this._io = io;
            this._claw = claw;
            this._logger = logger;
            this._globalStauts = globalStatus;

            IntialPosData();
        }

        #endregion

        #region Public Methods

        public abstract bool UpdatePosData();

        /// <summary>
        /// 开始移液
        /// </summary>
        /// <param name="num"></param>
        /// <param name="volume"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> DoPipettingAsync(int num, double volume, CancellationTokenSource cts)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// 取试管
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> GetTubeAsync(double x,double y,double z,CancellationTokenSource cts)
        {

            //判断手爪是否抓取物件 在指定打开位置
            if (await ClawIsGetchPiece())
            {
                return true;
            }

            //判断Z1 Z2是否在原位
            var result = await CheckAxisZInSafePos(cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Info($"GetTubeAsync Z轴不在安全位置!");
                return false;
            }

            //XY轴移动到目标位置
            ushort[] axes = new ushort[2] {_axisX,_axisY };
            double[] posArray = new double[2] { x, y };
            result = await _motion.InterPolation_2D_lineWithCheckDone(axes, posArray, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"GetTubeAsync XY移动到指定位置失败");
                return false;
            }

            //打开手爪到指定位置
            result = await OpenClaw(_clawOpenPos).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }


            //Z1轴下降到指定位置
            result = await _motion.P2pMoveWithCheckDone(_axisZ1, z, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"GetTubeAsync Z1轴移动到指定位置失败");
                return false;
            }

            //手爪抓取物件
            var ret = await CloseClaw(255).ConfigureAwait(false);
          
            //Z1轴上升到安全位置
            result = await _motion.P2pMoveWithCheckDone(_axisZ1, 0, _moveVel, cts).ConfigureAwait(false);
            if (!ret)
            {
                _logger?.Warn($"手爪抓取物件失败");
                return false;
            }

            return result;
        }

        /// <summary>
        /// 放试管
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> PutTubeAsync(double x,double y,double z, CancellationTokenSource cts)
        {
            //判断手爪上是否有物件
            if (!await ClawIsGetchPiece())
            {
                return true;
            }

            //判断Z1 Z2是否在原位
            var result = await CheckAxisZInSafePos(cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Info($"PutTubeAsync Z轴不在安全位置!");
                return false;
            }

            //XY轴移动到目标位置
            ushort[] axes = new ushort[2] { _axisX, _axisY };
            double[] posArray = new double[2] { x, y };
            result = await _motion.InterPolation_2D_lineWithCheckDone(axes, posArray, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"PutTubeAsync XY移动到指定位置失败");
                return false;
            }

            //再次判断手爪是否有物件
            if (!await ClawIsGetchPiece())
            {
                _logger?.Warn($"手爪物件掉落");
                return false;
            }

            //Z1轴下降到指定位置
            result = await _motion.P2pMoveWithCheckDone(_axisZ1, z, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"PutTubeAsync Z1轴移动到指定位置失败");
                return false;
            }

            //打开手爪到指定位置
            var ret  = await OpenClaw(_clawOpenPos).ConfigureAwait(false);

            //Z1轴上升到安全位置
            result = await _motion.P2pMoveWithCheckDone(_axisZ1, 0, _moveVel, cts).ConfigureAwait(false);
            if (!ret)
            {
                _logger?.Warn($"手爪打开失败");
                return false;
            }

            return result;
        }

        /// <summary>
        /// 取枪头
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        protected async Task<bool> GetNeedleAsync(double x,double y,double z, CancellationTokenSource cts)
        {
            //判断移液器是否有枪头
            if (_isGotNeedle)
            {
                return true;
            }

            //判断Z1 Z2是否在原位
            var result = await CheckAxisZInSafePos(cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Info($"GetNeedleAsync Z轴不在安全位置!");
                return false;
            }

            //XY轴移动到目标位置
            ushort[] axes = new ushort[2] { _axisX, _axisY };
            double[] posArray = new double[2] { x, y };
            result = await _motion.InterPolation_2D_lineWithCheckDone(axes, posArray, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"GetNeedleAsync XY移动到指定位置失败");
                return false;
            }

            //移液器移动到0位
            result = await _motion.GohomeWithCheckDone(_axisP, 21, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"GetNeedleAsync 移液器回零失败");
                return false;
            }

            //Z2轴下降到指定位置
            result = await _motion.P2pMoveWithCheckDone(_axisZ2, z, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"GetNeedleAsync Z2轴移动到指定位置失败");
                return false;
            }

            _isGotNeedle = true;
            Thread.Sleep(1000);

            //Z2轴上升到安全位置
            result = await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"GetNeedleAsync Z2轴移动到安全位置失败");
                return false;
            }


            return result;
        }

        /// <summary>
        /// 放枪头
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> PutNeedleAsync(double x,double y,double z, CancellationTokenSource cts)
        {
            //判断移液器是否有枪头
            if (!_isGotNeedle)
            {
                return true;
            }

            //判断Z1 Z2是否在原位
            var result = await CheckAxisZInSafePos(cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Info($"PutNeedleAsync Z轴不在安全位置!");
                return false;
            }

            //XY轴移动到目标位置
            ushort[] axes = new ushort[2] { _axisX, _axisY };
            double[] posArray = new double[2] { x, y };
            result = await _motion.InterPolation_2D_lineWithCheckDone(axes, posArray, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"PutNeedleAsync XY移动到指定位置失败");
                return false;
            }

            //Z2轴下降到指定位置
            result = await _motion.P2pMoveWithCheckDone(_axisZ2, z, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"PutNeedleAsync Z2轴移动到指定位置失败");
                return false;
            }

            //移液器移动到推枪头位
            result = await _motion.P2pMoveWithCheckDone(_axisP, _putOffNeedle, _absorbVel,cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn("移液器推枪头失败");
                return false;
            }

            _isGotNeedle = false;
            Thread.Sleep(100);

            //移液器回零
            SyringHome(cts);

            //Z2轴上升到安全位置
            result = await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"GetNeedleAsync Z2轴移动到安全位置失败");
                return false;
            }
           
            return result;
        }

        /// <summary>
        /// 开始移液器
        /// </summary>
        /// <param name="num"></param>
        /// <param name="volume"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> DoPipettingAsync(double sourceX,double sourceY,double sourceZ,double targetX,double targetY,double targetZ,double volume, CancellationTokenSource cts)
        {
            ushort[] axes = new ushort[2] { _axisX, _axisY };
            double[] sourcePosArray = new double[2] { sourceX, sourceY };
            double[] targetPosArray = new double[2] { targetX, targetY };

            //判断Z1 Z2是否在原位
            var result = await CheckAxisZInSafePos(cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Info($"DoPipettingAsync Z轴不在安全位置!");
                return false;
            }

            //判断是否已经吸取溶液
            if (_syringHaveLiquid )
            {
                goto syringOut;
            }

            #region 去吸取溶液

            //XY轴移动到取液位置
            result = await _motion.InterPolation_2D_lineWithCheckDone(axes, sourcePosArray, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"DoPipettingAsync XY移动到取液位置失败");
                return false;
            }

            //Z2轴下降到取位置
            result = await _motion.P2pMoveWithCheckDone(_axisZ2, sourceZ, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"DoPipettingAsync Z2轴移动到取液位置失败");
                return false;
            }

            //开始吸液
            result = await _motion.P2pMoveWithCheckDone(_axisP, volume, _absorbVel,cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"DoPipettingAsync 移液器吸液失败");
                return false;
            }

            Thread.Sleep(500);
            _syringHaveLiquid = true;

            //Z2轴上升到安全位置
            result = await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"DoPipettingAsync Z2轴移动到安全位置失败");
                return false;
            }

            //吸取空气柱

        #endregion

        syringOut:

            #region 去目标位置吐液

            //XY轴移动到吐液位置
            result = await _motion.InterPolation_2D_lineWithCheckDone(axes, targetPosArray, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"DoPipettingAsync XY移动到吐液位置失败");
                return false;
            }

            //Z2轴下降到取位置
            result = await _motion.P2pMoveWithCheckDone(_axisZ2, sourceZ, _moveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"DoPipettingAsync Z2轴移动到吐液位置失败");
                return false;
            }

            //开始吐液
            result = await _motion.P2pMoveWithCheckDone(_axisP, 0, _syringVel, cts).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Warn($"DoPipettingAsync 移液器吐液失败");
                return false;
            }

            _syringHaveLiquid = false;

            #endregion

            //Z2轴上升到安全位置
            _motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, cts);
        
            return result;
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
                    _logger?.Warn($"Z1轴移动到安全位置失败!");
                    return false;
                }

            }
            if (!AxisIsInSafePos(_axisZ2))
            {
                var result = await _motion.P2pMoveWithCheckDone(_axisZ2, 0, _moveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    _logger?.Warn($"Z2轴移动到安全位置失败!");
                    return false;
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
                _logger?.Warn($"手爪打开失败！");
                return false;
            }
            int status = 0;
            do
            {
                status = _claw.ClawGetchStatus(_clawSlaveId).GetAwaiter().GetResult();
                if (status == 3)
                {
                    return true;
                }
                if (status == 1)
                {
                    break;
                }
                Thread.Sleep(300);

            } while (status == 0);

            return false;
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
                _logger?.Warn($"手爪抓取物件失败！");
                return false;
            }
            int status = 0;
            do
            {
                status = _claw.ClawGetchStatus(_clawSlaveId).GetAwaiter().GetResult();
                if (status == 2)
                {
                    return true;
                }
                if (status == 3)
                {
                    break;
                }
                Thread.Sleep(300);

            } while (status == 0);

            return false;
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
                _logger?.Warn($"PutNeedleAsync 移液器回零失败");
                return false;
            }
            return true;
        }

        //=======================================================//

        /// <summary>
        /// 判断轴是否在安全位置
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        protected bool AxisIsInSafePos(ushort axisNo)
        {
            var currentPos = _motion.GetCurrentPos(axisNo);
            return Math.Round(currentPos, 1) != 0;
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
        /// 手爪是否打开
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> ClawIsOpen()
        {
            var status = await _claw.GetClawStatus(_clawSlaveId).ConfigureAwait(false);
            if (status.Position < _clawOpenPos)
            {
                return true;
            }
            return false;

        }


        /// <summary>
        /// 初始化点位数据
        /// </summary>
        protected abstract void IntialPosData();


        #endregion

   
    }
}
