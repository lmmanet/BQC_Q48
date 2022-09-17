using BQJX.Common.Common;
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
    public class CapperThree : CapperBase, ICapperThree
    {

        private static ILogger logger = new MyLogger(typeof(CapperThree));

        #region Construtors

        public CapperThree(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess) : base(io, motion, globalStatus, dataAccess, logger)
        {
            _axisY = 16;
            _axisC1 = 17;
            _axisC2 = 18;
            _axisZ = 29;
            _holding = 41;
            _claw = 42;

            _xOffset = 60;    //拧盖X偏移量

        }

        #endregion





























        protected CapperPosData GetPosData()
        {
            return _dataAccess.GetCapperPosData(3);
        }
    }
}
