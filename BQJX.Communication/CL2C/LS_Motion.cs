using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BQJX.Core.Interface;
using BQJX.Communication.Modbus;
using BQJX.Core.Common;
using BQJX.Common.Common;

namespace BQJX.Communication.CL2C
{
    public class LS_Motion : ILS_Motion
    {
        #region Private Members

        private IModbusBase _modbus;

        private ILogger _logger;

        private List<StepAxisEleGear> _eleGearList;

        #endregion

        #region Properties

        /// <summary>
        /// 通讯失败尝试次数
        /// </summary>
        public int AttemptTimes { get; set; } = 3;

        #endregion

        #region Constructors

        public LS_Motion(IModbusBase modbus, ILogger logger, List<StepAxisEleGear> eleGearList)
        {
            _modbus = modbus;
            this._logger = logger;
            _eleGearList = eleGearList;
        }

        #endregion

        #region Public Methods

        public List<StepAxisEleGear> GetAxisInfos()
        {
           return _eleGearList;
        }


        /// <summary>
        /// 判断电机是否停止
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> CheckDone(int axisNo, CancellationTokenSource cts)
        {
            await Task.Delay(500).ConfigureAwait(false);
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            var status = 0;
            do
            {
                status = await GetMotionIoStatus(axisNo).ConfigureAwait(false);

                if (cts != null && cts.IsCancellationRequested)
                {
                    return false;
                }
                //电机在报警状态
                if ((status & 0x01) == 0x01)
                {
                    throw new Exception("电机报警");
                }
                //电机未在使能状态
                if ((status & 0x02) != 0x02)
                {
                    throw new Exception("电机未使能");
                }

                //电机不在运行状态
                if ((status & 0x04) != 0x04)
                {
                    return true;
                }
                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                }
            } while ((status & 0x02) == 0x02);

            return false;
        }

