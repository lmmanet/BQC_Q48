using BQJX.Common;
using BQJX.Common.Interface;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface IMainPro
    {
        /// <summary>
        /// 继续程序
        /// </summary>
        void ContinuePro();

        /// <summary>
        /// 回零程序
        /// </summary>
        /// <param name="homeDoneFlag"></param>
        void GoHome(Expression<Func<bool>> homeDoneFlag);

        /// <summary>
        /// 启动程序
        /// </summary>
        void StartPro();

        /// <summary>
        /// 停止程序
        /// </summary>
        void StopPro();

        /// <summary>
        /// 暂停程序
        /// </summary>
        /// <param name="pauseFlag"></param>
        void PausePro(Expression<Func<bool>> pauseFlag);

        /// <summary>
        /// 照明开关
        /// </summary>
        void SwitchLight();

        /// <summary>
        /// 清空工作列表
        /// </summary>
        void ClearWorkList();

        /// <summary>
        /// 离心程序
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        void Centrifugal(Sample sample, IGlobalStatus gs);

        /// <summary>
        /// 回湿程序
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        void WetBack(Sample sample, IGlobalStatus gs);

        /// <summary>
        /// 加盐回调
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        void AddSalt(Sample sample, IGlobalStatus gs);

        /// <summary>
        /// 离心回调
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        void CentrifugalCallBack(Sample sample, IGlobalStatus gs);

        /// <summary>
        /// 移液回调
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>

        void PipettingCallBack(Sample sample, IGlobalStatus gs);
    }
}