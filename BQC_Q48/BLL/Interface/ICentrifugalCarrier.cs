using BQJX.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICentrifugalCarrier
    {
        Task<bool> GoHome(CancellationTokenSource cts);

        Task StartPipetting(Sample sample, Action<Sample, CancellationTokenSource> actionCallBack, CancellationTokenSource cts);

        //==================================================================离心移栽部分======================================================================================//

        /// <summary>
        /// 搬运试管到离心机  （离心机调用）
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">离心机动作GoStation</param>
        /// <param name="isBig">大小管 获取移栽坐标</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetTubeInCentrifugal(Sample sample, Func<ushort, Task<bool>> func, bool isBig, CancellationTokenSource cts);

        /// <summary>
        /// 从离心机取出试管  （离心机调用）
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">离心机动作GoStation</param>
        /// <param name="isBig">大小管 获取移栽坐标</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetTubeOutCentrifugal(Sample sample, Func<ushort, Task<bool>> func, bool isBig, CancellationTokenSource cts);

        //==================================================================离心部分======================================================================================//

        /// <summary>
        /// 从冰浴取试管到移栽   Centrifugal => CentrifugalCarrier => CapperTwo (管理冰浴仓) => CarrierOne 
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromColdToTransfer(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从净化管架取试管到移栽（离心） Centrifugal => CentrifugalCarrier => CapperThree (决定由哪里取料) => CarrierTwo
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromMarterialToTransfer(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 离心完成后从移栽中取出试管  Centrifugal(大管/小管) => CentrifugalCarrier(大管/小管) => CapperThree/CapperTwo => Carrier
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isBig">大管或者小管</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToMarterial(Sample sample, bool isBig ,CancellationTokenSource cts);

        //==================================================================移液部分（大管到小管  小管到大管）.（农残氮吹移液）======================================================================================//


        /// <summary>
        /// 从拧盖3取净化管到移栽  移液 =》 CentrifugalCarrier => CapperThree => CarrierTwo
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToTransfer(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从移栽取出净化管，空管 或 上清液管   移液 =》 CentrifugalCarrier => CapperThree（装盖完决定放回试管架或振荡） => CarrierTwo
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToMarterialPiperttor(Sample sample, CancellationTokenSource cts);

        //==================================================================移液部分（兽药氮吹移液）大管到西林瓶  兽药======================================================================================//

        /// <summary>
        /// 从拧盖2取无盖试管到移栽      移液 =》 CentrifugalCarrier => CapperTwo（拆盖完）(传入动作) => CarrierOne(传入动作)
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromMaterialToTransfer(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从移栽取回无盖试管到拧盖2    移液 =》 CentrifugalCarrier => CapperTwo（拆盖完）(传入动作) => CarrierOne(传入动作)
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromTransferToMaterial(Sample sample, CancellationTokenSource cts);





    }
}