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
using PropertyChanged;
using BQJX.Common.Interface;

namespace Q_Platform.ViewModels.Page
{
    [AddINotifyPropertyChangedInterface]
    public class MainPageViewModel : MyViewModelBase
    {
        private readonly IMainPro _mainPro;
        private readonly IGlobalStatus _globalStatus;
        private readonly IRunService _service;

        #region Properties

        public ObservableCollection<SampleModel> SampleList { get; set; }

        public ObservableCollection<WorkLog> WorkLogList { get; set; } = new ObservableCollection<WorkLog>();


        private double _temperatrue1 = 30;

        public double Temperatrue1
        {
            get { return _temperatrue1; }
            set 
            {
                if (_temperatrue1 == value)
                {
                    return;
                }
                _temperatrue1 = value;
                SetTemperature1();
            }
        }

        private double _temperatrue2 = 30;

        public double Temperatrue2
        {
            get { return _temperatrue2; }
            set
            {
                if (_temperatrue2 == value)
                {
                    return;
                }
                _temperatrue2 = value;
                SetTemperature2();
            }
        }

        private double _temperatrue3 = 30;

        public double Temperatrue3
        {
            get { return _temperatrue3; }
            set
            {
                if (_temperatrue3 == value)
                {
                    return;
                }
                _temperatrue3 = value;
                SetTemperature3();
            }
        }





        /// <summary>
        /// 运行按钮
        /// </summary>
        public bool RunBtnEnable { get; set; } = true;

        /// <summary>
        /// 暂停按钮
        /// </summary>
        public bool PauseBtnEnable { get; set; } = true;

        /// <summary>
        /// 停止按钮
        /// </summary>
        public bool StopBtnEnable { get; set; } = true;

        /// <summary>
        /// 继续按钮
        /// </summary>
        public bool ContinueBtnEnable { get; set; } = false;

        /// <summary>
        /// 初始化按钮
        /// </summary>
        public bool InitBtnEnable { get; set; } = true;

        /// <summary>
        /// 回零完成
        /// </summary>
        public bool HomeDoneFlag { get; set; }


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

        public ICommand AddSampleCommand { get; set; }
        public ICommand ResetAlmCommand { get; set; }


        #endregion

        #region Construtors

        public MainPageViewModel(IMainPro mainPro,IRunService runService, IGlobalStatus globalStatus)
        {
            Messenger.Default.Register<WorkLog>(this, "logWorkLog", LoggingWorkLog);

            RegisterCommnand();

            this._mainPro = mainPro;
            this._service = runService;
            this._globalStatus = globalStatus;
            SetTemperature1();
            SetTemperature2();
            SetTemperature3();
            _service.Run();
            _service.AlmOccuCallBack += OccuAlm;
            _globalStatus.PauseProgramEventArgs += GlobalStatus_PauseProgramEventArgs;
        }

        private bool GlobalStatus_PauseProgramEventArgs()
        {
            ContinueBtnEnable = true;
            PauseBtnEnable = false;
            return true;
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
            InitialSysCommand = new RelayCommand(async()=>await InitialSys());
            PauseTaskCommand = new RelayCommand(PausePro);
            ResetAlmCommand = new RelayCommand(ResetAlm);
            AddSampleCommand = new RelayCommand<Object>(AddSample);
        }

        private void AddSample(object obj)
        {
            //string btnName = obj.ToString();
        }

        private async Task InitialSys()
        {
            await _mainPro.GoHome(()=>HomeDoneFlag).ConfigureAwait(false);

            //回零完成判断是否回零成功  并使能启动按钮
            if (HomeDoneFlag)
            {
                RunBtnEnable = true;
            }
            else
            {
                RunBtnEnable = false;
            }
        }

        private void ContinuePro()
        {
            _mainPro.ContinuePro();

            PauseBtnEnable = true;
            ContinueBtnEnable = false;
        }

        private void StopPro()
        {
            _mainPro.StopPro();
        }
           
        private void PausePro()
        {
            _mainPro.PausePro(()=>ContinueBtnEnable);

            PauseBtnEnable = false;
        }

        private void StartPro()
        {
            _mainPro.StartPro();

            PauseBtnEnable = true;
            ContinueBtnEnable = false;
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

        private void SetTemperature1()
        {
            _service.SetTemperature1 = _temperatrue1;
        }
        private void SetTemperature2()
        {
            _service.SetTemperature2 = _temperatrue2;
        }
        private void SetTemperature3()
        {
            _service.SetTemperature3 = _temperatrue3;
        }



        private void ResetAlm()
        {
            _service.ResetAlm();
            Messenger.Default.Send<string>("ResetAlm", "ResetAlm");
        }


        private void OccuAlm(string msg)
        {
            Messenger.Default.Send<string>(msg, "AlmOccu");
        }


        #endregion


        public override void Cleanup()
        {
            _service.StopPro();
            base.Cleanup();
        }




    }
}
