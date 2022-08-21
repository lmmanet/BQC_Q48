using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Communication.CL2C
{
    public enum MotionIoEnum
    {
        Falt = 0,
        Enable,
        Running,
        Invalid,
        CommandDone,
        MoveDone,
        HomeDone
    }
}
