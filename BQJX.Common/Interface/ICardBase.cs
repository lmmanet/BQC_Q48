using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Core.Interface
{
    public interface ICardBase
    {

        /// <summary>
        /// 关闭板卡
        /// </summary>
        /// <returns></returns>
        Task<bool> Close();

        /// <summary>
        /// 获取板卡是否初始化成功
        /// </summary>
        /// <returns></returns>
        bool GetCaraIsIni();

        /// <summary>
        /// 初始化板卡资源
        /// </summary>
        /// <param name="filePath">配置文件路径</param>
        /// <returns></returns>
        Task<int> Initialize(string filePath);

        /// <summary>
        /// 热复位
        /// </summary>
        /// <param name="card"></param>
        /// <returns></returns>
        Task<bool> ResetFieldBus(int card);

        /// <summary>
        /// 初始化板卡
        /// </summary>
        /// <returns></returns>
        bool InitiCard();

        /// <summary>
        /// 设置偏移量的为零
        /// <paramref name="cardNo">卡号</paramref>
        /// <paramref name="axisCount">轴数</paramref>
        /// </summary>
        bool SetOffsetZero(ushort cardNo, int axisCount);

        /// <summary>
        /// 下载轴配置文件
        /// </summary>
        /// <param name="cardNo">卡号</param>
        /// <param name="filePath">配置文件路径</param>
        /// <returns></returns>
        bool DownLoadConfigFile(ushort cardNo,string filePath);

        /// <summary>
        /// 读取总线状态
        /// </summary>
        /// <param name="cardNo"></param>
        /// <returns></returns>
        int GetFieldBusErrorCode(ushort cardNo);

        /// <summary>
        /// 清除总线错误
        /// </summary>
        /// <param name="cardNo"></param>
        /// <returns></returns>
        bool ClearFieldBusError(ushort cardNo);

        /// <summary>
        /// 停止总线运行
        /// </summary>
        /// <param name="cardNo"></param>
        /// <returns></returns>
        bool StopFieldBus(ushort cardNo);

        /// <summary>
        /// 获取总线从站个数
        /// </summary>
        /// <param name="cardNo"></param>
        /// <returns></returns>
        int GetFieldBusTotalSlaves(ushort cardNo);

        /// <summary>
        /// 获取总线轴数
        /// </summary>
        /// <param name="cardNo"></param>
        /// <returns></returns>
        int GetFieldBusAxes(ushort cardNo);
              
        /// <summary>
        /// 读取从站对象字典参数值
        /// </summary>
        /// <param name="cardNo">卡号</param>
        /// <param name="modNo">从站地址 1000+i</param>
        /// <param name="index">索引</param>
        /// <param name="subIndex">子索引</param>
        /// <param name="valueLength">数据长度（单位bit）</param>
        /// <returns></returns>
        int GetPDO(ushort cardNo, ushort modNo, ushort index, ushort subIndex, ushort valueLength);

        /// <summary>
        /// 设置从站对象字典参数
        /// </summary>
        /// <param name="cardNo">卡号</param>
        /// <param name="modNo">从站地址 1000+i</param>
        /// <param name="index">索引</param>
        /// <param name="subIndex">子索引</param>
        /// <param name="valueLength">数据长度（单位bit）</param>
        /// <param name="value">写入的值</param>
        /// <returns></returns>
        bool SetPDO(ushort cardNo, ushort modNo, ushort index, ushort subIndex, ushort valueLength, int value);





        /// <summary>
        /// 设置从站扩展有符号RxPDO值
        /// </summary>
        /// <param name="cardNo">卡号</param>
        /// <param name="address">扩展PDO首地址</param>
        /// <param name="dataLen">数据长度 按16bit计算 最大为2（32bit）</param>
        /// <param name="value">数据值</param>
        /// <returns></returns>
        bool Write_rxPDO_Extra(ushort cardNo, ushort address, ushort dataLen, int value);

        /// <summary>
        /// 读取从站扩展有符号RxPDO值
        /// </summary>
        /// <param name="cardNo">卡号</param>
        /// <param name="address">扩展PDO首地址</param>
        /// <param name="dataLen">数据长度 按16bit计算 最大为2（32bit）</param>
        /// <returns></returns>
        int Read_rxPDO_Extra(ushort cardNo, ushort address, ushort dataLen);

        /// <summary>
        /// 读取从站扩展有符号TxPDO值
        /// </summary>
        /// <param name="cardNo">卡号</param>
        /// <param name="address">扩展PDO首地址</param>
        /// <param name="dataLen">数据长度 按16bit计算 最大为2（32bit）</param>
        /// <returns></returns>
        int Read_txPDO_Extra(ushort cardNo, ushort address, ushort dataLen);

        /// <summary>
        /// 设置从站扩展无符号rxPDO值
        /// </summary>
        /// <param name="cardNo">卡号</param>
        /// <param name="address">扩展PDO首地址</param>
        /// <param name="dataLen">数据长度 按16bit计算 最大为2（32bit）</param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool Write_rxPDO_Extra_uint(ushort cardNo, ushort address, ushort dataLen, uint value);

        /// <summary>
        /// 读取从站扩展无符号rxPDO值 
        /// </summary>
        /// <param name="cardNo">卡号</param>
        /// <param name="address">扩展PDO首地址</param>
        /// <param name="dataLen">数据长度 按16bit计算 最大为2（32bit）</param>
        /// <returns></returns>
        uint Read_rxPDO_Extra_uint(ushort cardNo, ushort address, ushort dataLen);

        /// <summary>
        /// 读取从站扩展无符号txPDO值
        /// </summary>
        /// <param name="cardNo">卡号</param>
        /// <param name="address">扩展PDO首地址</param>
        /// <param name="dataLen">数据长度 按16bit计算 最大为2（32bit）</param>
        /// <returns></returns>
        uint Read_txPDO_Extra_uint(ushort cardNo, ushort address, ushort dataLen);







    }
}
