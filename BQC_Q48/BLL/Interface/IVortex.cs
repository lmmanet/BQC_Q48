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
        /// <param name="step">涡旋步骤号</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool StartVortex(Sample sample, int step, CancellationTokenSource cts);





        //==============================================涡旋单独用===================================================//
        void AddSampleToVortexList(Sample sample, CancellationTokenSource cts);
        Task StartVortex(CancellationTokenSource cts);





    }
}