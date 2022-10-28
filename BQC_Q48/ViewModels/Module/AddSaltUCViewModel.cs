using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Q_Platform.Models;
using BQJX.Common.Common;
using System.Windows;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using Q_Platform.DAL;
using System.Reflection;
using GalaSoft.MvvmLight.Ioc;
using Q_Platform.BLL;
using System.Threading;

namespace Q_Platform.ViewModels.Module
{
    [AddINotifyPropertyChangedInterface]
    public class AddSaltUCViewModel :MyViewModelBase
    {
        protected readonly ILS_Motion _iLS_Motion;
        protected readonly IEtherCATMotion _motion;
        protected readonly IIoDevice _io;
        protected readonly IAddSolidPosDataAccess _dataAccess;

        private readonly ushort _axisY1 = 5;    //伺服Y轴
        private readonly ushort _axisY2 = 1;    //步进Y轴
        private readonly ushort _axisC1 = 3;    //步进旋转轴
        private readonly ushort _axisC2 = 2;    //步进旋转轴
        private readonly ushort _axisZ1 = 30;    //步进Z1轴
        private readonly ushort _axisZ2 = 31;    //步进Z2轴
        private readonly ushort _XCylinderControl = 9; //X气缸控制
        private readonly ushort _XCylinderRight = 12;    //X气缸右感应
        private readonly ushort _XCylinderLeft = 13;     //X气缸左感应
        private ushort _weightId1 = 1;                   //称台1
        private ushort _weightId2 = 2;                   //称台2


        #region Properties

        /// <summary>
        /// 电机状态（使能）
        /// </summary>
        public int MotionStatus { get; set; }

        /// <summary>
        /// 电机io状态 (限位 回零)
        /// </summary>
        public uint MotionIoStatus { get; set; }

        /// <summary>
        /// Y1轴当前值
        /// </summary>
        public double Y1CurrentPos { get; set; }

        /// <summary>
        /// Y2轴当前值
        /// </summary>
        public double Y2CurrentPos { get; set; }

        /// <summary>
        /// Z1轴当前值
        /// </summary>
        public double Z1CurrentPos { get; set; }

        /// <summary>
        /// Z2轴当前值
        /// </summary>
        public double Z2CurrentPos { get; set; }

        /// <summary>
        /// C1轴当前值
        /// </summary>
        public double C1CurrentPos { get; set; }

        /// <summary>
        /// C2轴当前值
        /// </summary>
        public double C2CurrentPos { get; set; }

        /// <summary>
        /// 报警信息
        /// </summary>
        public string AlarmMessage { get; set; }

        /// <summary>
        /// 报警信息显示
        /// </summary>
        public Visibility ShowAlarmMsg { get; set; } = Visibility.Hidden;

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
        public ushort AxisNo { get; set; } = 1;


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


        public Visibility ShowServoStatus { get; set; } = Visibility.Visible;
        public Visibility ShowStepStatus { get; set; } = Visibility.Collapsed;

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


        public ICommand CylinderLeftCommand { get; set; }
        public ICommand CylinderRightCommand { get; set; }

        /// <summary>
        /// 旋转轴启动
        /// </summary>
        public ICommand RotateStartCommand { get; set; }

        /// <summary>
        /// 旋转轴停止
        /// </summary>
        public ICommand RotateStopCommand { get; set; }


        /// <summary>
        /// 轴选择
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


        #endregion


