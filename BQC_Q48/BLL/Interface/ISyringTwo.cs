using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ISyringTwo
    {
        Task<bool> GoHome(CancellationTokenSource cts);
        Task<bool> AddSolve(byte solve, double volume, CancellationTokenSource cts);
    }
}
