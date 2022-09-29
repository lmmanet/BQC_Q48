using BQJX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICapperFive
    {

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> GoHome(CancellationTokenSource cts);

        /// <summary>
        /// 提取净化管样品液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoPipettingFromCapperThreeToBottle(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 提取浓缩样品液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoPipettingFromCapperFourToBottle(Sample sample, CancellationTokenSource cts);
    }

}
