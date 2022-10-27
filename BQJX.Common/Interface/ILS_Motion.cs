using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BQJX.Core.Interface
{
    public interface ILS_Motion
    {

        /// <summary>
        /// 获取轴配置信息
        /// </summary>
        /// <returns></returns>
        List<StepAxisEleGear> GetAxisInfos();

        /// <summary>
        /// 判断轴定位是否完成
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        Task<bool> CheckDone(int axisNo, IGlobalStatus gs);

        /// <summary>
        /// 急停
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        Task<bool> Emg_stop(int axisNo);

        /// <summary>
        /// 获取轴当前位置  unit
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        Task<double> GetCurrentPos(int axisNo);

        /// <summary>
        /// 读取轴状态Io bit0:故障  1：使能  2:运行  3：无效   4：指令完成   5：路径完成  6：回零完成
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        Task<int> GetMotionIoStatus(int axisNo);

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="mode">2：限位回零负向  3：限位回零正向  6：原点负向回零 7：原点正向回零    14：负向力矩回零  15：正向力矩回零 </param>
        /// <param name="gs"></param>
        /// <returns></returns>
        Task<bool> GoHomeWithCheckDone(int axisNo, ushort mode,IGlobalStatus gs);

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        Task<bool> GoHomeWithCheckDone(int axisNo, IGlobalStatus gs);

        /// <summary>
        /// DM2C驱动回零用
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        Task<bool> DM2C_GoHomeWithCheckDone(int axisNo,IGlobalStatus gs);

        /// <summary>
        /// 力矩回零
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="torque">力矩百分比</param>
        /// <param name="direction">0：负向回零  1：正向回零</param>
        /// <param name="gs"></param>
        /// <returns></returns>

        Task<bool> GoHomeTorqueWithCheckDone(int axisNo, short torque, int direction, IGlobalStatus gs);

        /// <summary>
        /// 正向点动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        Task<bool> JogF(int axisNo);

        /// <summary>
        /// 负向点动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        Task<bool> JogR(int axisNo);

        /// <summary>
        /// 定位运动
        /// </summary>
        /// <param name="axisNo">从站Id</param>
        /// <param name="offset">目标位置unit</param>
        /// <param name="velocity">速度rpm</param>
        /// <param name="acc">加速时间ms</param>
        /// <param name="dec">减速时间ms</param>
        /// <returns></returns>
        Task<bool> P2pMove(int axisNo, double offset, double velocity, double acc, double dec);

        /// <summary>
        /// 定位运动
        /// </summary>
        /// <param name="axisNo">从站Id</param>
        /// <param name="offset">目标位置unit</param>
        /// <param name="velocity">速度rpm</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        Task<bool> P2pMoveWithCheckDone(int axisNo, double offset, double velocity, IGlobalStatus gs);

        /// <summary>
        /// 读取轴报警代码
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        Task<ushort> ReadAlmCode(int axisNo);

        /// <summary>
        /// 相对运动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="offset"></param>
        /// <param name="velocity"></param>
        /// <param name="acc"></param>
        /// <param name="dec"></param>
        /// <returns></returns>
        Task<bool> RelativeMove(int axisNo, double offset, double velocity, double acc, double dec);

        /// <summary>
        /// 相对运动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="offset"></param>
        /// <param name="velocity"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        Task<bool> RelativeMoveWithCheckDone(int axisNo, double offset, double velocity,IGlobalStatus gs);

        /// <summary>
        /// 复位报警
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        Task<bool> ResetAxisAlm(int axisNo);

        /// <summary>
        /// 保存参数到Flash
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        Task<bool> SaveParamsToFlash(int axisNo);

        /// <summary>
        /// 电机上电
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        Task<bool> ServeOff(int axisNo);

        /// <summary>
        /// 电机下电
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        Task<bool> ServoOn(int axisNo);

        /// <summary>
        /// 设置点动速度
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="acc_dec"></param>
        /// <param name="velocity"></param>
        /// <returns></returns>
        Task<bool> SetJogVel(int axisNo, double acc_dec, double velocity);

        /// <summary>
        /// 停止运动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        Task<bool> StopMove(int axisNo);

        /// <summary>
        /// 力矩转动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="velocity"></param>
        /// <param name="torque"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        Task<bool> TorqueMoveWithCheckDone(int axisNo, double velocity, double torque, int timeout, IGlobalStatus gs);

        /// <summary>
        /// 速度运动
        /// </summary>
        /// <param name="axisNo"></param>
        /// <param name="velocity"></param>
        /// <returns></returns>
        Task<bool> VelocityMove(int axisNo, double velocity);
    }
}
