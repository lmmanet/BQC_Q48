using BQJX.Common.Interface;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class VibrationOne : VibrationBase
    {


        #region Construtors

        public VibrationOne(IEtherCATMotion motion, IIoDevice io, IGlobalStatus globalStauts, ILogger logger) : base(motion, io, globalStauts, logger)
        {
            _axisNo = 4;
            _holding = 14;
            _holdingOpenSensor = 16; //原位
            _holdingCloseSensor = 15; //到位
        } 

        #endregion












    }
}
