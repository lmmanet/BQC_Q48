using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.Common
{
    public class CapperInfo
    {
        /// <summary>
        /// 1~5
        /// </summary>
        public int CapperId { get; set; }
        public string CapperName { get; set; }
        public ushort AxisY { get; set; }
        public ushort AxisC1 { get; set; }
        public ushort AxisC2 { get; set; }
        public ushort AxisZ { get; set; }
        public ushort ClawCtl { get; set; }
        public ushort HoldCtl { get; set; }
        public ushort Hold_HP { get; set; }
        public ushort Hold_WP { get; set; }

        public int CapperOnTorque { get; set; }
        public double CapperOffDistance { get; set; }


    }
}
