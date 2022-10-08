using BQJX.Common;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface IMainPro
    {
        void ContinuePro();
        Task GoHome(Expression<Func<bool>> homeDoneFlag);
        void StartPro();
        void StopPro();
        void PausePro(Expression<Func<bool>> pauseFlag);
        void SwitchLight();






        void Centrifugal(Sample sample, CancellationTokenSource cts);

        void WetBack(Sample sample, CancellationTokenSource cts);
        void AddSalt(Sample sample, CancellationTokenSource cts);
        void CentrifugalCallBack(Sample sample, CancellationTokenSource cts);

        void PipettingCallBack(Sample sample, CancellationTokenSource cts);
    }
}