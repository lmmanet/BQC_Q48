using BQJX.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface IAddSolid
    {
        Task<bool> AddSolidAsync(Sample sample, CancellationTokenSource cts);
    }
}