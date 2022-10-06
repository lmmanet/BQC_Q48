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





        Task<bool> GetSeilingAndWeight(Sample sample, CancellationTokenSource cts);



        /// <summary>
        /// 农残移液  从净化管 ==》 西林瓶  从净化管==》小瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">1:浓缩移液 2:提取样品</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoPipettingOne(Sample sample,int var, CancellationTokenSource cts);



    }

}
