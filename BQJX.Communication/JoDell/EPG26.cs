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

        #region Properties

        /// <summary>
        /// 通讯失败尝试次数
        /// </summary>
        public int AttemptTimes { get; set; } = 3;

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
            int attempt = 0;
            func: try
            {
                var result = await _modbus.ReadMultiInputRegister<ushort>((byte)id, 0x07d0, 4).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"GetClawStatus err:{result.Data}");
                    throw new CommunicationException($"{result.Message}");
                }
                return AnalysisData(result.Data);
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

       
        public async Task<bool> Enable(int id)
        {
            //0x03e8  无参控制指令寄存器    控制寄存器
            int attempt = 0;
        func: try
            {
                var result = await _modbus.WriteKeepRegisterMulti<short>((byte)id, 0x03e8, 1).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"Enable err:{result.Message}");
                    throw new CommunicationException($"{result.Message}");
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

    
        public async Task<bool> Disable(int id)
        {
            //0x03e8  无参控制指令寄存器    控制寄存器
            int attempt = 0;
        func: try
            {
                var result = await _modbus.WriteKeepRegisterMulti<short>((byte)id, 0x03e8, 0).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"Disable err:{result.Message}");
                    throw new CommunicationException($"{result.Message}");
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

       
        public async Task<bool> SendCommand(int id, byte pos, byte vel, byte torque)
        {
            //0x03e8  无参控制指令寄存器    控制寄存器
            //0x03e9  位置寄存器            保留
            //0x03ea  力设置                速度设置
            int attempt = 0;
        func: try
            {
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
                    throw new CommunicationException($"{result.Message}");
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
            if (status.Falt != 0)
            {
                throw new Exception($"手爪{id}报错：{status.Falt}");
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

        public async Task<bool> CheckDone(int id,int timeout)
        {
            bool busy = true;
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(timeout);
            do
            {
                await Task.Delay(1000).ConfigureAwait(false);
                busy = await ClawIsBusy(id).ConfigureAwait(false);
                if (DateTime.Now>end)
                {
                    throw new ActionTimeoutException("CheckDone 手爪动作超时");
                }
            } while (busy);
            return true;
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
                throw new InvalidOperationException(ex.Message);
            }
            return status;
        }


        #endregion

    }




}
