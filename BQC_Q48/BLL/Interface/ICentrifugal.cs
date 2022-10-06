using BQJX.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICentrifugal
    {
        Task<bool> GoHome(CancellationTokenSource cts);

        void AddSampleToCentrifugalList(Sample sample, string actionCallBack, int var = 0);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="actionCallBack"></param>
        /// <param name="cts"></param>
        /// <param name="var">离心试管种类0： 样品管 1：净化管 2：萃取管</param>
        /// <returns></returns>
        Task StartCentrifugal(CancellationTokenSource cts);




        //测试

        bool DoCentrifugal(Sample sample, CancellationTokenSource cts);
    }
}