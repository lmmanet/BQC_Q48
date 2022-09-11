using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;

namespace Q_Platform.BLL
{
    public class CapperFive : CapperBase
    {

        #region Construtors

        public CapperFive(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess, ILogger logger) : base(io, motion, globalStatus, dataAccess, logger)
        {
            _yMoveVel = 10;
            _axisY = 22;
            _axisC1 = 23;
            _axisC2 = 24;
            _axisZ = 27;
            _holding = 47;
            _claw = 48;
        }

        #endregion



    }
}
