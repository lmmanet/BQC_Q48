using BQJX.Models;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Q_Platform.ViewModels.Page
{
    public class MainPageViewModel : MyViewModelBase
    {

        #region Properties

        public ObservableCollection<SampleModel> SampleList { get; set; }

        public ObservableCollection<WorkLog> WorkLogList { get; set; } = new ObservableCollection<WorkLog>();

        #endregion

        #region Commands

        public ICommand LightSwichCommand { get; set; }

        public ICommand UpCommand { get; set; }

        public ICommand EditCommand { get; set; }

        public ICommand DeleteCommand { get; set; }

        #endregion

        #region Construtors


        public MainPageViewModel()
        {
            Messenger.Default.Register<WorkLog>(this, "logWorkLog", LoggingWorkLog);


            RegisterCommnand();


        }

        private void LoggingWorkLog(WorkLog obj)
        {
            Application.Current.Dispatcher.Invoke(()=> 
            {
                if (obj == null)
                {
                    return;
                }
                WorkLogList.Add(obj);
            });
        }


        #endregion


        #region Private Methods
        private void RegisterCommnand()
        {
            LightSwichCommand = new RelayCommand(LightSwich);
            UpCommand = new RelayCommand<object>(Up);
            EditCommand = new RelayCommand<object>(Edit);
            DeleteCommand = new RelayCommand<object>(Delete);
        }

        private void Delete(object obj)
        {
            throw new NotImplementedException();
        }

        private void Edit(object obj)
        {
            throw new NotImplementedException();
        }

        private void Up(object obj)
        {
            throw new NotImplementedException();
        }

        private void LightSwich()
        {
            throw new NotImplementedException();
        }


        #endregion


        public override void Cleanup()
        {
            base.Cleanup();
        }













    }
}
