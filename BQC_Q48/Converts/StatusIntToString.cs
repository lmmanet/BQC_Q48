using System;
using System.Globalization;

namespace BQJX.Converts
{
    public class StatusIntToString : BaseValueConverter<StatusIntToString>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            int i; string status = string.Empty;
            if (int.TryParse(value.ToString(), out i))
            {
                if ((i & 0x0f) == 0x01)
                {
                    status =  "回零中...";
                }
                if ((i & 0x0f) == 0x02)
                {
                    status = "急停中...";
                }
                if ((i & 0x0f) == 0x04)
                {
                    status = "自动运行中...";
                }
                if ((i & 0x0f) == 0x08)
                {
                    status = "待机中...";
                }

                if ((i & 0xf00) == 0x00)
                {
                    status += "--未回零";
                }
                if ((i & 0x800) == 0x800)
                {
                    status += "--暂停中";
                }
                if ((i & 0x400) == 0x400)
                {
                    status += "--停止中";
                }
                if ((i & 0x200) == 0x200)
                {
                    status += "--回零完成";
                }
                if ((i & 0x100) == 0x100)
                {
                    status += "--发生故障";
                }

            }
            return status;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
