using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class CapperFour : CapperBase, ICapperFour
    {

        private static ILogger logger = new MyLogger(typeof(CapperFour));

        private readonly ICarrierTwo _carrier;

        #region Construtors

        public CapperFour(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess) : base(io, motion, globalStatus, dataAccess, logger)
        {
            _axisY = 19;
            _axisC1 = 20;
            _axisC2 = 21;
            _axisZ = 28;
            _holding = 44;
            _claw = 45;

            _holdingCloseSensor = 45;  //I0.5
            _holdingOpenSensor = 16;   //I0.6

            _xOffset = 60;    //拧盖X偏移量
            _capperTorque = 50;  //拧盖力度
            _capperTimeout = 40;  //拧盖超时时间 S 
            _posData = _dataAccess.GetCapperPosData(4);

        }

        #endregion





    }
}
