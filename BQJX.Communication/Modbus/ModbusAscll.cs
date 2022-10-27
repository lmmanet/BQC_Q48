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
    public class ModbusAscll : ModbusBase
    {
        #region Private Members

        private SerialPort serialPort;
        private readonly static object lockObj = new object();
        private readonly ILogger _logger;

        #endregion

        #region Construtors

        public ModbusAscll(SerialPortParams spParams, ILogger logger)
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
                reqBaseCommand.Add(this.LRC(reqBaseCommand));
                var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);

                receive.RemoveRange(0, 3);
                result.Data = base.AnalysisData<bool>(receive, 1)[0];
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

        public override async Task<Result<List<bool>>> ReadMultiCoils(byte deviceAddr, ushort startAddr,ushort count)
        {
            Result<List<bool>> result = new Result<List<bool>>(new List<bool>());
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x01, startAddr, count));
                reqBaseCommand.Add(this.LRC(reqBaseCommand));
                var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);

                receive.RemoveRange(0, 3);
                result.Data = base.AnalysisData<bool>(receive, count);
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

        public async override Task<Result<T>> ReadSingleKeepRegister<T>(byte deviceAddr, ushort startAddr)
        {
            Result<T> result = new Result<T>();
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x03, startAddr, (byte)(1 * size / 2)));
                reqBaseCommand.Add(this.LRC(reqBaseCommand));
                var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);

                receive.RemoveRange(0, 3);
                result.Data = base.AnalysisData<T>(receive, 1)[0];
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

        public async override Task<Result<List<T>>> ReadMultiKeepRegister<T>(byte deviceAddr, ushort startAddr, ushort count)
        {
            Result<List<T>> result = new Result<List<T>>(new List<T>());
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x03, startAddr, (byte)(count * size / 2)));
                reqBaseCommand.Add(this.LRC(reqBaseCommand));
                var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);

                receive.RemoveRange(0, 3);
                result.Data = base.AnalysisData<T>(receive, count);
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

        public async override Task<Result<bool>> ReadSingleInputCoils(byte deviceAddr, ushort startAddr)
        {
            Result<bool> result = new Result<bool>();
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x02, startAddr, 1));
                reqBaseCommand.Add(this.LRC(reqBaseCommand));
                var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);

                receive.RemoveRange(0, 3);
                result.Data = base.AnalysisData<bool>(receive, 1)[0];
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

        public async override Task<Result<List<bool>>> ReadMultiInputCoils(byte deviceAddr, ushort startAddr, ushort count)
        {
            Result<List<bool>> result = new Result<List<bool>>(new List<bool>());
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x02, startAddr, count));
                reqBaseCommand.Add(this.LRC(reqBaseCommand));
                var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);

                receive.RemoveRange(0, 3);
                result.Data = base.AnalysisData<bool>(receive, count);
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

        public async override Task<Result<T>> ReadSingleInputRegister<T>(byte deviceAddr, ushort startAddr)
        {
            Result<T> result = new Result<T>();
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x04, startAddr, (byte)(1 * size / 2)));
                reqBaseCommand.Add(this.LRC(reqBaseCommand));
                var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);

                receive.RemoveRange(0, 3);
                result.Data = base.AnalysisData<T>(receive, 1)[0];
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

        public async override Task<Result<List<T>>> ReadMultiInputRegister<T>(byte deviceAddr, ushort startAddr, ushort count)
        {
            Result<List<T>> result = new Result<List<T>>(new List<T>());
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x04, startAddr, (byte)(count * size / 2)));
                reqBaseCommand.Add(this.LRC(reqBaseCommand));
                var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);

                receive.RemoveRange(0, 3);
                result.Data = base.AnalysisData<T>(receive, count);
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

        public async override Task<Result> WriteCoils(byte deviceAddr, ushort starAddr, params bool[] values)
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
                List<byte> reqBaseCommand = new List<byte>(base.BuildWriteQuest(deviceAddr, funcode, starAddr, bytesValue, (byte)values.Length));
                reqBaseCommand.Add(this.LRC(reqBaseCommand));
                var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);
        
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

        public async override Task<Result> WriteKeepRegister<T>(byte deviceAddr, ushort starAddr, params T[] values)
        {
            Result result = new Result();
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            ushort regCount = (ushort)(values.Length * size / 2);

            byte funcode = 0x06;
            if (values.Length > 1)
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
                List<byte> reqBaseCommand = new List<byte>(base.BuildWriteQuest(deviceAddr, funcode, starAddr, bytesList.ToArray(), regCount));
                reqBaseCommand.Add(this.LRC(reqBaseCommand));
                var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);
               
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

            if (startAddr < 10000)
            {
                try
                {
                    List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x01, startAddr, count));
                    reqBaseCommand.Add(this.LRC(reqBaseCommand));
                    var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);

                    receive.RemoveRange(0, 3);
                    result.Data = receive;
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

            else if (startAddr < 20000)
            {
                try
                {
                    List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x02, startAddr, count));
                    reqBaseCommand.Add(this.LRC(reqBaseCommand));
                    var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);

                    receive.RemoveRange(0, 3);
                    result.Data = receive;
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

            else if (startAddr < 40000)
            {

                try
                {
                    List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x04, startAddr, count ));
                    reqBaseCommand.Add(this.LRC(reqBaseCommand));
                    var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);

                    receive.RemoveRange(0, 3);
                    result.Data = receive;
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

            else
            {
                try
                {
                    List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x03, startAddr, count));
                    reqBaseCommand.Add(this.LRC(reqBaseCommand));
                    var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);

                    receive.RemoveRange(0, 3);
                    result.Data = receive;
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
                //bytesList.AddRange(new byte[] { bytesValue[0], bytesValue[1] });

                List<byte> reqBaseCommand = new List<byte>(base.BuildWriteQuest(deviceAddr, funcode, starAddr, bytesList.ToArray(), 1));
                reqBaseCommand.Add(this.LRC(reqBaseCommand));
                var receive = await this.SendAndReceiveData(BuildAscllMessage(reqBaseCommand).ToArray()).ConfigureAwait(false);

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

        protected async override Task<List<byte>> SendAndReceiveData(byte[] sendData)
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
                        sendData.ToArray().ToList().ForEach(bt => dataSend += bt.ToString("X2") + " ");
                        _logger?.Debug($"DataSend:{dataSend}");
                    }

                    int flag = 0;
                    while (flag < ReadTimeout)//是否超时
                    {
                        Thread.Sleep(10);
                        flag++;
                        byte[] buffer = new byte[serialPort.BytesToRead];
                        int lenthToRead = serialPort.Read(buffer, 0, buffer.Length);
                        bufferList.AddRange(buffer);
                        int listCount = bufferList.Count;
                        if (listCount > 3)//接收长度足够头部和尾部数量
                        {
                            if (bufferList[0] == 0x3A && bufferList[listCount - 2] == 0x0d && bufferList[listCount - 1] == 0x0a)
                            {
                                break;
                            }
                        }
                    }
                   
                }
            }).ConfigureAwait(false);

            if (IsDebug)
            {
                string dataReceive = "";
                bufferList.ForEach(bt => dataReceive += bt.ToString("X2") + " ");
                _logger?.Debug($"DataReceive:{dataReceive}");
            }

            List<byte> respBytes = ParseAscllMessage(bufferList);
            base.CheckResponseData(ParseAscllMessage(respBytes), Check_LRC);
            return respBytes;
        }

        #endregion

        #region Private Methods

        private bool Check_LRC(List<byte> responseBytes)
        {
            byte checkLRC = responseBytes[responseBytes.Count - 1];
            responseBytes.RemoveRange(responseBytes.Count - 1, 1);
            byte calcLRC = LRC(responseBytes);
            return checkLRC == calcLRC;
        }

        private byte LRC(List<byte> value)
        {
            if (value == null) return 0;

            int sum = 0;
            for (int i = 0; i < value.Count; i++)
            {
                sum += value[i];
            }

            sum %= 256;
            sum = 256 - sum;

            return (byte)sum;
        }

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

        private List<byte> BuildAscllMessage(List<byte> data)
        {
            string[] strs = data.Select(b => b.ToString("X2")).ToArray();
            byte[] bytes = Encoding.ASCII.GetBytes(string.Join("", strs.Select(s => s)));
            List<byte> bytesList = new List<byte>(bytes);
            bytesList.Insert(0, 0x3a);
            bytesList.Add(0x0d);
            bytesList.Add(0x0a);
            return bytesList;
        }

        private List<byte> ParseAscllMessage(List<byte> data)
        {
            data.RemoveAt(0);
            data.RemoveRange(data.Count - 2, 2);
            List<byte> bytesList = new List<byte>();
            string str = Encoding.ASCII.GetString(data.ToArray());
            for (int i = 0; i < str.Length; i++)
            {
                byte b = byte.Parse(str.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                i++;
                bytesList.Add(b);
            }
            return bytesList;
        }

        #endregion
    }
}
