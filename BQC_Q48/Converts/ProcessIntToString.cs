using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Converts
{
    public class ProcessIntToString : BaseValueConverter<ProcessIntToString>
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
