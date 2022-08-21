using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Core.Base
{
    public enum MotionIoStatus
    {
        ALM = 1,
        EL_P = 2,
        EL_N = 4,
        EMG = 8,
        ORG = 16,

        SL_P = 64,
        SL_N =128,
        INP = 256,
        RDY = 512,
        DSTP = 1024,
        SEVON = 2048

    }
}
