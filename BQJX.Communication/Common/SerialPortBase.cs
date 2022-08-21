using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BQJX.Communication
{
    public abstract class SerialPortBase
    {
        #region Private Members

        private readonly static object lockObj = new object();

        protected ILogger _logger;

        protected SerialPort serialPort;

        #endregion

        #region Properties

        public bool IsDebug { get; set; } = false;

        public int ReadTimeout { get; set; } = 1000;

        #endregion

        #region Constructors

        public SerialPortBase(ILogger logger)
        {
            this._logger = logger;
        }

        #endregion

        #region Public Methods

        public virtual async Task<Result> Open(int timeout = 5)
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

        public virtual Result Close()
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

        #endregion

        #region Protected Methods

        /// <summary>
        /// 发送接收数据
        /// </summary>
        /// <param name="sendData"></param>
        /// <returns></returns>
        protected virtual Task<List<byte>> SendAndReceiveData(byte[] sendData)
        {
            throw new NotImplementedException();
        }
       
        /// <summary>
        /// 发送接收字符
        /// </summary>
        /// <param name="sendData"></param>
        /// <returns></returns>
        protected virtual Task<string> SendAndReceiveData(string sendData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 发送接收数据
        /// </summary>
        /// <param name="sendData">需要发送的数据</param>
        /// <param name="receiveCount">需要接收的报文长度</param>
        /// <returns></returns>
        protected virtual async Task<List<byte>> SendAndReceiveData(byte[] sendData,int receiveCount)
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

            await Task.Delay(10).ConfigureAwait(false);

            List<byte> bufferList = new List<byte>(receiveCount);
           
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
                int lenthToRead = 0;
                while (flag < ReadTimeout)//是否超时
                {
                    Thread.Sleep(10);
                    flag++;
                    byte[] buffer = new byte[receiveCount];
                    lenthToRead += serialPort.Read(buffer, 0, bufferList.Capacity - lenthToRead);
                    bufferList.AddRange(buffer);
                    if (lenthToRead == bufferList.Capacity)
                    {
                        break; // 接收数据完成
                    } 
                }
            }

            if (IsDebug)
            {
                string dataReceive = "";
                bufferList.ForEach(bt => dataReceive += bt.ToString("X2") + " ");
                _logger?.Debug($"DataReceive:{dataReceive}");
            }

            return bufferList;
        }

        /// <summary>
        /// 发送接收字符
        /// </summary>
        /// <param name="sendData">要发送的字符</param>
        /// <param name="receiveStart">接收数据字符起始位</param>
        /// <param name="receiveEnd">接收数据字符结束位</param>
        /// <returns></returns>
        protected virtual async Task<string> SendAndReceiveData(string sendData,char? receiveStart,char receiveEnd)
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

            await Task.Delay(10).ConfigureAwait(false);

            lock (lockObj)
            {
                serialPort.Write(sendData);

                if (IsDebug)
                {
                    _logger?.Debug($"DataSend:{sendData}");
                }

                string sb = string.Empty;
                List<char> charBuff = new List<char>();
                int flag = 0;
                while (flag < ReadTimeout)//是否超时
                {
                    Thread.Sleep(10);
                    flag++;
                    int charRead = serialPort.ReadChar();

                    //判断是否起始字符
                    if (receiveStart != null && charBuff.Count == 0 && charRead != receiveStart.Value)
                    {
                        continue;
                    }

                    charBuff.Add((char)charRead);
                    //判断是否结束字符
                    if (receiveEnd == charRead)
                    {
                        break;
                    }

                }
                string result = new string(charBuff.ToArray());

                if (IsDebug)
                {
                    _logger?.Debug($"DataReceive:{result}");
                }

                return result;
            }

        }

        #endregion

        #region Private Methods

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
