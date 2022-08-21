using BQJX.Common.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BQJX.Core.Interface
{
    public interface IEPG26
    {

        /// <summary>
        /// 手爪目标物检测
        /// </summary>
        /// <param name="id"></param>
        /// <returns> -1：该状态无效 0：正在移动 1：张开检测到物体  2：关闭检测到物体  3：到达指定位置</returns>
        Task<int> ClawGetchStatus(int id);

        /// <summary>
        /// 手爪忙状态
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true：前往目标位置 false：停止</returns>
        Task<bool> ClawIsBusy(int id);

        /// <summary>
        /// 手爪是否使能
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> ClawIsEnable(int id);

        /// <summary>
        /// 手爪是否激活完成
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> ClawIsEnableDone(int id);

        /// <summary>
        /// 手爪工作模式
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true：无参控制模式 false：有参控制模式</returns>
        Task<bool> ClawMode(int id);

        /// <summary>
        /// 禁用电动夹爪
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> Disable(int id);

        /// <summary>
        /// 使能电动夹爪
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> Enable(int id);

        /// <summary>
        /// 获取电动夹爪状态
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<EPG_ClawStatus> GetClawStatus(int id);

        /// <summary>
        /// 发送手爪动作命令
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pos"></param>
        /// <param name="vel"></param>
        /// <param name="torque"></param>
        /// <returns></returns>
        Task<bool> SendCommand(int id, byte pos, byte vel, byte torque);
    }
}