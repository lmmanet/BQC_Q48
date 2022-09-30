using BQJX.Common;
using GalaSoft.MvvmLight.Messaging;
using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.ViewModels.UC
{
    public class SampleStatusMonitorViewModel : MyViewModelBase
    {

        public ObservableCollection<Sample> SampleList { get; set; } = new ObservableCollection<Sample>();

        public SampleStatusMonitorViewModel()
        {
            Messenger.Default.Register<Sample>(this, "Add", AddSample);
         
        }
        private void AddSample(Sample obj)
        {
            SampleList.Add(obj);
        }
    }
}
