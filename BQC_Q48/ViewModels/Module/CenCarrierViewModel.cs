using BQJX.Core.Interface;
using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using System.Windows;
using System.Collections.ObjectModel;
using Q_Platform.Models;
using BQJX.Core.Common;
using GalaSoft.MvvmLight.Ioc;
using Q_Platform.DAL;
using BQJX.Common.Common;
using System.Reflection;
using Q_Platform.BLL;
using System.Threading;

namespace Q_Platform.ViewModels.Module
{
    [AddINotifyPropertyChangedInterface]
    public class CenCarrierViewModel : MyViewModelBase
    {
        protected readonly IIoDevice _io;
        protected readonly ILS_Motion _iLS_Motion;
        protected readonly IEtherCATMotion _motion;
        protected readonly IEPG26 _clawInstance;
        protected readonly ILogger _logger;

        private ushort _axisZ = 7;               //搬运Z轴
        private ushort _axisX = 14;              //搬运X轴
        private ushort _axisC = 15;              //搬运旋转轴

        private ushort _y_Ctr = 2;               //Y气缸控制
        private ushort _y_HP = 3;                //Y气缸缩回感应
        private ushort _y_WP = 2;                //Y气缸伸出感应

        private ushort _clawId = 2;              //电爪485地址
        private ushort _selectAxis = 7;

        /// <summary>
        /// 轴信息
        /// </summary>
        /// 
        public ObservableCollection<AxisEleGear> ListAxisInfo { get; set; }

        /// <summary>
        /// 轴点位数据信息
        /// </summary>
        public ObservableCollection<AxisPosInfo> AxisPosInfos { get; set; }


        #region Properties

        /// <summary>
        /// 电机状态（使能）
        /// </summary>
        public int MotionStatus { get; set; }

        /// <summary>
        /// 电机io状态 (限位 回零)
        /// </summary>
        public uint MotionIoStatus { get; set; }
        public string AlarmMessage { get; set; }

        public Visibility ShowAlarmMsg { get; set; }

        public double XCurrentPos { get; set; }
        public double ZCurrentPos { get; set; }
        public double CCurrentPos { get; set; }

        public double TargetVel { get; set; } = 80;

        public double TargetPos { get; set; }

        /// <summary>
        /// 电爪状态
        /// </summary>
        public Byte ClawStatus { get; set; }

        /// <summary>
        /// 手爪控制目标位置
        /// </summary>
        public byte ClawTargetPos { get; set; } = 0;

        /// <summary>
        /// 手爪控制目标速度
        /// </summary>
        public byte ClawTargetVel { get; set; } = 255;

        /// <summary>
        /// 手爪控制目标转矩
        /// </summary>
        public byte ClawTargetTorque { get; set; } = 255;

        public Visibility ShowServoStatus { get; set; } = Visibility.Visible;
        public Visibility ShowStepStatus { get; set; } = Visibility.Collapsed;

        #endregion

        #region Commands

        public ICommand AbsMoveCommand { get; set; }
        public ICommand StopMoveCommand { get; set; }
        public ICommand HomeMoveCommand { get; set; }
        public ICommand EnableMotionCommand { get; set; }
        public ICommand DisableMotionCommand { get; set; }
        public ICommand ResetAxisAmlCommand { get; set; }
        public ICommand ExtendCommand { get; set; }
        public ICommand RetrieveCommand { get; set; }



        /// <summary>
        /// 激活夹爪
        /// </summary>
        public ICommand EnableCommand { get; set; }

        /// <summary>
        /// 禁用夹爪
        /// </summary>
        public ICommand DisableCommand { get; set; }

        /// <summary>
        /// 打开夹爪
        /// </summary>
        public ICommand OpenClawCommand { get; set; }

        /// <summary>
        /// 关闭夹爪
        /// </summary>
        public ICommand CloseClawCommand { get; set; }

        /// <summary>
        /// 手爪自定义命令
        /// </summary>
        public ICommand ExcuteUserCommand { get; set; }

