using BQJX.Common.Interface;
using BQJX.Core.Interface;
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

        #endregion

        #region Variable

        protected double _moveVel = 10;


        protected ushort _axisY;

        protected ushort _claw1;
        protected ushort _claw2;

        protected double _xOffset = 50;    //浓缩X偏移量

        #endregion

        #region Constructors

        public Concentration(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, IWeight weight)
        {
            this._io = io;
            this._motion = motion;
            this._weight = weight;
            this._logger = new MyLogger(typeof(Concentration));
            this._globalStauts = globalStatus;

            //IntialPosData();
        }

        #endregion

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> GoHome(CancellationTokenSource cts)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    return false;
                }
                _logger?.Error(ex.Message);
                return false;
            }
           
        }







    }
}
