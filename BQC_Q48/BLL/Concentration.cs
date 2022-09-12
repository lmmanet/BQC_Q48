using BQJX.Common.Interface;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public Concentration(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, IWeight weight, ILogger logger)
        {
            this._io = io;
            this._motion = motion;
            this._weight = weight;
            this._logger = logger;
            this._globalStauts = globalStatus;

            //IntialPosData();
        }

        #endregion




        public void Dond()
        {

        }










    }
}
