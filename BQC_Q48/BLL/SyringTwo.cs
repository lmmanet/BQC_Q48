using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class SyringTwo : SyringBase, ISyringTwo
    {
        private static ILogger logger = new MyLogger(typeof(SyringTwo));

        #region Construtors

        public SyringTwo(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus) : base(io, motion,logger, globalStatus)
        {
            _axisAddLiquid = 26;
            _port1 = 58;    //Q2.2
            _port2 = 59;
            _port3 = 60;
            _port4 = 61;
            _port5 = 62;
            _port6 = 63;
            _port7 = 64;
            _port8 = 65;
            _syringHomePos = 0; //注射器原点位置
        }

        #endregion





    }
}
