using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Common
{
    public class CapperPosData
    {
        [PosName("拆盖接驳位")]
        public double PutGetPos { get; set; }

        [PosName("拆盖加液位")]
        public double AddLiquidPos { get; set; }

        [PosName("拆盖拆盖位")]
        public double CapperPos { get; set; }

        [PosName("拆盖Z轴拆盖位",Is_Z2_Axis =true)]
        public double CapperPos_Z { get; set; }
    }
}
