using BQJX.Common;
using BQJX.Common.Interface;
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
        Task<bool> GoHome(IGlobalStatus gs);

        /// <summary>
        /// 判断涡旋任务是否结束
        /// </summary>
        bool IsVortexTaskDone { get; }

        /// <summary>
        /// 更新涡旋位置数据
        /// </summary>
        void UpdatePosData();

        /// <summary>
        /// 涡旋
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="step">涡旋步骤号</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool StartVortex(Sample sample, int step, IGlobalStatus gs);

        //==============================================涡旋单独用===================================================//
        void AddSampleToVortexList(Sample sample, IGlobalStatus gs);
        Task StartVortex(IGlobalStatus gs);





    }
}