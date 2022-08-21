using System;
using System.Globalization;
using System.Windows;

namespace BQJX.Converts
{
    public class IntToVisibilityConverter : BaseValueConverter<IntToVisibilityConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int i, param;
            if (int.TryParse(value.ToString(), out i) && int.TryParse(parameter.ToString(), out param))
            {
                if (i == param)
                {
                    return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
