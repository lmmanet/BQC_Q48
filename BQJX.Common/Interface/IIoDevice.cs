using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Core.Interface
{
    public interface IIoDevice
    {
        /// <summary>
        /// 读取指定符号地址的输入位状态
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        bool ReadBit_DI(string str);

        /// <summary>
        ///  读取指定地址的输入位状态
        /// </summary>
        /// <param name="bitNO"></param>
        /// <returns></returns>
        bool ReadBit_DI(ushort bitNO);

        /// <summary>
        ///  读取指定符号地址的输出位状态
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        bool ReadBit_DO(string str);

        /// <summary>
        /// 读取指定地址的输出位状态
        /// </summary>
        /// <param name="bitNO"></param>
        /// <returns></returns>
        bool ReadBit_DO(ushort bitNO);

        /// <summary>
        /// 读取指定端口的输入双字状态（每32bit为一个port）
        /// </summary>
        /// <param name="portNo">指定Io地址起始地址</param>
        /// <returns></returns>
        uint ReadByte_DI(ushort portNo);

        /// <summary>
        /// 读取指定端口的输出双字状态（每32bit为一个port）
        /// </summary>
        /// <param name="portNo">指定Io地址起始地址</param>
        /// <returns></returns>
        uint ReadByte_DO(ushort portNo);

        /// <summary>
        /// 复位指定符号地址位状态
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        bool ResetBit_DO(string str);

        /// <summary>
        /// 置位指定符号地址位状态
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        bool SetBit_DO(string str);

        /// <summary>
        /// 写入指定Io位状态
        /// </summary>
        /// <param name="bitNO"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool WriteBit_DO(ushort bitNO, bool value);

        /// <summary>
        /// 写入指定Io双字状态
        /// </summary>
        /// <param name="portNo">Io起始地址，连续32位</param>
        /// <param name="value">写入值</param>
        /// <returns></returns>
        bool WriteByte_DO(ushort portNo, uint value);

        /// <summary>
        /// 输出指定Io，延时一定时间后翻转状态
        /// </summary>
        /// <param name="bitNO"></param>
        /// <param name="delayTime">延时时间S，0为无限大</param>
        /// <returns></returns>
        bool WriteBit_DO_Delay_Reverse(ushort bitNO, double delayTime);

        /// <summary>
        /// 设置Io计数模式
        /// </summary>
        /// <param name="bitNO"></param>
        /// <param name="mode">0:禁用 1：上升沿计数 2:下降沿计数</param>
        /// <param name="filterTime">/滤波时间S </param>
        /// <returns></returns>
        bool SetBit_DI_Count_Mode(ushort bitNO, ushort mode, double filterTime);

        /// <summary>
        /// 设置Io计数值
        /// </summary>
        /// <param name="bitNo"></param>
        /// <param name="value">io计数值</param>
        /// <returns></returns>
        bool SetBit_DI_Count_Value(ushort bitNo, uint value);

        /// <summary>
        /// 读取Io计数值
        /// </summary>
        /// <param name="bitNo"></param>
        /// <returns></returns>
        uint GetBit_DI_Count_Value(ushort bitNo);


        /// <summary>
        /// 读取AD
        /// </summary>
        /// <param name="portNo">0~3</param>
        /// <returns></returns>
        double ReadByte_AD(ushort portNo);

        /// <summary>
        /// 写DA
        /// </summary>
        /// <param name="portNo"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool WriteByte_DA(ushort portNo, double value);

        /// <summary>
        /// 读取DA
        /// </summary>
        /// <param name="portNo"></param>
        /// <returns></returns>
        double ReadDoubleDA(ushort portNo);

        /// <summary>
        /// 配置AD模块
        /// </summary>
        /// <param name="slaveId">从站1000+i</param>
        /// <param name="slot">模块插槽</param>
        /// <param name="channel">通道1~4</param>
        /// <param name="mode">输入模式0: -5~+5V  1:1~5V  2:-10~10V 3:0~10V 4:0~20mA 5:4~20mA</param>
        /// <param name="filterTime">滤波时间1-255ms</param>
        /// <returns></returns>
        bool Config_AD(ushort slaveId, ushort slot, ushort channel, int mode, int filterTime);

        /// <summary>
        /// 配置DA模块
        /// </summary>
        /// <param name="slaveId">从站1000+i</param>
        /// <param name="slot">模块插槽</param>
        /// <param name="channel">通道1~4</param>
        /// <param name="mode">输出模式0: 0~5V  1:1~5V  2:-5~5V 3:0~10V 4:-10~10V 5:0~20mA 6:4~20mA</param>
        /// <param name="enable">输出使能 0：禁止 1：使能</param>
        /// <returns></returns>
        bool Config_DA(ushort slaveId, ushort slot, ushort channel, int mode, int enable);

    }
}
