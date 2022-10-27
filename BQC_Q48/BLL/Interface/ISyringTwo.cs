using BQJX.Common.Interface;
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
        Task<bool> GoHome(IGlobalStatus gs);
        Task<bool> AddSolve(byte solve, double volume, IGlobalStatus gs);
    }
}
