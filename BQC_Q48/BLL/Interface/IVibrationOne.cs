using BQJX.Common;
using BQJX.Common.Interface;
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

        Task<bool> GoHome(IGlobalStatus gs);

        bool IsVibrationTaskDone { get; }

        void AddSampleToVibrationList(Sample sample, IGlobalStatus gs);

        Task StartVibration(IGlobalStatus gs);
    }
}
