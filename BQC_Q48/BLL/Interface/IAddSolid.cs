using BQJX.Common;
using BQJX.Common.Interface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface IAddSolid
    {
        Task<bool> GoHome(IGlobalStatus gs);

        void UpdatePosData();

        Task<bool> AddSaltExtract(Sample sample, Func<Sample, bool, IGlobalStatus, Task<bool>> addSolveFunc, Func<bool> func1, Func<bool> func2, IGlobalStatus gs);

        /// <summary>
        /// 加固
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="weights">加固重量</param>
        /// <param name="gs"></param>
        /// <returns></returns>
        Task<bool> AddSolidAsync(Sample sample, double[] weights, Func<bool> func1, Func<bool> func2, IGlobalStatus gs);

    }
}