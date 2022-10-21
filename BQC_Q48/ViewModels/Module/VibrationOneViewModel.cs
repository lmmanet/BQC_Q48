using BQJX.Core.Interface;
using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.ViewModels.Module
{
    public class VibrationOneViewModel :VibrationViewModelBase
    {
       


        public VibrationOneViewModel(IEtherCATMotion motion, IIoDevice io, ILogger logger) : base(motion, io, logger)
        {
            _axis = 4;
            _holding = 14;
            _holdingOpenSensor = 16; //原位
            _holdingCloseSensor = 15; //到位
            _refreshTask = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        RefreshStatus();
                        if (_stopRefresh)
                        {
                            break;
                        }
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        logger?.Error($"_refreshTask err:{ex.Message}");
                    }
                }

            });
        }



    }


}
