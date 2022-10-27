using BQJX.Common;
using BQJX.Common.Interface;
using Q_Platform.Common;
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

        Task<bool> GoHome(IGlobalStatus gs);

        bool AddSaltExtract(Sample sample, IGlobalStatus gs);
        bool AddSolveExtract(Sample sample, IGlobalStatus gs);
        bool AddWaterExtract(Sample sample, IGlobalStatus gs);

        void UpdatePosData();
        CapperInfo GetCapperInfo();


    }

}
