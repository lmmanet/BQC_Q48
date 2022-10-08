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

        //===================================样品管部分=======================================//

        /// <summary>
        /// 从加固取样品管到拧盖1
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromAddSolidToCapperOne(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖1取样品管到加固
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func1"></param>
        /// <param name="func2"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperOneToAddSolid(Sample sample, Func<bool> func1, Func<bool> func2, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖1搬运试管到振荡
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperOneToVibration(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖1搬运样品管到试管架1
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperOneToMaterial(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从试管架取样品管到拧盖1
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromMaterialToCapperOne(Sample sample, CancellationTokenSource cts);


        /// <summary>
        /// 从试管架取试管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromMaterialToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);
        /// <summary>
        /// 从振荡1取试管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromVibrationToMaterial(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从涡旋搬运样品管到试管架1
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromVortexToMaterial(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从振荡1取样品管到冰浴
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromVibrationToCold(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从移栽取样品管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToMaterial(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖2取样品管到试管架（移液后）
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperTwoToMaterial(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从冰浴取样品管到移栽 （离心前）
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromColdToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);

        /// <summary>
        /// 从振荡1取样品管到移栽 （振荡完离心前）
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromVibrationOneToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        /// <summary>
        /// 取样品离心管到拧盖2
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromMaterialToCapperTwo(Sample sample, CancellationTokenSource cts);

        


        bool GetSampleToVortex(Sample sample, CancellationTokenSource cts);

        bool GetSampleToVibration(Sample sample, CancellationTokenSource cts);

        //===================================移液部分=======================================//

        bool DoPipetting(Sample sample, bool bigToSmall, CancellationTokenSource cts);


        //===================================萃取管部分=======================================//

        /// <summary>
        /// 从拧盖2取萃取管到冰浴
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromCapperTwoToCold(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从移栽取萃取无盖管到拧盖2
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromTransferToCapperTwo(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖2取萃取管到试管架2
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromCapperTwoToMaterial(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 取预放粉包，均质子离心管到拧盖2 取离心完成后除脂萃取试管
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromMaterialToCapperTwo(Sample sample, CancellationTokenSource cts);


        /// <summary>
        /// 从试管架取萃取管到振荡1  振荡
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromMaterialToVibration(Sample sample, CancellationTokenSource cts);


        /// <summary>
        /// 从拧盖2取无盖萃取管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromCapperTwoToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);

        /// <summary>
        /// 从振荡1取萃取管到冰浴
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func1"></param>
        /// <param name="func2"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromVibrationToCold(Sample sample, Func<bool> func1, Func<bool> func2, CancellationTokenSource cts);

        /// <summary>
        /// 从涡旋取萃取管到冰浴
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromVortexToCold(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从振荡1取萃取管到涡旋
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func1"></param>
        /// <param name="func2"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromVibrationToVortex(Sample sample, Func<bool> func1, Func<bool> func2, CancellationTokenSource cts);

        bool GetPolishFromVibrationToMaterial(Sample sample, CancellationTokenSource cts);

        bool GetPolishFromVortexToMaterial(Sample sample, CancellationTokenSource cts);
        /// <summary>
        /// 从拧盖2取萃取管到振荡
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromCapperTwoToVibration(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从冰浴搬运萃取管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromColdToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);

        /// <summary>
        /// 从试管架搬运萃取管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromMaterialToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);

        /// <summary>
        /// 从离心移栽取出离心完后的萃取管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromTransferToMaterial(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts);
    }
}
