using BQJX.Core.Interface;
using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.ViewModels.Module
{
    public class VibrationTwoViewModel :VibrationViewModelBase
    {
       


        public VibrationTwoViewModel(IEtherCATMotion motion, IIoDevice io, ILogger logger) : base(motion, io, logger)
        {
            _axis = 13;
            _holding = 40;
            _holdingOpenSensor = 41; //原位
            _holdingCloseSensor = 40; //到位
        }



    }


}
