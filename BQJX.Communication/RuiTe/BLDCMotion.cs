using System;
using BQJX.Communication.Modbus;
using System.Threading.Tasks;

namespace BQJX.Communication.RuiTe
{
    public class BLDCMotion
    {
        private readonly ModbusBase modbus;
        public BLDCMotion(ModbusBase modbus)
        {
            this.modbus = modbus;
        }

        /// <summary>
        /// 启动电机
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<bool> Start(byte addr)
        {
            var result = await modbus.WriteKeepRegister<short>(addr, 24, 1).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new Exception($"Start{addr},Exception:{result.Message}");
            }
            return result.Data;
        }

        /// <summary>
        /// 停止电机
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<bool> Stop(byte addr)
        {
            var result = await modbus.WriteKeepRegister<short>(addr, 24, 0).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new Exception($"Stop{addr},Exception:{result.Message}");
            }
            return result.Data;
        }

        /// <summary>
        /// 设置转速
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <param name="vel">速度值</param>
        /// <returns></returns>
        public async Task<bool> SetVelocity(byte addr, short vel)
        {
            var result = await modbus.WriteKeepRegister<short>(addr, 47, vel).ConfigureAwait(false); 
            if (!result.IsSuccess)
            {
                throw new Exception($"SetVelocity{addr},Exception:{result.Message}");
            }
            return result.Data;
        }

    }
}
