using BQJX.Common.Interface;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class CapperThree : CapperBase
    {

        public CapperThree(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ILogger logger) : base(io, motion, globalStatus, logger)
        {

        }
    }
}
