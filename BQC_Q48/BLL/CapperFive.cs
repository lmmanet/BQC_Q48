using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
using Q_Platform.Logger;

namespace Q_Platform.BLL
{
    public class CapperFive : CapperBase ,ICapperFive
    {

        private static ILogger logger = new MyLogger(typeof(CapperFive));

        #region Construtors

        public CapperFive(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess) : base(io, motion, globalStatus, dataAccess, logger)
        {
            _axisY = 22;
            _axisC1 = 23;
            _axisC2 = 24;
            _axisZ = 27;
            _holding = 47;
            _claw = 48;

            _xOffset = 60;    //拧盖X偏移量

            

        }

        #endregion



    }
}
