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
using Q_Platform.Views.Windows;
using BQJX.Common;
using CommonServiceLocator;
using Q_Platform.DAL;

namespace Q_Platform.ViewModels.Page
{
    [AddINotifyPropertyChangedInterface]
    public class MainPageViewModel : MyViewModelBase
    {
        private readonly IMainPro _mainPro;
        private readonly IGlobalStatus _globalStatus;
        private readonly IRunService _service;
        private readonly ISampleDataAccess _dataAccess;
        private ushort _addSampleId;

        private List<Sample> _workSampleList = new List<Sample>();

        #region Properties

        /// <summary>
        /// 样品列表模型
        /// </summary>
        public ObservableCollection<SampleModel> SampleList { get; set; } = new ObservableCollection<SampleModel>();

        /// <summary>
        /// 运行日志
        /// </summary>
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


        private int _sampleCount;

        [DoNotNotify]
        public int SampleCount
        {
            get { return _sampleCount; }
            set
            {
                if (_sampleCount == value)
                {
                    return;
                }
                _sampleCount = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 系统运行状态
        /// </summary>
        public int RunStatus { get; set; }

        /// <summary>
        /// 冰浴当前温度
        /// </summary>
        public double ColdTemperature { get; set; }

        /// <summary>
        /// 小瓶当前温度
        /// </summary>
        public double BottleTemperature { get; set; }

        /// <summary>
        /// 标准品当前温度
        /// </summary>
        public double StandardTemperature { get; set; }


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

        /// <summary>
        /// 删除使能
        /// </summary>
        public bool DelectSampleEnable { get; set; } = true;

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

        public MainPageViewModel(IMainPro mainPro, IRunService runService, IGlobalStatus globalStatus,ISampleDataAccess dataAccess)
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
            _service.TemperatureChangedEventHandler += _service_TemperatureChangedEventHandler;
            _service.AlmOccuCallBack += OccuAlm;
            _globalStatus.PauseProgramEventArgs += GlobalStatus_PauseProgramEventArgs;
            _globalStatus.MachineStatusChangedEventArgs += () => { RunStatus = _globalStatus.MachineStatus; };
            _service.EmgStopOccuEventArgs += _globalStatus.EmgStop;
            Messenger.Default.Register<SampleModel>(this, "AddSampleModel", AddSampleToList);
            _dataAccess = dataAccess;
            ServiceLocator.Current.GetInstance<AlarmPageViewModel>();//加载一下报警页面
        }

      
        private bool GlobalStatus_PauseProgramEventArgs()
        {
            ContinueBtnEnable = true;
            PauseBtnEnable = false;
            return true;
        }

        private void LoggingWorkLog(WorkLog obj)
        {
            Application.Current.Dispatcher.Invoke(() =>
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
            ResetAlmCommand = new RelayCommand(ResetAlm);
            AddSampleCommand = new RelayCommand<Object>(AddSample);
        }

        private void AddSample(object obj)
        {
            string btnName = obj.ToString();
            string subStr = btnName.Substring(3, btnName.Length - 3);
            if (ushort.TryParse(subStr, out _addSampleId))
            {
                new AddSampleWin().ShowDialog();
            }
        }

        private void InitialSys()
        {
            _mainPro.ClearWorkList();
            _workSampleList = new List<Sample>();
            SampleList = new ObservableCollection<SampleModel>();
             _mainPro.GoHome(() => HomeDoneFlag);
            DelectSampleEnable = true;
            UpdateSampleCount(); //更新样品数量
            //回零完成判断是否回零成功  并使能启动按钮
            //if (HomeDoneFlag)
            //{
            //    RunBtnEnable = true;
            //}
            //else
            //{
            //    RunBtnEnable = false;
            //}
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
            _mainPro.PausePro(() => ContinueBtnEnable);

            PauseBtnEnable = false;
        }

        private void StartPro()
        {
            GenerateSampleList();//转换数据
            GlobalCache.Instance.WorkList = _workSampleList;

            _mainPro.StartPro();

            PauseBtnEnable = true;
            ContinueBtnEnable = false;
            DelectSampleEnable = false;


        }

        private void Delete(object obj)
        {
            var sample = obj as SampleModel;
            if (sample != null)
            {
                var result = MessageBox.Show("确认删除该样品", "警告", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    var work = SampleList?.FirstOrDefault(s => s.Id == sample.Id);
                    if (work != null)
                    {
                        int index = SampleList.IndexOf(work);
                        SampleList.Remove(work);
                    }
                }
            }
            UpdateSampleCount();
        }

        private void Edit(object obj)
        {
            throw new NotImplementedException();
        }

        private void Up(object obj)
        {
            var sample = obj as SampleModel;
            if (sample != null)
            {
                //在任务列表中找出当前样品
                var work = SampleList?.FirstOrDefault(s => s.Id == sample.Id);
                if (work != null)
                {
                    //更新任务列表顺序
                    int index = SampleList.IndexOf(work);
                    if (index > 1)
                    {
                        SampleList.Remove(work);
                        SampleList.Insert(1, work);
                    }

                    //更新页面顺序
                    //int workIndex = SampleList.IndexOf(WorkSampleList[0]);//当前工作样品在页面列表的位置

                    //SampleList.Remove(sample);
                    //SampleList.Insert(workIndex + 1, sample);
                }
                else
                {
                    SampleList.Remove(sample);
                    SampleList.Insert(0, sample);
                }


            }
        }

        private void LightSwich()
        {
            _mainPro.SwitchLight();
        }

        /// <summary>
        /// 设置温控值1
        /// </summary>
        private void SetTemperature1()
        {
            _service.SetTemperature1 = _temperatrue1;
        }

        /// <summary>
        /// 设置温控值2
        /// </summary>
        private void SetTemperature2()
        {
            _service.SetTemperature2 = _temperatrue2;
        }

        /// <summary>
        /// 设置温控值3
        /// </summary>
        private void SetTemperature3()
        {
            _service.SetTemperature3 = _temperatrue3;
        }

        /// <summary>
        /// 复位报警
        /// </summary>
        private void ResetAlm()
        {
            _service.ResetAlm();
            _globalStatus.ResetAlm();
            Messenger.Default.Send<string>("ResetAlm", "ResetAlm");
        }

        /// <summary>
        /// 发生报警
        /// </summary>
        /// <param name="msg"></param>
        private void OccuAlm(string msg)
        {
            _globalStatus.AlmOccu();
            Messenger.Default.Send<string>(msg, "AlmOccu");
        }

        /// <summary>
        /// 添加样品
        /// </summary>
        /// <param name="sampleModel"></param>
        private void AddSampleToList(SampleModel sampleModel)
        {
            sampleModel.Id = _addSampleId;
            if (SampleList.FirstOrDefault(s => s.Id == sampleModel.Id) != null)
            {
                return;
            }
            SampleList.Add(sampleModel);
            UpdateSampleCount();
        }

        /// <summary>
        /// 更新试管位置状态
        /// </summary>
        private void UpdateSampleCount()
        {
            int value = 0;
            foreach (var item in SampleList)
            {
                int id = item.Id - 1;
                int i = (int)Math.Pow(2, id);
                value = value ^ i;
            }
            SampleCount = value;
        }


        /// <summary>
        /// 样品模型转换成样品任务
        /// </summary>
        private void GenerateSampleList()
        {
            var list = new List<Sample>();
            foreach (var item in SampleList)
            {
                item.Sample = new Sample()
                {
                    Id = item.Id,
                    Status = 0x11100101001,
                    MainStep = item.StartMainStep,
                    TechParams = item.TechParams
                };
                list.Add(item.Sample);
            }
            if (_workSampleList.Count != 0)
            {
                return;
            }
            _workSampleList = list;
        }




        private void _service_TemperatureChangedEventHandler(double[] value)
        {
            ColdTemperature = value[0];
            BottleTemperature = value[1];
            StandardTemperature = value[2];
        }


        #endregion


        public override void Cleanup()
        {
            _service.StopPro();
            base.Cleanup();
        }



        /// <summary>
        /// 清除离心机占用状态
        /// </summary>
        private void ClearCentrifugalOccupy()
        {
            GlobalCache.Instance.TubeInCentrifugal = new List<ushort>();
        }

        /// <summary>
        /// 保存样品到数据库
        /// </summary>
        /// <param name="sampleModel"></param>
        /// <returns></returns>
        private bool SaveSampleModelToDataBase(SampleModel sampleModel)
        {
            SampleInfo sampleInfo1 = new SampleInfo()
            {
                Name = sampleModel.Name1,
                SnNum = sampleModel.SnNum1,
                TechName = sampleModel.TechName,
                CreateTime = DateTime.Now,
                Status = 0,
            };
            SampleInfo sampleInfo2 = new SampleInfo()
            {
                Name = sampleModel.Name2,
                SnNum = sampleModel.SnNum2,
                TechName = sampleModel.TechName,
                CreateTime = DateTime.Now,
                Status = 0,
            };
           var result1 =  _dataAccess.InsertSampleInfo(sampleInfo1);
           var result2 = _dataAccess.InsertSampleInfo(sampleInfo2);
            return result1 && result2;
        }

    }
}
