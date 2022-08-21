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
    public class CarrierTwo : CarrierBase
    {

        #region Private Members

        private readonly static object lockObj = new object();

        private string _currentMethodName = string.Empty;

        /// <summary>
        /// 注射器
        /// </summary>
        private ushort _axisSyring;

        private CarrierTwoPosData _posData;

        private ICarrierTwoDataAccess _dataAccess;

        #endregion

        #region Construtors
        public CarrierTwo(IEtherCATMotion motion, IIoDevice io, IEPG26 claw, IGlobalStatus globalStatus, ILogger logger, ICarrierTwoDataAccess dataAccess) : base(motion, io, claw, globalStatus, logger)
        {
            _axisX = 9;
            _axisY = 10;
            _axisZ1 = 11;
            _axisZ2 = 12;
            _axisP = 14;
            _clawSlaveId = 3;
            _axisSyring = 15;
            _putOffNeedle = -0.5;
            this._dataAccess = dataAccess;
        }

        #endregion

        #region Public Methods

        public override bool UpdatePosData()
        {
            return _dataAccess.UpdatePosData(_posData);
        }

        #endregion

        #region Protected Methods

        protected override void IntialPosData()
        {
            _posData = _dataAccess.GetPosData();
        }


        #endregion

    }
}
