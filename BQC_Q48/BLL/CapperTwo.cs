using BQJX.Common.Common;
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
    public class CapperTwo : CapperBase
    {


        #region Construtors

        public CapperTwo(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess, ILogger logger) : base(io, motion, globalStatus, dataAccess, logger)
        {
            _yMoveVel = 10;
            _axisY = 9;
            _axisC1 = 10;
            _axisC2 = 11;
            _axisZ = 13;
            _holding = 19;
            _claw = 20;

            _xOffset = 60;    //拧盖X偏移量
        }

        #endregion









































        protected CapperPosData GetPosData()
        {
            return _dataAccess.GetCapperPosData(2);
        }



    }
}
