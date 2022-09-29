using BQJX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICapperOne
    {

        Task<bool> GoHome(CancellationTokenSource cts);

        bool AddSaltExtract(Sample sample, CancellationTokenSource cts);
        bool AddSolveExtract(Sample sample, CancellationTokenSource cts);
        bool AddWaterExtract(Sample sample, CancellationTokenSource cts);


    }

}
