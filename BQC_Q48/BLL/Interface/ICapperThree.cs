using BQJX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICapperThree
    {

        /// <summary>
        /// 拧盖3回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> GoHome(CancellationTokenSource cts);


        //==================================================================离心部分======================================================================================//

        /// <summary>
        /// 从净化管架取试管到移栽（离心） 
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <returns></returns>
        bool GetSampleFromMarterialToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);


        /// <summary>
        /// 离心完成后从移栽中取出试管 
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToMarterial(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);


        //==================================================================移液部分======================================================================================//

        /// <summary>
        /// 从拧盖3取净化管到移栽  移液 =》 CentrifugalCarrier => CapperThree => CarrierTwo
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        /// <summary>
        /// 从移栽搬运无盖净化管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToCapperThree(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);
        
        /// <summary>
        /// 从拧盖3搬运有盖净化盖到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToMaterial(Sample sample, CancellationTokenSource cts);



    }

}
