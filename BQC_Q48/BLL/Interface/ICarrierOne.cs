using BQJX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICarrierOne
    {


        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> GoHome(CancellationTokenSource cts);

        bool GetSampleToCapperOne(Sample sample, CancellationTokenSource cts);

        bool GetSampleToVortex(Sample sample, CancellationTokenSource cts);

        bool GetSampleToAddSolid(Sample sample, CancellationTokenSource cts);

        bool GetSampleToVibration(Sample sample, CancellationTokenSource cts);

        bool GetSampleToAddSolid(Sample sample, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts);

        bool GetSampleToMaterial(Sample sample, CancellationTokenSource cts);

        bool GetSampleToCold(Sample sample,ushort posNum, CancellationTokenSource cts);

        //===================================离心移栽=======================================//
        bool GetSampleToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        bool GetSampleFromTransferToCarrierTwo(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        bool GetSampleFromTransferToMaterial(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        bool GetSampleFromMaterialToCapperTwo(Sample sample, CancellationTokenSource cts);

        bool GetSampleFromCapperTwoToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        bool GetSampleFromTransferToCapperTwo(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        bool GetSampleFromCapperTwoToMaterial(Sample sample, CancellationTokenSource cts);


        bool GetSampleFromColdToTransfer(Sample sample,ushort posNum, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);


        bool GetSampleFromVibrationOneToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);


        //===================================移液部分=======================================//

        bool DoPipetting(Sample sample, bool bigToSmall, CancellationTokenSource cts);



    }
}
