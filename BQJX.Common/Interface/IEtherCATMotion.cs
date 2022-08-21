using BQJX.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BQJX.Core.Interface
{
    public interface IEtherCATMotion
    {

        /// <summary>
        /// 编码器清零(绝对值电机回零)
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        bool AbsSysClear(ushort axisNo);

        /// <summary>
        /// 检测轴是否完成定位
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> CheckDone(ushort axisNo, CancellationTokenSource cts);

        /// <summary>
        /// 检测坐标系是否完成定位
        /// </summary>
        /// <param name="cardNo"></param>
        /// <param name="coordinate">坐标系号</param>
        /// <returns></returns>
        Task<bool> CheckDoneMulti(ushort cardNo, ushort coordinate, CancellationTokenSource cts);

        /// <summary>
        /// 配置软限位
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="enableLimit"></param>
        /// <param name="nLimit"></param>
        /// <param name="pLimit"></param>
        /// <returns></returns>
        bool ConfigSoftLimit(ushort axisNo, ushort enableLimit, double nLimit, double pLimit);

        /// <summary>
        /// 配置软限位
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="enableLimit"></param>
        /// <returns></returns>
        bool ConfigSoftLimit(ushort axisNo, ushort enableLimit);

        /// <summary>
        /// 急停
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        bool Emg_stop(ushort axisNo);

        /// <summary>
        /// 获取轴配置信息
        /// </summary>
        /// <returns></returns>
        List<AxisEleGear> GetAxisInfos();

        /// <summary>
        /// 读取轴当前位置
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        double GetCurrentPos(ushort axisNo);

        /// <summary>
        /// 读取轴当前速度
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        double GetCurrentVel(ushort axisNo);

        /// <summary>
        /// 读取轴Io状态
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns><see cref="MotionIoStatus"/></returns>
        uint GetMotionIoStatus(ushort axisNo);

        /// <summary>
        /// 获取轴状态机
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        ushort GetMotionStatus(ushort axisNo);

        /// <summary>
        /// 读取总线轴错误代码
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        uint GetAxisAlmCode(ushort axisNo);

        /// <summary>
        /// 设置轴映射Io
        /// </summary>
        /// <param name="axisNo">轴号</param>
        /// <param name="ioType">3：急停信号 4：减速停止信号 6：通用输入端口</param>
        /// <param name="mapIoIndex">Io端口号（本体0~7）</param>
        /// <param name="filterTime">滤波时间</param>
        /// <returns></returns>
        bool SetAxisIoMap(ushort axisNo, ushort ioType, ushort mapIoIndex, int filterTime);


        int Get_actual_torque(ushort axisNo);
        bool SetPositonToSystem(ushort axisNo, double pos);
        bool StopTorqueMove(ushort axisNo);
        bool TorqueMove(ushort axisNo, short targetTorque, uint torqueSlope, uint velocity);
        Task<bool> TorqueMoveWithCheckDone(ushort axisNo, short targetTorque, uint torqueSlope, uint velocity, double maxOffset, CancellationTokenSource cts);





        /// <summary>
        /// 获取轴停止代码
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns>
        /// 0：正常停止
        /// 1：保留
        /// 2：保留
        /// 3：LTC 外部触发立即停止，IMD_STOP_AT_LTC
        /// 4：EMG 立即停止，IMD_STOP_AT_EMG
        /// 5：正硬限位立即停止，IMD_STOP_AT_ELP
        /// 6：负硬限位立即停止，IMD_STOP_AT_ELN
        /// 7：正硬限位减速停止，DEC_STOP_AT_ELP
        /// 8：负硬限位减速停止，DEC_STOP_AT_ELN
        /// 9：正软限位立即停止，IMD_STOP_AT_SOFT_ELP
        /// 10：负软限位立即停止，IMD_STOP_AT_SOFT_ELN
        /// 11：正软限位减速停止，DEC_STOP_AT_SOFT_ELP
        /// 12：负软限位减速停止，DEC_STOP_AT_SOFT_ELN
        /// 13：命令立即停止，IMD_STOP_AT_CMD
        /// 14：命令减速停止，DEC_STOP_AT_CMD
        /// 15：其它原因立即停止，IMD_STOP_AT_OTHER
        /// 16：其它原因减速停止，DEC_STOP_AT_OTHER
        /// 17：未知原因立即停止，IMD_STOP_AT_UNKOWN
        /// 18：未知原因减速停止，DEC_STOP_AT_UNKOWN
        /// 19：保留，DEC_STOP_AT_DEC
        /// </returns>
        int Get_stop_code(ushort axisNo);

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> GohomeWithCheckDone(ushort axisNo,ushort homeMode, CancellationTokenSource cts);

        /// <summary>
        /// 2D直线插补
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="PositionArray"></param>
        /// <param name="velocity"></param>
        /// <returns></returns>
        Task<bool> InterPolation_2D_lineWithCheckDone(ushort[] axisNo, double[] PositionArray, double velocity, CancellationTokenSource cts);

        /// <summary>
        /// 点动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="velocity"></param>
        /// <param name="direction"></param>
        void JogMove(ushort axisNo, double velocity, ushort direction);

        /// <summary>
        /// 停止点动
        /// </summary>
        /// <param name="axisNo"></param>
        void JogStop(ushort axisNo);

        /// <summary>
        /// 绝对运动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="offset"></param>
        /// <param name="velocity"></param>
        /// <returns></returns>
        bool P2pMove(ushort axisNo, double offset, double velocity);

        /// <summary>
        /// 绝对运动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="offset"></param>
        /// <param name="velocity"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> P2pMoveWithCheckDone(ushort axisNo, double offset, double velocity, CancellationTokenSource cts);

        /// <summary>
        /// 相对运动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="offset"></param>
        /// <param name="velocity"></param>
        /// <returns></returns>
        bool RelativeMove(ushort axisNo, double offset, double velocity);

        /// <summary>
        /// 相对运动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="offset"></param>
        /// <param name="velocity"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> RelativeMoveWithCheckDone(ushort axisNo, double offset, double velocity, CancellationTokenSource cts);

        /// <summary>
        /// 复位轴报警
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        bool ResetAxisAlm(ushort axisNo);

        /// <summary>
        /// 失能轴
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        bool ServoOff(ushort axisNo);

        /// <summary>
        /// 使能轴
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        bool ServoOn(ushort axisNo);

        /// <summary>
        /// 绝对位置初始清零
        /// </summary>
        /// <param name="axisNo"></param>
        void SetOffset(ushort axisNo);
      
        /// <summary>
        /// 停止轴运动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        bool StopMove(ushort axisNo);
    
        /// <summary>
        /// 速度运动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="velocity"></param>
        /// <param name="direction">0:负方向 1:正方向</param>
        /// <returns></returns>
        bool VelocityMove(ushort axisNo, double velocity, ushort direction);

        /// <summary>
        /// 配置函数调用日志
        /// </summary>
        /// <param name="fileName"></param>
        void WriteDebugLog(string fileName);
    }
}
