using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using PropertyChanged;

namespace Q_Platform.ViewModels.UC
{
    [AddINotifyPropertyChangedInterface]
    public class BalanceTestUCViewModel
    {

        #region Private Members

        private readonly IWeight _weight;
        private readonly ILogger _logger;
        private Task _refreshTask;
        private bool _stopRefresh;
        private bool _refresh;

        

        #endregion

        #region Properties

        public double  WeightValue { get; set; }
        public double  WeightValue2 { get; set; }
        public double  WeightValue3 { get; set; }

        public int WeightStatus { get; set; }  
        public int WeightStatus2 { get; set; }  
        public int WeightStatus3 { get; set; }  


        [DoNotNotify]
        public ushort SlaveId { get; set; } = 1;

        #endregion

        #region Command

        public ICommand ClearCommand { get; set; }

        #endregion



        #region Constructors

        public BalanceTestUCViewModel(IWeight weight, ILogger logger)
        {
            _logger = logger;
            _weight = weight;
            
            RegisterCommand();
           
            _refreshTask = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        WeightValue = _weight.ReadWeight(1).GetAwaiter().GetResult();
                        WeightValue2 = _weight.ReadWeight(2).GetAwaiter().GetResult();
                        WeightValue3 = _weight.ReadWeight(3).GetAwaiter().GetResult();
                        WeightStatus = _weight.ReadStatus(1).GetAwaiter().GetResult();
                        WeightStatus2 = _weight.ReadStatus(2).GetAwaiter().GetResult();
                        WeightStatus3 = _weight.ReadStatus(3).GetAwaiter().GetResult();
                      
                        if (_stopRefresh)
                        {
                            break;
                        }
                        while (_refresh)
                        {
                            Thread.Sleep(1000);
                        }
                        Thread.Sleep(50);
                    }
                    catch (Exception ex)
                    {
                        logger?.Error($"_refreshTask err:{ex.Message}");
                    }
               
                }
              
            });
        }

        private void RegisterCommand()
        {
            ClearCommand = new RelayCommand(Clear);
        }

        private void Clear()
        {
            _weight.Clear(SlaveId);
        }
    }

        #endregion
 }

