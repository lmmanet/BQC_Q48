using BQJX.Common.Interface;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class VibrationTwo : VibrationBase
    {

        #region Construtors


        public VibrationTwo(IEtherCATMotion motion, IIoDevice io, IGlobalStatus globalStauts, ILogger logger) : base(motion, io, globalStauts, logger)
        {

        } 


        #endregion
    }
}
