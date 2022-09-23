using BQJX.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICentrifugal
    {
        Task<bool> GoHome(CancellationTokenSource cts);

        Task StartCentrifugal(Sample sample, Action<Sample, CancellationTokenSource> actionCallBack, CancellationTokenSource cts);




        //测试

        bool DoCentrifugal(Sample sample, CancellationTokenSource cts);
    }
}