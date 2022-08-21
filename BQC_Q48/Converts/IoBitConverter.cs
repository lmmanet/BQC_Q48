using System;
using System.Globalization;

namespace BQJX.Converts
{
    public class IoBitConverter : BaseValueConverter<IoBitConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return false;
            }
            else
            {
                uint i = (uint)value;
                int a = (int)Math.Pow(2, int.Parse(parameter.ToString()));
                return (i & a) == a;
            }
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
