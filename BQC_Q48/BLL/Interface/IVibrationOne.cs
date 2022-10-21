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

        bool IsVibrationTaskDone { get; }

        void AddSampleToVibrationList(Sample sample, CancellationTokenSource cts);

        Task StartVibration(CancellationTokenSource cts);
    }
}
