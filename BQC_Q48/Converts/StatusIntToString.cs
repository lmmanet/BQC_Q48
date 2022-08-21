using System;
using System.Globalization;

namespace BQJX.Converts
{
    public class StatusIntToString : BaseValueConverter<StatusIntToString>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            int i;
            if (int.TryParse(value.ToString(), out i))
            {
                switch (i)
                {
                    default:
                        break;
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
