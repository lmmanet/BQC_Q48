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

        Task<bool> GoHome(CancellationTokenSource cts);


        //==================================================================离心部分======================================================================================//

        /// <summary>
        /// 从净化管架取试管到移栽（离心） Centrifugal => CentrifugalCarrier => CapperThree (决定由哪里取料) => CarrierTwo
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <returns></returns>
        bool GetSampleFromMarterialToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);


        /// <summary>
        /// 离心完成后从移栽中取出试管  Centrifugal (小管) => CentrifugalCarrier(小管) => CapperThree => Carrier
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToMarterial(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);


        //==================================================================移液部分（大管到小管  小管到大管）.（农残氮吹移液）======================================================================================//

        /// <summary>
        /// 从拧盖3取净化管到移栽  移液 =》 CentrifugalCarrier => CapperThree => CarrierTwo
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        /// <summary>
        /// 从移栽取出净化管，空管 或 上清液管   移液 =》 CentrifugalCarrier => CapperThree（装盖完决定放回试管架或振荡） => CarrierTwo
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToMarterialPiperttor(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);






    }

}
