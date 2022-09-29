using BQJX.Models;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Q_Platform.BLL;
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
        private readonly IMainPro _mainPro;

        #region Properties

        public ObservableCollection<SampleModel> SampleList { get; set; }

        public ObservableCollection<WorkLog> WorkLogList { get; set; } = new ObservableCollection<WorkLog>();

        #endregion

        #region Commands

        public ICommand LightSwichCommand { get; set; }

        public ICommand UpCommand { get; set; }

        public ICommand EditCommand { get; set; }

        public ICommand DeleteCommand { get; set; }

        public ICommand StartTaskCommand { get; set; }
        public ICommand StopTaskCommand { get; set; }
        public ICommand ContinueCommand { get; set; }
        public ICommand InitialSysCommand { get; set; }
        public ICommand PauseTaskCommand { get; set; }

        #endregion

        #region Construtors


        public MainPageViewModel(IMainPro mainPro)
        {
            Messenger.Default.Register<WorkLog>(this, "logWorkLog", LoggingWorkLog);


            RegisterCommnand();

            this._mainPro = mainPro;
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
            StartTaskCommand = new RelayCommand(StartPro);
            StopTaskCommand = new RelayCommand(StopPro);
            ContinueCommand = new RelayCommand(ContinuePro);
            InitialSysCommand = new RelayCommand(InitialSys);
            PauseTaskCommand = new RelayCommand(PausePro);
        }

        private void InitialSys()
        {
            _mainPro.GoHome();
        }

        private void ContinuePro()
        {
            _mainPro.ContinuePro();
        }

        private void StopPro()
        {
            _mainPro.StopPro();
        }
           
        private void PausePro()
        {
            _mainPro.PausePro();
        }

        private void StartPro()
        {
            _mainPro.StartPro();
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
            _mainPro.SwitchLight();
        }


        #endregion


        public override void Cleanup()
        {
            base.Cleanup();
        }













    }
}
