using BQJX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common
{
    public static class SampleStatusHelper
    {
        /// <summary>
        /// 判断某位是否为1
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bit"></param>
        /// <returns></returns>
        public static bool BitIsOn(Sample sample, int bit)
        {
            uint value = sample.Status;
            int temp = 1 << bit;
            return (value & temp) == temp;
        }

        public static bool BitIsOn(Sample sample, SampleStatus index)
        {
            return SampleStatusHelper.BitIsOn(sample, (int)index);
        }


        /// <summary>
        /// 设定某位On
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bit"></param>
        public static void SetBitOn(Sample sample, int bit)
        {
            int temp = 1 << bit;
            sample.Status = (uint)(sample.Status | temp);
        }

        public static void SetBitOn(Sample sample, SampleStatus index)
        {
            SampleStatusHelper.SetBitOn(sample, (int)index);
        }

        /// <summary>
        /// 复位某位
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="bit"></param>
        public static void ResetBit(Sample sample, int bit)
        {
            int temp = ~(1 << bit);
            sample.Status = (uint)(sample.Status & temp);
        }

        public static void ResetBit(Sample sample, SampleStatus index)
        {
            SampleStatusHelper.ResetBit(sample, (int)index);
        }
    }
}
