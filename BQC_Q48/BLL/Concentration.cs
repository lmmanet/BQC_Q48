using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class Concentration : IConcentration
    {


        #region Private Members

        private readonly IIoDevice _io;

        private readonly ILogger _logger;

        private readonly ILS_Motion _motion;

        private readonly IGlobalStatus _globalStauts;

        private readonly IWeight _weight;

        private ConcentrationPosData _posData;

        #endregion

        #region Variable

        protected double _yMoveVel = 500;


        protected ushort _axisY = 25;

        protected ushort _zCylinderDown = 56;      //Q2.0
        protected ushort _zCylinderUp = 55;        //Q1.7
        protected ushort _zCylinderHigh = 55;      //I1.7
        protected ushort _zCylinderLow = 56;       //I2.0
        protected ushort _vaccumSensor1 = 57;      //I2.1
        protected ushort _vaccumSensor2 = 58;      //I2.2
        protected ushort _vaccum1 = 52;            //Q1.4
        protected ushort _vaccum2 = 53;            //Q1.5


        protected double _xOffset = 50;    //浓缩X偏移量

        #endregion

        #region Constructors

        public Concentration(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, IWeight weight, IConcentrationPosDataAccess dataAccess)
        {
            this._io = io;
            this._motion = motion;
            this._weight = weight;
            this._logger = new MyLogger(typeof(Concentration));
            this._globalStauts = globalStatus;

            _posData = dataAccess.GetPosData();
        }

        #endregion

        #region Public Methods


        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> GoHome(CancellationTokenSource cts)
        {
            try
            {
                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                }
                _logger?.Info("浓缩回零");

                //Z轴上升
                //_io.WriteBit_DO(_claw, true);

                //使能Y轴
                await _motion.ServoOn(_axisY).ConfigureAwait(false);
                //Y轴回零
                var result = await _motion.GoHomeWithCheckDone(_axisY, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }


                //复位真空

                //Y轴移动到上下料位置
                result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
                //回零完成
                return true;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    _logger?.Info("浓缩回零 停止");
                    return false;
                }
                _logger?.Warn($"浓缩回零 err:{ex.Message}");
                return false;
            }

        }
        #endregion


        #region Private Methods


        #endregion




    }
}
