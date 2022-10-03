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

      

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> GoHome(CancellationTokenSource cts);

      

        //加标    清洗     浓缩混匀     取样1 2 

        //=========================================搬运=========================================================//


        /// <summary>
        /// 从试管架搬运净化管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromMaterialToCapperThree(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖3搬运净化管到振荡
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToVibration(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从振荡搬运净化管到哪拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromVibrationToCapperThree(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从振荡搬运净化管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromVibrationToMaterial(Sample sample ,CancellationTokenSource cts);

        //=======离心移栽===//

        /// <summary>
        /// 从试管架取净化管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromMaterialToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);

        /// <summary>
        /// 从移栽取净化管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToMaterial(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖3取净化管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖3取净化管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToMaterial(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从移栽取净化管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToCapperThree(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);

   



        bool GetSampleToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);



        //========================================西林瓶========================================================//

        /// <summary>
        /// 从西林瓶架搬运西林瓶到拧盖4
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSelingFromMaterialToCapperFour(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖4搬运西林瓶到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSelingFromCapperFourToMaterial(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖4搬运西林瓶到浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSelingFromCapperFourToConcentration(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从浓缩搬运西林瓶到拧盖4
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSelingFromConcentrationToCapperFour(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖4搬运西林瓶到称重 并搬运回
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSelingFromCapperFourToWeightAndBack(Sample sample, Func<Sample, CancellationTokenSource, bool> addMarkFunc, CancellationTokenSource cts);

        /// <summary>
        /// 从浓缩搬运西林瓶到称重 并搬运回
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="addMarkFunc"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSelingFromConcentrationToWeight(Sample sample,Func<Sample, CancellationTokenSource, bool> addMarkFunc, CancellationTokenSource cts);

        //========================================进样小瓶=========================================================//

        /// <summary>
        /// 从小瓶架搬运小瓶到拧盖5
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isFirst">是否是第一个样品</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetBottleFromMaterialToCapperFive_One(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从小瓶架搬运小瓶到拧盖5
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isFirst">是否是第一个样品</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetBottleFromMaterialToCapperFive_Two(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖5搬运小瓶到小瓶架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isFirst">是否是第一个样品</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetBottleFromCapperFiveToMaterial_One(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖5搬运小瓶到小瓶架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isFirst">是否是第一个样品</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetBottleFromCapperFiveToMaterial_Two(Sample sample, CancellationTokenSource cts);

        //========================================移液=========================================================//

        /// <summary>
        /// 第一组移液  从净化管到小瓶  从西林瓶到小瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">  1:从净化管到小瓶  2:从西林瓶到小瓶</param>
        /// <param name="func">小瓶上下料动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoPipettingOne(Sample sample, int var, Func<Sample, CancellationTokenSource, bool> func, CancellationTokenSource cts);


        /// <summary>
        /// 第二组移液 浓缩 净化管（2ml） ==》西林瓶   大管==》西林瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">1:净化管（2ml） ==》西林瓶 2:大管==》西林瓶</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoPipettingTwo(Sample sample, int var, CancellationTokenSource cts);

      

        //========================================加标=========================================================//

        /// <summary>
        /// 从取加标液位到称重位
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool AddMarkFromSourceToWeight(Sample sample,int var, CancellationTokenSource cts);


       // bool AddMarkFromSourceToCapperFour(Sample sample,int var, CancellationTokenSource cts);






    }
}
