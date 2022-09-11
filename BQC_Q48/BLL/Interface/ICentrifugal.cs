using BQJX.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICentrifugal
    {
        Task<bool> GoHome();
        Task<bool> CentrifugalAsync(Sample sample, CancellationTokenSource cts);
    }
}