using BQJX.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface IVortex
    {

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> GoHome(CancellationTokenSource cts);

        /// <summary>
        /// 涡旋
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> StartVortexAsync(Sample sample, CancellationTokenSource cts);
    }
}