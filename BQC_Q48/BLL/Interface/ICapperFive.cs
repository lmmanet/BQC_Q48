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
    public interface ICapperFive
    {

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="gs"></param>
        /// <returns></returns>
        Task<bool> GoHome(IGlobalStatus gs);

        /// <summary>
        /// 提取净化管样品液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        bool DoPipettingFromCapperThreeToBottle(Sample sample, IGlobalStatus gs);

        /// <summary>
        /// 提取浓缩样品液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        bool DoPipettingFromCapperFourToBottle(Sample sample, IGlobalStatus gs);


        /// <summary>
        /// 拆盖  拧盖4调用
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        bool CapperOff(Sample sample, IGlobalStatus gs);
        void UpdatePosData();
        CapperInfo GetCapperInfo();
    }

}
