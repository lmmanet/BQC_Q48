using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Communication.Modbus
{
    public abstract class ModbusBase : IModbusBase
    {
        public int ReadTimeout { get; set; } = 1000;
        public EndianType EndianType { get; set; } = EndianType.ABCD;

        protected static Dictionary<int, string> Errors = new Dictionary<int, string>
        {
            { 0x01, "非法功能码"},
            { 0x02, "非法数据地址"},
            { 0x03, "非法数据值"},
            { 0x04, "从站设备故障"},
            { 0x05, "确认，从站需要一个耗时操作"},
            { 0x06, "从站忙"},
            { 0x08, "存储奇偶性差错"},
            { 0x0A, "不可用网关路径"},
            { 0x0B, "网关目标设备响应失败"},
        };
        public abstract Task<Result> Open(int timeout = 2000);
        public abstract Result Close();

        #region Properties

        public bool IsDebug { get; set; } = false;

        #endregion

        //=======================================================================================================//

        /// <summary>
        /// 读单线圈（PLC地址 00001~09999）
        /// </summary>
        /// <param name="deviceAddr">从站地址</param>
        /// <param name="startAddr">起始地址</param>
        /// <returns></returns>
        public abstract Task<Result<bool>> ReadSingleCoils(byte deviceAddr, ushort startAddr);

        /// <summary>
        /// 读多线圈（PLC地址 00001~09999）
        /// </summary>
        /// <param name="deviceAddr">从站地址</param>
        /// <param name="startAddr">起始地址</param>
        /// <param name="count">线圈个数</param>
        /// <returns></returns>
        public abstract Task<Result<List<bool>>> ReadMultiCoils(byte deviceAddr, ushort startAddr, ushort count);

        /// <summary>
        /// 读单输入线圈（PLC地址 10001~19999）
        /// </summary>
        /// <param name="deviceAddr">从站地址</param>
        /// <param name="startAddr">起始地址</param>
        /// <returns></returns>
        public abstract Task<Result<bool>> ReadSingleInputCoils(byte deviceAddr, ushort startAddr);

        /// <summary>
        /// 读多输入线圈（PLC地址 10001~19999）
        /// </summary>
        /// <param name="deviceAddr">从站地址</param>
        /// <param name="startAddr">起始地址</param>
        /// <param name="count">线圈个数</param>
        /// <returns></returns>
        public abstract Task<Result<List<bool>>> ReadMultiInputCoils(byte deviceAddr, ushort startAddr, ushort count);

        /// <summary>
        /// 读单输入寄存器（PLC地址 30001~39999）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="deviceAddr">从站地址</param>
        /// <param name="startAddr">起始地址</param>
        /// <returns></returns>
        public abstract Task<Result<T>> ReadSingleInputRegister<T>(byte deviceAddr, ushort startAddr);

        /// <summary>
        /// 读多输入寄存器（PLC地址 30001~39999）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="deviceAddr">从站地址</param>
        /// <param name="startAddr">起始地址</param>
        /// <param name="count">数据个数</param>
        /// <returns></returns>
        public abstract Task<Result<List<T>>> ReadMultiInputRegister<T>(byte deviceAddr, ushort startAddr, ushort count);

        /// <summary>
        /// 读单保持寄存器 （PLC地址 40001~49999）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="deviceAddr">从站地址</param>
        /// <param name="startAddr">起始地址</param>
        /// <param name="count">数据个数</param>
        /// <returns></returns>
        public abstract Task<Result<T>> ReadSingleKeepRegister<T>(byte deviceAddr, ushort startAddr);

        /// <summary>
        /// 读多保持寄存器 （PLC地址 40001~49999）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="deviceAddr">从站地址</param>
        /// <param name="startAddr">起始地址</param>
        /// <param name="count">数据个数</param>
        /// <returns></returns>
        public abstract Task<Result<List<T>>> ReadMultiKeepRegister<T>(byte deviceAddr, ushort startAddr, ushort count);

        /// <summary>
        /// 写单/多个线圈（PLC地址 00001~09999）
        /// </summary>
        /// <param name="deviceAddr">从站地址</param>
        /// <param name="starAddr">起始地址</param>
        /// <param name="values">布尔数组</param>
        /// <returns></returns>
        public abstract Task<Result> WriteCoils(byte deviceAddr, ushort starAddr, params bool[] values);

        /// <summary>
        /// 写保持寄存器 （PLC地址 40001~49999）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="deviceAddr">从站地址</param>
        /// <param name="starAddr">起始地址</param>
        /// <param name="values">数值数组</param>
        /// <returns></returns>
        public abstract Task<Result> WriteKeepRegister<T>(byte deviceAddr, ushort starAddr, params T[] values);

        /// <summary>
        /// 读取从站寄存器数据
        /// </summary>
        /// <param name="deviceAddr">从站ID</param>
        /// <param name="startAddr">起始地址
        /// 线圈（PLC地址 00001~09999）
        /// 输入线圈（PLC地址 10001~19999）
        /// 输入寄存器（PLC地址 30001~39999）
        /// 保持寄存器 （PLC地址 40001~49999）
        /// </param>
        /// <param name="count">寄存器个数</param>
        /// <returns></returns>
        public abstract Task<Result<List<byte>>> ReadBytesFromSlave(byte deviceAddr, ushort startAddr, ushort count);

        /// <summary>
        /// 使用0x10功能码写单寄存器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="deviceAddr"></param>
        /// <param name="starAddr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract Task<Result> WriteKeepRegisterMulti<T>(byte deviceAddr, ushort starAddr, T value);

        //===============================================================================================================================//

        /// <summary>
        /// 发送接收报文
        /// </summary>
        /// <param name="sendData"></param>
        /// <returns></returns>
        protected abstract Task<List<byte>> SendAndReceiveData(byte[] sendData);

        /// <summary>
        /// 生成读取数据请求报文
        /// </summary>
        /// <param name="deviceAddr"></param>
        /// <param name="funcCode">01 读线圈状态 02 读输入线圈状态 03 读保持寄存器 04 读输入寄存器</param>
        /// <param name="startAddr"></param>
        /// <param name="length">最大125个</param>
        /// <returns></returns>
        protected byte[] BuildReadQuest(byte deviceAddr, byte funcCode, ushort startAddr, ushort length)
        {
            List<byte> command = new List<byte>
            {
                deviceAddr,
                funcCode,
                BitConverter.GetBytes(startAddr)[1],
                BitConverter.GetBytes(startAddr)[0],
                BitConverter.GetBytes(length)[1],
                BitConverter.GetBytes(length)[0]
            };
            return command.ToArray();
        }

        /// <summary>
        /// 生成写入数据请求报文
        /// </summary>
        /// <param name="deviceAddr"></param>
        /// <param name="funcCode">05 写单线圈 0F 写多线圈 06 写单保持寄存器 10 写多保持寄存器</param>
        /// <param name="startAddr"></param>
        /// <param name="length">最大125个</param>
        /// <returns></returns>
        protected byte[] BuildWriteQuest(byte deviceAddr, byte funcCode, ushort startAddr, byte[] values, ushort writeCount)
        {
            List<byte> command = new List<byte>
            {
                deviceAddr,
                funcCode,
                BitConverter.GetBytes(startAddr)[1],
                BitConverter.GetBytes(startAddr)[0]
            };
            if (funcCode == 0x0f)
            {
                command.Add((byte)(writeCount >> 8));//写入线圈数量 Hi
                command.Add((byte)writeCount);//写入线圈数量 Lo
                command.Add((byte)values.Length);//写入字节数
            }
            if (funcCode == 0x10)//写多个
            {
                command.Add((byte)(writeCount >> 8));//写入寄存器数量 Hi
                command.Add((byte)writeCount);//写入寄存器数量 Lo
                command.Add((byte)values.Length);//写入字节数
            }
            command.AddRange(values);
            return command.ToArray();
        }

        /// <summary>
        /// 校验收到的数据,返回true通过校验，否则抛出异常
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckResponseData(List<byte> respBytes, Func<List<byte>, bool> validation)
        {
            if (respBytes.Count == 0)
            {
                throw new Exception("响应超时");
            }

            // 响应报文的校验
            // LRC  -》 1个字节
            // TCP没有校验
            if (validation != null && !(validation?.Invoke(respBytes) == true))
            {
                throw new Exception("数据传输异常，校验不通过");
            }

            // 功能码
            int func = respBytes[1];
            if ((func & 0x80) == 0x80)
            {
                int code = respBytes[2];
                string mes = Errors.ContainsKey(code) ? Errors[code] : "自定义异常";
                throw new Exception($"错误码：{code}， {mes}");
                // 如果异常，再检查异常Code
            }

            return true;
        }

        /// <summary>
        /// 解析收到的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="respBytes"></param>
        /// <returns></returns>
        protected List<T> AnalysisData<T>(List<byte> respBytes, int len)
        {
            List<T> list = new List<T>();
            // 解析线圈  Bool  
            if (typeof(T).Equals(typeof(bool)))
            {
                respBytes.Reverse();
                List<char> valueTemp = string.Join("", respBytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')).ToList())
                    .ToList();
                valueTemp.Reverse();

                Type tConvert = typeof(Convert);
                // 查找Convet这个类里面的ToBoolean方法
                MethodInfo method = tConvert.GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .FirstOrDefault(mi => mi.Name == "ToBoolean") as MethodInfo;

                if (method != null)
                {
                    for (int i = 0; i < Math.Min(valueTemp.Count, len); i++)
                    {
                        //result.Datas.Add((T)method.Invoke(tConvert, new object[] { int.Parse(valueTemp[i].ToString()) }));
                        list.Add((T)method.Invoke(tConvert, new object[] { int.Parse(valueTemp[i].ToString()) }));
                    }
                }
                else
                {
                    throw new Exception("未找到匹配的数据类型转换方法");
                }
            }
            else
            {
                int typeLen = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

                //T value = default(T);
                Type tBitConverter = typeof(BitConverter);
                MethodInfo method = tBitConverter.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(mi => mi.ReturnType == typeof(T)) as MethodInfo;
                if (method == null)
                    throw new Exception("未找到匹配的数据类型转换方法");

                for (int i = 0; i < respBytes.Count;)
                {
                    List<byte> valueByte = new List<byte>();

                    for (int sit = 0; sit < typeLen; sit++)
                    {
                        valueByte.Add(respBytes[i++]);
                    }
                    // 字节序的调整
                    valueByte = new List<byte>(this.SwitchEndian(valueByte.ToArray(), this.EndianType));

                    list.Add((T)method?.Invoke(tBitConverter, new object[] { valueByte.ToArray(), 0 }));
                }
            }

            return list;
        }

        /// <summary>
        /// 数据大小端切换
        /// </summary>
        /// <param name="value">4个字节</param>
        /// <param name="endian"></param>
        /// <returns></returns>
        protected byte[] SwitchEndian(byte[] value, EndianType endianType)
        {
            List<byte> result = new List<byte>(value);
            if (value.Length == 2)
            {
                switch (endianType)
                {
                    case EndianType.ABCD:
                        result.Reverse();
                        return result.ToArray();
                    case EndianType.CDAB:
                    case EndianType.BADC:
                    case EndianType.DCBA:
                        return value;
                    default:
                        result.Reverse();
                        return result.ToArray();
                }
            }
            if (value.Length == 4)
            {
                switch (endianType)
                {
                    case EndianType.ABCD:
                        result.Reverse();
                        return result.ToArray();
                    case EndianType.CDAB:
                        result[3] = value[2];
                        result[2] = value[3];
                        result[1] = value[0];
                        result[0] = value[1];
                        return result.ToArray();
                    case EndianType.BADC:
                        result[3] = value[1];
                        result[2] = value[0];
                        result[1] = value[3];
                        result[0] = value[2];
                        return result.ToArray();
                    case EndianType.DCBA:
                        return value;
                    default:
                        break;
                }
            }
            if (value.Length == 8)
            {
                switch (endianType)
                {
                    case EndianType.ABCD:
                        result[3] = value[0];
                        result[2] = value[1];
                        result[1] = value[2];
                        result[0] = value[3];
                        result[7] = value[4];
                        result[6] = value[5];
                        result[5] = value[6];
                        result[4] = value[7];
                        return result.ToArray();
                    case EndianType.CDAB:
                        result[3] = value[2];
                        result[2] = value[3];
                        result[1] = value[0];
                        result[0] = value[1];
                        result[7] = value[6];
                        result[6] = value[7];
                        result[5] = value[4];
                        result[4] = value[5];
                        return result.ToArray();
                    case EndianType.BADC:
                        result[3] = value[1];
                        result[2] = value[0];
                        result[1] = value[3];
                        result[0] = value[2];
                        result[7] = value[5];
                        result[6] = value[4];
                        result[5] = value[7];
                        result[4] = value[6];
                        return result.ToArray();
                    case EndianType.DCBA:
                        return value;
                    default:
                        break;
                }
            }
            return null;
        }

        /// <summary>
        /// 布尔数组转换成Byte
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        protected byte[] BoolArrayToBytes(bool[] values)
        {
            if (values.Length == 1)
            {
                byte b = (byte)(values[0] ? 0xff : 0x00);
                return new byte[] { b, 0x00 };
            }
            else
            {
                List<int> intValues = new List<int>();
                int sum = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    if (i > 0 && i % 8 == 0)
                    {
                        intValues.Add(sum);
                        sum = 0;
                    }
                    if (values[i])
                    {
                        sum += (int)Math.Pow(2, i % 8);
                    }
                }
                if (sum > 0)
                    intValues.Add(sum);

                byte[] result = intValues.Select(i => Convert.ToByte(i)).ToArray();
                return result;
            }
        }

        /// <summary>
        /// 转换成字节数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="endianType"></param>
        /// <returns></returns>
        protected byte[] ToBytes<T>(T value)
        {
            List<byte> byteList = new List<byte>();
            //MethodInfo mi = typeof(BitConverter).GetMethod("GetBytes",new Type[] { typeof(T) });
            //var obj = mi.Invoke(null, new object[] { value });
            //byteList.AddRange((byte[])obj);
            dynamic d = value;
            byteList.AddRange(BitConverter.GetBytes(d));
            var result = SwitchEndian(byteList.ToArray(), EndianType);
            return result;
        }

    }
}
