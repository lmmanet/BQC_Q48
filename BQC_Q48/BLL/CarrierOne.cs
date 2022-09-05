using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class CarrierOne : CarrierBase
    {

        #region Private Members

        private readonly static object lockObj = new object();

        private string _currentMethodName = string.Empty;

        private ICarrierOneDataAccess _dataAccess;

        private CarrierOnePosData _posData;

        #endregion

        #region Construtors

        public CarrierOne(IEtherCATMotion motion, IIoDevice io, IEPG26 claw, IGlobalStatus globalStatus, ILogger logger, ICarrierOneDataAccess dataAccess) : base(motion, io, claw, globalStatus, logger)
        {
            _axisX = 0;
            _axisY = 1;
            _axisZ1 = 2;
            _axisZ2 = 3;
            _axisP = 6;
            _clawSlaveId = 1;
            _putOffNeedle = -3;
            _dataAccess = dataAccess;
        }

        #endregion

        #region Public Methods

        public override bool UpdatePosData()
        {
            return _dataAccess.UpdatePosData(_posData); 
        }

        //public async Task<bool> GetTubeFromMa

        #endregion

        #region Protected Methods

        protected override void IntialPosData()
        {
            _posData = _dataAccess.GetPosData();
        }


        #endregion


        /// <summary>
        /// 开始移液
        /// </summary>
        /// <param name="num"></param>
        /// <param name="volume"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> DoPipettingAsync(Sample sample, CancellationTokenSource cts)
        {
            throw new NotImplementedException();
        }









    }
}
