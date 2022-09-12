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

        #region Private Methods

        /// <summary>
        /// 获取15ml试管位置
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] GetSampleTubeCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.PurifyTubePos1;
            if (tubeId > 48)
            {
                xyz = _posData.PurifyTubePos2;
            }

            //计算偏移
            int id = (tubeId - 1) % 48;

            //计算结果

            return base.GetCoordinate(id + 1, 12, 4, -32, 32, xyz);

        }

        /// <summary>
        /// 获取西林瓶坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] GetSeilingCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.SeilingPos1;
            if (tubeId > 48)
            {
                xyz = _posData.SeilingPos2;
            }

            //计算偏移
            int id = (tubeId - 1) % 48;

            //计算结果

            return base.GetCoordinate(id + 1, 12, 4, -32, 32, xyz);
        }

        /// <summary>
        /// 获取气质小瓶坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] Get_GC_BottleCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.BottlePos1;
            if (tubeId > 48)
            {
                xyz = _posData.BottlePos2;
            }

            //计算偏移
            int id = (tubeId - 1) % 48;

            //计算结果

            return base.GetCoordinate(id + 1, 12, 4, -16, 15, xyz);
        }

        /// <summary>
        /// 获取液质小瓶坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] Get_LC_BottleCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.BottlePos3;
            if (tubeId > 48)
            {
                xyz = _posData.BottlePos4;
            }

            //计算偏移
            int id = (tubeId - 1) % 48;

            //计算结果

            return base.GetCoordinate(id + 1, 12, 4, -16, 15, xyz);
        }

        /// <summary>
        /// 获取Tip1头坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] GetTip1Coordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.NeedlePos1;
            return base.GetCoordinate(tubeId, 8, 12, -10, 20, xyz);
        }

        /// <summary>
        /// 获取Tip2头坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] GetTip2Coordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.NeedlePos2;
            return base.GetCoordinate(tubeId, 8, 12, -10, 20, xyz);
        }

        #endregion


    }
}
