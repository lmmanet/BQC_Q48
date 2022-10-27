using BQJX.Common;
using BQJX.Common.Interface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICentrifugalCarrier
    {
        Task<bool> GoHome(IGlobalStatus gs);

        bool IsPipttorTaskDone { get; }

        bool IsConcentrationTaskDone { get; }

        void UpdatePosData();
        void AddSampleToPipettingList(Sample sample, string actionCallBack);
        void AddSampleToConcentrationList(Sample sample);

        void ClearTubeOut();

        Task StartPipetting(IGlobalStatus gs);

        Task StartConcentration(IGlobalStatus gs);

        //==================================================================离心移栽部分======================================================================================//

        bool GetSampleFromColdToCentrifugal(Sample sample, Func<ushort, Task<bool>> GoStation, IGlobalStatus gs);

        bool GetSampleFromCentrifugalToMaterial(Sample sample, Func<ushort, Task<bool>> GoStation, IGlobalStatus gs);

        bool GetPolishFromMaterialToCentrifugal(Sample sample, Func<ushort, Task<bool>> GoStation, IGlobalStatus gs);

        bool GetPolishFroCentrifugaToShelf(Sample sample, Func<ushort, Task<bool>> GoStation, IGlobalStatus gs);

        bool GetPurifyFromMaterialToCentrifugal(Sample sample, Func<ushort, Task<bool>> GoStation, IGlobalStatus gs);

        bool GetPurifyFromCentrifugalToMaterial(Sample sample, Func<ushort, Task<bool>> GoStation, IGlobalStatus gs);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sample1">大管</param>
        /// <param name="sample2">小管</param>
        /// <param name="var"></param>
        /// <param name="GoStation"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetBigAndSmallToCentrifugal(Sample sample1, Sample sample2, int var, Func<ushort, Task<bool>> GoStation, IGlobalStatus gs);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sample1">大管</param>
        /// <param name="sample2">小管</param>
        /// <param name="var"></param>
        /// <param name="GoStation"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetBigAndSmallToToMarterial(Sample sample1, Sample sample2, int var, Func<ushort, Task<bool>> GoStation, IGlobalStatus gs);

        /// <summary>
        /// 搬运试管到离心机  （离心机调用）
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">离心机动作GoStation</param>
        /// <param name="varTube">1:样品大管 2;净化小管 3;萃取大管</param>
        /// <param name="cts"></param>
        /// <returns></returns>
       // bool GetTubeInCentrifugal(Sample sample, Func<ushort, Task<bool>> func, int varTube, IGlobalStatus gs);

        /// <summary>
        /// 从离心机取出试管  （离心机调用）
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">离心机动作GoStation</param>
        /// <param name="varTube">1:样品大管 2;净化小管 3;萃取大管</param>
        /// <param name="cts"></param>
        /// <returns></returns>
       // bool GetTubeOutCentrifugal(Sample sample, Func<ushort, Task<bool>> func, int varTube, IGlobalStatus gs);

        //==================================================================离心部分======================================================================================//

        ///// <summary>
        ///// 从冰浴取试管到移栽  
        ///// </summary>
        ///// <param name="sample"></param>
        ///// <param name="cts"></param>
        ///// <returns></returns>
        //bool GetSampleFromColdToTransfer(Sample sample, IGlobalStatus gs);

        ///// <summary>
        ///// 从冰浴搬运萃取管到移栽
        ///// </summary>
        ///// <param name="sample"></param>
        ///// <param name="cts"></param>
        ///// <returns></returns>
        //bool GetPolishFromColdToTransfer(Sample sample, IGlobalStatus gs);

        ///// <summary>
        ///// 从试管架搬运净化管到移栽
        ///// </summary>
        ///// <param name="sample"></param>
        ///// <param name="cts"></param>
        ///// <returns></returns>
        //bool GetPurifyFromMaterialToTransfer(Sample sample, IGlobalStatus gs);



        ///// <summary>
        ///// 从移栽取离心完成后的试管到试管架
        ///// </summary>
        ///// <param name="sample"></param>
        ///// <param name="isBig"></param>
        ///// <param name="cts"></param>
        ///// <returns></returns>
        //bool GetSampleFromTransferToMarterial(Sample sample, IGlobalStatus gs);

        ///// <summary>
        ///// 从移栽取离心完成后的萃取管到试管架
        ///// </summary>
        ///// <param name="sample"></param>
        ///// <param name="isBig"></param>
        ///// <param name="cts"></param>
        ///// <returns></returns>
        //bool GetPolishFromTransferToMarterial(Sample sample, IGlobalStatus gs);

        ///// <summary>
        ///// 离心完成后从移栽中取净化管  
        ///// </summary>
        ///// <param name="sample"></param>
        ///// <param name="cts"></param>
        ///// <returns></returns>
        //bool GetPurifyFromTransferToMarterial(Sample sample, IGlobalStatus gs);

        //==================================================================移液部分（大管到小管  小管到大管）.（农残氮吹移液）======================================================================================//




        /// <summary>
        /// 从移栽取出净化管，空管 或 上清液管   移液 =》 CentrifugalCarrier => CapperThree（装盖完决定放回试管架或振荡） => CarrierTwo
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        //bool GetSampleFromTransferToMarterialPiperttor(Sample sample, IGlobalStatus gs);

        //==================================================================移液部分（兽药氮吹移液）大管到西林瓶  兽药======================================================================================//

        ///// <summary>
        ///// 从移栽取回无盖试管到拧盖2    移液 =》 CentrifugalCarrier => CapperTwo（拆盖完）(传入动作) => CarrierOne(传入动作)
        ///// </summary>
        ///// <param name="sample"></param>
        ///// <param name="cts"></param>
        ///// <returns></returns>
        //bool GetPolishFromTransferToMaterial(Sample sample, IGlobalStatus gs);





    }
}