        public AddSaltUCViewModel(IEtherCATMotion motion,ILS_Motion iLS_Motion,IIoDevice io, IAddSolidPosDataAccess dataAccess)
        {
            this._motion = motion;
            this._iLS_Motion = iLS_Motion;
            this._io = io;
            this._dataAccess = dataAccess;
            ListAxisInfo = new ObservableCollection<StepAxisEleGear>();
            ListAxisInfo.Add(new StepAxisEleGear { AxisName = "加盐Y1轴", SlaveId = 5, EleGear = 0.1 });
            ListAxisInfo.Add(new StepAxisEleGear { AxisName = "加盐Y2轴", SlaveId = 1, EleGear = 88.2, HomeHigh = 10, UpdateParams = true, JogVel = 10, JogAccDec = 50 });
            ListAxisInfo.Add(new StepAxisEleGear { AxisName = "加盐C1轴", SlaveId = 3, EleGear = 10000 });
            ListAxisInfo.Add(new StepAxisEleGear { AxisName = "加盐C2轴", SlaveId = 2, EleGear = 10000 });
            ListAxisInfo.Add(new StepAxisEleGear { AxisName = "加盐Z1轴", SlaveId = 30, EleGear = 2500 });
            ListAxisInfo.Add(new StepAxisEleGear { AxisName = "加盐Z2轴", SlaveId = 31, EleGear = 2500 });

            _refreshTask = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        RefreshIoStatus();
                        if (_stopRefresh)
                        {
                            break;
                        }
                        Thread.Sleep(1000);
                    }
                    catch (Exception )
                    {
                        //logger?.Error($"_refreshTask err:{ex.Message}");
                    }
                }

            });
            RegisterCommand();
        }



        protected void RefreshIoStatus()
        {
            Y1CurrentPos = _motion.GetCurrentPos(_axisY1);
            Z1CurrentPos = _iLS_Motion.GetCurrentPos(_axisZ1).GetAwaiter().GetResult();
            Z2CurrentPos = _iLS_Motion.GetCurrentPos(_axisZ2).GetAwaiter().GetResult();
            Y2CurrentPos = _iLS_Motion.GetCurrentPos(_axisZ2).GetAwaiter().GetResult();
            C1CurrentPos = _iLS_Motion.GetCurrentPos(_axisC1).GetAwaiter().GetResult();
            C2CurrentPos = _iLS_Motion.GetCurrentPos(_axisC2).GetAwaiter().GetResult();

            if (AxisNo == 5)
            {
                MotionIoStatus = _motion.GetMotionIoStatus(5);
            }
            else
            {
                MotionIoStatus = (uint)_iLS_Motion.GetMotionIoStatus(AxisNo).GetAwaiter().GetResult();
            }

            if (_motion.GetMotionStatus(5) == 4)
            {
                MotionStatus = 1;
            }
            else
            {
                MotionStatus = 0;
            }

            AlarmMessage = "";
            ShowAlarmMsg = Visibility.Visible;


        }

        protected void RegisterCommand()
        {
            StopMoveCommand = new RelayCommand(StopMove);
            ResetAxisAmlCommand = new RelayCommand(ResetAxisAml);
            AbsMoveCommand = new RelayCommand(async()=>await AbsMove());
            HomeMoveCommand = new RelayCommand(HomeMove);
            EnableMotionCommand = new RelayCommand(EnableMotion);
            DisableMotionCommand = new RelayCommand(DisableMotion);


            ComboxSelectChangedCommand = new RelayCommand<object>(ComboxSelectChanged);
            AxisPosInfoChangedCommand = new RelayCommand<object>(AxisPosInfoChanged);


            TechCommand = new RelayCommand<object>(TechAxisPos);
            SavePosDataCommand = new RelayCommand(SaveAxisPos);
            CylinderLeftCommand = new RelayCommand(CylinderLeft);
            CylinderRightCommand = new RelayCommand(CylinderRight);
            RotateStartCommand = new RelayCommand(RotateStart);
            RotateStopCommand = new RelayCommand(RotateStop);
             
        }


        /// <summary>
        /// 保存点位数据
        /// </summary>
        protected void SaveAxisPos()
        {
            var list = AxisPosInfos.ToList();
            if (AxisNo == 5)
            {
                bool result = _dataAccess.UpdatePosDataByAxisPosInfo(1, list);
            }
            else if (AxisNo == _axisY2)
            {
                bool result = _dataAccess.UpdatePosDataByAxisPosInfo(0, list);
            }
            else if(AxisNo == _axisZ1 || AxisNo ==_axisZ2)
            {
                bool result = _dataAccess.UpdatePosDataByAxisPosInfo(2, list);
            }
            SimpleIoc.Default.GetInstance<IAddSolid>().UpdatePosData();
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
            if (AxisNo == 5)
            {
                posInfo.PosData = Y1CurrentPos;
            }
            if (AxisNo == _axisY2)
            {
                posInfo.PosData = Y1CurrentPos;
            }
            if (AxisNo == _axisZ1)
            {
                posInfo.PosData = Y1CurrentPos;
            }
            if (AxisNo == _axisZ2)
            {
                posInfo.PosData = Y1CurrentPos;
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
                try
                {
                    GetAxisPosInfo(stepAxisEleGear);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message) ;
                }

          

                if (AxisNo == 5)
                {
                    ShowServoStatus = Visibility.Visible;
                    ShowStepStatus = Visibility.Collapsed;
                }
                else
                {
                    ShowServoStatus = Visibility.Collapsed;
                    ShowStepStatus = Visibility.Visible;
                }
            }

        }

        /// <summary>
        /// 获取轴点位信息
        /// </summary>
        /// <param name="axis"></param>
        protected void GetAxisPosInfo(StepAxisEleGear axis)
        {
            AxisPosInfos = new ObservableCollection<AxisPosInfo>();
            AddSolidPosData data = _dataAccess.GetPosData();
            Type type = typeof(AddSolidPosData);
            PropertyInfo[] propertyInfos = type.GetProperties();
            int index = 0;
            foreach (var item in propertyInfos)
            {
                var values = (double[])item.GetValue(data);
                string posName = item.Name;
                if (item.IsDefined(typeof(PosNameAttribute)))
                {
                    var posNameAtt = item.GetCustomAttribute(typeof(PosNameAttribute)) as PosNameAttribute;
                    posName = posNameAtt.PosName;
                    if (axis.SlaveId == _axisY1)
                    {
                        index = 0;
                    }
                    else if(axis.SlaveId == _axisY2)
                    {
                        index = 1;
                    }
                    else if(axis.SlaveId == _axisZ1 || axis.SlaveId == _axisZ2)
                    {
                        index = 2;
                    }
                    else
                    {
                        return;
                    }
                }
                AxisPosInfos.Add(new AxisPosInfo() { AxisName = axis.AxisName, MemberName = item.Name, AxisNo = axis.SlaveId, PosName = posName, PosData = values[index] });

            }
        }


        protected void StopMove()
        {
            RunCommandAsync(() =>
            {
                _motion.StopMove(_axisY1);
                _iLS_Motion.StopMove(_axisY2);
                _iLS_Motion.StopMove(_axisC1);
                _iLS_Motion.StopMove(_axisC2);
                _iLS_Motion.StopMove(_axisZ1);
                _iLS_Motion.StopMove(_axisZ2);
            });
        }

        protected void ResetAxisAml()
        {
            try
            {
                if (AxisNo == 5)
                {
                    _motion.ResetAxisAlm(5);
                }
                else
                {
                _iLS_Motion.ResetAxisAlm(AxisNo);

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected async Task AbsMove()
        {
            await RunCommandAsync(async() =>
            {
                if (AxisNo == _axisY1)
                {
                    AbsMoveDone = await _motion.P2pMoveWithCheckDone(_axisY1, TargetPos, TargetVel, null);
                }
                else
                {
                    AbsMoveDone =await _iLS_Motion.P2pMoveWithCheckDone(AxisNo, TargetPos, TargetVel, null);
                }
                
            }).ConfigureAwait(false);
        }

        protected void HomeMove()
        {
            RunCommandAsync(() =>
            {
                if (AxisNo == _axisY1)
                {
                    _motion.P2pMoveWithCheckDone(_axisY1, 0, 50, null).GetAwaiter().GetResult();
                }
                else if(AxisNo == _axisZ1)
                {
                    _iLS_Motion.DM2C_GoHomeWithCheckDone(AxisNo, null).GetAwaiter().GetResult();
                }
                else if(AxisNo == _axisZ2)
                {
                    _iLS_Motion.DM2C_GoHomeWithCheckDone(AxisNo, null).GetAwaiter().GetResult();
                }
                else
                {
                    _iLS_Motion.GoHomeWithCheckDone(AxisNo, null).GetAwaiter().GetResult();
                }
            });
        }

        protected void EnableMotion()
        {
            RunCommandSync(() =>
            {
                if (AxisNo == _axisY1)
                {
                    _motion.ServoOn(_axisY1);
                }
                else
                {
                    _iLS_Motion.ServoOn(AxisNo);
                }
            });
         
        }

        protected void DisableMotion()
        {
            RunCommandSync(() =>
            {
                if (AxisNo == _axisY1)
                {
                    _motion.ServoOff(_axisY1);
                }
                else
                {
                    _iLS_Motion.ServeOff(AxisNo);
                }
            });

        }

        private void RotateStart()
        {
            RunCommandSync(() =>
            {
                _iLS_Motion.VelocityMove(_axisC1, TargetVel).ConfigureAwait(false);
                _iLS_Motion.VelocityMove(_axisC2, TargetVel).ConfigureAwait(false);
            });
        }

        private void RotateStop()
        {
            RunCommandSync(() =>
            {
                _iLS_Motion.StopMove(_axisC1);
                _iLS_Motion.StopMove(_axisC2);

            });
        }

        private void CylinderLeft()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_XCylinderControl, false);
            });
        }
        private void CylinderRight()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_XCylinderControl, true);
            });
        }


    }


}
