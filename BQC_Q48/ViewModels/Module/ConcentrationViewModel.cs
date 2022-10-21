using BQJX.Core.Interface;
using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using Q_Platform.Models;
using Q_Platform.DAL;
using BQJX.Common.Common;
using System.Reflection;
using System.Threading;
using GalaSoft.MvvmLight.Ioc;
using Q_Platform.BLL;

namespace Q_Platform.ViewModels.Module
{
    [AddINotifyPropertyChangedInterface]
    public class ConcentrationViewModel :MyViewModelBase
    {
        protected readonly IIoDevice _io;
        protected readonly ILS_Motion _iLS_Motion;
        protected readonly IConcentrationPosDataAccess _dataAccess;
        protected readonly ILogger _logger;

        private readonly ushort _axisY = 25;
        private readonly ushort _pressCtl1 = 55;//上
        private readonly ushort _pressCtl2 = 56;//下


        #region Properties


        public string AlarmMessage { get; set; }

        public Visibility ShowAlarmMsg { get; set; }

        public double YCurrentPos { get; set; }

        public double TargetVel { get; set; } = 100;

        public double TargetPos { get; set; }

        public int ConcentrationVel { get; set; } = 5000;

        /// <summary>
        /// 轴点位数据信息
        /// </summary>
        public ObservableCollection<AxisPosInfo> AxisPosInfos { get; set; }


        #endregion

        #region Commands

        public ICommand AbsMoveCommand { get; set; }
        public ICommand StopMoveCommand { get; set; }
        public ICommand HomeMoveCommand { get; set; }
        public ICommand EnableMotionCommand { get; set; }
        public ICommand DisableMotionCommand { get; set; }
        public ICommand ResetAxisAmlCommand { get; set; }
        public ICommand PressUpCommand { get; set; }
        public ICommand PressDownCommand { get; set; }
        public ICommand StartConcentrationCommand { get; set; }
        public ICommand StopConcentrationCommand { get; set; }
        public ICommand OpenVacuumeCommand { get; set; }
        public ICommand CloseVaccumeCommand { get; set; }


        /// <summary>
        /// 选择点位变化
        /// </summary>
        public ICommand AxisPosInfoChangedCommand { get; set; }

        /// <summary>
        /// 示教保存按钮
        /// </summary>
        public ICommand SavePosDataCommand { get; set; }

        /// <summary>
        /// 示教更新
        /// </summary>
        public ICommand TechCommand { get; set; }


        #endregion