        /// <summary>
        /// 轴号变化
        /// </summary>
        public ICommand ComboxSelectChangedCommand { get; set; }

        /// <summary>
        /// 选择轴点位数据
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

        public CenCarrierViewModel(IEtherCATMotion motion, ILS_Motion iLS_Motion, IIoDevice io, IEPG26 clawInstance,ILogger logger)
        {
            this._motion = motion;
            this._iLS_Motion = iLS_Motion;
            this._io = io;
            this._clawInstance = clawInstance;
            this._logger = logger;

            ListAxisInfo = new ObservableCollection<AxisEleGear>();
            ListAxisInfo.Add(new AxisEleGear() { AxisName ="移栽X轴",AxisNo = 14});
            ListAxisInfo.Add(new AxisEleGear() { AxisName ="移栽C轴",AxisNo = 15});
            ListAxisInfo.Add(new AxisEleGear() { AxisName ="搬运Z轴",AxisNo = 7});

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

            RegisterCommand();
        }



        private void RefreshStatus()
        {
            XCurrentPos = _iLS_Motion.GetCurrentPos(_axisX).GetAwaiter().GetResult();
            ZCurrentPos = _motion.GetCurrentPos(_axisZ);
            CCurrentPos = _iLS_Motion.GetCurrentPos(_axisC).GetAwaiter().GetResult();
            var result = _clawInstance.GetClawStatus(_clawId).GetAwaiter().GetResult();
            if (result != null)
            {
                ClawStatus = result.ClawStatus;
            }

            if (_selectAxis == 7)
            {
                MotionIoStatus = _motion.GetMotionIoStatus(7);
            }
            else
            {
                MotionIoStatus = (uint)_iLS_Motion.GetMotionIoStatus(_selectAxis).GetAwaiter().GetResult();
            }

            if (_motion.GetMotionStatus(7) == 4)
            {
                MotionStatus = 1;
            }
            else
            {
                MotionStatus = 0;
            }
        }

        private void RegisterCommand()
        {
            AbsMoveCommand = new RelayCommand(AbsMove);
            StopMoveCommand = new RelayCommand(StopMove);
            HomeMoveCommand = new RelayCommand(HomeMove);
            EnableMotionCommand = new RelayCommand(EnableMotion);
            DisableMotionCommand = new RelayCommand(DisableMotion);
            ResetAxisAmlCommand = new RelayCommand(ResetAxisAml);
            ExtendCommand = new RelayCommand(Extend);
            RetrieveCommand = new RelayCommand(Retrieve);
            EnableCommand = new RelayCommand(EnableClaw);
            DisableCommand = new RelayCommand(DisableClaw);
            OpenClawCommand = new RelayCommand<object>(OpenClaw);
            CloseClawCommand = new RelayCommand(CloseClaw);

            ComboxSelectChangedCommand = new RelayCommand<object>(ComboxSelectChanged);
            AxisPosInfoChangedCommand = new RelayCommand<object>(AxisPosInfoChanged);
            TechCommand = new RelayCommand<object>(TechAxisPos);
            SavePosDataCommand = new RelayCommand(SaveAxisPos);
            ExcuteUserCommand = new RelayCommand(ExcuteUser);
        }

        private void AbsMove()
        {
            RunCommandAsync(() =>
            {
                if (_selectAxis == _axisZ)
                {
                    _motion.P2pMoveWithCheckDone(_axisZ,TargetPos,TargetVel,null);
                }
                else
                {
                    _iLS_Motion.P2pMoveWithCheckDone(_selectAxis,TargetPos,TargetVel,null);
                }

            });
        }

        private void StopMove()
        {
            RunCommandSync(() =>
            {
                if (_selectAxis == _axisZ)
                {
                    _motion.StopMove(_axisZ);
                }
                else
                {
                    _iLS_Motion.StopMove(_selectAxis);
                }

            });
        }

        private void HomeMove()
        {
            RunCommandSync(() =>
            {
                if (_selectAxis == _axisZ)
                {
                    _motion.P2pMoveWithCheckDone(_axisZ,0,50,null);
                }
                else
                {
                    _iLS_Motion.GoHomeWithCheckDone(_selectAxis,null);
                }

            });
        }

