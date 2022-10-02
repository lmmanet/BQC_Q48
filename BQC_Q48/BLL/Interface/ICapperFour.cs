using BQJX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICapperFour
    {

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        Task<bool> GoHome(CancellationTokenSource cts);

        Task StartConcentration(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 农残浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoConcentrationOne(Sample sample, CancellationTokenSource cts);

        /// <summary>
        /// 兽残浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoConcentrationTwo(Sample sample, CancellationTokenSource cts);


        /// <summary>
        /// 从净化管移液到小瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool GetSampleFromPurify(Sample sample, CancellationTokenSource cts);






        Task<bool> GetSeilingAndWeight(Sample sample, CancellationTokenSource cts);




        bool DoPipettingOne(Sample sample, CancellationTokenSource cts);
        bool DoPipettingTwo(Sample sample, CancellationTokenSource cts);


    }

}
