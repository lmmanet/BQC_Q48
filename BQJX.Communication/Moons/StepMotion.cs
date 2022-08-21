using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using BQJX.Core.Interface;

namespace BQJX.Communication.Moons
{
    public class StepMotion : SerialPortBase
    {
        public string V { get; }

        #region Construtors

        public StepMotion(string portName,ILogger logger) : base(logger)
        {
            this._logger = logger;
            serialPort = new SerialPort
            {
                PortName = portName,
                BaudRate = 115200,
                DataBits = 8,
                StopBits = StopBits.One,
                Parity = Parity.None,
                ReceivedBytesThreshold = 1
            };
            // serialPort.Encoding = Encoding.ASCII;
            ReadTimeout = 500;
        }

        public StepMotion(SerialPort serialPort,ILogger logger) : base(logger)
        {
            this._logger = logger;
            this.serialPort = serialPort;
            ReadTimeout = 500;
        }


        #endregion

        #region Public Methods

        /// <summary>
        /// 回原点
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <param name="vel">回零速度</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<int> Homing(ushort addr, int vel, int timeout)
        {
            try
            {
                string command = MotionHelper.Homing(addr, vel);
                string[] condition = { "\r\n" };
                string[] cmds = command.Split(condition, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in cmds)
                {
                    string send = item + "\r\n";
                    var ret = await this.SendAndReceive(send).ConfigureAwait(false);
                    MotionHelper.AnalysisResponseData(ret);
                }
                DateTime now = DateTime.Now;
                while (true)
                {
                    int status = ReadStatusCode(addr).GetAwaiter().GetResult();
                    if ((status & (int)StatusCode.InPosition) == (int)StatusCode.InPosition)
                    {
                        break;
                    }
                    else if ((status & (int)StatusCode.Alarm) == (int)StatusCode.Alarm)//电机失去使能
                    {
                        //"Alarm";
                        return -2;
                    }

                    else if ((status & (int)StatusCode.DriveFault) == (int)StatusCode.DriveFault)//
                    {
                        //"DriveFault";
                        return -3;
                    }
                    TimeSpan consume = DateTime.Now - now;
                    if (consume.Milliseconds > timeout)
                    {
                        throw new Exception($"Homing timeout：{timeout}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"回零出错，Exception:{ex.Message}");
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// 硬限位回零
        /// </summary>
        /// <param name="addr">HardStopHoming</param>
        /// <param name="homeVel">回零速度</param>
        /// <param name="current">电流设定</param>
        /// <returns></returns>
        public async Task<int> HardHoming(ushort addr,int homeVel, int current,int timeout)
        {
            try
            {
                string command = MotionHelper.HardStopHoming(addr,current);
                string[] condition = { "\r\n" };
                string[] cmds = command.Split(condition, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in cmds)
                {
                    string send = item + "\r\n";
                    var ret = await this.SendAndReceive(send).ConfigureAwait(false);
                    MotionHelper.AnalysisResponseData(ret);
                }
                DateTime now = DateTime.Now;
                while (true)
                {
                    int status = ReadStatusCode(addr).GetAwaiter().GetResult();
                    if ((status & (int)StatusCode.InPosition) == (int)StatusCode.InPosition)
                    {
                        break;
                    }
                    else if ((status & (int)StatusCode.Alarm) == (int)StatusCode.Alarm)//电机失去使能
                    {
                        //"Alarm";
                        return -1;
                    }

                    else if ((status & (int)StatusCode.DriveFault) == (int)StatusCode.DriveFault)//
                    {
                        //"DriveFault";
                        return -2;
                    }
                    TimeSpan consume = DateTime.Now - now;
                    if (consume.Milliseconds > timeout)
                    {
                        throw new Exception($"Homing timeout：{timeout}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"回零出错，Exception:{ex.Message}");
                return -3;
            }
            return 0;
        }

        /// <summary>
        /// 正向点动
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <param name="vel">点动速度</param>
        /// <returns></returns>
        public async Task<bool> JogF(ushort addr, int vel)
        {
            try
            {
                string command = MotionHelper.JogF(addr, vel);
                string[] condition = { "\r\n" };
                string[] cmds = command.Split(condition, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in cmds)
                {
                    string send = item + "\r\n";
                    var ret = await this.SendAndReceive(send).ConfigureAwait(false);
                    MotionHelper.AnalysisResponseData(ret);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"JogF,Exception:{ex.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 反向点动
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <param name="vel">点动速度</param>
        /// <returns></returns>
        public async Task<bool> JogR(ushort addr, int vel)
        {
            try
            {
                string command = MotionHelper.JogR(addr, vel);
                string[] condition = { "\r\n" };
                string[] cmds = command.Split(condition, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in cmds)
                {
                    string send = item + "\r\n";
                    var ret = await this.SendAndReceive(send).ConfigureAwait(false);
                    MotionHelper.AnalysisResponseData(ret);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"JogR,Exception:{ex.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 点动停止
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<bool> StopJog(ushort addr)
        {
            try
            {
                string send = MotionHelper.StopJogging(addr);
                var ret = await this.SendAndReceive(send).ConfigureAwait(false);
                MotionHelper.AnalysisResponseData(ret);
            }
            catch (Exception ex)
            {
                _logger?.Error($"StopJog,Exception:{ex.Message}");
                return false;
            }
            return true;  
        }

        /// <summary>
        /// 读取电机编码器位置
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<int> ReadPosition(ushort addr)
        {
            try
            {
                string send = MotionHelper.ReadPositon(addr);
                string ret = await this.SendAndReceive(send).ConfigureAwait(false);
                return (int)MotionHelper.AnalysisResponseData(ret);
            }
            catch (Exception ex)
            {
                _logger?.Error($"ReadPosition,Exception:{ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 设置运动速度
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <param name="vel">运动速度</param>
        /// <returns></returns>
        public async Task<bool> SetVel(ushort addr, int vel)
        {
            try
            {
                string send = MotionHelper.VelSetting(addr, vel);
                string ret = await this.SendAndReceive(send).ConfigureAwait(false);
                MotionHelper.AnalysisResponseData(ret);
            }
            catch (Exception ex)
            {
                _logger?.Error($"SetVel,Exception:{ex.Message}");
                return false;
            }
            return true;
          
        }

        /// <summary>
        /// 绝对定位
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <param name="vel">运动速度</param>
        /// <param name="position">目标位置</param>
        /// <returns></returns>
        public async Task<Result> MoveDistance(ushort addr, int vel, int position)
        {
            Result result = new Result();
            try
            {
                string command = MotionHelper.MoveDistance(addr, vel, position);
                string[] condition = { "\r\n" };
                string[] cmds = command.Split(condition, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in cmds)
                {
                    string send = item + "\r\n";
                    var ret = await this.SendAndReceive(send).ConfigureAwait(false);
                    MotionHelper.AnalysisResponseData(ret);
                }
                while (true)
                {
                    int status = ReadStatusCode(addr).GetAwaiter().GetResult();
                    if ((status & (int)StatusCode.InPosition) == (int)StatusCode.InPosition)
                    {
                        break;
                    }
                    else if ((status & (int)StatusCode.Alarm) == (int)StatusCode.Alarm)//电机失去使能
                    {
                        result.Message = "Alarm";
                        result.IsSuccess = false;
                        result.Code = status;
                        break;
                    }

                    else if ((status & (int)StatusCode.DriveFault) == (int)StatusCode.DriveFault)//
                    {
                        result.Message = "DriveFault";
                        result.IsSuccess = false;
                        result.Code = status;
                        break;
                    }
                    else if ((status & (int)StatusCode.Stopping) == (int)StatusCode.Stopping)
                    {
                        result.Message = "Stopping";
                        result.IsSuccess = false;
                        result.Code = status;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                result.Code = -2;
            }
            return result;
        }
        /// <summary>
        /// 相对定位
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <param name="vel">运动速度</param>
        /// <param name="offset">目标位置</param>
        /// <returns></returns>
        public async Task<Result> MoveRelative(ushort addr, int vel, int offset)
        {
            Result result = new Result();
            try
            {
                string command = MotionHelper.MoveRelative(addr, vel, offset);
                string[] condition = { "\r\n" };
                string[] cmds = command.Split(condition, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in cmds)
                {
                    string send = item + "\r\n";
                    var ret = await this.SendAndReceive(send).ConfigureAwait(false);
                    MotionHelper.AnalysisResponseData(ret);
                }
                while (true)
                {
                    int status = ReadStatusCode(addr).GetAwaiter().GetResult();
                    if ((status & (int)StatusCode.InPosition) == (int)StatusCode.InPosition)
                    {
                        break;
                    }
                    else if ((status & (int)StatusCode.Stopping) == (int)StatusCode.Stopping)
                    {
                        result.Message = "Stopping";
                        result.IsSuccess = false;
                        result.Code = status;
                        break;
                    }//DriveFault
                    else if ((status & (int)StatusCode.Alarm) == (int)StatusCode.Alarm)//电机失去使能
                    {
                        result.Message = "Alarm";
                        result.IsSuccess = false;
                        result.Code = status;
                        break;
                    }

                    else if ((status & (int)StatusCode.DriveFault) == (int)StatusCode.DriveFault)//
                    {
                        result.Message = "DriveFault";
                        result.IsSuccess = false;
                        result.Code = status;
                        break;
                    }
                    }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                result.Code = -2;
            }
            return result;
        }
        /// <summary>
        /// 停止运动
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<bool> StopMotion(ushort addr)
        {
            try
            {
                string send = MotionHelper.StopMotion(addr);
                string ret = await this.SendAndReceive(send).ConfigureAwait(false);
                MotionHelper.AnalysisResponseData(ret);
            }
            catch (Exception ex)
            {
                _logger?.Error($"StopMotion,Exception:{ex.Message}");
                return false;
            }
            return true;
          
        }

        /// <summary>
        /// 复位报警
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<bool> ResetAlm(ushort addr)
        {
            try
            {
                string send = MotionHelper.ResetAlm(addr);
                string ret = await this.SendAndReceive(send).ConfigureAwait(false);
                MotionHelper.AnalysisResponseData(ret);
            }
            catch (Exception ex)
            {
                _logger?.Error($"ResetAlm,Exception:{ex.Message}");
                return false;
            }
            return true;
           
        }

        /// <summary>
        /// 读取电机报警代码
        /// PositionLimit = 0x0001,                //0001 Position Limit
        /// CCW_Limit = 0x0002,                    //0002 CCW Limit
        /// CW_Limit = 0x0004,                     //0004 CW Limit
        /// Over_Temp = 0x0008,                    //0008 Over Temp
        /// Internal_Voltage = 0x0010,             //0010 Internal Voltage
        /// Over_Voltage = 0x0020,                 //0020 Over Voltage
        /// Under_Voltage = 0x0040,                //0040 Under Voltage Under Voltage Under Voltage
        /// Over_Current = 0x0080,                 //0080 Over Current
        /// Open_Motor_Winding = 0x0100,           //0100  Open Motor Winding Bad Hall
        /// Bad_Encoder = 0x0200,                  //0200 Bad Encoder
        /// Cmd_Error = 0x0400,                    //0400 Comm Error
        /// Bad_Flash = 0x0800,                    //0800 Bad Flash
        /// No_Move = 0x1000,                      //1000 No Move No Move Excess Regen
        /// Current_Foldback = 0x2000,             //2000 Current Foldback
        /// Bland_Q_Segment = 0x4000,              //4000 Bland Q Segment
        /// NV_Memory_Double_Error = 0x8000        //8000  NV Memory Double Error No Move
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public async Task<int> ReadAlmCode(ushort addr)
        {
            try
            {
                string send = MotionHelper.ReadAlm(addr);
                string ret = await SendAndReceive(send).ConfigureAwait(false);
                return (int) MotionHelper.AnalysisResponseData(ret);
            }
            catch (Exception ex)
            {
                _logger?.Error($"ReadAlmCode,Exception:{ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 读取电机状态
        /// MotorEnable = 0x0001,         //0001 Motor Enabled (Motor Disabled if this bit = 0)
        /// Sampling = 0x0002,            //0002 Sampling(for Quick Tuner)
        /// DriveFault = 0x0004,          //0004 Drive Fault(check Alarm Code)
        /// InPosition = 0x0008,          //0008 In Position(motor is in position)
        /// Moving = 0x0010,              //0010 Moving(motor is moving)
        /// Jogging = 0x0020,             //0020 Jogging(currently in jog mode)
        /// Stopping = 0x0040,            //0040 Stopping(in the process of stopping from a stop command)
        /// WaitingForInput = 0x0080,     //0080 Waiting(for an input; executing a WI command)
        /// Saving = 0x0100,              //0100 Saving(parameter data is being saved)
        /// Alarm = 0x0200,               //0200 Alarm present(check Alarm Code)
        /// Homing = 0x0400,              //0400 Homing(executing an SH command)
        /// WaitingForTime = 0x0800,      //0800 Waiting(for time; executing a WD or WT command)
        /// WizardRunning = 0x1000,       //1000 Wizard running(Timing Wizard is running)
        /// CheckingEncoder = 0x2000,     //2000 Checking encoder(Timing Wizard is running)
        /// Q_Program_is_running = 0x4000,//4000 Q Program is running
        /// Initializing = 0x8000         //8000 Initializing(happens at power up)     
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public async Task<int> ReadStatusCode(ushort addr)
        {
            try
            {
                string send = MotionHelper.ReadStatus(addr);
                string ret = await SendAndReceive(send).ConfigureAwait(false);
                return (int) MotionHelper.AnalysisResponseData(ret);
            }
            catch (Exception ex)
            {
                _logger?.Error($"ReadStatusCode,Exception:{ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 使能电机
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<bool> EnableMotion(ushort addr)
        {
            try
            {
                string send = MotionHelper.Enable(addr);
                string ret = await this.SendAndReceive(send).ConfigureAwait(false);
                MotionHelper.AnalysisResponseData(ret);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"EnableMotion,Exception:{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 失能电机
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<bool> DisableMotion(ushort addr)
        {
            try
            {
                string send = MotionHelper.Disable(addr);
                string ret = await this.SendAndReceive(send).ConfigureAwait(false);
                MotionHelper.AnalysisResponseData(ret);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"DisableMotion,Exception:{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 编码器清零
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<bool> SetZero(ushort addr)
        {
            try
            {
                string send = MotionHelper.SetZero(addr);
                string ret = await this.SendAndReceive(send).ConfigureAwait(false);
                MotionHelper.AnalysisResponseData(ret);
            }
            catch (Exception ex)
            {
                _logger?.Error($"SetZero,Exception:{ex.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 调用Q程序
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <param name="segment">程序号</param>
        /// <returns></returns>
        public async Task<bool> Call_Q_Program(ushort addr, ushort segment)
        {
            try
            {
                string send = MotionHelper.Call_Q_Program(addr, segment);
                string ret = await this.SendAndReceive(send).ConfigureAwait(false);
                MotionHelper.AnalysisResponseData(ret);
            }
            catch (Exception ex)
            {
                _logger?.Error($"Call_Q_Program,Exception:{ex.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 运动是否到位
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<bool> IsMoveDone(ushort addr)
        {
            try
            {
                var status = await ReadStatusCode(addr).ConfigureAwait(false);
                return (status & (int)StatusCode.InPosition) == (int)StatusCode.InPosition;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Call_Q_Program,Exception:{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 是在回零中
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<BoolResult> IsHoming(ushort addr)
        {
            BoolResult result = new BoolResult();
            try
            {
                var status = await ReadStatusCode(addr).ConfigureAwait(false);
                result.Data = (status & (int)StatusCode.Homing) == (int)StatusCode.Homing;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                result.Code = -2;
            }
            return result;
        }
        /// <summary>
        /// Q程序是否运行中
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<BoolResult> IsQ_Program_Running(ushort addr)
        {
            BoolResult result = new BoolResult();
            try
            {
                var status = await ReadStatusCode(addr).ConfigureAwait(false);
                result.Data = (status & (int)StatusCode.Q_Program_is_running) == (int)StatusCode.Q_Program_is_running;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                result.Code = -2;
            }
            return result;
        }
        /// <summary>
        /// 电机驱动是否报警
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<BoolResult> IsAlarm(ushort addr)
        {
            BoolResult result = new BoolResult();
            try
            {
                var status = await ReadStatusCode(addr).ConfigureAwait(false);
                result.Data = (status & (int)StatusCode.Alarm) == (int)StatusCode.Alarm;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                result.Code = -2;
            }
            return result;
          
        }
        /// <summary>
        /// 电机是否在运动中
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<BoolResult> IsMoving(ushort addr)
        {
            BoolResult result = new BoolResult();
            try
            {
                var status = await ReadStatusCode(addr).ConfigureAwait(false);
                result.Data = (status & (int)StatusCode.Moving) == (int)StatusCode.Moving;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                result.Code = -2;
            }
            return result;
        }
        /// <summary>
        /// 是否到达正向限位处
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<BoolResult> IsCW(ushort addr)
        {
            BoolResult result = new BoolResult();
            try
            {
                var status = await ReadAlmCode(addr).ConfigureAwait(false);
                result.Data = (status & (int)AlmCode.CW_Limit) == (int)AlmCode.CW_Limit;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                result.Code = -2;
            }
            return result;
        }
        /// <summary>
        /// 是否到达反向限位处
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<BoolResult> IsCCW(ushort addr)
        {
            BoolResult result = new BoolResult();
            try
            {
                var status = await ReadAlmCode(addr).ConfigureAwait(false);
                result.Data = (status & (int)AlmCode.CCW_Limit) == (int)AlmCode.CCW_Limit;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                result.Code = -2;
            }
            return result;
        }
        /// <summary>
        /// 电机是否使能
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<BoolResult> IsMotorEnable(ushort addr)
        {
            BoolResult result = new BoolResult();
            try
            {
                var status = await ReadStatusCode(addr).ConfigureAwait(false);
                result.Data = (status & (int)StatusCode.MotorEnable) == (int)StatusCode.MotorEnable;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                result.Code = -2;
            }
            return result;
        }

        #endregion

        #region Private Methods

        private async Task<string> SendAndReceive(string data)
        {
            //"\r"
            return await base.SendAndReceiveData(data, null, '\r');

        }


        #endregion
    }
}
