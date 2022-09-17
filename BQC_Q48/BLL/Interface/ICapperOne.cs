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

        Task<bool> AddSolve(Sample sample, CancellationTokenSource cts);


        Task<bool> MoveOut(Sample sample, CancellationTokenSource cts);

        Task<bool> MovePutGetPos(CancellationTokenSource cts);



        bool CloseHold(ushort num);

        bool OpenHold(ushort num);

    }

}
