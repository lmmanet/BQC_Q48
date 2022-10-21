using BQJX.Common.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Common
{
    public class GlobalStatus : IGlobalStatus
    {
        private bool _stop;

        private bool _pause;

        public event Func<bool> StopProgramEventArgs;

        public event Func<bool> PauseProgramEventArgs;

        public event Func<bool> ContinueProgramEventArgs;



        public bool IsStopped { get { return _stop; } }
        public bool IsPause { get { return _pause; } }
        public void StopProgram(Func<bool> stopDoneFunc)
        {
            _stop = true;
            _pause = true;
            StopProgramEventArgs?.Invoke();

            Task.Run(() =>
            {
                var result = stopDoneFunc?.Invoke() == true;
                if (result)
                {
                    _stop = false;
                    _pause = false;
                }
            });
          
        }
        public void PauseProgram()
        {
            _pause = true;
            PauseProgramEventArgs?.Invoke();
        }
        public void ContinueProgram()
        {
            _stop = false;
            _pause = false;
            ContinueProgramEventArgs?.Invoke();
        }
    }
}
