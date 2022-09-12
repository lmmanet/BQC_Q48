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




        #region Private Methods

        /// <summary>
        /// 获取50ml试管位置
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] GetSampleTubeCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.SamplePos1;
            if (tubeId > 24 && tubeId <= 48)
            {
                xyz = _posData.SamplePos2;
            }
            if (tubeId > 48 && tubeId <= 72)
            {
                xyz = _posData.SamplePos3;
            }
            if (tubeId > 72 && tubeId <= 96)
            {
                xyz = _posData.SamplePos4;
            }
            //计算偏移
            int id = (tubeId - 1) % 24;

            //计算结果

            return base.GetCoordinate(id + 1, 8, 3, -45, 45, xyz);

        }

        /// <summary>
        /// 获取冷却缓存坐标
        /// </summary>
        /// <param name="tubeId">1-16</param>
        /// <returns></returns>
        private double[] GetColdCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.ColdPos;
            return base.GetCoordinate(tubeId, 8, 2, -45, 45, xyz);
        }

        /// <summary>
        /// 获取Tip头坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] GetTipCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.NeedlePos;
            return base.GetCoordinate(tubeId, 16, 6, -1.5, 1.5, xyz);
        }

        #endregion




    }
}