        public async Task<bool> Emg_stop(int axisNo)
        {
            // 01 06 60 02 00 40 37 FA   急停   触发
            int attempt = 0;
            func: try
            {
                var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x6002, 0x0040).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"Emg_stop err:{result.Data}");
                    throw new CommunicationException(result.Message);
                }
                return true;
            }
            catch(CommunicationException cmex)
            {
                attempt++;
                if (attempt >AttemptTimes)
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

        public async Task<double> GetCurrentPos(int axisNo)
        {
            int attempt = 0;
        func: try
            {
                var result = await _modbus.ReadMultiKeepRegister<int>((byte)axisNo, 0x602a, 2).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"GetCurrentPos err:{result.Message}");
                    throw new CommunicationException(result.Message);
                }
                var value = result.Data[0] / _eleGearList.Find(a => a.SlaveId == axisNo).EleGear;
                return Math.Round(value, 3);
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

        public async Task<int> GetMotionIoStatus(int axisNo)
        {
            int attempt = 0;
        func: try
            {
                var result = await _modbus.ReadSingleKeepRegister<ushort>((byte)axisNo, 0x1003).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"GetMotionIoStatus err:{result.Message}");
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

        public async Task<ushort> ReadAlmCode(int axisNo)
        {
            int attempt = 0;
        func: try
            {
                var result = await _modbus.ReadSingleKeepRegister<ushort>((byte)axisNo, 0x2203).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"GetMotionIoStatus err:{result.Message}");
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

        public async Task<bool> GoHomeWithCheckDone(int axisNo,ushort mode,CancellationTokenSource cts)
        {
            int attempt = 0; 
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
        func: try
            {

                //01 06 60 02 00 20 37 D2   回零   触发
                StepAxisEleGear ele = _eleGearList.Find(a => a.SlaveId == axisNo);
                List<ushort> param = new List<ushort>()
                {
                     mode,          //回零参数
                     0,                     //原点位置
                     0,
                     (ushort)(ele.HomeOffset>>16),    //回原点后偏移
                     (ushort)ele.HomeOffset,
                     ele.HomeHigh,                    //回零高速
                     ele.HomeLow,                     //回零低速
                     100,                   //加速度
                     100,                   //减速度
                     100,                   //力矩回零时间ms
                     ele.HomeTorque                     //力矩回零电流百分比
                 };
                //写入回零参数
                var result1 = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x600a, param.ToArray()).ConfigureAwait(false);
                if (!result1.IsSuccess)
                {
                    _logger?.Error($"GohomeWithCheckDone参数写入失败 err{result1.Message}");
                    throw new CommunicationException(result1.Message);
                }

                //触发回零
                var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x6002, 0x0020).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"GohomeWithCheckDone触发失败 err{result1.Message}");
                    throw new CommunicationException(result.Message);
                }

                //判断回零是否完成
                await Task.Delay(500).ConfigureAwait(false);
                //电机在使能状态
                var status = 0;
                do
                {
                    status = await GetMotionIoStatus(axisNo).ConfigureAwait(false);
                    if (cts != null && cts.IsCancellationRequested)
                    {
                        return false;
                    }
                    //电机完成回零
                    if ((status & 0x40) == 0x40)
                    {
                        return true;
                    }

                    //电机停止运动   触发停止
                    if ((status & 0x04) != 0x04)
                    {
                        return false;
                    }
                    if (cts?.IsCancellationRequested == true)
                    {
                        throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                    }
                } while ((status & 0x02) == 0x02);

                return false;
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

        public async Task<bool> GoHomeWithCheckDone(int axisNo, CancellationTokenSource cts)
        {
            int attempt = 0;
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
        func: try
            {
                //01 06 60 02 00 20 37 D2   回零   触发
                StepAxisEleGear ele = _eleGearList.Find(a => a.SlaveId == axisNo);
                ushort offsetLo = (ushort)ele.HomeOffset;
                ushort offsetHi = (ushort)(ele.HomeOffset >> 16);

                List<ushort> param = new List<ushort>()
            {
                 ele.HomeMode,          //回零参数
                 0,                     //原点位置
                 0,
                 (ushort)(ele.HomeOffset >> 16),  //回原点后偏移
                 (ushort)ele.HomeOffset,          //低位
                 ele.HomeHigh,                    //回零高速
                 ele.HomeLow,                     //回零低速
                 100,                   //加速度
                 100,                   //减速度
                 100,                   //力矩回零时间ms
                 ele.HomeTorque         //力矩回零电流百分比
             };
                //写入回零参数
                var result1 = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x600a, param.ToArray()).ConfigureAwait(false);
                if (!result1.IsSuccess)
                {
                    _logger?.Error($"GohomeWithCheckDone参数写入失败 err{result1.Message}");
                    throw new CommunicationException(result1.Message);
                }

                //触发回零
                var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x6002, 0x0020).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"GohomeWithCheckDone触发失败 err{result1.Message}");
                    throw new CommunicationException(result.Message);
                }

                //判断回零是否完成
                await Task.Delay(500).ConfigureAwait(false);
                //电机在使能状态
                var status = 0;
                do
                {
                    status = await GetMotionIoStatus(axisNo).ConfigureAwait(false);
                    if (cts != null && cts.IsCancellationRequested)
                    {
                        return false;
                    }
                    //电机完成回零
                    if ((status & 0x40) == 0x40)
                    {
                        return true;
                    }

                    //电机停止运动   触发停止
                    if ((status & 0x04) != 0x04)
                    {
                        return false;
                    }
                    if (cts?.IsCancellationRequested == true)
                    {
                        throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                    }
                } while ((status & 0x02) == 0x02);

                return false;
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

        public async Task<bool> GoHomeTorqueWithCheckDone(int axisNo, short torque,int direction, CancellationTokenSource cts)
        {
            int attempt = 0; 
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
        func: try
            {
                //01 06 60 02 00 20 37 D2   回零   触发
                short mode = 14;
                if (direction == 1)
                {
                    mode = 15;
                }
                List<short> param = new List<short>()
            {
                mode,           //回零参数
                 0,            //原点位置
                 0,
                 0,            //回原点后偏移
                 0,
                 50,          //回零高速
                 5,           //回零低速
                 100,          //加速度
                 100,          //减速度
                 100,          //力矩回零时间ms
                 torque            //力矩回零电流百分比
             };
                //写入回零参数
                var result1 = await _modbus.WriteKeepRegister<short>((byte)axisNo, 0x600a, param.ToArray()).ConfigureAwait(false);
                if (!result1.IsSuccess)
                {
                    _logger?.Error($"GohomeWithCheckDone参数写入失败 err{result1.Message}");
                    throw new CommunicationException(result1.Message);
                }

                //触发回零
                var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x6002, 0x0020).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"GohomeWithCheckDone触发失败 err{result1.Message}");
                    throw new CommunicationException(result.Message);
                }

                //判断回零是否完成
                await Task.Delay(500).ConfigureAwait(false);
                //电机在使能状态
                var status = 0;
                do
                {
                    status = await GetMotionIoStatus(axisNo).ConfigureAwait(false);
                    if (cts != null && cts.IsCancellationRequested)
                    {
                        return false;
                    }
                    //电机完成回零
                    if ((status & 0x40) == 0x40)
                    {
                        return true;
                    }

                    //电机停止运动   触发停止
                    if ((status & 0x04) != 0x04)
                    {
                        return false;
                    }
                    if (cts?.IsCancellationRequested == true)
                    {
                        throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                    }
                } while ((status & 0x02) == 0x02);

                return false;
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

        public async Task<bool> DM2C_GoHomeWithCheckDone(int axisNo, CancellationTokenSource cts)
        {
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            int attempt = 0;
        func: try
            {
                //01 06 60 02 00 20 37 D2   回零   触发
                StepAxisEleGear ele = _eleGearList.Find(a => a.SlaveId == axisNo);
                ushort offsetLo = (ushort)ele.HomeOffset;
                ushort offsetHi = (ushort)(ele.HomeOffset >> 16);

                List<ushort> param = new List<ushort>()
            {
                 ele.HomeMode,          //回零参数
                 0,                     //原点位置
                 0,
                 (ushort)(ele.HomeOffset >> 16),  //回原点后偏移
                 (ushort)ele.HomeOffset,          //低位
                 ele.HomeHigh,                    //回零高速
                 ele.HomeLow,                     //回零低速
                 100,                   //加速度
                 100                    //减速度

             };
                //写入回零参数
                var result1 = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x600a, param.ToArray()).ConfigureAwait(false);
                if (!result1.IsSuccess)
                {
                    _logger?.Error($"DM2C_GoHomeWithCheckDone写参数 err{result1.Message}");
                    throw new CommunicationException(result1.Message);
                }

                //触发回零
                var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x6002, 0x0020).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"DM2C_GoHomeWithCheckDone触发 err{result1.Message}");
                    throw new CommunicationException(result.Message);
                }

                //判断回零是否完成
                await Task.Delay(500).ConfigureAwait(false);
                //电机在使能状态
                var status = 0;
                do
                {
                    status = await GetMotionIoStatus(axisNo).ConfigureAwait(false);
                    if (cts != null && cts.IsCancellationRequested)
                    {
                        return false;
                    }
                    //电机完成回零
                    if ((status & 0x40) == 0x40)
                    {
                        return true;
                    }

                    //电机停止运动   触发停止
                    if ((status & 0x04) != 0x04)
                    {
                        return false;
                    }
                    if (cts?.IsCancellationRequested == true)
                    {
                        throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                    }
                } while ((status & 0x02) == 0x02);

                return false;
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

        public async Task<bool> JogF(int axisNo)
        {
            //是否更新点动速度
            var ele = _eleGearList.Find(a => a.SlaveId == axisNo);
            if (ele.UpdateParams)
            {
                ele.UpdateParams = false;
                await SetJogVel(axisNo, ele.JogAccDec, ele.JogVel).ConfigureAwait(false);
            }
            //对 0x1801 写 0x4001，正向 JOG
            var result = await _modbus.WriteKeepRegister<short>((byte)axisNo, 0x1801, 0x4001).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                _logger?.Error("JogF 出错!");
                throw new CommunicationException(result.Message);
            }
            return true;
        }

        public async Task<bool> JogR(int axisNo)
        {
            //是否更新点动速度
            var ele = _eleGearList.Find(a => a.SlaveId == axisNo);
            if (ele.UpdateParams)
            {
                ele.UpdateParams = false;
                await SetJogVel(axisNo, ele.JogAccDec, ele.JogVel).ConfigureAwait(false);
            }
            //对 0x1801 写 0x4002，反向 JOG；
            var result = await _modbus.WriteKeepRegister<short>((byte)axisNo, 0x1801, 0x4002).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                _logger?.Error("JogR 出错!");
                throw new CommunicationException(result.Message);
            }
            return true;
        }

        public async Task<bool> SetJogVel(int axisNo, double acc_dec, double velocity)
        {
            int attempt = 0;
        func: try
            {
                //JOG 速度：Pr6.00（0x01E1）；  JOG 加减速时间：Pr6.03（0x01E7）；
                ushort vel = (ushort)velocity;
                ushort acceleration = (ushort)acc_dec;
                var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x01e1, vel).ConfigureAwait(false);

                var result2 = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x01e7, acceleration).ConfigureAwait(false);
                if (!result.IsSuccess || !result2.IsSuccess)
                {
                    _logger?.Error($"SetJogVel err:{result.Message + result2.Message}");
                    throw new CommunicationException(result.Message + result2.Message);
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

        public async Task<bool> P2pMove(int axisNo, double offset, double velocity, double acc, double dec)
        {
            int attempt = 0;
        func: try
            {
                int target = (int)(offset * _eleGearList.Find(a => a.SlaveId == axisNo).EleGear);
                ushort vel = (ushort)velocity;
                ushort acceleration = (ushort)acc;
                ushort deceleration = (ushort)dec;
                List<ushort> param = new List<ushort>()
            {
                1,                         //设定 Pr0 模式为绝对位置 
                (ushort)(target>>16),      //设定 PR0 位置高位 
                (ushort)target,            //设定 PR0 位置低位 
                vel,                       //设定 PR0 速度 600rpm 
                acceleration,              //设定 PR0 加速度 
                deceleration,              //设定 PR0 减速度 
                0,                         //停顿时间
                0x10                       //触发 PR0 运行
            };

                var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x6200, param.ToArray()).ConfigureAwait(false); ;
                if (!result.IsSuccess)
                {
                    _logger?.Error($"P2pMove err:{result.Message}");
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

        public async Task<bool> P2pMoveWithCheckDone(int axisNo, double offset, double velocity, CancellationTokenSource cts)
        {
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            if (Math.Round(await GetCurrentPos(axisNo).ConfigureAwait(false), 1) == Math.Round(offset, 1))
            {
                return true;
            }

            var ret = await P2pMove(axisNo, offset, velocity, 50, 50).ConfigureAwait(false);
            if (!ret)
            {
                return false;
            }

            var isDone = await CheckDone(axisNo, cts).ConfigureAwait(false);
            if (!isDone)
            {
                return false;
            }

            var currentPos = Math.Round(await GetCurrentPos(axisNo).ConfigureAwait(false), 1);
            var targetPos = Math.Round(offset, 1);
            if (targetPos == currentPos)
            {
                return true;
            }
            return false;

        }

        public async Task<bool> RelativeMove(int axisNo, double offset, double velocity, double acc, double dec)
        {
            int attempt = 0;
        func: try
            {
                int target = (int)(offset * _eleGearList.Find(a => a.SlaveId == axisNo).EleGear);
                ushort vel = (ushort)velocity;
                ushort acceleration = (ushort)acc;
                ushort deceleration = (ushort)dec;
                List<ushort> param = new List<ushort>()
            {
                0x41,                      //设定 Pr0 模式为相对位置 
                (ushort)(target>>16),      //设定 PR0 位置高位 
                (ushort)target,            //设定 PR0 位置低位 
                vel,                       //设定 PR0 速度 600rpm 
                acceleration,              //设定 PR0 加速度 
                deceleration,              //设定 PR0 减速度 
                0,                         //停顿时间
                0x10                       //触发 PR0 运行
            };

                var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x6200, param.ToArray());
                if (!result.IsSuccess)
                {
                    _logger?.Error(result.Message);
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

        public async Task<bool> RelativeMoveWithCheckDone(int axisNo, double offset, double velocity, CancellationTokenSource cts)
        {
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            var result = await RelativeMove(axisNo, offset, velocity, 50, 50).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }
            var ret = await CheckDone(axisNo, cts).ConfigureAwait(false);
            if (ret)
            {
                return true;
            }
            return false;

        }

        public async Task<bool> ResetAxisAlm(int axisNo)
        {
            int attempt = 0;
        func: try
            {
                var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x1801, 0x1111).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"SaveParamsToFlash err{result.Data}");
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

        public async Task<bool> ServeOff(int axisNo)
        {
            var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x000f, 0).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                _logger?.Error($"ServeOff err{result.Data}");
                throw new CommunicationException(result.Message);
            }
            return true;

        }

        public async Task<bool> ServoOn(int axisNo)
        {
            var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x000f, 1).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                _logger?.Error($"ServoOn err{result.Data}");
                throw new CommunicationException(result.Message);
            }
            return true;
        }

        public async Task<bool> StopMove(int axisNo)
        {
            return await this.Emg_stop(axisNo).ConfigureAwait(false);
        }

        public async Task<bool> TorqueMoveWithCheckDone(int axisNo, double velocity, double torque,int timeout, CancellationTokenSource cts)
        {
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            int attempt = 0;
        func: try
            {
                ushort para = 15;
                if (velocity < 0)
                {
                    para = 14;
                }
                ushort vel = (ushort)velocity;
                List<ushort> param = new List<ushort>()
            {
                 para,                       //回零参数（方向+模式）
                 0,                          //原点位置
                 0,
                 0,                          //回原点后偏移
                 0,
                 vel,                        //回零高速
                 vel,                         //回零低速
                 100,                        //加速度
                 100,                        //减速度
                 100,                        //力矩回零时间ms
                 (ushort)(torque)            //力矩回零电流百分比
             };
                //写入回零参数
                var result1 = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x600a, param.ToArray()).ConfigureAwait(false);
                if (!result1.IsSuccess)
                {
                    _logger?.Error($"GohomeWithCheckDone参数写入失败 err{result1.Message}");
                    throw new CommunicationException(result1.Message);
                }

                //触发回零
                var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x6002, 0x0020).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error($"GohomeWithCheckDone触发失败 err{result1.Message}");
                    throw new CommunicationException(result.Message);
                }

                //判断回零是否完成
                //判断回零是否完成
                await Task.Delay(500).ConfigureAwait(false);
                //电机在使能状态
                var status = 0;
                DateTime endTime = DateTime.Now + TimeSpan.FromSeconds(timeout);
                do
                {
                    status = await GetMotionIoStatus(axisNo).ConfigureAwait(false);
                    if (cts != null && cts.IsCancellationRequested)
                    {
                        return false;
                    }
                    //电机完成回零
                    if ((status & 0x40) == 0x40)
                    {
                        return true;
                    }

                    //电机停止运动   触发停止
                    if ((status & 0x04) != 0x04)
                    {
                        return false;
                    }
                    //力矩超时
                    if (DateTime.Now > endTime && timeout > 0)
                    {
                        return false;
                    }
                    if (cts?.IsCancellationRequested == true)
                    {
                        throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                    }
                } while ((status & 0x02) == 0x02);

                return false;
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

        public async Task<bool> VelocityMove(int axisNo, double velocity)
        {
            int attempt = 0;
        func: try
            {
                ushort vel = (ushort)velocity;
                List<ushort> param = new List<ushort>()
            {
                2,                        //设定 Pr0 模式为相对位置 
                0,                         //设定 PR0 位置高位 
                0xff,                      //设定 PR0 位置低位 
                vel,                       //设定 PR0 速度 600rpm 
                100,                       //设定 PR0 加速度 
                100,                       //设定 PR0 减速度 
                0,                         //停顿时间
                0x10                       //触发 PR0 运行
            };

                var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x6200, param.ToArray()).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    _logger?.Error(result.Message);
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

        public async Task<bool> SaveParamsToFlash(int axisNo)
        {
            //对寄存器地址 0x1801 写值 0x2211，即可执行参数保存操作
            //对寄存器地址 0x1801 写值 0x2233，即可执行驱动器恢复出厂设置
            var result = await _modbus.WriteKeepRegister<ushort>((byte)axisNo, 0x1801, 0x2211).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                _logger?.Error($"SaveParamsToFlash err{result.Data}");
                throw new CommunicationException(result.Message);
            }
            return true;
        }

      

        #endregion
    }
}
