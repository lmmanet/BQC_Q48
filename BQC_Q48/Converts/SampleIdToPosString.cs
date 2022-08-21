using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Converts
{
    public class SampleIdToPosString : BaseValueConverter<SampleIdToPosString>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int i;
            if (int.TryParse(value.ToString(), out i))
            {
                if (i <= 2)
                {
                    return $"A{3*i-2}-A{3*i}";
                }
                if (i > 2&& i<=4)
                {
                    return $"B{3 * (i-2) - 2}-B{3 * (i-2)}";
                }
                if (i > 4 && i <= 6)
                {
                    return $"C{3 * (i - 4) - 2}-C{3 * (i - 4)}";
                }
                if (i > 6 && i <= 8)
                {
                    return $"D{3 * (i - 6) - 2}-D{3 * (i - 6)}";
                }
                if (i > 8 && i <= 10)
                {
                    return $"E{3 * (i - 8) - 2}-E{3 * (i - 8)}";
                }
                if (i > 10 && i <= 12)
                {
                    return $"F{3 * (i - 10) - 2}-F{3 * (i - 10)}";
                }
                if (i > 12 && i <= 14)
                {
                    return $"G{3 * (i - 12) - 2}-G{3 * (i - 12)}";
                }
                if (i > 14 && i <= 16)
                {
                    return $"H{3 * (i - 14) - 2}-H{3 * (i - 14)}";
                }
           

            }
            return "";
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
