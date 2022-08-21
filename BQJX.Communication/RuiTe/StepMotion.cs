using System;
using BQJX.Communication.Modbus;
using System.Threading.Tasks;
using System.Threading;

namespace BQJX.Communication.RuiTe
{
    public class StepMotion 
    {
        private readonly ModbusBase modbus;
        public StepMotion(ModbusBase modbus)
        {
            this.modbus = modbus;
        }
        public async Task<bool> StopGoHome(byte addr)
        {
            var result = await modbus.WriteKeepRegister<ushort>(addr, 287, 0).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new Exception($"StopGoHome{addr}，Exception:{result.Message}");
            }
            return result.Data;
        }
        /// <summary>
        /// 回原点
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="offset">原点偏移</param>
        /// <param name="timeout">超时时间（0.1S）</param>
        /// <param name="mode">
        /// 0:正向回原点减速点：原点开关原点：原点开关
        /// 1:负向回原点减速点：原点开关原点：原点开关
        /// 2:正向回原点减速点：正向限位开关原点：正向限位开关
        /// 3:负向回原点减速点：负向限位开关原点：负向限位开关
        /// 4:正向回原点减速点：机械极限位置原点：机械极限位置
        /// 5:正向回原点减速点：机械极限位置原点：机械极限位置
        /// </param>
        /// <param name="highVel">回零高速</param>
        /// <param name="lowVel">回零低速</param>
        /// <param name="acc_dec">回零加减速</param>
        /// <returns></returns>
        public async Task<int> GoHome(byte addr, int offset, int timeout, short mode = 3, short highVel = 50, short lowVel = 10, short acc_dec = 200)
        {
            var ret1 = await modbus.WriteKeepRegister<int>(addr, 293, offset).ConfigureAwait(false); ;
            if (!ret1.IsSuccess)
            {
                throw new Exception($"GoHome{addr}，Exception:{ret1.Message}");
            }
            byte trigger = 4;
            var ret2 = await modbus.WriteKeepRegister<short>(addr, 287, trigger, mode, highVel, lowVel, acc_dec).ConfigureAwait(false); ;
            if (!ret2.IsSuccess)
            {
                throw new Exception($"GoHome{addr}，Exception:{ret2.Message}");
            }
            int time = 0;
            while (true)
            {
                int status = ReadStatusCode(addr).GetAwaiter().GetResult();
                if ((status & 0x02) == 0x02)//发生报警
                {
                    //"驱动报警";
                    return -1;
                }
                if ((status & 0x10) == 0x10)//是否完成回零
                {
                    break;
                }
                if (time >= timeout)
                {
                    //"回零超时";
                    return -2;//超时报错
                }
                time++;
                Thread.Sleep(100);
            }
            return 0;
        }
        /// <summary>
        /// 回原点
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="timeout">超时时间（0.1S）</param>
        /// <returns></returns>
        public async Task<int> GoHome(byte addr, int timeout = 600)
        {
            var ret = await modbus.WriteKeepRegister<short>(addr, 287, 4).ConfigureAwait(false); ;
            if (!ret.IsSuccess)
            {
                throw new Exception($"GoHome{addr}，Exception:{ret.Message}");
            }
            int time = 0;
            while (true)
            {
                int status = ReadStatusCode(addr).GetAwaiter().GetResult();
                if ((status & 0x02) == 0x02)//发生报警
                {
                    // "驱动报警";
                    return -1;
                }
                if ((status & 0x10) == 0x10)//是否完成回零
                {
                    break;
                }
                if (time >= timeout)
                {
                    // "回零超时";
                    return -2;//超时报错
                }
                time++;
                Thread.Sleep(100);
            }
            return 0;//正常结束
        }
        public async Task<int> P2PMove(byte addr, short acc, short dec, short vel, int target)
        {
            var ret1 = await modbus.WriteKeepRegister<short>(addr, 70, acc, dec, vel, (short)target, (short)(target << 8)).ConfigureAwait(false);
            if (!ret1.IsSuccess)
            {
                throw new Exception($"P2PMove{addr}，Exception:{ret1.Message}");
            }
            var ret2 = await modbus.WriteKeepRegister<short>(addr, 18, 1).ConfigureAwait(false);
            if (!ret2.IsSuccess)
            {
                throw new Exception($"P2PMove{addr}，Exception:{ret2.Message}");
            }
            while (true)
            {
                int status = ReadStatusCode(addr).GetAwaiter().GetResult();
                if ((status & 0x02) == 0x02)//发生报警
                {
                    //"驱动报警";
                    return -1;
                }
                if ((status & 0x08) == 0x08)//是否停止运动
                {
                    break;
                }
                Thread.Sleep(100);
            }
            return 0;
        }
        public async Task<int> P2PMove(byte addr, short vel, int target)
        {
            var ret1 = await modbus.WriteKeepRegister<short>(addr, 72, vel, (short)target, (short)(target << 8)).ConfigureAwait(false);
            if (!ret1.IsSuccess)
            {
                throw new Exception($"P2PMove{addr}，Exception:{ret1.Message}");
            }
            var ret2 = await modbus.WriteKeepRegister<short>(addr, 18, 1).ConfigureAwait(false);
            if (!ret2.IsSuccess)
            {
                throw new Exception($"P2PMove{addr}，Exception:{ret2.Message}");
            }
            while (true)
            {
                int status = ReadStatusCode(addr).GetAwaiter().GetResult();
                if ((status & 0x02) == 0x02)//发生报警
                {
                    // "驱动报警";
                    return -1;
                }
                if ((status & 0x08) == 0x08)//是否停止运动
                {
                    break;
                }
                Thread.Sleep(100);
            }
            return 0;
        }
        public async Task<bool> JogF(byte addr, short acc, short dec, short vel, short emgDec)
        {
            var ret1 = await modbus.WriteKeepRegister(addr, 75, acc, dec, vel, emgDec).ConfigureAwait(false); ;
            if (!ret1.IsSuccess)
            {
                throw new Exception($"JogR{addr}，Exception:{ret1.Message}");
            }
            var result = await modbus.WriteKeepRegister<short>(addr, 18, 3).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new Exception($"JogF{addr}，Exception:{result.Message}");
            }
            return result.Data;
        }
        public async Task<bool> JogR(byte addr, short acc, short dec, short vel, short emgDec)
        {
            var ret1 = await modbus.WriteKeepRegister(addr, 75, acc, dec, vel, emgDec).ConfigureAwait(false); ;
            if (!ret1.IsSuccess)
            {
                throw new Exception($"JogR{addr}，Exception:{ret1.Message}");
            }
            var result = await modbus.WriteKeepRegister<short>(addr, 18, 4).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new Exception($"JogR{addr}，Exception:{result.Message}");
            }
            return result.Data;
        }
        public async Task<bool> JogF(byte addr)
        {
            var result = await modbus.WriteKeepRegister<short>(addr, 18, 3).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new Exception($"JogF{addr}，Exception:{result.Message}");
            }
            return result.Data;
        }
        public async Task<bool> JogR(byte addr)
        {
            var result = await modbus.WriteKeepRegister<short>(addr, 18, 4).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new Exception($"JogR{addr}，Exception:{result.Message}");
            }
            return result.Data;
        }
        public async Task<bool> StopJog(byte addr)
        {
            var result = await modbus.WriteKeepRegister<ushort>(addr, 18, 6).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new Exception($"StopJog{addr}，Exception:{result.Message}");
            }
            return result.Data;
        }
        public async Task<bool> StopMove(byte addr)
        {
            var result = await modbus.WriteKeepRegister<ushort>(addr, 18, 6).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new Exception($"StopMove{addr}，Exception:{result.Message}");
            }
            return result.Data;
        }
        public async Task<bool> EmgStop(byte addr)
        {
            var result = await modbus.WriteKeepRegister<ushort>(addr, 18, 5).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new Exception($"EmgStop{addr}，Exception:{result.Message}");
            }
            return result.Data;
        }
        public async Task<short> ReadStatusCode(byte addr)
        {
            var result = await modbus.ReadSingleKeepRegister<short>(addr, 1).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new Exception($"ReadStatusCode{addr}，Exception:{result.Message}");
            }
            return result.Data;
        }
        public async Task<short> ReadAlmCode(byte addr)
        {
            var result = await modbus.ReadSingleKeepRegister<short>(addr, 0).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new Exception($"ReadAlmCode{addr}，Exception:{result.Message}");
            }
            return result.Data;
        }
        public async Task<int> GetCurrentPosition(byte addr)
        {   //8 2个字节
            var result = await modbus.ReadSingleKeepRegister<int>(addr, 8).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new Exception($"GetCurrentPosition{addr}，Exception:{result.Message}");
            }
            return result.Data;
        }
    }

}
