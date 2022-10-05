using BQJX.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICentrifugal
    {
        Task<bool> GoHome(CancellationTokenSource cts);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="actionCallBack"></param>
        /// <param name="cts"></param>
        /// <param name="var">离心试管种类0： 样品管 1：净化管 2：萃取管</param>
        /// <returns></returns>
        Task StartCentrifugal(Sample sample, string actionCallBack, CancellationTokenSource cts, int var = 0);




        //测试

        bool DoCentrifugal(Sample sample, CancellationTokenSource cts);
    }
}