        private void EnableMotion()
        {
            RunCommandSync(() =>
            {
                if (_selectAxis == _axisZ)
                {
                    _motion.ServoOn(_axisZ);
                }
                else
                {
                    _iLS_Motion.ServoOn(_selectAxis);
                }

            });
        }

        private void DisableMotion()
        {
            RunCommandSync(() =>
            {
                if (_selectAxis == _axisZ)
                {
                    _motion.ServoOff(_axisZ);
                }
                else
                {
                    _iLS_Motion.ServeOff(_selectAxis);
                }

            });
        }

        private void ResetAxisAml()
        {
            RunCommandSync(() =>
            {
                if (_selectAxis == _axisZ)
                {
                    _motion.ResetAxisAlm(_axisZ);
                }
                else
                {
                    _iLS_Motion.ResetAxisAlm(_selectAxis);
                }
               
            });
        }

        private void Extend()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_y_Ctr, true);
            });
        }

        private void Retrieve()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_y_Ctr, false);
            });
        }

        /// <summary>
        /// 使能电爪
        /// </summary>
        private void EnableClaw()
        {
            RunCommandAsync(() =>
            {
                _clawInstance.Enable(_clawId);
            });
        }

        /// <summary>
        /// 禁用电爪
        /// </summary>
        private void DisableClaw()
        {
            RunCommandAsync(() =>
            {
                _clawInstance.Disable(_clawId);
            });
        }

        /// <summary>
        /// 打开电爪
        /// </summary>
        private void OpenClaw(object openPos)
        {
            if (!byte.TryParse(openPos.ToString(), out byte pos))
            {
                return;
            }
            RunCommandAsync(() =>
            {
                byte open = pos;
                _clawInstance.SendCommand(_clawId, open, 255, 128);
            });

        }

        /// <summary>
        /// 关闭电爪
        /// </summary>
        private void CloseClaw()
        {
            RunCommandAsync(() =>
            {
                _clawInstance.SendCommand(_clawId, 255, 255, 128);
            });
        }



        /// <summary>
        /// 选择轴
        /// </summary>
        /// <param name="obj"></param>
        private void ComboxSelectChanged(object obj)
        {
            var axis = obj as AxisEleGear;
            if (axis == null)
            {
                return;
            }
            _selectAxis = axis.AxisNo;
            if (_selectAxis == 7)
            {
                ShowServoStatus = Visibility.Visible;
                ShowStepStatus = Visibility.Collapsed;
            }
            else
            {
                ShowServoStatus = Visibility.Collapsed;
                ShowStepStatus = Visibility.Visible;
            }
            //更新轴点位信息
            GetAxisPosInfo(axis);

        }

        /// <summary>
        /// 选择位置数
        /// </summary>
        /// <param name="obj"></param>
        private void AxisPosInfoChanged(object obj)
        {
            var axisPosInfo = obj as AxisPosInfo;
            if (axisPosInfo != null)
            {
                TargetPos = axisPosInfo.PosData;
            }
        }

        /// <summary>
        /// 获取轴点位信息
        /// </summary>
        /// <param name="axis"></param>
        private void GetAxisPosInfo(AxisEleGear axis)
        {
            AxisPosInfos = new ObservableCollection<AxisPosInfo>();

            //离心X轴 14
            if (_selectAxis == 14)
            {
                CentrifugalCarrierPosData data = SimpleIoc.Default.GetInstance<ICentrifugalCarrierPosDataAccess>().GetPosData();
                Type type = typeof(CentrifugalCarrierPosData);
                PropertyInfo[] propertyInfos = type.GetProperties();

                foreach (var item in propertyInfos)
                {
                    var values = (double)item.GetValue(data);
                    string posName = item.Name;
                    if (item.IsDefined(typeof(PosNameAttribute)))
                    {
                        var posNameAtt = item.GetCustomAttribute(typeof(PosNameAttribute)) as PosNameAttribute;
                        if (posNameAtt.Is_RotateAxis || posNameAtt.Is_Z2_Axis)
                        {
                            continue;
                        }
                        posName = posNameAtt.PosName;

                    }
                    AxisPosInfos.Add(new AxisPosInfo() { AxisName = axis.AxisName, MemberName = item.Name, AxisNo = axis.AxisNo, PosName = posName, PosData = values });
                }
            }

            //离心C轴
            if (_selectAxis == 15)
            {
                CentrifugalCarrierPosData data = SimpleIoc.Default.GetInstance<ICentrifugalCarrierPosDataAccess>().GetPosData();
                Type type = typeof(CentrifugalCarrierPosData);
                PropertyInfo[] propertyInfos = type.GetProperties();

                foreach (var item in propertyInfos)
                {
                    var values = (double)item.GetValue(data);
                    string posName = item.Name;
                    if (item.IsDefined(typeof(PosNameAttribute)))
                    {
                        var posNameAtt = item.GetCustomAttribute(typeof(PosNameAttribute)) as PosNameAttribute;
                        if (!posNameAtt.Is_RotateAxis)
                        {
                            continue;
                        }
                        posName = posNameAtt.PosName;

                    }
                    AxisPosInfos.Add(new AxisPosInfo() { AxisName = axis.AxisName, MemberName = item.Name, AxisNo = axis.AxisNo, PosName = posName, PosData = values });
                }
            }

            //离心Z轴  7
            if (axis.AxisNo == 7)
            {
                CentrifugalCarrierPosData data = SimpleIoc.Default.GetInstance<ICentrifugalCarrierPosDataAccess>().GetPosData();
                Type type = typeof(CentrifugalCarrierPosData);
                PropertyInfo[] propertyInfos = type.GetProperties();

                foreach (var item in propertyInfos)
                {
                    var values = (double)item.GetValue(data);
                    string posName = item.Name;
                    if (item.IsDefined(typeof(PosNameAttribute)))
                    {
                        var posNameAtt = item.GetCustomAttribute(typeof(PosNameAttribute)) as PosNameAttribute;
                        if (!posNameAtt.Is_Z2_Axis)
                        {
                            continue;
                        }
                        posName = posNameAtt.PosName;

                    }
                    AxisPosInfos.Add(new AxisPosInfo() { AxisName = axis.AxisName, MemberName = item.Name, AxisNo = axis.AxisNo, PosName = posName, PosData = values });
                }

            }

        }


        /// <summary>
        /// 保存点位数据
        /// </summary>
        private void SaveAxisPos()
        {
            var list = AxisPosInfos.ToList();
            bool result = false;

            //离心X轴 14
            if (_selectAxis == 14 || _selectAxis == 15)
            {
                ushort id = 1;
                result = SimpleIoc.Default.GetInstance<ICentrifugalCarrierPosDataAccess>().UpdatePosDataByAxisPosInfo(id, list);
            }
            //Z轴
            if (_selectAxis == 7)
            {
                ushort id = 1;
                result = SimpleIoc.Default.GetInstance<ICentrifugalCarrierPosDataAccess>().UpdatePosDataByAxisPosInfo(id, list);
            }
            SimpleIoc.Default.GetInstance<ICentrifugalCarrier>().UpdatePosData();
        }

        /// <summary>
        /// 示教点位数据
        /// </summary>
        /// <returns></returns>
        private void TechAxisPos(object obj)
        {
            var posInfo = obj as AxisPosInfo;
            if (posInfo == null)
            {
                //return false;
                return;
            }
            if (_selectAxis == _axisX)
            {
                posInfo.PosData = XCurrentPos;
            }
            else if (_selectAxis == _axisC)
            {
                posInfo.PosData = CCurrentPos;
            }
            else if (_selectAxis == _axisZ)
            {
                posInfo.PosData = ZCurrentPos;
            }
           

        }

        protected void ExcuteUser()
        {
            try
            {
                _clawInstance.SendCommand(_clawId, ClawTargetPos, ClawTargetVel, ClawTargetTorque);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



    }



}
