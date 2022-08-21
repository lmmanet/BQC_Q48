using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Core.Base
{
    public enum MotionStatus
    {
        NotReady = 1,
        Disable = 2,
        Ready = 4,
        Running = 8,
        Enable = 16,
        Stop = 32 ,
        FaultActive = 64,
        Fault = 128

    }
}
