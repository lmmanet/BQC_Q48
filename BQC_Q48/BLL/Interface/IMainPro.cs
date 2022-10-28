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
        void ContinuePro();
        void GoHome(Expression<Func<bool>> homeDoneFlag);
        void StartPro();
        void StopPro();
        void PausePro(Expression<Func<bool>> pauseFlag);
        void SwitchLight();

        /// <summary>
        /// 清空工作列表
        /// </summary>
        void ClearWorkList();


        void Centrifugal(Sample sample, IGlobalStatus gs);

        void WetBack(Sample sample, IGlobalStatus gs);
        void AddSalt(Sample sample, IGlobalStatus gs);
        void CentrifugalCallBack(Sample sample, IGlobalStatus gs);

        void PipettingCallBack(Sample sample, IGlobalStatus gs);
    }
}