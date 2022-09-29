using BQJX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface IVibrationTwo
    {

        /// <summary>
        /// 振荡回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> GoHome(CancellationTokenSource cts);

        /// <summary>
        /// 提取完上清液振荡
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool StartVibrationOne(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 提取上清液前振荡  兽药 加入醋酸铵水溶液后
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool StartVibrationTwo(Sample sample, CancellationTokenSource cts);

    }
}
