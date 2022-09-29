using BQJX.Common;
using System.Threading;

namespace Q_Platform.BLL
{
    public interface IMainPro
    {
        void ContinuePro();
        void GoHome();
        void StartPro();
        void StopPro();
        void PausePro();
        void SwitchLight();






        void Centrifugal(Sample sample, CancellationTokenSource cts);

        void AddSolve(Sample sample, CancellationTokenSource cts);
        void AddSalt(Sample sample, CancellationTokenSource cts);
        void CentrifugalCallBack(Sample sample, CancellationTokenSource cts);
    }
}