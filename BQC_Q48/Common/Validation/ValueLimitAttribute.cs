using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common
{
    public class ValueLimitAttribute : Attribute
    {
        public double MaxValue { get; set; }
        public double MinValue { get; set; }
        public ValueLimitAttribute(double maxValue, double minValue)
        {
            this.MaxValue = maxValue;
            this.MinValue = minValue;
        }
    }
}
