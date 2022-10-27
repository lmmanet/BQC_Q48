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
            int i ,param;
            if (int.TryParse(value.ToString(), out i) && int.TryParse(parameter.ToString(),out param))
            {
                if (param == 1)
                {
                    switch (i)
                    {
                        case 1:
                            return "A1==>A1";
                        case 2:
                            return "C1==>C1";
                        case 3:
                            return "E1==>E1";
                        case 4:
                            return "G1==>G1";
                        case 5:
                            return "A2==>I1";
                        case 6:
                            return "C2==>K1";
                        case 7:
                            return "E2==>A2";
                        case 8:
                            return "G2==>C2";
                        case 9:
                            return "A3==>E2";
                        case 10:
                            return "C3==>G2";
                        case 11:
                            return "E3==>I2";
                        case 12:
                            return "G3==>K2";
                        case 13:
                            return "A1==>A3";
                        case 14:
                            return "C1==>C3";
                        case 15:
                            return "E1==>E3";
                        case 16:
                            return "G1==>G3";
                        case 17:
                            return "A2==>I3";
                        case 18:
                            return "C2==>K3";
                        case 19:
                            return "E2==>A4";
                        case 20:
                            return "G2==>C4";
                        case 21:
                            return "A3==>E4";
                        case 22:
                            return "C3==>G4";
                        case 23:
                            return "E3==>I4";
                        case 24:
                            return "G3==>K4";
                        case 25:
                            return "";
                        case 26:
                            return "";
                        case 27:
                            return "";
                        case 28:
                            return "";
                        case 29:
                            return "";
                        case 30:
                            return "";
                        case 31:
                            return "";
                        case 32:
                            return "";
                        case 33:
                            return "";
                        case 34:
                            return "";
                        case 35:
                            return "";
                        case 36:
                            return "";
                        case 37:
                            return "";
                        case 38:
                            return "";
                        case 39:
                            return "";
                        case 40:
                            return "";
                        case 41:
                            return "";
                        case 42:
                            return "";
                        case 43:
                            return "";
                        case 44:
                            return "";
                        case 45:
                            return "";
                        case 46:
                            return "";
                        case 47:
                            return "";
                        case 48:
                            return "";
                        default:
                            break;
                    }
                }

                if (param == 2)
                {
                    switch (i)
                    {
                        case 1:
                            return "B1==>B1";
                        case 2:
                            return "D1==>D1";
                        case 3:
                            return "F1==>F1";
                        case 4:
                            return "H1==>H1";
                        case 5:
                            return "B2==>J1";
                        case 6:
                            return "D2==>L1";
                        case 7:
                            return "F2==>B2";
                        case 8:
                            return "H2==>D2";
                        case 9:
                            return "B3==>F2";
                        case 10:
                            return "D3==>H2";
                        case 11:
                            return "F3==>J2";
                        case 12:
                            return "H3==>L2";
                        case 13:
                            return "B1==>B3";
                        case 14:
                            return "D1==>D3";
                        case 15:
                            return "F1==>F3";
                        case 16:
                            return "H1==>H3";
                        case 17:
                            return "B2==>J3";
                        case 18:
                            return "D2==>L3";
                        case 19:
                            return "F2==>B4";
                        case 20:
                            return "H2==>D4";
                        case 21:
                            return "B3==>F4";
                        case 22:
                            return "D3==>H4";
                        case 23:
                            return "F3==>J4";
                        case 24:
                            return "H3==>L4";
                        case 25:
                            return "";
                        case 26:
                            return "";
                        case 27:
                            return "";
                        case 28:
                            return "";
                        case 29:
                            return "";
                        case 30:
                            return "";
                        case 31:
                            return "";
                        case 32:
                            return "";
                        case 33:
                            return "";
                        case 34:
                            return "";
                        case 35:
                            return "";
                        case 36:
                            return "";
                        case 37:
                            return "";
                        case 38:
                            return "";
                        case 39:
                            return "";
                        case 40:
                            return "";
                        case 41:
                            return "";
                        case 42:
                            return "";
                        case 43:
                            return "";
                        case 44:
                            return "";
                        case 45:
                            return "";
                        case 46:
                            return "";
                        case 47:
                            return "";
                        case 48:
                            return "";
                        default:
                            break;
                    }
                }
               
            }
            return "";
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }




}
