using System.Collections.Generic;
using System.Threading.Tasks;

namespace BQJX.Communication.Modbus
{
    public interface IModbusBase
    {
        EndianType EndianType { get; set; }
        int ReadTimeout { get; set; }
        bool IsDebug { get; set; } 
        Result Close();
        Task<Result> Open(int timeout = 2000);
        Task<Result<List<byte>>> ReadBytesFromSlave(byte deviceAddr, ushort startAddr, ushort count);
        Task<Result<List<bool>>> ReadMultiCoils(byte deviceAddr, ushort startAddr, ushort count);
        Task<Result<List<bool>>> ReadMultiInputCoils(byte deviceAddr, ushort startAddr, ushort count);
        Task<Result<List<T>>> ReadMultiInputRegister<T>(byte deviceAddr, ushort startAddr, ushort count);
        Task<Result<List<T>>> ReadMultiKeepRegister<T>(byte deviceAddr, ushort startAddr, ushort count);
        Task<Result<bool>> ReadSingleCoils(byte deviceAddr, ushort startAddr);
        Task<Result<bool>> ReadSingleInputCoils(byte deviceAddr, ushort startAddr);
        Task<Result<T>> ReadSingleInputRegister<T>(byte deviceAddr, ushort startAddr);
        Task<Result<T>> ReadSingleKeepRegister<T>(byte deviceAddr, ushort startAddr);
        Task<Result> WriteCoils(byte deviceAddr, ushort starAddr, params bool[] values);
        Task<Result> WriteKeepRegister<T>(byte deviceAddr, ushort starAddr, params T[] values);

        /// <summary>
        /// 使用0x10功能码写单寄存器
        /// </summary>
        /// <param name="deviceAddr"></param>
        /// <param name="starAddr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<Result> WriteKeepRegisterMulti<T>(byte deviceAddr, ushort starAddr, T value);

    }
}