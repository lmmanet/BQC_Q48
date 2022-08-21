using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BQJX.Communication.Modbus
{
    /* 1.transmissionFlag  内存溢出
     * 2.tcpClient 异步连接超时
     * 3.检查MABP?
     * 
     */
    public class ModbusTcp : ModbusBase
    {
        #region Private Members

        private TcpClient tcpClient;
        private NetworkStream netWorkStream;
        private readonly IPAddress address;
        private readonly int port;
        private short transmissionFlag = -1;
        private readonly static object lockObj = new object();
        private readonly ILogger _logger;
        private int _timeout;

        #endregion

        #region Properties

        public bool IsDebug { get; set; } = false;

        #endregion

        #region Constructors

        public ModbusTcp(string ip,int port,ILogger logger)
        {
            this._logger = logger;
            if (!IPAddress.TryParse(ip, out address))
            {
                throw new Exception("IP地址格式不正确！");
            }
            this.port = port;
            tcpClient = new TcpClient();
        }

        #endregion

        #region Public Methods

       
        public async override Task<Result> Open(int timeout = 2000)
        {
            Result result = new Result();
            try
            {
                _logger?.Debug("打开连接");
                await tcpClient.ConnectAsync(address, port).ConfigureAwait(false);
                netWorkStream = tcpClient.GetStream();
                netWorkStream.WriteTimeout = timeout;
                netWorkStream.ReadTimeout = timeout;
                _timeout = timeout;
                result.IsSuccess = tcpClient.Connected;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Code = -2;
                result.Message = ex.Message;
                _logger?.Error($"打开连接失败! Exception:{ex}");
            }
            return result;
        }

        public override Result Close()
        {
            Result result = new Result();
            if (tcpClient!=null)
            {
                try
                {
                    tcpClient.Close();
                    tcpClient = null;
                }
                catch (Exception ex)
                {
                    result.IsSuccess = false;
                    result.Code = -2;
                    result.Message = ex.Message;
                    _logger?.Error($"关闭连接失败! Exception:{ex}");
                }
            }
            return result;
        }

        public async override Task<Result<bool>> ReadSingleCoils(byte deviceAddr, ushort startAddr)
        {
            Result<bool> result = new Result<bool>();
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x01, startAddr, 1));
                byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag,(short)reqBaseCommand.Count);
                reqBaseCommand.InsertRange(0, MABP_bytes);

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

        public async override Task<Result<List<bool>>> ReadMultiCoils(byte deviceAddr, ushort startAddr, ushort count)
        {
            Result<List<bool>> result = new Result<List<bool>>(new List<bool>());
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x01, startAddr, count));
                byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag, (short)reqBaseCommand.Count);
                reqBaseCommand.InsertRange(0, MABP_bytes);

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

        public async override Task<Result<T>> ReadSingleKeepRegister<T>(byte deviceAddr, ushort startAddr)
        {
            Result<T> result = new Result<T>();
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x03, startAddr, (byte)(1 * size / 2)));
                byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag, (short)reqBaseCommand.Count);
                reqBaseCommand.InsertRange(0, MABP_bytes);
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

        public async override Task<Result<List<T>>> ReadMultiKeepRegister<T>(byte deviceAddr, ushort startAddr, ushort count)
        {
            Result<List<T>> result = new Result<List<T>>( new List<T>());
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x03, startAddr, (byte)(count * size / 2)));
                byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag, (short)reqBaseCommand.Count);
                reqBaseCommand.InsertRange(0, MABP_bytes);
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

        public async override Task<Result<bool>> ReadSingleInputCoils(byte deviceAddr, ushort startAddr)
        {
            Result<bool> result = new Result<bool>();
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x02, startAddr, 1));
                byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag, (short)reqBaseCommand.Count);
                reqBaseCommand.InsertRange(0, MABP_bytes);

                var receive = await this.SendAndReceiveData(reqBaseCommand.ToArray()).ConfigureAwait(false);
                List<byte> respBytes = new List<byte>(receive);
                base.CheckResponseData(respBytes, null);

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

        public async override Task<Result<List<bool>>> ReadMultiInputCoils(byte deviceAddr, ushort startAddr, ushort count)
        {
            Result<List<bool>> result = new Result<List<bool>>(new List<bool>());
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x02, startAddr, count));
                byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag,(short)reqBaseCommand.Count);
                reqBaseCommand.InsertRange(0, MABP_bytes);

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

        public async override Task<Result<T>> ReadSingleInputRegister<T>(byte deviceAddr, ushort startAddr)
        {
            Result<T> result = new Result<T>();
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x04, startAddr, (byte)(1 * size / 2)));
                byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag, (short)reqBaseCommand.Count);
                reqBaseCommand.InsertRange(0, MABP_bytes);
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

        public async override Task<Result<List<T>>> ReadMultiInputRegister<T>(byte deviceAddr, ushort startAddr, ushort count)
        {
            Result<List<T>> result = new Result<List<T>>(new List<T>());
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            try
            {
                List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x04, startAddr, (byte)(count * size / 2)));
                byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag,(short)reqBaseCommand.Count);
                reqBaseCommand.InsertRange(0, MABP_bytes);
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
                byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag, (short)reqBaseCommand.Count);
                reqBaseCommand.InsertRange(0, MABP_bytes);
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
                byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag, (short)reqBaseCommand.Count);
                reqBaseCommand.InsertRange(0, MABP_bytes);
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

            if (startAddr < 10000)
            {
                try
                {
                    List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x01, startAddr, count));
                    byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag, (short)reqBaseCommand.Count);
                    reqBaseCommand.InsertRange(0, MABP_bytes);

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

            else if (startAddr < 20000)
            {
                try
                {
                    List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x02, startAddr, count));
                    byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag, (short)reqBaseCommand.Count);
                    reqBaseCommand.InsertRange(0, MABP_bytes);

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

            else if (startAddr < 40000)
            {

                try
                {
                    List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x04, startAddr, count));
                    byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag, (short)reqBaseCommand.Count);
                    reqBaseCommand.InsertRange(0, MABP_bytes);
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

            else
            {
                try
                {
                    List<byte> reqBaseCommand = new List<byte>(base.BuildReadQuest(deviceAddr, 0x03, startAddr,count ));
                    byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag, (short)reqBaseCommand.Count);
                    reqBaseCommand.InsertRange(0, MABP_bytes);
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
                //bytesList.AddRange(new byte[] {bytesValue[0],bytesValue[1] });

                List<byte> reqBaseCommand = new List<byte>(base.BuildWriteQuest(deviceAddr, funcode, starAddr, bytesList.ToArray(), 1));
                byte[] MABP_bytes = this.BuildHeader_MABP(++transmissionFlag, (short)reqBaseCommand.Count);
                reqBaseCommand.InsertRange(0, MABP_bytes);
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
        /// 发送接收报文数据
        /// </summary>
        /// <param name="sendData"></param>
        /// <returns></returns>
        protected async override Task<List<byte>> SendAndReceiveData(byte[] sendData)
        {
            if (tcpClient?.Connected == false)
            {
                lock (lockObj)
                {
                    if (tcpClient?.Connected == false)
                    {
                        var result = this.Open().GetAwaiter().GetResult();
                        if (!result.IsSuccess)
                        {
                            _logger?.Error("Tcp连接建立失败!");
                            throw new Exception("Tcp连接建立失败!");
                        }
                    }
                }
            }

            List<byte> bytesMABP = new List<byte>(6);
            List<byte> bytesModbusData;

            await netWorkStream.WriteAsync(sendData, 0, sendData.Length).ConfigureAwait(false);

            if (IsDebug)
            {
                string dataSend = "";
                sendData.ToArray().ToList().ForEach(bt => dataSend += bt.ToString("X2") + " ");
                _logger?.Debug($"DataSend:{dataSend}");
            }

            //接收数据
            lock (lockObj)
            {
                int readBackBytes = 0;
                int attemps = 0;
                while (true)
                {
                    Thread.Sleep(10);
                    byte[] buff = new byte[bytesMABP.Capacity];
                    readBackBytes += netWorkStream.Read(buff, 0, bytesMABP.Capacity - readBackBytes);
                    bytesMABP.AddRange(buff);
                    if (readBackBytes == bytesMABP.Capacity)
                    {
                        break;
                    }
                    attemps++;
                    if (attemps > _timeout/10)
                    {
                        throw new Exception("netWorkStream 接收数据超时");
                    }
                }

                //检查MABP是否正确  事务ID
                int length = bytesMABP[5] + (bytesMABP[4]<<8); //获得报文长度
                bytesModbusData = new List<byte>(length);

                readBackBytes = 0;
                attemps = 0;
                while (true)
                {
                    Thread.Sleep(10);
                    byte[] buff = new byte[bytesModbusData.Capacity];
                    readBackBytes += netWorkStream.Read(buff, 0, bytesModbusData.Capacity - readBackBytes);
                    bytesModbusData.AddRange(buff);
                    if (readBackBytes == bytesModbusData.Capacity)
                    {
                        break;
                    }
                    attemps++;
                    if (attemps > _timeout / 10)
                    {
                        throw new Exception("netWorkStream 接收数据超时");
                    }
                }

            }
         
            if (IsDebug)
            {
                string dataReceive = "";
                bytesMABP.ForEach(bt => dataReceive += bt.ToString("X2") + " ");
                bytesModbusData.ForEach(bt => dataReceive += bt.ToString("X2") + " ");
                _logger?.Debug($"DataReceive:{dataReceive}");
            }

            //校验事务编号
            var checkThrougth = Check_MABP(sendData, bytesMABP.ToArray()) ;

            //校验Modbus数据
            base.CheckResponseData(bytesModbusData, null);

            return bytesModbusData.ToList();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 生成报文头部
        /// </summary>
        /// <param name="transmission"></param>
        /// <param name="dataLength"></param>
        /// <returns></returns>
        private byte[] BuildHeader_MABP(short transmission,short dataLength)
        {
            byte[] result = new byte[6];
            result[0] = (byte)(transmission>>8);//传输标识
            result[1] = (byte)(transmission);
            result[2] = 0x00;//协议标识
            result[3] = 0x00;
            result[4] = (byte)(dataLength >> 8);//字节长度
            result[5] = (byte)(dataLength);
            return result;
        }

        /// <summary>
        /// 校验事务编号
        /// </summary>
        /// <param name="responseBytes"></param>
        /// <returns></returns>
        private bool Check_MABP(byte[] send , byte[] receive)
        {
            int sendId = (send[0] << 8) + send[1];
            int receiveId = (receive[0] << 8) + receive[1];
            _logger?.Debug($"sendId:{sendId} --- receiveId:{receiveId}");
            return sendId == receiveId;
        }

    

        #endregion
    }
}
