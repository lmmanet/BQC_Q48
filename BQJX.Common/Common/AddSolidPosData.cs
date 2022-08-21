using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Common
{
    public class AddSolidPosData
    {
        //上下两层Y1 Y2
        [PosName("加固A位")]
        public double[] Solid_A { get; set; }

        [PosName("加固B位")]
        public double[] Solid_B { get; set; }

        [PosName("加固C位")]
        public double[] Solid_C { get; set; }

        [PosName("加固D位")]
        public double[] Solid_D { get; set; }

        [PosName("加固E位")]
        public double[] Solid_E { get; set; }

        [PosName("加固F位")]
        public double[] Solid_F { get; set; }

    }
}
