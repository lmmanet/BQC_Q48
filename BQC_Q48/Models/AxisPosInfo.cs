using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;

namespace Q_Platform.Models
{
    [AddINotifyPropertyChangedInterface]
    public class AxisPosInfo
    {
        /// <summary>
        /// 轴名
        /// </summary>
        [DoNotNotify]
        public string AxisName { get; set; }

        /// <summary>
        /// 轴ID
        /// </summary>
        [DoNotNotify]
        public ushort AxisNo { get; set; }

        /// <summary>
        /// 点位数据别名
        /// </summary>
        [DoNotNotify]
        public string PosName { get; set; }

        /// <summary>
        /// 点位数据值
        /// </summary>
        public double PosData { get; set; }

        /// <summary>
        /// 点位数据成员属性名
        /// </summary>
        [DoNotNotify]
        public string MemberName { get; set; }
    }
}
