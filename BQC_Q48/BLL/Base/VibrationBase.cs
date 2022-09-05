using BQJX.Common.Common;
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
    public class VibrationBase
    {

        #region Fields

        protected readonly IEtherCATMotion _motion;
        protected readonly IIoDevice _io;
        protected readonly IGlobalStatus _globalStauts;
        protected readonly ILogger _logger;

        #endregion

        #region Member

        protected ushort _axisNo;
        protected string _clawOutput;
        protected string _clawOpenSensor;
        protected string _clawCloseSensor;


        #endregion


        #region Constructors

        public VibrationBase(IEtherCATMotion motion, IIoDevice io, IGlobalStatus globalStauts, ILogger logger)
        {
            this._motion = motion;
            this._io = io;
            this._globalStauts = globalStauts;
            this._logger = logger;
        }

        #endregion


        /// <summary>
        /// 启动振荡
        /// </summary>
        /// <param name="vel"></param>
        /// <returns></returns>
        public async Task<bool> StartVibration(double vel)
        {

            //判断抱夹气缸是否夹紧
            if (!_io.ReadBit_DI(_clawCloseSensor))
            {
                if (!await SetHolding().ConfigureAwait(false))
                {
                    _logger?.Error("启动振荡失败！");
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

            return true;
        }

        /// <summary>
        /// 停止振荡
        /// </summary>
        public void StopVibration()
        {
            var result = _motion.StopMove(_axisNo);
            if (!result)
            {
                throw new Exception("振荡停止失败！");
            }
        }

        /// <summary>
        /// 振荡回零位
        /// </summary>
        /// <returns></returns>
        public async Task<bool> HomeVibration(CancellationTokenSource cts)
        {
           
            //开始回零  Z相回零
            bool ret = await _motion.GohomeWithCheckDone(_axisNo,33, cts);
            if (!ret)
            {
                _logger?.Error("振荡回零失败！");
                cts = new CancellationTokenSource();
                return false;
            }

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
