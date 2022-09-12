using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class CapperFour : CapperBase
    {

        #region Construtors

        public CapperFour(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess, ILogger logger) : base(io, motion, globalStatus, dataAccess, logger)
        {
            _yMoveVel = 10;
            _axisY = 19;
            _axisC1 = 20;
            _axisC2 = 21;
            _axisZ = 28;
            _holding = 44;
            _claw = 45;

            _xOffset = 60;    //拧盖X偏移量

        }

        #endregion





    }
}
