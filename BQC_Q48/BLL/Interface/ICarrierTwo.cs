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

        bool GetSampleToCapperThree(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> GoHome(CancellationTokenSource cts);

        /// <summary>
        /// 从试管架到拧盖3
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromMaterialToCapperThree(ushort num, CancellationTokenSource cts);

        /// <summary>
        /// 从试管架到振荡
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromMaterialToVibration(ushort num, CancellationTokenSource cts);

        /// <summary>
        /// 从试管架到移栽
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromMaterialToTransfer(ushort num, Func<ushort, bool> func, CancellationTokenSource cts);



        /// <summary>
        /// 从拧盖3到试管架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖3到振荡
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToVibration(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖3到移栽
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="func3">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperThreeToTransfer(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, Func<ushort, bool> func3, CancellationTokenSource cts);




        /// <summary>
        /// 从振荡到试管架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromVibrationToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts);

        /// <summary>
        /// 从振荡到移栽
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="func3">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromVibrationToTransfer(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, Func<ushort, bool> func3, CancellationTokenSource cts);





        /// <summary>
        /// 从移栽到试管架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToMaterial(ushort num, Func<ushort, bool> func, CancellationTokenSource cts);

        /// <summary>
        /// 从移栽到拆盖3
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromTransferToCapperThree(ushort num, Func<ushort, bool> func, CancellationTokenSource cts);






        /// <summary>
        /// 从西林瓶架到拧盖4
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSeilingFromMaterialToCapperFour(ushort num, CancellationTokenSource cts);




        /// <summary>
        /// 从拧盖4到西林瓶架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSeilingFromCapperFourToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖4到浓缩
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSeilingFromCapperFourToConcentration(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖4到称重
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSeilingFromCapperFourToWeight(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts);




        /// <summary>
        /// 从浓缩到拧盖4
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSeilingFromConcentrationToCapperFour(ushort num, CancellationTokenSource cts);

        /// <summary>
        /// 从浓缩到称重
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSeilingFromConcentrationToWeight(ushort num, CancellationTokenSource cts);

        /// <summary>
        /// 从称重到拧盖4
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>

        bool GetSeilingFromWeightToCapperFour(ushort num, CancellationTokenSource cts);

        /// <summary>
        /// 从称重到浓缩
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSeilingFromWeightToConcentration(ushort num,CancellationTokenSource cts);



        



        /// <summary>
        /// 从气质小瓶架到拧盖5
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool Get_GC_BottleFromMaterialToCapperFive(ushort num, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖5到气质小瓶架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool Get_GC_BottleFromCapperFiveToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts);


        /// <summary>
        /// 从液质小瓶架到拧盖5
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool Get_LC_BottleFromMaterialToCapperFive(ushort num, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖5到气质液质小瓶架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool Get_LC_BottleFromCapperFiveToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts);






        /// <summary>
        /// 开始移液  样品移液  浓缩移液 浓缩定容后取样移液  
        /// </summary>
        /// <param name="num"></param>
        /// <param name="src">移液取液</param>
        /// <param name="dst">移液目标吐液位</param>
        /// <param name="volume"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoPipetting(ushort num,double[] src,double[] dst, double volume, CancellationTokenSource cts);



        //加标    清洗     浓缩混匀     取样1 2 








    }
}