        public ConcentrationViewModel(ILS_Motion iLS_Motion, IIoDevice io, IConcentrationPosDataAccess dataAccess, ILogger logger)
        {
            this._iLS_Motion = iLS_Motion;
            this._io = io;
            this._dataAccess = dataAccess;
            this._logger = logger;
            RegisterCommand();
            GetAxisPosInfo(new StepAxisEleGear { AxisName = "浓缩Y轴", SlaveId = 25, EleGear = 787.40, HomeMode = 14, HomeHigh = 50, HomeLow = 50 });
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



        private void RefreshStatus()
        {
            YCurrentPos = _iLS_Motion.GetCurrentPos(_axisY).GetAwaiter().GetResult();
        }

        private void RegisterCommand()
        {
            AbsMoveCommand = new RelayCommand(AbsMove);
            StopMoveCommand = new RelayCommand(StopMove);
            HomeMoveCommand = new RelayCommand(HomeMove);
            EnableMotionCommand = new RelayCommand(EnableMotion);
            DisableMotionCommand = new RelayCommand(DisableMotion);
            ResetAxisAmlCommand = new RelayCommand(ResetAxisAml);
            PressUpCommand = new RelayCommand(PressUp);
            PressDownCommand = new RelayCommand(PressDown);
            StartConcentrationCommand = new RelayCommand(StartConcentration);
            StopConcentrationCommand = new RelayCommand(StopConcentration);

            AxisPosInfoChangedCommand = new RelayCommand<object>(AxisPosInfoChanged);
            TechCommand = new RelayCommand<object>(TechAxisPos);
            SavePosDataCommand = new RelayCommand(SaveAxisPos);

            OpenVacuumeCommand = new RelayCommand(OpenVacuume);
            CloseVaccumeCommand = new RelayCommand(CloseVaccume);
        }

       
        private void AbsMove()
        {
            RunCommandAsync(() =>
            {
                _iLS_Motion.P2pMoveWithCheckDone(_axisY, TargetPos, TargetVel, null);
            });
        }

        private void StopMove()
        {
            RunCommandAsync(() =>
            {
                _iLS_Motion.StopMove(_axisY);
            });
        }

        private void HomeMove()
        {
            RunCommandAsync(() =>
            {
                _iLS_Motion.GoHomeWithCheckDone(_axisY, null);
            });
        }

        private void EnableMotion()
        {
            RunCommandAsync(() =>
            {
                _iLS_Motion.ServoOn(_axisY);
            });
        }

        private void DisableMotion()
        {
            RunCommandAsync(() =>
            {
                _iLS_Motion.ServeOff(_axisY);
            });
        }

        private void ResetAxisAml()
        {
            RunCommandAsync(() =>
            {
                _iLS_Motion.ResetAxisAlm(_axisY);
            });
        }

        private void PressUp()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_pressCtl1, true);
                _io.WriteBit_DO(_pressCtl2, false);
            });
        }

        private void PressDown()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_pressCtl1, false);
                _io.WriteBit_DO(_pressCtl2, true);
            });
        }

        private void StartConcentration()
        {
            RunCommandSync(() =>
            {
                _io.WriteByte_DA(4, ConcentrationVel * 3);
                _io.WriteByte_DA(5, ConcentrationVel * 3);
                _io.WriteBit_DO(66, true);
                _io.WriteBit_DO(67, true);
            });
        }

        private void StopConcentration()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(66, false);
                _io.WriteBit_DO(67, false);
            });
        }


        private void OpenVacuume()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(52, true);
                _io.WriteBit_DO(53, true);
            });
        }

        private void CloseVaccume()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(52, false);
                _io.WriteBit_DO(53, false);
            });
        }


        /// <summary>
        /// 获取轴点位信息
        /// </summary>
        /// <param name="axis"></param>
        protected void GetAxisPosInfo(StepAxisEleGear axis)
        {
            AxisPosInfos = new ObservableCollection<AxisPosInfo>();
            ConcentrationPosData data = _dataAccess.GetPosData();
            Type type = typeof(ConcentrationPosData);
            PropertyInfo[] propertyInfos = type.GetProperties();

            foreach (var item in propertyInfos)
            {
                var values = (double)item.GetValue(data);
                string posName = item.Name;
                if (item.IsDefined(typeof(PosNameAttribute)))
                {
                    var posNameAtt = item.GetCustomAttribute(typeof(PosNameAttribute)) as PosNameAttribute;
                    posName = posNameAtt.PosName;
                }
                AxisPosInfos.Add(new AxisPosInfo() { AxisName = axis.AxisName, MemberName = item.Name, AxisNo = axis.SlaveId, PosName = posName, PosData = values });

            }
        }

        /// <summary>
        /// 选择点位数据
        /// </summary>
        /// <param name="obj"></param>
        protected void AxisPosInfoChanged(object obj)
        {
            var axisPosInfo = obj as AxisPosInfo;
            if (axisPosInfo != null)
            {
                TargetPos = axisPosInfo.PosData;
            }
        }

        /// <summary>
        /// 保存点位数据
        /// </summary>
        protected void SaveAxisPos()
        {
            var list = AxisPosInfos.ToList();
            bool result = _dataAccess.UpdatePosDataByAxisPosInfo(1, list);
            SimpleIoc.Default.GetInstance<IConcentration>().UpdatePosData();

        }

        /// <summary>
        /// 示教点位数据
        /// </summary>
        /// <returns></returns>
        protected void TechAxisPos(object obj)
        {
            var posInfo = obj as AxisPosInfo;
            if (posInfo == null)
            {
                return;
            }
            posInfo.PosData = YCurrentPos;
        }


    }

}
