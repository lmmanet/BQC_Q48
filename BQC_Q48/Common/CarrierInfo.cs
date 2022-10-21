using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.Common
{
    public class CarrierInfo
    {  
        /// <summary>
       /// 1~2
       /// </summary>
        public int CarrierId { get; set; }
        public string CarrierName { get; set; }
        public ushort AxisX{ get; set; }
        public ushort AxisY{ get; set; }
        public ushort AxisZ1{ get; set; }
        public ushort AxisZ2{ get; set; }
        public ushort AxisF { get; set; }
        public ushort ClawId { get; set; }

        public double PutOffNeedle { get; set; }

        public double AbsorbVel { get; set; }
        public double SyringVel { get; set; }

 

    }
}
