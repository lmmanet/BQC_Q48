using BQJX.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface IAddSolid
    {
        Task<bool> GoHome(CancellationTokenSource cts);

        Task<bool> AddSaltExtract(Sample sample, Func<Sample, CancellationTokenSource, Task<bool>> addSolveFunc, CancellationTokenSource cts);

        /// <summary>
        /// 加固
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="solid">加固种类</param>
        /// <param name="weight">加固重量</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> AddSolidAsync(Sample sample, int[] solids, double[] weights, CancellationTokenSource cts);

    }
}