using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BQJX.Communication.Modbus
{
    //多线程读取未测试
    public class ModbusRtu : ModbusBase 
    {
        #region Private Members

        private SerialPort serialPort;
        private readonly object lockObj = new object();
        private readonly ILogger _logger;

        #endregion

        #region Constructors

        /// <summary>
        /// 初始化串口参数
        /// </summary>
        /// <param name="spParams"></param>
        public ModbusRtu(SerialPortParams spParams , ILogger logger)
        {
            this._logger = logger;
            serialPort = new SerialPort
            {
                PortName = spParams.PortName,
                BaudRate = spParams.BaudRate,
                Parity = (Parity)spParams.Parity,
                DataBits = spParams.DataBits,
                StopBits = (StopBits)spParams.StopBits,
                Encoding = Encoding.ASCII,
                ReceivedBytesThreshold = 1
            };

        }

        #endregion

        #region Public Methods

        public override async Task<Result> Open(int timeout = 5)
        {
            Result result = new Result();
            try
            {
                int count = 0;
                while (count < timeout)
                {
                    try
                    {
                        _logger?.Debug($"打开串口,Timeout:{timeout}");
                        this.OpenSerialPort();
                        result.IsSuccess = serialPort.IsOpen;
                        return result;
                    }
                    catch (System.IO.IOException)
                    {
                        await Task.Delay(100).ConfigureAwait(false);
                        count++;
                    }
                }
                if (serialPort == null || !serialPort.IsOpen)
                {
                    throw new Exception("串口打开失败");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"串口打开失败,Exception:{ex}");
                result.IsSuccess = false;
                result.Code = -1;
                result.Message = ex.Message;
            }
            return result;
        }

        public override Result Close()
        {
            Result result = new Result();
            try
            {
                _logger?.Debug("关闭串口");
                if (serialPort != null && serialPort.IsOpen)
                    serialPort.Close();
                serialPort = null;

                return result;
            }
            catch (Exception ex)
            {
                _logger?.Error($"关闭串口出错，Exception:{ex}");
                result.IsSuccess = false;
                result.Code = -2;
                result.Message = ex.Message;
            }

            return result;
        }

        public override async Task<Result<bool>> ReadSingleCoils(byte deviceAddr, ushort startAddr)
        {
            Result<bool> result = new Result<bool>(false);
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x01, startAddr, 1));
                reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
                List<byte> respBytes = new List<byte>(receive);

                respBytes.RemoveRange(0, 3);
                result.Data = base.AnalysisData<bool>(respBytes, 1)[0];

            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Code = -2;
                result.Message = ex.Message;
                _logger?.Error($"ReadSingleCoils 从站：{deviceAddr}" + ex.Message);
            }
            return result;
        }

        public override async Task<Result<List<bool>>> ReadMultiCoils(byte deviceAddr, ushort startAddr, ushort count)
        {
            Result<List<bool>> result = new Result<List<bool>>(new List<bool>());
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x01, startAddr, count));
                reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
                List<byte> respBytes = new List<byte>(receive);

                respBytes.RemoveRange(0, 3);
                result.Data = base.AnalysisData<bool>(respBytes, count);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Code = -2;
                result.Message = ex.Message;
                _logger?.Error($"ReadMultiCoils 从站：{deviceAddr}" + ex.Message);
            }
            return result;
        }

        public override async Task<Result<T>> ReadSingleKeepRegister<T>(byte deviceAddr, ushort startAddr)
        {
            Result<T> result = new Result<T>(default);
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x03, startAddr, (byte)(1 * size / 2)));
                reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
                List<byte> respBytes = new List<byte>(receive);

                respBytes.RemoveRange(0, 3);
                result.Data = base.AnalysisData<T>(respBytes, 1)[0];
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Code = -2;
                result.Message = ex.Message;
                _logger?.Error($"ReadSingleKeepRegister 从站：{deviceAddr}" + ex.Message);
            }
            return result;
        }

        public override async Task<Result<List<T>>> ReadMultiKeepRegister<T>(byte deviceAddr, ushort startAddr, ushort count)
        {
            Result<List<T>> result = new Result<List<T>>(new List<T>());
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x03, startAddr, (byte)(count * size / 2)));
                reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
                List<byte> respBytes = new List<byte>(receive);

                respBytes.RemoveRange(0, 3);
                result.Data = base.AnalysisData<T>(respBytes, count);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Code = -2;
                result.Message = ex.Message;
                _logger?.Error($"ReadMultiKeepRegister 从站：{deviceAddr}" + ex.Message);
            }
            return result;
        }

        public override async Task<Result<bool>> ReadSingleInputCoils(byte deviceAddr, ushort startAddr)
        {
            Result<bool> result = new Result<bool>(false);
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x02, startAddr, 1));
                reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
                List<byte> respBytes = new List<byte>(receive);

                respBytes.RemoveRange(0, 3);
                result.Data = base.AnalysisData<bool>(respBytes, 1)[0];
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Code = -2;
                result.Message = ex.Message;
                _logger?.Error($"ReadSingleInputCoils 从站：{deviceAddr}" + ex.Message);
            }
            return result;
        }

        public override async Task<Result<List<bool>>> ReadMultiInputCoils(byte deviceAddr, ushort startAddr, ushort count)
        {
            Result<List<bool>> result = new Result<List<bool>>(new List<bool>());
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x02, startAddr, count));
                reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
                List<byte> respBytes = new List<byte>(receive);

                respBytes.RemoveRange(0, 3);
                result.Data = base.AnalysisData<bool>(respBytes, count);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Code = -2;
                result.Message = ex.Message;
                _logger?.Error($"ReadMultiInputCoils 从站：{deviceAddr}" + ex.Message);
            }
            return result;
        }

        public override async Task<Result<T>> ReadSingleInputRegister<T>(byte deviceAddr, ushort startAddr)
        {
            Result<T> result = new Result<T>(default);
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x04, startAddr, (byte)(1 * size / 2)));
                reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
                List<byte> respBytes = new List<byte>(receive);

                respBytes.RemoveRange(0, 3);
                result.Data = base.AnalysisData<T>(respBytes, 1)[0];
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Code = -2;
                result.Message = ex.Message;
                _logger?.Error($"ReadSingleInputRegister 从站：{deviceAddr}" + ex.Message);
            }
            return result;
        }

        public override async Task<Result<List<T>>> ReadMultiInputRegister<T>(byte deviceAddr, ushort startAddr, ushort count)
        {
            Result<List<T>> result = new Result<List<T>>(new List<T>());
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x04, startAddr, (byte)(count * size / 2)));
                reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
                List<byte> respBytes = new List<byte>(receive);

                respBytes.RemoveRange(0, 3);
                result.Data = base.AnalysisData<T>(respBytes, count);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Code = -2;
                result.Message = ex.Message;
                _logger?.Error($"ReadMultiInputRegister 从站：{deviceAddr}" + ex.Message);
            }
            return result;
        }

        public override async Task<Result> WriteCoils(byte deviceAddr, ushort startAddr, params bool[] values)
        {
            Result result = new Result();
            byte funcode = 0x05;
            if (values.Length > 1)
            {
                funcode = 0x0f;
            }
            try
            {
                var bytesValue = base.BoolArrayToBytes(values);
                List<byte> reqBaseCommand = new List<byte>(base.BuildWriteQuest(deviceAddr, funcode, startAddr, bytesValue, (byte)values.Length));
                reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Code = -2;
                result.Message = ex.Message;
                _logger?.Error($"WriteCoils 从站：{deviceAddr}" + ex.Message);
            }
            return result;
        }

        public override async Task<Result> WriteKeepRegister<T>(byte deviceAddr, ushort startAddr,params T[] values)
        {
            Result result = new Result();
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            ushort regCount = (ushort)(values.Length * size / 2);
            byte funcode = 0x06;
            if (values.Length > 1 || size > 2)
            {
                funcode = 0x10;
            }
            try
            {
                List<byte> bytesList = new List<byte>();
                foreach (var value in values)
                {
                    var bytesValue = base.ToBytes(value);
                    bytesList.AddRange(bytesValue);
                }
                List<byte> reqBaseCommand = new List<byte>(base.BuildWriteQuest(deviceAddr, funcode, startAddr, bytesList.ToArray(), regCount));
                reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
        
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Code = -2;
                result.Message = ex.Message;
                _logger?.Error($"WriteKeepRegister 从站：{deviceAddr}" + ex.Message);
            }
            return result;
        }

        public override async Task<Result<List<byte>>> ReadBytesFromSlave(byte deviceAddr, ushort startAddr, ushort count)
        {
            Result<List<byte>> result = new Result<List<byte>>(new List<byte>());
            //输入线圈
            if (startAddr > 10000 && startAddr < 20000)
            {
                try
                {
                    startAddr = (ushort)(startAddr - 10000);
                    List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x01, startAddr, count));
                    reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                    var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
                    List<byte> respBytes = new List<byte>(receive);

                    respBytes.RemoveRange(0, 3);
                    result.Data = respBytes;
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.Code = -2;
                    result.Message = ex.Message;
                    _logger?.Error($"ReadBytesFromSlave 从站：{deviceAddr}" + ex.Message);
                }
                return result;
            }
            //输入寄存器
            else if(startAddr > 20000 && startAddr < 30000)
            {
                try
                {
                    startAddr = (ushort)(startAddr - 20000);
                    List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x02, startAddr, count));
                    reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                    var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
                    List<byte> respBytes = new List<byte>(receive);

                    respBytes.RemoveRange(0, 3);
                    result.Data = respBytes;
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.Code = -2;
                    result.Message = ex.Message;
                    _logger?.Error($"ReadBytesFromSlave 从站：{deviceAddr}" + ex.Message);
                }
                return result;
            }
            //保存寄存器
            else if(startAddr > 40000)
            {
               
                try
                {
                    startAddr = (ushort)(startAddr - 40000);
                    List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x04, startAddr, count));
                    reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                    var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
                    List<byte> respBytes = new List<byte>(receive);

                    respBytes.RemoveRange(0, 3);
                    result.Data = respBytes;
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.Code = -2;
                    result.Message = ex.Message;
                    _logger?.Error($"ReadBytesFromSlave 从站：{deviceAddr}" + ex.Message);
                }
                return result;
            }
            //线圈
            else
            {

                try
                {
                    List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x03, startAddr, count));
                    reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                    var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
                    List<byte> respBytes = new List<byte>(receive);

                    respBytes.RemoveRange(0, 3);
                    result.Data = respBytes;
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.Code = -2;
                    result.Message = ex.Message;
                    _logger?.Error($"ReadBytesFromSlave 从站：{deviceAddr}" + ex.Message);
                }
                return result;
            }

        }

        public override async Task<Result> WriteKeepRegisterMulti<T>(byte deviceAddr, ushort starAddr, T value)
        {
            Result result = new Result();
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            byte funcode = 0x10;
            try
            {
                List<byte> bytesList = new List<byte>();
              
                var bytesValue = base.ToBytes(value);
                bytesList.AddRange(bytesValue);
                //bytesList.AddRange(new byte[] { bytesValue[0] , bytesValue[1] });

                List<byte> reqBaseCommand = new List<byte>(base.BuildWriteQuest(deviceAddr, funcode, starAddr, bytesList.ToArray(), 1));
                reqBaseCommand.AddRange(this.Calculate_CRC16(reqBaseCommand));
                var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Code = -2;
                result.Message = ex.Message;
                _logger?.Error($"WriteKeepRegisterMulti 从站：{deviceAddr}" + ex.Message);
            }
            return result;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// 发送接收数据
        /// </summary>
        /// <param name="sendData"></param>
        /// <returns></returns>
        protected override async Task<List<byte>> SendAndReceiveData(byte[] sendData)
        {
            if (!serialPort.IsOpen)//断线重连
            {
                lock (lockObj)
                {
                    if (!serialPort.IsOpen)
                    {
                        var isOpen = this.Open(1).GetAwaiter().GetResult();
                        if (!isOpen.IsSuccess)
                        {
                            throw new Exception(isOpen.Message);
                        }
                    }
                }
            }

            List<byte> bufferList = new List<byte>();
            await Task.Run(() =>
            {
                lock (lockObj)
                {
                    serialPort.Write(sendData, 0, sendData.Length);

                    if (IsDebug)
                    {
                        string dataSend = "";
                        sendData.ToList().ForEach(bt => dataSend += bt.ToString("X2") + " ");
                        _logger?.Debug($"DataSend:{dataSend}");
                    }

                    int flag = 0;
                    while (flag < ReadTimeout)//是否超时
                    {
                        Thread.Sleep(100);
                        flag++;
                        byte[] buffer = new byte[serialPort.BytesToRead];
                        int lenthToRead = serialPort.Read(buffer, 0, buffer.Length);
                        bufferList.AddRange(buffer);
                        if (bufferList.Count >= 3)
                        {
                            byte funcCode = bufferList[1];
                            if ((bufferList[1] & 0x80) == 0x80)//通讯异常情况
                            {
                                if (bufferList.Count == 5)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (funcCode < 0x05)//报文为读取数据请求
                                {
                                    int length = bufferList[2] + 5;
                                    if (length == bufferList.Count)//读取返回数据长度
                                    {
                                        break;
                                    }
                                }
                                else //报文为写入数据请求
                                {
                                    if (bufferList.Count == 8)//写入数据返回标准8个字节
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }).ConfigureAwait(false);

            base.CheckResponseData(bufferList, Check_CRC);

            if (IsDebug)
            {
                string dataReceive = "";
                bufferList.ForEach(bt => dataReceive += bt.ToString("X2") + " ");
                _logger?.Debug($"DataReceive:{dataReceive}");
            }
            return bufferList;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 校验CRC码
        /// </summary>
        /// <param name="responseBytes"></param>
        /// <returns></returns>
        private bool Check_CRC(List<byte> responseBytes)
        {
            List<byte> checkCRC = responseBytes.GetRange(responseBytes.Count - 2, 2);
            responseBytes.RemoveRange(responseBytes.Count - 2, 2);
            var calcCRC = Calculate_CRC16(responseBytes);
            return checkCRC.SequenceEqual(calcCRC);
        }

        /// <summary>
        /// 计算CRC码
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private List<byte> Calculate_CRC16(List<byte> value)
        {
            ushort poly = 0xA001;
            ushort crcInit = 0xFFFF;

            if (value == null || !value.Any())
                throw new ArgumentException("");

            ushort crc = crcInit;
            for (int i = 0; i < value.Count; i++)
            {
                crc = (ushort)(crc ^ (value[i]));
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ poly) : (ushort)(crc >> 1);
                }
            }
            byte hi = (byte)((crc & 0xFF00) >> 8);  //高位置
            byte lo = (byte)(crc & 0x00FF);         //低位置

            List<byte> buffer = new List<byte>
            {
                lo,
                hi
            };
            return buffer;
        }

        /// <summary>
        /// 打开串口
        /// </summary>
        /// <returns></returns>
        private bool OpenSerialPort()
        {
            if (serialPort == null)
            {
                throw new Exception("串口对象未初始化");
            }
            if (serialPort.IsOpen)
            {
                return true;
            }
            serialPort.Open();
            return true;
        }

      

        #endregion
    }
}
