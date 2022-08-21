using BQJX.Common.Common;
using BQJX.Communication.Modbus;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BQJX.Communication.JoDell
{
    public class EPG26 : IEPG26
    {
        #region Private Members

        private IModbusBase _modbus;

        private ILogger _logger;

        #endregion

        #region Constructors

        public EPG26(IModbusBase modbus, ILogger logger)
        {
            _modbus = modbus;
            this._logger = logger;
            _modbus.IsDebug = true;
        }

        #endregion

        #region Public Methods

   
        public async Task<EPG_ClawStatus> GetClawStatus(int id)
        {
            // 0x07d0 空              夹爪状态
            // 0x07d1 位置状态        故障错误状态
            // 0x07d2 力状态          速度状态
            // 0x07d3 环境温度        母线电压
            var result = await _modbus.ReadMultiInputRegister<ushort>((byte)id, 0x07d0, 4).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                _logger?.Error($"GetClawStatus err:{result.Data}");
                return null;
            }
            return AnalysisData(result.Data);
        }

       
        public async Task<bool> Enable(int id)
        {
            //0x03e8  无参控制指令寄存器    控制寄存器
            var result = await _modbus.WriteKeepRegisterMulti<short>((byte)id, 0x03e8, 1).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                _logger?.Error($"Enable err:{result.Message}");
                return false;
            }
            return true;
        }

    
        public async Task<bool> Disable(int id)
        {
            //0x03e8  无参控制指令寄存器    控制寄存器
            var result = await _modbus.WriteKeepRegisterMulti<short>((byte)id, 0x03e8, 0).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                _logger?.Error($"Disable err:{result.Message}");
                return false;
            }
            return true;
        }

       
        public async Task<bool> SendCommand(int id, byte pos, byte vel, byte torque)
        {
            //0x03e8  无参控制指令寄存器    控制寄存器
            //0x03e9  位置寄存器            保留
            //0x03ea  力设置                速度设置
            ushort[] values = new ushort[3]
            {
                0x0009,                         //执行指令
                (ushort)(pos<<8),                            //位置设置
                (ushort)((torque<<8)+vel)        //力和速度
            };

            var result = await _modbus.WriteKeepRegister<ushort>((byte)id, 0x03e8, values).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                _logger?.Error($"SendCommand err:{result.Message}");
                return false;
            }
            return true;
        }

      
        public async Task<bool> ClawIsEnable(int id)
        {
            var status = await GetClawStatus(id);
            if (status == null)
            {
                return false;
            }
            bool enable = (status.ClawStatus & 0x01) == 0x01;
            return enable;
        }

  
        public async Task<bool> ClawMode(int id)
        {
            var status = await GetClawStatus(id);
            if (status == null)
            {
                return false;
            }
            bool mode = (status.ClawStatus & 0x04) == 0x04;
            return mode;
        }

    
        public async Task<bool> ClawIsBusy(int id)
        {
            var status = await GetClawStatus(id);
            if (status == null)
            {
                return false;
            }
            bool busy = (status.ClawStatus & 0x08) == 0x08;
            return busy;
        }

      
        public async Task<bool> ClawIsEnableDone(int id)
        {
            var status = await GetClawStatus(id);
            if (status == null)
            {
                return false;
            }
            bool enableDone = (status.ClawStatus & 0x30) == 0x30;
            return enableDone;
        }

    
        public async Task<int> ClawGetchStatus(int id)
        {
            var status = await GetClawStatus(id);
            if (status == null)
            {
                return -1;
            }

            int value = status.ClawStatus & 0xc0;
            if (value == 0xc0) //到达指定位置
            {
                return 3;
            }
            if (value == 0x80) //闭合检测到物体
            {
                return 2;
            }
            if (value == 0x40) //张开检测到物体
            {
                return 1;
            }
            if (value == 0x00)
            {
                return 0;
            }
            return -1;
        }


        #endregion

        #region Private Methods

        /// <summary>
        /// 解析数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private EPG_ClawStatus AnalysisData(List<ushort> data)
        {
            EPG_ClawStatus status = new EPG_ClawStatus();
            try
            {
                status.ClawStatus = (byte)data[0];
                status.Falt = (byte)data[1];
                status.Position = (byte)(data[1] >> 8);
                status.Velocity = (byte)data[2];
                status.Torque = (byte)(data[2] >> 8);
                status.Voltage = (byte)data[3];
                status.Temperature = (byte)(data[3] >> 8);
            }
            catch (Exception ex)
            {
                _logger?.Error($"AnalysisData Err:{ex.Message}");
                return null;
            }
            return status;
        }


        #endregion

    }




}
