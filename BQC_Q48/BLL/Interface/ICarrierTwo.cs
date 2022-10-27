using BQJX.Common;
using BQJX.Common.Interface;
using Q_Platform.Common;
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
        Task<bool> GoHome(IGlobalStatus gs);

        void UpdatePosData();

        CarrierInfo GetCarrierInfo();

        //加标    清洗     浓缩混匀     取样1 2 

        //=========================================搬运=========================================================//


        /// <summary>
        /// 从试管架搬运净化管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromMaterialToCapperThree(Sample sample, IGlobalStatus gs);

        /// <summary>
        /// 从拧盖3搬运净化管到振荡
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToVibration(Sample sample, IGlobalStatus gs);

        /// <summary>
        /// 从振荡搬运净化管到哪拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromVibrationToCapperThree(Sample sample, IGlobalStatus gs);

        /// <summary>
        /// 从振荡搬运净化管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromVibrationToMaterial(Sample sample ,IGlobalStatus gs);

        //=======离心移栽===//

        /// <summary>
        /// 从试管架取净化管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromMaterialToTransfer(Sample sample, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs);

        /// <summary>
        /// 从移栽取净化管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToMaterial(Sample sample, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs);

        /// <summary>
        /// 从拧盖3取净化管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToTransfer(Sample sample, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs);

        /// <summary>
        /// 从拧盖3取净化管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToMaterial(Sample sample, IGlobalStatus gs);

        /// <summary>
        /// 从移栽取净化管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToCapperThree(Sample sample, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs);

   



        bool GetSampleToTransfer(Sample sample, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs);



        //========================================西林瓶========================================================//

        /// <summary>
        /// 从西林瓶架搬运西林瓶到拧盖4
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSelingFromMaterialToCapperFour(Sample sample, IGlobalStatus gs);

        /// <summary>
        /// 从拧盖4搬运西林瓶到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSelingFromCapperFourToMaterial(Sample sample, IGlobalStatus gs);

        /// <summary>
        /// 从拧盖4搬运西林瓶到浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSelingFromCapperFourToConcentration(Sample sample, IGlobalStatus gs);

        /// <summary>
        /// 从浓缩搬运西林瓶到拧盖4
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSelingFromConcentrationToCapperFour(Sample sample, IGlobalStatus gs);


        /// <summary>
        /// 从拧盖4搬运西林瓶到称重 并搬运回
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSelingFromCapperFourToWeightAndBack(Sample sample, int var,double volume, IGlobalStatus gs);

        /// <summary>
        /// 从浓缩搬运西林瓶到称重 并搬运回
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">加标种类1~4 0：不加</param>
        /// <param name="volume">加标量</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSelingFromConcentrationToWeight(Sample sample, int var, double volume, IGlobalStatus gs);

        //========================================进样小瓶=========================================================//

        /// <summary>
        /// 从小瓶架搬运小瓶到拧盖5
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isFirst">是否是第一个样品</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetBottleFromMaterialToCapperFive_One(Sample sample, IGlobalStatus gs);




        //========================================移液=========================================================//

        /// <summary>
        /// 第一组移液  从净化管到小瓶  从西林瓶到小瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">  1:从净化管到小瓶  2:从西林瓶到小瓶</param>
        /// <param name="capperOn">小瓶装盖</param>
        /// <param name="capperOff">小瓶拆盖</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoPipettingOne(Sample sample, int var, Func<Sample, int,IGlobalStatus, bool> capperOn, Func<Sample,int, IGlobalStatus, bool> capperOff, IGlobalStatus gs);


        /// <summary>
        /// 第二组移液 浓缩 净化管（2ml） ==》西林瓶   大管==》西林瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">1:净化管（2ml） ==》西林瓶 2:大管==》西林瓶</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoPipettingTwo(Sample sample, int var, IGlobalStatus gs);

      


    }
}
