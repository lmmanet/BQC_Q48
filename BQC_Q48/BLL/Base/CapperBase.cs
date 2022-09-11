using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
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

        protected readonly IGlobalStatus _globalStauts;

        protected readonly ICapperPosDataAccess _dataAccess;

        #endregion

        #region Variable

        protected double _yMoveVel = 500;
        protected double _cMoveVel = 80;

        protected ushort _axisY;
        protected ushort _axisZ;
        protected ushort _axisC1;
        protected ushort _axisC2;
        protected ushort _claw;
        protected ushort _holding;
        protected ushort _holdingOpenSensor;
        protected ushort _holdingCloseSensor;


        protected bool _haveCapper = false;  //手爪上有盖


        protected CapperPosData _posData;

        #endregion

        #region Constructors

        public CapperBase(IIoDevice io,ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess, ILogger logger)
        {
            this._io = io;
            this._motion = motion;
            this._dataAccess = dataAccess;
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
            //手爪打开
            _io.WriteBit_DO(_claw, true);

            //使能Z轴
            await _motion.ServoOn(_axisZ).ConfigureAwait(false);
            //Z轴回零
            var result = await _motion.DM2C_GoHomeWithCheckDone(_axisZ, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }

            //使能Y轴
            await _motion.ServoOn(_axisY).ConfigureAwait(false);
            //Y轴回零
            result = await _motion.GoHomeWithCheckDone(_axisY, cts).ConfigureAwait(false);
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
            result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _yMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }
            //回零完成
            return true;

        }

        /// <summary>
        /// 装盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public virtual async Task<bool> CapperOnAsync(Sample sample, CancellationTokenSource cts)
        {
            //判断样品是否有盖


            var result = await CapperOn(80, 40, cts).ConfigureAwait(false);


            return result;
        }

        /// <summary>
        /// 拆盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public virtual async Task<bool> CapperOffAsync(Sample sample, CancellationTokenSource cts)
        {
            //判断样品是否有盖
            var result = await CapperOff(cts).ConfigureAwait(false);

            return result;
        }




        #endregion

        #region Protected Methods


        /// <summary>
        /// 装盖
        /// </summary>
        /// <param name="torque"></param>
        /// <returns></returns>
        protected async Task<bool> CapperOn(int torque,int timeout,CancellationTokenSource cts)
        {
            try
            {
                //判断手爪是否有盖
                if (!_haveCapper)
                {
                    return true;
                }

                //抱夹夹紧
                CloseHolding();

                //Y轴移动到拧盖位
                var result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.CapperPos, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {

                    return false;
                }

                //Z轴下降到位
                result = await _motion.P2pMoveWithCheckDone(_axisZ, _posData.CapperPos_Z, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {

                    return false;
                }
                //开始拧紧
                var result1 =  _motion.TorqueMoveWithCheckDone(_axisC1, _cMoveVel, torque, timeout,cts).ConfigureAwait(false);
                var result2 = _motion.TorqueMoveWithCheckDone(_axisC2, _cMoveVel, torque, timeout, cts).ConfigureAwait(false);
                if (!await result1 || !await result2)
                {
                    //拧盖失败异常
                    return false;
                }

                //手爪松开
                OpenClaw();
                //Z轴上升到位
                result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {

                    return false;
                }
                //Y轴移动到上下料位
                result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {

                    return false;
                }
                //抱夹松开
                OpenHolding();
                _haveCapper = false;
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// 拆盖
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> CapperOff(CancellationTokenSource cts)
        {

            try
            {
                //判断手爪是否有盖
                if (_haveCapper)
                {
                    return true;
                }

                //抱夹夹紧
                CloseHolding();

                //Y轴移动到位
                var result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.CapperPos, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {

                    return false;
                }
                //手爪打开
                OpenClaw();
                //Z轴下降到位
                result = await _motion.P2pMoveWithCheckDone(_axisZ, _posData.CapperPos_Z, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {

                    return false;
                }
                //手爪夹紧
                CloseClaw();
                //开始松开
                var result1 =  _motion.RelativeMoveWithCheckDone(_axisC1, -0.75, 50, cts).ConfigureAwait(false);
                var result2 =  _motion.RelativeMoveWithCheckDone(_axisC2, -0.75, 50, cts).ConfigureAwait(false);

                if (!await result1 || !await result2)
                {
                    //拆失败异常
                    return false;
                }

                //Z轴上升到位
                result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {

                    return false;
                }
                //Y轴移动到上下料位
                result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {

                    return false;
                }
                //抱夹松开
                OpenHolding();
                _haveCapper = true;
                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// Y轴移动到上下料位置
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> Y_MoveToPutGet(CancellationTokenSource cts)
        {
            try
            {  
                //Z轴上升到位
                var result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {

                    return false;
                }
                 result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// 关闭抱夹
        /// </summary>
        protected virtual void CloseHolding(bool checkSensor = true)
        {
            //抱夹夹紧
            var result =  _io.WriteBit_DO(_holding, true);
            if (!result)
            {
                throw new Exception("CloseHolding Err!");
            }
            if (!checkSensor)
            {
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
                    throw new ActionTimeoutException("CloseHolding 超时");
                }
            } while (!result);



        }

        /// <summary>
        /// 打开抱夹
        /// </summary>
        protected virtual void OpenHolding(bool checkSensor = true)
        {
            //抱夹释放
            var result = _io.WriteBit_DO(_holding, false);
            if (!result)
            {
                throw new Exception("OpenHolding Err!");
            }

            if (!checkSensor)
            {
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
                    throw new ActionTimeoutException("OpenHolding超时");
                }
            } while (!result);

        }

        /// <summary>
        /// 关闭手爪
        /// </summary>
        protected virtual void CloseClaw()
        {
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
            //手爪松开
            var result = _io.WriteBit_DO(_claw, true);
            Thread.Sleep(500);
            if (!result)
            {
                throw new Exception("OpenClaw Err!");
            }
        }


        #endregion

        
    }
}
