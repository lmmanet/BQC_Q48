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

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> GoHome(CancellationTokenSource cts);

        /// <summary>
        /// 从冰浴取试管到移栽  拧盖无需锁 只做中转作用
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
        /// 从试管架2搬运无盖萃取管到移栽移液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromMaterialToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        /// <summary>
        /// 从移栽搬运萃取空管到拧盖2   
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromTransferToCapperTwo(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖2搬运萃取空管到试管架2  拧盖2内部（装盖，下料到试管架） 或搬运取完上清液的萃取管等待振荡
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromCapperTwoToMaterial(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖2搬运萃取管到冰浴 
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromCapperTwoToCold(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从试管架2搬运萃取管到拧盖2 接受上清液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetPolishFromMaterialToCapperTwo(Sample sample, CancellationTokenSource cts);


        //================================================移液部分 =================================================//

        /// <summary>
        /// 从试管架1取样品管到拧盖2移去上清液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromMaterialToCapperTwo(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 从拧盖2取回移液完后的样品管到试管架1
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromCapperTwoToMaterial(Sample sample, CancellationTokenSource cts);

        bool DoPipettingOne(Sample sample, CancellationTokenSource cts);
        bool DoPipettingTwo(Sample sample, CancellationTokenSource cts);


    }

}
