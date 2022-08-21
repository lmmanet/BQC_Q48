using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Communication.HMLT
{
    /*-----------------注射器操作说明--------------------------------------------
 * 时间：2019-08-01
 * 描述：此为曼哈顿注射器操作，型号为PSD/4
 * 相应机器接线为： 1 24 VDC
 *                  2 RS-232 TxD line Output data
 *                  3 RS-232 RxD line Input data
 *                  4 RS-232 HI line Line is high with power on
 *                  5 CAN high signal line
 *                  6 CAN low signal line
 *                  7 Auxiliary Input #1 Digital level
 *                  8 Auxiliary Input #2 Digital level
 *                  9 Ground Power and Logic
 *                  10 Ground Power and Logic
 *                  11 RS-485 A line
 *                  12 RS-485 B line
 *                  13 Auxiliary Output #1 Digital level
 *                  14 Auxiliary Output #2 Digital level
 *                  15 Auxiliary Output #3 Digital level
 * 拨码为（Switch Circuit）：1    2    3     4     5     6     7     8
 *                           OFF                               ON    ON
 * Jumper configuration:
 * 
 * Address switch:
 * 
 * * F        命令缓冲区状态
             * Q        泵状态
             * ?        绝对位置
             * ?1       启动速度
             * ?2       最大速度
             * ?3       停止速度
             * ?4       实际位置
             * ?12      返回步数
             * ?13      输入1状态
             * ?14      输入2状态     
 * 
 * 
 * 
 * 
 * 
 * 
 -----------------------------------------------------------------------------------------------------*/
    public class Syring : SerialPortBase
    {
        #region Constructors

        public Syring(string portName, ILogger logger) : base(logger)
        {
            serialPort = new SerialPort
            {
                PortName = portName,
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One
            };
            ReadTimeout = 500;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化注射器
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <returns></returns>
        public async Task<Result> Initialize(byte addr)
        {
            Result result = new Result();
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string str = "/ZR\r\n";
                string command = str.Insert(1, address);
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                AnalysisResponseData(resp);
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
        /// 输入口吸取
        /// </summary>
        /// <param name="addr">从站地址</param>
        /// <param name="value">吸取量</param>
        /// <returns></returns>
        public async Task<Result> InAbsorb(int addr,int value)
        {
            Result result = new Result();
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string cmd = "/IPR\r\n";// "/1IPR\r\n"
                string command = cmd.Insert(1, address);
                command = command.Insert(4, value.ToString()); 
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                AnalysisResponseData(resp);
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
        /// 输出口吸取
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<Result> OutAbsorb(int addr, int value)
        {
            Result result = new Result();
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string cmd = "/OPR\r\n"; ;// "/1OPR\r\n"
                string command = cmd.Insert(1, address);
                command = command.Insert(4, value.ToString());
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                AnalysisResponseData(resp);
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
        /// 输入口注射
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<Result> InSyringe(int addr, int value)
        {
            Result result = new Result(); 
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string cmd = "/IDR\r\n";// "/1IDR\r\n"
                string command = cmd.Insert(1, address);
                command = command.Insert(4, value.ToString());
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                AnalysisResponseData(resp);
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
        /// 输出口注射
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<Result> OutSyringe(int addr, int value)
        {
            Result result = new Result();
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string cmd = "/ODR\r\n"; ;// "/1ODR\r\n"
                string command = cmd.Insert(1, address);
                command = command.Insert(4, value.ToString());
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                AnalysisResponseData(resp);
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
        /// 输入口阀门打开
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public async Task<Result> ValveIn(int addr)
        {
            Result result = new Result();
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string cmd = "/IR\r\n";// "/1IR\r\n"
                string command = cmd.Insert(1, address);
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                AnalysisResponseData(resp);
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
        /// 输出口阀门打开
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public async Task<Result> ValveOut(int addr)
        {
            Result result = new Result();
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string cmd = "/OR\r\n";// "/1OR\r\n"
                string command = cmd.Insert(1, address);
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                AnalysisResponseData(resp);
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
        /// 同时打开输入输出口
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public async Task<Result> ValveInOut(int addr)
        {
            Result result = new Result();
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string cmd = "/sR\r\n";//  "/1sR\r\n"
                string command = cmd.Insert(1, address);
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                AnalysisResponseData(resp);
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
        /// 标准精度 3000
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public async Task<Result> StandardPrecision(int addr)
        {   
            Result result = new Result();
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string cmd = "/N0R\r\n";//  "/1N0R\r\n"
                string command = cmd.Insert(1, address);
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                AnalysisResponseData(resp);
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
        /// 高精度 24000
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public async Task<Result> HighPrecision(int addr)
        {
            Result result = new Result();
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string cmd = "/N1R\r\n";//  "/1N1R\r\n"
                string command = cmd.Insert(1, address);
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                AnalysisResponseData(resp);
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
        /// 停止动作
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public async Task<Result> SyringeStop(int addr)
        {
            Result result = new Result();
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string cmd = "/TR\r\n";//  "/1TR\r\n"
                string command = cmd.Insert(1, address);
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                AnalysisResponseData(resp);
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
        /// 设定速度
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value">速度代码1-40</param>
        /// <returns></returns>
        public async Task<Result> VelocitySet(int addr, int value)
        {
            Result result = new Result();
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string cmd = "/VR\r\n";//  "/1VR\r\n"
                string command = cmd.Insert(1, address);
                command = cmd.Insert(3, value.ToString());
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                AnalysisResponseData(resp);
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
        /// 读取注射器位置
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public async Task<Result<int>> ReadPosition(int addr)
        {
            Result<int> result = new Result<int>(-1);
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string cmd = "/?\r\n";//  "/1?\r\n"
                string command = cmd.Insert(1, address);
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                string str = (string)AnalysisResponseData(resp);
                result.Data = int.Parse(str);
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
        /// 读取泵状态
        /// </summary>
        /// <param name="addr"></param>
        /// <returns>是否准备好（完成动作）</returns>
        public async Task<BoolResult> QueryStatus(int addr)
        {
            BoolResult result = new BoolResult();
            try
            {
                byte byteAddr = (byte)(addr + 48);
                string address = Encoding.ASCII.GetString(new byte[] { byteAddr });
                string cmd = "/Q\r\n";//  "/1?\r\n"
                string command = cmd.Insert(1, address);
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                bool IsReady = (bool)AnalysisResponseData(resp);
                result.Data = IsReady;
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
        /// 用户自定义命令报文
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<Result> UserDefineCommand(string command)
        {
            Result result = new Result();
            try
            {
                var resp = await SendAndReceive(command).ConfigureAwait(false);
                AnalysisResponseData(resp);
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
        /// 用户自定义请求数据
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<Result<string>> UserDefineQuery(string query)
        {
            Result<string> result = new Result<string>(true, 0, "OK", "");
            try
            {
                var resp = await SendAndReceive(query).ConfigureAwait(false);
                result.Data=(string)AnalysisResponseData(resp);
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
        /// 测试命令
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public async Task<Result<string>> CommandTest(string cmd)
        {
            Result<string> result = new Result<string>(true, 0, "OK", "");
            try
            {
                var resp = await SendAndReceive(cmd).ConfigureAwait(false);
                result.Data = AnalysisResponseData(resp) as string;
                result.Data = $"Send:{cmd}Receive:{resp}Data:{result.Data}";
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

        /// <summary>
        /// 解析收到数据
        /// </summary>
        /// <param name="mes"></param>
        /// <returns></returns>
        private object AnalysisResponseData(string mes)
        {
            if (string.IsNullOrEmpty(mes)||string.IsNullOrWhiteSpace(mes))
            {
                throw new Exception("数据接收超时！ 返回数据为空");
            }
            byte[] bytes = Encoding.ASCII.GetBytes(mes);
            if (bytes[bytes.Length - 2] == 0x0d && bytes[bytes.Length - 1] == 0x0a)
            {
                if ((bytes[2] & 0x0F) != 0x00)//发生错误
                {
                    int code = (bytes[2] & 0x0F);
                    switch (code)
                    {
                        case 1:
                            throw new Exception("Initialization error!");
                        case 2:
                            throw new Exception("Invalid command!");
                        case 3:
                            throw new Exception("Parameter out of range!");
                        case 4:
                            throw new Exception("Too many loops!");
                        case 6:
                            throw new Exception("EEPROM error!");
                        case 7:
                            throw new Exception("Syringe not initialized!");
                        case 9:
                            throw new Exception("Syringe overload!");
                        case 10:
                            throw new Exception("Valve overload!");
                        case 11:
                            throw new Exception("Syringe move not allowed!");
                        case 15:
                            throw new Exception("PSD/4 busy error!");
                        default:
                            throw new Exception($"位置错误{code}");
                    }
                }
                if (bytes.Length == 6)//解析状态
                {
                    bool b = (bytes[2] & 0x20) == 0x20;
                    return b;
                   
                }
                else if (bytes.Length >= 7)//解析位置数据
                {
                    byte[] datas = new byte[bytes.Length - 6];
                    Array.Copy(bytes,3, datas,0, datas.Length);
                    return Encoding.ASCII.GetString(datas);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                throw new Exception("返回数据错误");
            }
        }

        /// <summary>
        /// 发送接收数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private async Task<string> SendAndReceive(string data)
        {
            //"\r\n"    0x0d,0x0a
            return await base.SendAndReceiveData(data, null, '\n');
   
        }

        #endregion
        
    }
}
