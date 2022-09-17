using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common
{
    public static class TechStatusHelper
    {
        /// <summary>
        /// 判断某位是否为1
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bit"></param>
        /// <returns></returns>
        public static bool BitIsOn(TechParams tech, int bit)
        {
            int value = tech.Tech;
            int temp = 1 << bit;
            return (value & temp) == temp;
        }

        public static bool BitIsOn(TechParams tech, TechStatus index)
        {
            return TechStatusHelper.BitIsOn(tech, (int)index);
        }


        /// <summary>
        /// 设定某位On
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bit"></param>
        public static void SetBitOn(TechParams tech, int bit)
        {
            int temp = 1 << bit;
            tech.Tech = tech.Tech | temp;
        }

        public static void SetBitOn(TechParams tech, TechStatus index)
        {
            TechStatusHelper.SetBitOn(tech, (int)index);
        }

        /// <summary>
        /// 复位某位
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="bit"></param>
        public static void ResetBit(TechParams tech, int bit)
        {
            int temp = ~(1 << bit);
            tech.Tech = tech.Tech & temp;
        }

        public static void ResetBit(TechParams tech, TechStatus index)
        {
            TechStatusHelper.ResetBit(tech, (int)index);
        }
    }
}
