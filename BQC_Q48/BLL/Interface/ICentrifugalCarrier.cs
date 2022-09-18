using BQJX.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICentrifugalCarrier
    {
        Task<bool> GoHome(CancellationTokenSource cts);


        bool GetTubeInCentrifugal(Sample sample, Func<ushort, Task<bool>> func, bool isBig, CancellationTokenSource cts);


        bool GetTubeOutCentrifugal(Sample sample, Func<ushort, Task<bool>> func, bool isBig, CancellationTokenSource cts);





    }
}