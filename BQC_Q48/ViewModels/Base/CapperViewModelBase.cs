using BQJX.Common.Common;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using PropertyChanged;
using Q_Platform.BLL;
using Q_Platform.Common;
using Q_Platform.DAL;
using Q_Platform.Models;
using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Q_Platform.ViewModels.Base
{
    [AddINotifyPropertyChangedInterface]
    public abstract class CapperViewModelBase : MyViewModelBase
    {
        protected readonly ILS_Motion _iLS_Motion;
        protected readonly IIoDevice _io;

        protected CapperInfo _capperInfo;

        [DoNotNotify]
        public CapperInfo CapperInfo
        {
            get { return _capperInfo; }
            set
            {
                if (_capperInfo != value)
                {
                    _capperInfo = value;
                    var list = _iLS_Motion?.GetAxisInfos();
                    var slaveArr = new ushort[] { _capperInfo.AxisY, _capperInfo.AxisZ, _capperInfo.AxisC1, _capperInfo.AxisC2 };
                    var select = from id in slaveArr from ax in list where ax.SlaveId == id select ax;

                    ListAxisInfo = new ObservableCollection<StepAxisEleGear>();
                    foreach (var item in select)
                    {
                        ListAxisInfo.Add(item);
                    }
                    if (ListAxisInfo.Count > 0)
                    {
                        AxisNo = ListAxisInfo[0].SlaveId;
                    }
                    GetAxisPosInfo(ListAxisInfo[0]);
                }
            }
        }


        protected readonly ICapperPosDataAccess _dataAccess;


        #region Properties

        /// <summary>
        /// 电机状态
        /// </summary>
        public int MotionIoStatus { get; set; }

        /// <summary>
        /// Y轴当前值
        /// </summary>
        public double YCurrentPos { get; set; }

        /// <summary>
        /// Z轴当前值
        /// </summary>
        public double ZCurrentPos { get; set; }

        /// <summary>
        /// 报警信息
        /// </summary>
        public string AlarmMessage { get; set; }

        /// <summary>
        /// 报警信息显示
        /// </summary>
        public Visibility ShowAlarmMsg { get; set; } = Visibility.Hidden;

        /// <summary>
        /// 注射器操作
        /// </summary>
        public Visibility SyringVisibility { get; set; } = Visibility.Collapsed;

        /// <summary>
        /// 定位目标值
        /// </summary>
        public double TargetPos { get; set; }

        /// <summary>
        /// 定位目标速度
        /// </summary>
        public double TargetVel { get; set; } = 100;

        /// <summary>
        /// 轴信息
        /// </summary>
        /// 
        public ObservableCollection<StepAxisEleGear> ListAxisInfo { get; set; }

        [DoNotNotify]
        public ushort AxisNo { get; set; } = 22;


        /// <summary>
        /// 轴点位数据信息
        /// </summary>
        public ObservableCollection<AxisPosInfo> AxisPosInfos { get; set; }


        /// <summary>
        /// 定位中
        /// </summary>
        public bool AbsMoveBusy { get; set; }

        /// <summary>
        /// 定位完成
        /// </summary>
        public bool AbsMoveDone { get; set; }



        #endregion

        #region ICommand

        /// <summary>
        /// 停止
        /// </summary>
        public ICommand StopMoveCommand { get; set; }

        /// <summary>
        /// 复位轴报警
        /// </summary>
        public ICommand ResetAxisAmlCommand { get; set; }

        /// <summary>
        /// 绝对运动
        /// </summary>
        public ICommand AbsMoveCommand { get; set; }

        /// <summary>
        /// 回零运动
        /// </summary>
        public ICommand HomeMoveCommand { get; set; }

        /// <summary>
        /// 使能电机
        /// </summary>
        public ICommand EnableMotionCommand { get; set; }

        /// <summary>
        /// 失能电机
        /// </summary>
        public ICommand DisableMotionCommand { get; set; }

        /// <summary>
        /// 正向点动
        /// </summary>
        public ICommand JogFCommand { get; set; }

        /// <summary>
        /// 负向点动
        /// </summary>
        public ICommand JogRCommand { get; set; }

        /// <summary>
        /// 停止点动
        /// </summary>
        public ICommand StopJogCommand { get; set; }




        /// <summary>
        /// 装盖
        /// </summary>
        public ICommand GetCapperOnCommand { get; set; }

        /// <summary>
        /// 拆盖
        /// </summary>
        public ICommand PutCapperOffCommand { get; set; }

        /// <summary>
        /// 轴号变化
        /// </summary>
        public ICommand ComboxSelectChangedCommand { get; set; }

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


        /// <summary>
        /// 打开手爪
        /// </summary>
        public ICommand OpenClawCommand { get; set; }

        /// <summary>
        /// 关闭手爪
        /// </summary>
        public ICommand CloseClawCommand { get; set; }

        /// <summary>
        /// 打开抱夹
        /// </summary>
        public ICommand OpenHoldingCommand { get; set; }

        /// <summary>
        /// 关闭抱夹
        /// </summary>
        public ICommand CloseHoldingCommand { get; set; }


        #endregion

        #region Construtors

        public CapperViewModelBase(ILS_Motion iLS_Motion, IIoDevice io, ICapperPosDataAccess dataAccess)
        {
            this._iLS_Motion = iLS_Motion;
            this._io = io;
            this._dataAccess = dataAccess;

            RegisterCommand();
        }

        #endregion

        protected virtual void RefreshIoStatus()
        {
            MotionIoStatus = _iLS_Motion.GetMotionIoStatus(AxisNo).GetAwaiter().GetResult();
            YCurrentPos = _iLS_Motion.GetCurrentPos(_capperInfo.AxisY).GetAwaiter().GetResult();
            ZCurrentPos = _iLS_Motion.GetCurrentPos(_capperInfo.AxisZ).GetAwaiter().GetResult();


            AlarmMessage = "";
            ShowAlarmMsg = Visibility.Visible;

        }


        protected virtual void RegisterCommand()
        {
            StopMoveCommand = new RelayCommand(StopMove);
            ResetAxisAmlCommand = new RelayCommand(ResetAxisAml);
            AbsMoveCommand = new RelayCommand(AbsMove);
            HomeMoveCommand = new RelayCommand(HomeMove);
            EnableMotionCommand = new RelayCommand(EnableMotion);
            DisableMotionCommand = new RelayCommand(DisableMotion);
            JogFCommand = new RelayCommand(JogF);
            JogRCommand = new RelayCommand(JogR);
            StopJogCommand = new RelayCommand(StopJog);

            GetCapperOnCommand = new RelayCommand(GetCapperOn);
            PutCapperOffCommand = new RelayCommand(PutCapperOff);

            ComboxSelectChangedCommand = new RelayCommand<object>(ComboxSelectChanged);
            AxisPosInfoChangedCommand = new RelayCommand<object>(AxisPosInfoChanged);


            TechCommand = new RelayCommand<object>(TechAxisPos);
            SavePosDataCommand = new RelayCommand(SaveAxisPos);

            OpenClawCommand = new RelayCommand(OpenClaw);
            CloseClawCommand = new RelayCommand(CloseClaw);
            OpenHoldingCommand = new RelayCommand(OpenHolding);
            CloseHoldingCommand = new RelayCommand(CloseHolding);
        }


        /// <summary>
        /// 保存点位数据
        /// </summary>
        protected virtual void SaveAxisPos()
        {
            var list = AxisPosInfos.ToList();
            bool result = _dataAccess.UpdatePosDataByAxisPosInfo((ushort)_capperInfo.CapperId, list);


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
            if (AxisNo == _capperInfo.AxisY)
            {
                posInfo.PosData = YCurrentPos;
            }
            if (AxisNo == _capperInfo.AxisZ)
            {
                posInfo.PosData = ZCurrentPos;
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
        /// 选择轴
        /// </summary>
        /// <param name="obj"></param>
        protected void ComboxSelectChanged(object obj)
        {
            StepAxisEleGear stepAxisEleGear = obj as StepAxisEleGear;
            if (stepAxisEleGear != null)
            {
                AxisNo = stepAxisEleGear.SlaveId;

                //更新轴点位信息

                GetAxisPosInfo(stepAxisEleGear);
            }

        }

        /// <summary>
        /// 获取轴点位信息
        /// </summary>
        /// <param name="axis"></param>
        protected void GetAxisPosInfo(StepAxisEleGear axis)
        {
            AxisPosInfos = new ObservableCollection<AxisPosInfo>();
            CapperPosData data = _dataAccess.GetCapperPosData(_capperInfo.CapperId);
            Type type = typeof(CapperPosData);
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



        protected void CloseHolding()
        {
            try
            {
                _io.WriteBit_DO(_capperInfo.HoldCtl, true);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message);

                });
            }

        }

        protected void OpenHolding()
        {
            try
            {
                _io.WriteBit_DO(_capperInfo.HoldCtl, false);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message);

                });
            }
        }

        protected void CloseClaw()
        {
            try
            {
                _io.WriteBit_DO(_capperInfo.ClawCtl, false);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message);

                });
            }
        }

        protected void OpenClaw()
        {
            try
            {
                _io.WriteBit_DO(_capperInfo.ClawCtl, true);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message);

                });
            }
        }

        protected void PutCapperOff()
        {
            try
            {
                _iLS_Motion.RelativeMove(_capperInfo.AxisC1, _capperInfo.CapperOffDistance, 50, 50, 50);
                _iLS_Motion.RelativeMove(_capperInfo.AxisC2, _capperInfo.CapperOffDistance, 50, 50, 50);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected void GetCapperOn()
        {
            try
            {
                _iLS_Motion.TorqueMoveWithCheckDone(_capperInfo.AxisC1, 30, _capperInfo.CapperOnTorque, 0, null);
                _iLS_Motion.TorqueMoveWithCheckDone(_capperInfo.AxisC2, 30, _capperInfo.CapperOnTorque, 0, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }

        protected void StopMove()
        {
            try
            {
                _iLS_Motion.StopMove(_capperInfo.AxisY);
                _iLS_Motion.StopMove(_capperInfo.AxisZ);
                _iLS_Motion.StopMove(_capperInfo.AxisC1);
                _iLS_Motion.StopMove(_capperInfo.AxisC2);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected void ResetAxisAml()
        {
            try
            {
                _iLS_Motion.ResetAxisAlm(AxisNo);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected async void AbsMove()
        {
            try
            {
                AbsMoveDone = await _iLS_Motion.P2pMoveWithCheckDone(AxisNo, TargetPos, TargetVel, null);

            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message);

                });
            }
        }

        protected void HomeMove()
        {
            try
            {
                if (AxisNo == 12 || AxisNo == 13 || AxisNo == 27 || AxisNo == 28 || AxisNo == 29)
                {//12 13 27 28 29
                    _iLS_Motion.DM2C_GoHomeWithCheckDone(AxisNo, null);
                }
                else
                {
                    _iLS_Motion.GoHomeWithCheckDone(AxisNo, null);
                }


            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message);

                });
            }
        }

        protected void EnableMotion()
        {
            try
            {
                _iLS_Motion.ServoOn(AxisNo);

            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message);

                });
            }
        }

        protected void DisableMotion()
        {
            try
            {
                _iLS_Motion.ServeOff(AxisNo);

            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message);

                });
            }

        }

        protected void JogF()
        {
            try
            {
                double jogVel = TargetVel > 200 ? 200 : TargetVel;
                _iLS_Motion.JogF(AxisNo);

            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message);

                });
            }

        }

        protected void JogR()
        {
            try
            {
                double jogVel = TargetVel > 200 ? 200 : TargetVel;
                _iLS_Motion.JogR(AxisNo);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message);

                });
            }

        }

        protected void StopJog()
        {
            try
            {
                _iLS_Motion.StopMove(AxisNo);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.Message);

                });
            }

        }


  


    }
}
