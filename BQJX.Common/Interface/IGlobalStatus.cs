using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Interface
{
    public interface IGlobalStatus
    {
        bool IsPause { get; }
        bool IsStopped { get; }

        event Func<bool> ContinueProgramEventArgs;
        event Func<bool> PauseProgramEventArgs;
        event Func<bool> StopProgramEventArgs;

        void ContinueProgram();
        void PauseProgram();
        void StopProgram(Func<bool> stopDoneFunc);
    }
}
