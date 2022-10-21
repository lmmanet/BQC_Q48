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
        private readonly ushort _XCylinderControl = 9; //X气缸控制
        private readonly ushort _Z1CylinderControl = 11; //Z1气缸控制
        private readonly ushort _Z2CylinderControl = 13; //Z2气缸控制
        private readonly ushort _XCylinderRight = 12;    //X气缸右感应
        private readonly ushort _XCylinderLeft = 13;     //X气缸左感应
        private readonly ushort _Z1CylinderDown = 9;     //Z1气缸下感应
        private readonly ushort _Z2CylinderDown = 11;    //Z2气缸下感应
        private ushort _weightId1 = 1;                   //称台1
        private ushort _weightId2 = 2;                   //称台2


        #region Properties

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
        /// Z1气缸下
        /// </summary>
        public ICommand PressDown1Command { get; set; }

        /// <summary>
        /// Z2气缸下
        /// </summary>
        public ICommand PressDown2Command { get; set; }

        /// <summary>
        /// Z1气缸上
        /// </summary>
        public ICommand PressUp1Command { get; set; }

        /// <summary>
        /// Z2气缸上
        /// </summary>
        public ICommand PressUp2Command { get; set; }
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
            RegisterCommand();
        }



        protected void RefreshIoStatus()
        {
            //var MotionStatusY = _iLS_Motion.GetMotionIoStatus(_capperInfo.AxisY).GetAwaiter().GetResult();
            //var MotionStatusZ = _iLS_Motion.GetMotionIoStatus(_capperInfo.AxisZ).GetAwaiter().GetResult();
            //YCurrentPos = _iLS_Motion.GetCurrentPos(_capperInfo.AxisY).GetAwaiter().GetResult();
            //ZCurrentPos = _iLS_Motion.GetCurrentPos(_capperInfo.AxisZ).GetAwaiter().GetResult();

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

            PressDown1Command = new RelayCommand(PressDown1);
            PressDown2Command = new RelayCommand(PressDown2);
            PressUp1Command = new RelayCommand(PressUp1);
            PressUp2Command = new RelayCommand(PressUp2);
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
            bool result = _dataAccess.UpdatePosDataByAxisPosInfo(0, list);
            //加盐Y为1
           // bool result = _dataAccess.UpdatePosDataByAxisPosInfo(1, list);


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
            //if (AxisNo == _capperInfo.AxisY)
            //{
            //    posInfo.PosData = YCurrentPos;
            //}
            //if (AxisNo == _capperInfo.AxisZ)
            //{
            //    posInfo.PosData = ZCurrentPos;
            //}
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
            AddSolidPosData data = _dataAccess.GetPosData();
            Type type = typeof(AddSolidPosData);
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


        protected void StopMove()
        {
            RunCommandAsync(() =>
            {
                _motion.StopMove(_axisY1);
                _iLS_Motion.StopMove(_axisY2);
                _iLS_Motion.StopMove(_axisC1);
                _iLS_Motion.StopMove(_axisC2);
            });
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


        private void PressDown1()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_Z1CylinderControl, true);
            });
        }
        
        private void PressDown2()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_Z2CylinderControl, true);
            });
        }
        
        private void PressUp1()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_Z1CylinderControl, false);
            });
        }
        
        private void PressUp2()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_Z2CylinderControl, false);
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
