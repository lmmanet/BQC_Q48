using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PropertyChanged;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Q_Platform.Models;
using BQJX.Common.Common;
using System.Reflection;
using Q_Platform.DAL;
using System.Threading;
using GalaSoft.MvvmLight.Ioc;
using Q_Platform.BLL;

namespace Q_Platform.ViewModels.Module
{
    [AddINotifyPropertyChangedInterface]
    public class VortexViewModel : MyViewModelBase
    {

        protected readonly IIoDevice _io;
        protected readonly ILS_Motion _iLS_Motion;
        protected readonly IVortexPosDataAccess _dataAccess;
        protected readonly ILogger _logger;
        private readonly ushort _axisY = 8;
        private readonly ushort _pressCtl = 15;


        #region Properties

        /// <summary>
        /// 电机状态
        /// </summary>
        public int MotionIoStatus { get; set; }

        public string AlarmMessage { get; set; }

        public Visibility ShowAlarmMsg { get; set; }

        public double YCurrentPos { get; set; }

        public double TargetVel { get; set; } = 100;

        public double TargetPos { get; set; }

        public int VortexVel { get; set; } = 2000;

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
        public ICommand StartVortexCommand { get; set; }
        public ICommand StopVortexCommand { get; set; }


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

        #region Construtors

        public VortexViewModel(ILS_Motion iLS_Motion, IIoDevice io, IVortexPosDataAccess dataAccess, ILogger logger)
        {
            this._iLS_Motion = iLS_Motion;
            this._io = io;
            this._dataAccess = dataAccess;
            this._logger = logger;
            RegisterCommand();
            GetAxisPosInfo(new StepAxisEleGear { AxisName = "涡旋Y轴", SlaveId = 8, EleGear = 787.40 });
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

        #endregion


        private void RefreshStatus()
        {
            MotionIoStatus = _iLS_Motion.GetMotionIoStatus(_axisY).GetAwaiter().GetResult();
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
            StartVortexCommand = new RelayCommand(StartVortex);
            StopVortexCommand = new RelayCommand(StopVortex);

            AxisPosInfoChangedCommand = new RelayCommand<object>(AxisPosInfoChanged);
            TechCommand = new RelayCommand<object>(TechAxisPos);
            SavePosDataCommand = new RelayCommand(SaveAxisPos);
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
                _iLS_Motion.GoHomeWithCheckDone(_axisY,null);
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
                _io.WriteBit_DO(_pressCtl, false);
            });
        }

        private void PressDown()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_pressCtl, true);
            });
        }

        private void StartVortex()
        {
            RunCommandSync(() =>
            {
                _io.WriteByte_DA(0,VortexVel*10);
                _io.WriteByte_DA(1,VortexVel*10);
                _io.WriteBit_DO(32, true);
                _io.WriteBit_DO(33, true);
            });
        }

        private void StopVortex()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(32, false);
                _io.WriteBit_DO(33, false);
            });
        }


        /// <summary>
        /// 获取轴点位信息
        /// </summary>
        /// <param name="axis"></param>
        protected void GetAxisPosInfo(StepAxisEleGear axis)
        {
            AxisPosInfos = new ObservableCollection<AxisPosInfo>();
            VortexPosData data = _dataAccess.GetPosData();
            Type type = typeof(VortexPosData);
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
            SimpleIoc.Default.GetInstance<IVortex>().UpdatePosData();
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
