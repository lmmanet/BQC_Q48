using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Converts
{
    public class MotionConvert : BaseValueConverter<MotionConvert>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return false;
            }
            else
            {
                int v = (int)value;
                int p = int.Parse(parameter.ToString());
                return ((v >> p & 1) == 1);
            }
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
