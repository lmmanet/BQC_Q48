using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
using Q_Platform.Logger;

namespace Q_Platform.BLL
{
    public class CapperFive : CapperBase ,ICapperFive
    {

        private static ILogger logger = new MyLogger(typeof(CapperFive));

        private readonly ICarrierTwo _carrier;

        #region Construtors

        public CapperFive(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess,ICarrierTwo carrier) : base(io, motion, globalStatus, dataAccess, logger)
        {
            this._carrier = carrier;
            _axisY = 22;
            _axisC1 = 23;
            _axisC2 = 24;
            _axisZ = 27;
            _holding = 47;
            _claw = 48;

            _holdingCloseSensor = 48;  //I1.0
            _holdingOpenSensor = 49;   //I1.1

            _xOffset = 60;    //拧盖X偏移量
            _capperTorque = 50;  //拧盖力度
            _capperTimeout = 40;  //拧盖超时时间 S 
            _posData = _dataAccess.GetCapperPosData(5);

        }

        #endregion



    }
}
