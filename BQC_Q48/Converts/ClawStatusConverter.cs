using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Converts
{
    public class ClawStatusConverter : BaseValueConverter<ClawStatusConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int i;
            if (int.TryParse(value.ToString(), out i))
            {
                if (i == 177)
                {
                    return "手爪抓取到试管";
                }
                if (i == 179)
                {
                    return "手爪抓取到试管";
                }
                if (i == 59)
                {
                    return "手爪正在移动";
                }
                if (i == 241)
                {
                    return "手爪未抓取到试管";
                }
                if (i == 243)
                {
                    return "手爪未抓取到试管";
                }
                //bit 0 为使能
            }
            return "";
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
