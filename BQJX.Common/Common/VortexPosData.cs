using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Common
{
    public class VortexPosData
    {
        [PosName("涡旋接驳位")]
        public double PutGetPos { get; set; }

        [PosName("涡旋位")]
        public double VortexPos { get; set; }
    }
}
