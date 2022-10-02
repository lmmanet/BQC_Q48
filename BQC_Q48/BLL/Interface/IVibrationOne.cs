using BQJX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface IVibrationOne
    {

        Task<bool> GoHome(CancellationTokenSource cts);


        Task StartVibrationAndVortex(Sample sample, string methodAction, CancellationTokenSource cts);


        bool StartPolishVibrationAndVortex(Sample sample, CancellationTokenSource cts);










    }
}
