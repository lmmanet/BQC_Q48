using BQJX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICarrierTwo
    {

        bool GetSampleToCapperThree(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> GoHome(CancellationTokenSource cts);

      
        /// <summary>
        /// 开始移液  样品移液  浓缩移液 浓缩定容后取样移液  
        /// </summary>
        /// <param name="num"></param>
        /// <param name="src">移液取液</param>
        /// <param name="dst">移液目标吐液位</param>
        /// <param name="volume"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoPipetting(ushort num,double[] src,double[] dst, double volume, CancellationTokenSource cts);



        //加标    清洗     浓缩混匀     取样1 2 


        //=========================================离心移栽=========================================================//

        bool GetSampleFromTransferToCapperThree(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        bool GetSampleToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

    }
}
