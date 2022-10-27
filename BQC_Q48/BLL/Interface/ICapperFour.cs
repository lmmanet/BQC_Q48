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
    public interface ICapperFour
    {

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="gs"></param>
        /// <returns></returns>
        Task<bool> GoHome(IGlobalStatus gs);


        /// <summary>
        /// 农残浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        bool DoConcentrationOne(Sample sample, IGlobalStatus gs);

        /// <summary>
        /// 兽残浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoConcentrationTwo(Sample sample, IGlobalStatus gs);





        Task<bool> GetSeilingAndWeight(Sample sample, IGlobalStatus gs);



        /// <summary>
        /// 农残移液  从净化管 ==》 西林瓶  从净化管==》小瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">1:浓缩移液 2:提取样品</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        bool DoPipettingOne(Sample sample,int var, IGlobalStatus gs);

        void UpdatePosData();
        CapperInfo GetCapperInfo();

    }

}
