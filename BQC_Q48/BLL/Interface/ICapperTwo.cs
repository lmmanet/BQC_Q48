using BQJX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICapperTwo
    {
        Task<bool> GoHome(CancellationTokenSource cts);

        Task<bool> CapperOffAsync(Sample sample, CancellationTokenSource cts);

        Task<bool> CapperOnAsync(Sample sample, CancellationTokenSource cts);


        /// <summary>
        /// 从冰浴或者振荡1取试管到移栽  由离心移栽GetSampleFromColdToTransfer调用
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromColdToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        /// <summary>
        /// 离心完成后从移栽中取出试管
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToMaterial(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        //================================================移液部分 兽药=================================================//

        /// <summary>
        /// 从拧盖2取无盖试管到移栽      移液 =》 CentrifugalCarrier => CapperTwo（拆盖完）(传入动作) => CarrierOne(传入动作)
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperTwoToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        /// <summary>
        /// 从移栽取回无盖试管到拧盖2    移液 =》 CentrifugalCarrier => CapperTwo（拆盖完）(传入动作) => CarrierOne(传入动作)
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToCapperTwo(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);




    }

}
