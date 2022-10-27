using BQJX.Common;
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
            Sample sample = value as Sample;
            if (sample != null)
            {
                int i = sample.MainStep;
                int param = sample.SubStep;
                switch (i)
                {
                    case 1: //加水提取
                        if (param == 0)
                        {
                            return "等待加水...";
                        }
                        if (param == 1)
                        {
                            return "加水中...";
                        }
                        if (param == 2)
                        {
                            return "加均质子中...";
                        }
                        if (param == 3)
                        {
                            return "加水完成...";
                        }
                        if (param == 4)
                        {
                            return "准备振荡混匀...";
                        }
                        if (param == 5)
                        {
                            return "搬运到振荡1...";
                        }
                        if (param == 6)
                        {
                            return "振荡混匀中...";
                        }
                        if (param == 7)
                        {
                            return "完成振荡1";
                        }
                        if (param == 8)
                        {
                            return "等待涡旋混匀...";
                        }
                        if (param == 9)
                        {
                            return "准备涡旋混匀";
                        }
                        if (param == 10)
                        {
                            return "搬运到涡旋...";
                        }
                        if (param == 11)
                        {
                            return "涡旋中...";
                        }
                        if (param == 12)
                        {
                            return "完成涡旋";
                        }
                        if (param == 13)
                        {
                            return "完成涡旋";
                        }
                        if (param == 14)
                        {
                            return "回湿中...";
                        }
                        break;
                    case 2: //加溶剂提取
                        if (param == 0)
                        {
                            return "等待添加溶剂...";
                        }
                        if (param == 1)
                        {
                            return "加溶剂中...";
                        }
                        if (param == 2)
                        {
                            return "加盐中...";
                        }
                        if (param == 3)
                        {
                            return "加溶剂完成...";
                        }
                        if (param == 4)
                        {
                            return "准备振荡混匀...";
                        }
                        if (param == 5)
                        {
                            return "搬运到振荡1...";
                        }
                        if (param == 6)
                        {
                            return "振荡混匀中...";
                        }
                        if (param == 7)
                        {
                            return "完成振荡1";
                        }
                        if (param == 8)
                        {
                            return "等待涡旋混匀...";
                        }
                        if (param == 9)
                        {
                            return "准备涡旋混匀";
                        }
                        if (param == 10)
                        {
                            return "搬运到涡旋...";
                        }
                        if (param == 11)
                        {
                            return "涡旋中...";
                        }
                        if (param == 12)
                        {
                            return "完成涡旋";
                        }
                        if (param == 13)
                        {
                            return "完成涡旋";
                        }
                        break;
                    case 3: //加盐提取
                        if (param == 0)
                        {
                            return "等待加入盐包...";
                        }
                        if (param == 1)
                        {
                            return "加溶剂中...";
                        }
                        if (param == 2)
                        {
                            return "加盐中...";
                        }
                        if (param == 3)
                        {
                            return "加盐包完成...";
                        }
                        if (param == 4)
                        {
                            return "准备振荡混匀...";
                        }
                        if (param == 5)
                        {
                            return "搬运到振荡1...";
                        }
                        if (param == 6)
                        {
                            return "振荡混匀中...";
                        }
                        if (param == 7)
                        {
                            return "完成振荡1";
                        }
                        if (param == 8)
                        {
                            return "等待涡旋混匀...";
                        }
                        if (param == 9)
                        {
                            return "准备涡旋混匀";
                        }
                        if (param == 10)
                        {
                            return "搬运到涡旋...";
                        }
                        if (param == 11)
                        {
                            return "涡旋中...";
                        }
                        if (param == 12)
                        {
                            return "完成涡旋";
                        }
                        if (param == 13)
                        {
                            return "完成涡旋";
                        }
                        break;
                    case 4: //一次离心
                        if (param == 0)
                        {
                            return "等待一次离心...";
                        }
                        if (param == 1)
                        {
                            return "搬运到离心机...";
                        }
                        if (param == 2)
                        {
                            return "一次离心中...";
                        }
                        if (param == 3)
                        {
                            return "离心完成,搬运中...";
                        }
                        if (param == 4)
                        {
                            return "离心完成,搬运中...";
                        }
                        if (param == 5)
                        {
                            return "完成一次离心";
                        }
                        break;
                    case 5: //提取上清液
                        if (param == 0)
                        {
                            return "等待提取上清液...";
                        }
                        if (param == 1)
                        {
                            return "准备提取上清液...";
                        }
                        if (param == 2)
                        {
                            return "准备净化管...";
                        }
                        if (param == 3)
                        {
                            return "加活化液中...";
                        }
                        if (param == 4)
                        {
                            return "加活化液完成";
                        }
                        if (param == 5)
                        {
                            return "活化液等待振荡混匀...";
                        }
                        if (param == 6)
                        {
                            return "活化液振荡混匀中...";
                        }
                        if (param == 7)
                        {
                            return "活化液振荡完成";
                        }
                        if (param == 8)
                        {
                            return "活化管中...";
                        }
                        if (param == 9)
                        {
                            return "等待加入上清液...";
                        }
                        if (param == 10)
                        {
                            return "等待加入上清液...";
                        }
                        if (param == 11)
                        {
                            return "搬运到移栽,等待加入上清液...";
                        }
                        if (param == 12)
                        {
                            return "净化管/活化管准备完成,等待加入上清液...";
                        }
                        if (param == 13)
                        {
                            return "提取上清液,准备离心管...";
                        }
                        if (param == 14)
                        {
                            return "提取上清液中...";
                        }
                        if (param == 15)
                        {
                            return "提取上清液完成";
                        }
                        if (param == 16)
                        {
                            return "搬运回净化/活化管...";
                        }
                        if (param == 17)
                        {
                            return "净化/活化管完成装盖,等待振荡...";
                        }
                        if (param == 18)
                        {
                            return "净化/活化管准备振荡...";
                        }
                        if (param == 19)
                        {
                            return "净化/活化管振荡中...";
                        }
                        if (param == 20)
                        {
                            return "净化/活化管振荡中...";
                        }
                        if (param == 21)
                        {
                            return "净化/活化管振荡完成";
                        }
                        if (param == 22)
                        {
                            return "净化/活化管振荡完成,等待二次离心";
                        } 
                        if (param == 23)
                        {
                            return "净化/活化管振荡完成,等待二次离心";
                        }
                        break;
                    case 6: //空
                        break;
                    case 7: //二次离心
                        if (param == 0)
                        {
                            return "等待二次离心...";
                        }
                        if (param == 1)
                        {
                            return "准备二次离心...";
                        }
                        if (param == 2)
                        {
                            return "二次离心中...";
                        }
                        if (param == 3)
                        {
                            return "二次离心完成";
                        }
                        if (param == 4)
                        {
                            return "二次离心完成";
                        }
                        if (param == 5)
                        {
                            return "二次离心完成";
                        }
                        break;
                    case 8: //提取净化液
                        if (param == 0)
                        {
                            return "二次离心完成,等待提取上清液";
                        }
                        if (param == 1)
                        {
                            return "准备提取上清液";
                        }
                        if (param == 2)
                        {
                            return "提取上清液拆盖中";
                        }
                        if (param == 3)
                        {
                            return "搬运活化管到移栽,准备提取活化液";
                        }
                        if (param == 4)
                        {
                            return "搬运活化管到移栽完成";
                        }
                        if (param == 11)
                        {
                            return "搬运活化管到移栽完成";
                        }
                        if (param == 12)
                        {
                            return "搬运活化管到移栽完成,准备提取活化液";
                        }
                        if (param == 13)
                        {
                            return "提取活化液中...";
                        }
                        if (param == 14)
                        {
                            return "提取活化液完成,萃取管等待振荡涡旋混匀...";
                        }
                        if (param == 15)
                        {
                            return "空活化管搬运中...";
                        }
                        if (param >= 16)
                        {
                            return "回收空活化管中...";
                        }
                        break;
                    case 9: //萃取振荡涡旋
                        if (param == 0)
                        {
                            return "萃取管等待振荡涡旋混匀";
                        }
                        if (param == 1)
                        {
                            return "萃取管等待振荡涡旋混匀";
                        }
                        if (param == 2)
                        {
                            return "萃取管等待振荡涡旋混匀";
                        }
                        if (param == 3)
                        {
                            return "萃取管等待振荡涡旋混匀";
                        }
                        if (param == 4)
                        {
                            return "萃取管振荡中...";
                        }
                        if (param == 5)
                        {
                            return "萃取管涡旋中...";
                        }
                        if (param == 6)
                        {
                            return "萃取管完成振荡涡旋,等待离心";
                        }
                        break;
                    case 10: //三次离心
                        if (param == 0)
                        {
                            return "萃取管完成振荡涡旋,等待离心";
                        }
                        if (param == 1)
                        {
                            return "萃取管准备离心";
                        }
                        if (param == 2)
                        {
                            return "萃取管离心中...";
                        }
                        if (param == 3)
                        {
                            return "萃取管离心完成";
                        }
                        if (param == 4)
                        {
                            return "萃取管离心完成";
                        }
                        if (param == 5)
                        {
                            return "萃取管离心完成";
                        }
                        break;
                    case 11: //提取浓缩液  兽药提取上清液
                        if (param == 0)
                        {
                            return "萃取管等待提取上清液";
                        }
                        if (param == 1)
                        {
                            return "萃取管准备提取上清液";
                        }
                        break;
                    case 12: //兽药提取上清液
                        if (param == 0)
                        {
                            return "萃取管提取上清液中...";
                        }
                        break;
                    case 13: //兽药提取上清液
                        if (param == 0)
                        {
                            return "萃取管提取上清液中...";
                        }
                        break;
                    case 14: //兽药提取上清液
                        if (param == 0)
                        {
                            return "回收空萃取管...";
                        }
                        break;
                    case 15: //兽药提取上清液  
                        if (param == 0)
                        {
                            return "回收空萃取管...";
                        }
                        break;
                    case 16: //浓缩
                        return "搬运样品到浓缩...";
                    case 17:
                        return "样品浓缩中...";
                    case 18:
                        return "样品浓缩完成,称重判断中...";
                    case 19:
                        return "样品浓缩完成,定容复溶中...";
                    case 20:
                        return "样品定容复溶完成";
                    case 21:
                        return "提取样品液到小瓶中...";
                    case 22:
                        return "提取样品液到小瓶完成,回收西林瓶";
                    case 23:
                        return "样品处理完成";
                    case 30:
                        return "样品处理完成";

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
