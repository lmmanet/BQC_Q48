using BQJX.Core.Interface;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class SyringTwo : SyringBase
    {
        private static ILogger logger = new MyLogger(typeof(SyringTwo));

        #region Construtors

        public SyringTwo(IIoDevice io, ILS_Motion motion) : base(io, motion,logger)
        {
            _axisAddLiquid = 4;
            _port1 = 24;
            _port2 = 25;
            _port3 = 26;
            _port4 = 27;
            _port5 = 28;
            _port6 = 29;
            _port7 = 30;
            _port8 = 31;
            _syringHomePos = 0; //注射器原点位置
        }

        #endregion





    }
}
