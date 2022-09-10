using BQJX.Common;
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
        protected string _holding;
        protected string _holdingOpenSensor; //原位
        protected string _holdingCloseSensor; //到位


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

        #region Public Methods

        /// <summary>
        /// 振荡回零位
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> GoHome(CancellationTokenSource cts)
        {

            //释放抱夹气缸
            ResetHolding();
            //开始回零  Z相回零
            bool ret = await _motion.GohomeWithCheckDone(_axisNo, 33, cts);
            if (!ret)
            {
                _logger?.Error("振荡回零失败！");
                return false;
            }

            //抱夹气缸伸出
            SetHolding();

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public virtual async Task<bool> StartVibrationAsync(Sample sample,CancellationTokenSource cts)
        {
            try
            {
                double vel = 300 / 60;
                var result = await StartVibration(30, vel, cts).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }





        #endregion

        #region Protected Methods

        /// <summary>
        /// 启动振荡
        /// </summary>
        /// <param name="vel"></param>
        /// <returns></returns>
        protected async Task<bool> StartVibration(int time, double vel, CancellationTokenSource cts)
        {

            //释放抱夹气缸
            ResetHolding();

            //开始振荡
            bool ret = _motion.VelocityMove(_axisNo, vel, 1);
            if (!ret)
            {
                throw new Exception("启动振荡失败！");
            }

            //开始定时
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(time);
            do
            {
                Thread.Sleep(1000);
                if (DateTime.Now > end)
                {
                    break;
                }
                if (cts.IsCancellationRequested)
                {
                    _motion.StopMove(_axisNo);
                    return false;
                }
            } while (true);

            //停止电机
            ret = _motion.StopMove(_axisNo);
            if (!ret)
            {
                throw new Exception("停止振荡失败！");
            }
            await Task.Delay(500).ConfigureAwait(false);

            return await GoHome(cts).ConfigureAwait(false);
        }

        /// <summary>
        /// 抱夹气缸伸出
        /// </summary>
        /// <returns></returns>
        protected virtual void SetHolding(bool checkSensor = true)
        {
            var result = _io.SetBit_DO(_holding);
            if (!result)
            {
                throw new Exception("SetHolding Err!");
            }
            if (!checkSensor)
            {
                return;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_holdingCloseSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new Exception("SetHolding超时");
                }
            } while (!result);
        }

        /// <summary>
        /// 抱夹气缸释放
        /// </summary>
        /// <returns></returns>
        protected virtual void ResetHolding(bool checkSensor = true)
        {
            var result = _io.ResetBit_DO(_holding);
            if (!result)
            {
                throw new Exception("ResetHolding Err!");
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
                    throw new Exception("ResetHolding 超时");
                }
            } while (!result);
        }

        #endregion
    }
}
