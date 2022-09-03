using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Communication.Modbus;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Communication.Balance
{
    public class Weight : IWeight
    {
        /* 40001/40002 : 显示重量
         * 40003（读）: 显示状态 bit0 :动态/稳态 1：非零位/零位 2：毛重/净重模式  3：没有/有上超载 4：没有/有下超载 5：低/高量程内 6：正常显示重量/分度缩小10倍显示重量
         * 40003（写）：1：清零  2：去皮  3：清皮  4：乘10 5：毛净模式切换
         * 40004（读写）：仪表小数点位数
         * 40031：从站ID
         */


        #region Private Members

        private IModbusBase _modbus;

        private ILogger _logger;

        #endregion

        #region Properties

        /// <summary>
        /// 通讯失败尝试次数
        /// </summary>
        public int AttemptTimes { get; set; } = 3;

        #endregion

        #region Construtors

        public Weight(IModbusBase modbus, ILogger logger)
        {
            _modbus = modbus;
            this._logger = logger;
            _modbus.IsDebug = true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 清零
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> Clear(ushort id)
        {
            var ret = await SendCommand(id, 1).ConfigureAwait(false);
            return ret;
        }


        /// <summary>
        /// 读取当前稳定值
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<double> ReadWeight(ushort id)
        {
            var status = await GetStatus(id).ConfigureAwait(false);
            int bitRight = status[3];                           //小数点位数
            byte[] bytesLow = BitConverter.GetBytes(status[0]);
            byte[] bytesHigh = BitConverter.GetBytes(status[1]);
            int value = BitConverter.ToInt32(new byte[] { bytesLow[0], bytesLow[1], bytesHigh[0], bytesHigh[1] },0);  //数值无单位
            int scale = (int)Math.Pow(10, bitRight);            //比例小数点位数
            double result = (double)value / scale;
            return result;
        }

        /// <summary>
        /// 读取称台当前是否稳定
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> ReadIsStatic(ushort id)
        {
            var status = await GetStatus(id).ConfigureAwait(false);
            bool result = (status[2] & 0x01) == 0x01;
            return result;
        }

        public async Task<int> ReadStatus(ushort id)
        {
            var status = await GetStatus(id).ConfigureAwait(false);
            return status[2];
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 获取秤台状态
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<List<short>> GetStatus(ushort id)
        {
            int attempt = 0;
        func: try
            {
                var result = await _modbus.ReadMultiKeepRegister<short>((byte)id, 0, 4).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    _logger?.Error($"GetStatus err!");
                    throw new CommunicationException(result.Message);
                }

                return result.Data;
            }
            catch (CommunicationException cmex)
            {
                attempt++;
                if (attempt > AttemptTimes)
                {
                    throw cmex;
                }
                goto func;
            }
            catch (Exception ex)
            {
                throw ex;
            }

           
        }

        /// <summary>
        /// 发送秤台命令
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cmd">1：清零  2：去皮  3：清皮  4：乘10 5：毛净模式切换</param>
        /// <returns></returns>
        private async Task<bool> SendCommand(ushort id,short cmd)
        {
            int attempt = 0;
        func: try
            {
                var result = await _modbus.WriteKeepRegisterMulti<short>((byte)id, 40002, cmd).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"SendCommand Err!");
                    throw new CommunicationException(result.Message);
                }
                return true;
            }
            catch (CommunicationException cmex)
            {
                attempt++;
                if (attempt > AttemptTimes)
                {
                    throw cmex;
                }
                goto func;
            }
            catch (Exception ex)
            {
                throw ex;
            }

         
        }

        #endregion











    }
}
