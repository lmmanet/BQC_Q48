using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Common
{
    public class ConcentrationPosData
    {
        [PosName("浓缩接驳位")]
        public double PutGetPos { get; set; }

        [PosName("浓缩位")]
        public double ConcPos { get; set; }

    }
}
