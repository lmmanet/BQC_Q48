using BQJX.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface IConcentration
    {
        /// <summary>
        /// 浓缩回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> GoHome(CancellationTokenSource cts);

        /// <summary>
        /// 样品浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoConcentration(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 样品复溶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool Redissolve(Sample sample, CancellationTokenSource cts);


    }
}