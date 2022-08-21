using BQJX.Common.Common;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class VibrationBase
    {
        protected ILogger _logger;
        protected GlobalStatus _proStatus;

        #region Fields
        protected static IEtherCATMotion _motion;
        protected static IIoDevice _io;
        protected CancellationTokenSource cts = new CancellationTokenSource();
        #endregion

        #region Member

        protected ushort _axisNo;
        protected string _clawOutput;
        protected string _clawOpenSensor;
        protected string _clawCloseSensor;

        /// <summary>
        /// 正在振荡中
        /// </summary>
        /// 
        private bool _isRunning;

        /// <summary>
        /// 是否在原点
        /// </summary>
        private bool _isAtHome;

        /// <summary>
        /// 正在回零中
        /// </summary>
        private bool _isHoming;

        #endregion


        public void Initial(ushort axisNo, string output, string open,string close)
        {
            this._axisNo = axisNo;
            this._clawOutput = output;
            this._clawOpenSensor = open;
            this._clawCloseSensor = close;
            //_motion = Global.GlobalCache.GetInstance().GetMotionInstance();
            //_io = Global.GlobalCache.GetInstance().GetIoDeviceInstance();
        }

        /// <summary>
        /// 启动振荡
        /// </summary>
        /// <param name="vel"></param>
        /// <returns></returns>
        public async Task<bool> StartVibration(double vel)
        {
            _isAtHome = false; 
            //判断是否在振荡
            if (_isRunning)
            {
                return true;
            }
            //判断抱夹气缸是否夹紧
            if (!_io.ReadBit_DI(_clawCloseSensor))
            {
                if (!await SetHolding().ConfigureAwait(false))
                {
                    _logger?.Error("启动涡旋失败！");
                    return false;
                }
            }

            //开始振荡
            bool ret = _motion.VelocityMove(_axisNo, vel, 1);
            if (!ret)
            {
                _logger?.Error("启动振荡失败！");
                return false;
            }

            _isRunning = true;
            return true;
        }

        /// <summary>
        /// 停止振荡
        /// </summary>
        public void StopVibration()
        {
            _isAtHome = false;
            //判断是否为停止状态
            if (!_isRunning)
            {
                return;
            }

            //判断在回零中，停止回零
            if (_isHoming)
            {
                cts.Cancel();
            }

            _isRunning = false;
        }

        /// <summary>
        /// 振荡回零位
        /// </summary>
        /// <returns></returns>
        public async Task<bool> HomeVibration()
        {
            //判断是否在原位
            if (_isAtHome)
            {
                return true;
            }
            //开始回零  Z相回零
            bool ret = await _motion.GohomeWithCheckDone(_axisNo,33, cts);
            _isHoming = true;
            if (!ret)
            {
                _logger?.Error("振荡回零失败！");
                cts = new CancellationTokenSource();
                _isHoming = false;
                return false;
            }

            _isHoming = false;
            _isAtHome = true;
            return true;
        }

        /// <summary>
        /// 抱夹夹紧
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SetHolding()
        {
            _io.SetBit_DO(_clawOutput);
            await Task.Delay(2000).ConfigureAwait(false);
            bool result = _io.ReadBit_DI(_clawCloseSensor);
            if (!result)
            {
                _logger?.Error("振荡气缸关闭未到位！");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 抱夹松开
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ResetHolding()
        {
            _io.ResetBit_DO(_clawOutput);
            await Task.Delay(2000).ConfigureAwait(false);
            bool result = _io.ReadBit_DI(_clawOpenSensor);
            if (!result)
            {
                _logger?.Error("振荡气缸打开未到位！");
                return false;
            }
            return true;
        }

    }
}
