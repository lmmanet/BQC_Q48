using BQJX.Common.Interface;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class CapperBase
    {


        #region Private Members

        private readonly IIoDevice _io;

        private readonly ILogger _logger;

        private readonly ILS_Motion _motion;

        private readonly IGlobalStatus _globalStauts;

        #endregion

        #region Variable

        protected double _moveVel = 10;


        protected ushort _axisY;
        protected ushort _axisZ;
        protected ushort _axisC1;
        protected ushort _axisC2;


        protected ushort _claw1;
        protected ushort _claw2;

        

        #endregion

        #region Constructors

        public CapperBase(IIoDevice io,ILS_Motion motion, IGlobalStatus globalStatus, ILogger logger)
        {
            this._io = io;
            this._motion = motion;
            this._logger = logger;
            this._globalStauts = globalStatus;

            //IntialPosData();
        }

        #endregion








    }
}
