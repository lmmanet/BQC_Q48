using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Communication.DataConvert
{
    public static class BoolArray
    {
        public static byte ToByte(bool[] value)
        {
            List<bool> valueList = new List<bool>(value);
            if (value.Length < 8)
            {
                for (int j = 0; j < 8 - value.Length; j++)
                {
                    valueList.Add(false);
                }
            }
            int sum = 0;
            for (int i = 0; i < valueList.Count; i++)
            {
                if (i > 7)
                {
                    break;
                }
                if (valueList[i])
                {
                    sum += (int)Math.Pow(2, i);
                }
            }
            return (byte)sum;
        }
        public static bool[] ByteToBoolArray(byte value)
        {
            int intValue = (int)value;
            List<bool> result = new List<bool>(8);
            for (int i = 0; i < 8; i++)
            {
                bool flag = (intValue & ((int)Math.Pow(2, i))) == (int)Math.Pow(2, i);
                result.Add(flag);
            }
            result.Reverse();
            return result.ToArray();
        }
    }
}
