using BQJX.Common;
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
    public class VibrationTwo : VibrationBase ,IVibrationTwo
    {
        private static ILogger logger = new MyLogger(typeof(VibrationTwo));

        private readonly static object _lockObj = new object();

        #region Construtors


        public VibrationTwo(IEtherCATMotion motion, IIoDevice io, IGlobalStatus globalStauts) : base(motion, io, globalStauts, logger)
        {
            _axisNo = 13;
            _holding = 40;
            _holdingOpenSensor = 41; //原位
            _holdingCloseSensor = 40; //到位
        }

        #endregion


     

    }
}
