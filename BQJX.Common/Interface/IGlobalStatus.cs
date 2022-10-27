using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace BQJX.Common.Interface
{
    public interface IGlobalStatus
    {

        /// <summary>
        /// 是否急停
        /// </summary>
        bool IsEmgStop { get; }
        /// <summary>
        /// 是否暂停
        /// </summary>
        bool IsPause { get; }

        /// <summary>
        /// 是否停止
        /// </summary>
        bool IsStopped { get; }

        /// <summary>
        /// 状态机
        /// </summary>
        int MachineStatus { get; }

        /// <summary>
        /// 继续事件
        /// </summary>
        event Func<bool> ContinueProgramEventArgs;

        /// <summary>
        /// 暂停事件
        /// </summary>
        event Func<bool> PauseProgramEventArgs;

        /// <summary>
        /// 停止事件
        /// </summary>
        event Func<bool> StopProgramEventArgs;

        /// <summary>
        /// 状态机改变事件
        /// </summary>

        event Action MachineStatusChangedEventArgs;

        /// <summary>
        /// 启动程序
        /// </summary>
        /// <returns></returns>
        bool StartProgram();

        /// <summary>
        /// 继续程序
        /// </summary>
        /// <returns></returns>
        bool ContinueProgram();

        /// <summary>
        /// 暂停程序
        /// </summary>
        /// <returns></returns>
        bool PauseProgram();

        /// <summary>
        /// 急停发生
        /// </summary>
        void EmgStop();

        /// <summary>
        /// 发生报警
        /// </summary>
        void AlmOccu();

        /// <summary>
        /// 复位报警
        /// </summary>
        void ResetAlm();

        /// <summary>
        /// 停止程序
        /// </summary>
        /// <param name="stopDoneFunc"></param>
        void StopProgram(Func<bool> stopDoneFunc);

        /// <summary>
        /// 初始化  需要保证所有任务已经停止
        /// </summary>
        bool InitStatus(Func<bool> stopDoneFunc, Func<Task<bool>> initFunc);
    }
}
