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



        //==================================================================移液部分======================================================================================//

        /// <summary>
        /// 从拧盖3取净化管到移栽  移液 =》 CentrifugalCarrier => CapperThree => CarrierTwo  根据工艺判断是否加液  
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToTransfer(Sample sample,Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖3取净化管到移栽 无振荡加液  
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToTransferWithoutVibration(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);

        /// <summary>
        /// 从移栽搬运无盖净化管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToCapperThree(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);
        
        /// <summary>
        /// 从拧盖3搬运有盖净化盖到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToVibration(Sample sample, CancellationTokenSource cts);

        bool GetSampleFromCapperThreeToMaterial(Sample sample, CancellationTokenSource cts);


        /// <summary>
        /// 拧盖3移液   由拧盖4移液调用
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移液动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoPipetting(Sample sample, Func<Sample, CancellationTokenSource, bool> func, CancellationTokenSource cts);



    }

}
