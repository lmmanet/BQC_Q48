using BQJX.Common.Common;
using BQJX.Core.Common;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using PropertyChanged;
using Q_Platform.BLL;
using Q_Platform.Common;
using Q_Platform.DAL;
using Q_Platform.Models;
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
    public abstract class CarrierViewModelBase : MyViewModelBase
    {
        protected readonly ICarrierOneDataAccess _dataAccess;

        protected readonly IEtherCATMotion _motion;
        protected readonly IEPG26 _clawInstance;
        protected readonly ILogger _logger;
      
        protected CarrierInfo _carrierInfo;

        [DoNotNotify]
        public CarrierInfo CarrierInfo
        {
            get { return _carrierInfo; }
            set
            {
                if (_carrierInfo != value)
                {
                    _carrierInfo = value;
                    var list = _motion.GetAxisInfos();
                    var slaveArr = new ushort[] { _carrierInfo.AxisX, _carrierInfo.AxisY, _carrierInfo.AxisZ1, _carrierInfo.AxisZ2 ,_carrierInfo.AxisF};
                    var select = from id in slaveArr from ax in list where ax.AxisNo == id select ax;

                    ListAxisInfo = new ObservableCollection<AxisEleGear>();
                    foreach (var item in select)
                    {
                        ListAxisInfo.Add(item);
                    }

                    GetAxisPosInfo(ListAxisInfo[0]);
                }
            }
        }


        #region Properties

        /// <summary>
        /// X轴当前值
        /// </summary>
        public double XCurrentPos { get; set; }

        /// <summary>
        /// Y轴当前值
        /// </summary>
        public double YCurrentPos { get; set; }

        /// <summary>
        /// Z1轴当前值
        /// </summary>
        public double Z1CurrentPos { get; set; }

        /// <summary>
        /// Z2轴当前值
        /// </summary>
        public double Z2CurrentPos { get; set; }       
    
        /// <summary>
        /// 移液器当前值
        /// </summary>
        public double FCurrentPos { get; set; }

        /// <summary>
        /// X轴当前速度
        /// </summary>
        public double XCurrentVel { get; set; }

        /// <summary>
        /// Y轴当前速度
        /// </summary>
        public double YCurrentVel { get; set; }

        /// <summary>
        /// Z1轴当前速度
        /// </summary>
        public double Z1CurrentVel { get; set; }

        /// <summary>
        /// Z2轴当前速度
        /// </summary>
        public double Z2CurrentVel { get; set; }

        /// <summary>
        /// 移液器当前速度
        /// </summary>
        public double FCurrentVel { get; set; }

        /// <summary>
        /// 电机状态（使能）
        /// </summary>
        public int MotionStatus { get; set; }

        /// <summary>
        /// 电机io状态 (限位 回零)
        /// </summary>
        public uint MotionIoStatus { get; set; }

        /// <summary>
        /// 电爪状态
        /// </summary>
        public Byte ClawStatus { get; set; }

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
        public double TargetVel { get; set; } = 50;

        /// <summary>
        /// 轴信息
        /// </summary>
        /// 
        public ObservableCollection<AxisEleGear> ListAxisInfo { get; set; }

        [DoNotNotify]
        public ushort AxisNo { get; set; }


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

        #region Commands

        /// <summary>
        /// 停止
        /// </summary>
        public ICommand StopMoveCommand { get; set; }

        /// <summary>
        /// 急停
        /// </summary>
        public ICommand EmgStopMoveCommand { get; set; }

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
        /// 编码器回读数据偏移清零
        /// </summary>
        public ICommand ClearPosOffsetCommand { get; set; }

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
        /// 移液器退枪头
        /// </summary>
        public ICommand PutNeeldCommand { get; set; }

        /// <summary>
        /// 移液器注射命令
        /// </summary>
        public ICommand SyringCommand { get; set; }

        /// <summary>
        /// 移液器吸液命令
        /// </summary>
        public ICommand ObsorbCommand { get; set; }

        /// <summary>
        /// 移液器回零命令
        /// </summary>
        public ICommand SyringHomeCommand { get; set; }

  

        #endregion

        public bool Homing { get; set; }


        public CarrierViewModelBase(ICarrierOneDataAccess dataAccess, IEtherCATMotion motion, IEPG26 clawInstance, ILogger logger)
        {
            this._dataAccess = dataAccess;
            this._motion = motion;
            this._clawInstance = clawInstance;
            this._logger = logger;

            RegisterCommand();

        }


        protected virtual void RefreshStatus()
        {
            MotionIoStatus = _motion.GetMotionIoStatus(AxisNo);
            if (_motion.GetMotionStatus(AxisNo) == 4)
            {
                MotionStatus = 1;
            }
            else
            {
                MotionStatus = 0;
            }
            XCurrentPos = _motion.GetCurrentPos(CarrierInfo.AxisX);
            XCurrentVel = _motion.GetCurrentVel(CarrierInfo.AxisX);
            YCurrentPos = _motion.GetCurrentPos(CarrierInfo.AxisY);
            YCurrentVel = _motion.GetCurrentVel(CarrierInfo.AxisY);
            Z1CurrentPos = _motion.GetCurrentPos(CarrierInfo.AxisZ1);
            Z1CurrentVel = _motion.GetCurrentVel(CarrierInfo.AxisZ1);
            Z2CurrentPos = _motion.GetCurrentPos(CarrierInfo.AxisZ2);
            Z2CurrentVel = _motion.GetCurrentVel(CarrierInfo.AxisZ2);
            FCurrentPos = _motion.GetCurrentPos(CarrierInfo.AxisF);
            FCurrentVel = _motion.GetCurrentVel(CarrierInfo.AxisF);

            var result = _clawInstance.GetClawStatus(CarrierInfo.ClawId).GetAwaiter().GetResult();
            if (result != null)
            {
                ClawStatus = result.ClawStatus;
            }
        }


        protected virtual void RegisterCommand()
        {
            StopMoveCommand = new RelayCommand(StopMove);
            EmgStopMoveCommand = new RelayCommand(EmgStopMove);
            ResetAxisAmlCommand = new RelayCommand(ResetAxisAml);
            AbsMoveCommand = new RelayCommand(AbsMove);
            HomeMoveCommand = new RelayCommand(HomeMove);
            EnableMotionCommand = new RelayCommand(EnableMotion);
            DisableMotionCommand = new RelayCommand(DisableMotion);
            JogFCommand = new RelayCommand(JogF);
            JogRCommand = new RelayCommand(JogR);
            StopJogCommand = new RelayCommand(StopJog);
            ClearPosOffsetCommand = new RelayCommand(ClearPosOffset);
            ComboxSelectChangedCommand = new RelayCommand<object>(ComboxSelectChanged);
            AxisPosInfoChangedCommand = new RelayCommand<object>(AxisPosInfoChanged);

            TechCommand = new RelayCommand<object>(TechAxisPos);
            SavePosDataCommand = new RelayCommand(SaveAxisPos);


            SyringCommand = new RelayCommand(Syring);
            ObsorbCommand = new RelayCommand<object>(Obsorb);
            SyringHomeCommand = new RelayCommand(SyringHome);
            PutNeeldCommand = new RelayCommand(PutNeedle);

            EnableCommand = new RelayCommand(EnableClaw);
            DisableCommand = new RelayCommand(DisableClaw);
            OpenClawCommand = new RelayCommand<object>(OpenClaw);
            CloseClawCommand = new RelayCommand(CloseClaw);
        }

        /// <summary>
        /// 保存点位数据
        /// </summary>
        protected abstract void SaveAxisPos();

        /// <summary>
        /// 选择轴
        /// </summary>
        /// <param name="obj"></param>
        protected void ComboxSelectChanged(object obj)
        {
            var axis = obj as AxisEleGear;
            if (axis == null)
            {
                return;
            }
            AxisNo = axis.AxisNo;
            //更新轴点位信息
            GetAxisPosInfo(axis);

        }

        /// <summary>
        /// 获取轴点位信息
        /// </summary>
        /// <param name="axis"></param>
        protected void GetAxisPosInfo(AxisEleGear axis)
        {
            AxisPosInfos = new ObservableCollection<AxisPosInfo>();

            if (axis.AxisNo <= 3)//提取搬运X轴 ,提取搬运Y轴,提取搬运Z1轴,提取搬运Z2轴
            {
                CarrierOnePosData data = SimpleIoc.Default.GetInstance<ICarrierOneDataAccess>().GetPosData();
                Type type = typeof(CarrierOnePosData);
                PropertyInfo[] propertyInfos = type.GetProperties();
                int index = 0;
                if (axis.AxisNo == 1)//提取搬运Y轴
                {
                    index = 1;
                }
                if (axis.AxisNo == 2 || axis.AxisNo == 3)//提取搬运Z1轴
                {
                    index = 2;
                }
                foreach (var item in propertyInfos)
                {
                    var values = (double[])item.GetValue(data);
                    string posName = item.Name;
                    if (item.IsDefined(typeof(PosNameAttribute)))
                    {
                        var posNameAtt = item.GetCustomAttribute(typeof(PosNameAttribute)) as PosNameAttribute;
                        if (axis.AxisNo == 2)//提取搬运Z1轴
                        {
                            if (posNameAtt.Is_Z2_Axis)
                            {
                                continue;
                            }
                        }
                        if (axis.AxisNo == 3)//提取搬运Z2轴
                        {
                            if (!posNameAtt.Is_Z2_Axis)
                            {
                                continue;
                            }
                        }
                        posName = posNameAtt.PosName;

                    }
                    AxisPosInfos.Add(new AxisPosInfo() { AxisName = axis.AxisName, MemberName = item.Name, AxisNo = axis.AxisNo, PosName = posName, PosData = values[index] });
                }
            }

            //净化搬运X轴  9-12
            if (axis.AxisNo >= 9 && axis.AxisNo <= 12)
            {
                CarrierTwoPosData data = SimpleIoc.Default.GetInstance<ICarrierTwoDataAccess>().GetPosData();
                Type type = typeof(CarrierTwoPosData);
                PropertyInfo[] propertyInfos = type.GetProperties();
                int index = 0;
                if (axis.AxisNo == 10)//Y轴
                {
                    index = 1;
                }
                if (axis.AxisNo == 11 || axis.AxisNo == 12)//Z1轴 Z2轴
                {
                    index = 2;
                }
                foreach (var item in propertyInfos)
                {
                    var values = (double[])item.GetValue(data);
                    string posName = item.Name;
                    if (item.IsDefined(typeof(PosNameAttribute)))
                    {
                        var posNameAtt = item.GetCustomAttribute(typeof(PosNameAttribute)) as PosNameAttribute;
                        if (axis.AxisNo == 11)//Z1轴
                        {
                            if (posNameAtt.Is_Z2_Axis || posNameAtt.HaveNoneZ_Axis)
                            {
                                continue;
                            }
                        }
                        if (axis.AxisNo == 12)//Z2轴
                        {
                            if (!posNameAtt.Is_Z2_Axis || posNameAtt.HaveNoneZ_Axis)
                            {
                                continue;
                            }
                        }
                        posName = posNameAtt.PosName;

                    }
                    AxisPosInfos.Add(new AxisPosInfo() { AxisName = axis.AxisName, MemberName = item.Name, AxisNo = axis.AxisNo, PosName = posName, PosData = values[index] });
                }
            }

        }

        /// <summary>
        /// 示教当前位置
        /// </summary>
        /// <param name="obj"></param>
        protected void TechAxisPos(object obj)
        {
            var posInfo = obj as AxisPosInfo;
            if (posInfo == null)
            {
                return;
            }
            if (AxisNo == 0 || AxisNo == 9) //X轴
            {
                posInfo.PosData = XCurrentPos;
            }
            else if(AxisNo == 1 || AxisNo == 10)//Y轴
            {
                posInfo.PosData = YCurrentPos;
            }
            else if(AxisNo == 2 || AxisNo == 11)//Z1轴
            {
                posInfo.PosData = Z1CurrentPos;
            }
            else if(AxisNo == 3 || AxisNo == 12) //Z2轴
            {
                posInfo.PosData = Z2CurrentPos;
            }
            //移液器6 15无需位置数据
        }

        /// <summary>
        /// 选择位置数
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
        /// 移液器回零
        /// </summary>
        protected void SyringHome()
        {
            try
            {
                _motion.GohomeWithCheckDone(CarrierInfo.AxisF, 21, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 移液器吐液
        /// </summary>
        protected void Syring()
        {
            try
            {
                double offset = 0;
                _motion.P2pMoveWithCheckDone(CarrierInfo.AxisF, offset, 1, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 移液器吸取
        /// </summary>
        protected void Obsorb(object obj)
        {
            if (!double.TryParse(obj.ToString(),out double offset))
            {
                return;
            }
            try
            {
                _motion.P2pMoveWithCheckDone(CarrierInfo.AxisF, offset, 1, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 移液器退枪头
        /// </summary>
        protected void PutNeedle()
        {
            try
            {
                double offset = CarrierInfo.PutOffNeedle;
                _motion.P2pMoveWithCheckDone(CarrierInfo.AxisF, offset, 1, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 使能电爪
        /// </summary>
        protected void EnableClaw()
        {
            try
            {
                _clawInstance.Enable(CarrierInfo.ClawId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 禁用电爪
        /// </summary>
        protected void DisableClaw()
        {
            try
            {
                _clawInstance.Disable(CarrierInfo.ClawId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 打开电爪
        /// </summary>
        protected void OpenClaw(object openPos)
        {
            if (!byte.TryParse(openPos.ToString(), out byte pos))
            {
                return;
            }
            try
            {
                byte open = pos;
                _clawInstance.SendCommand(CarrierInfo.ClawId, open, 255, 128);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 关闭电爪
        /// </summary>
        protected void CloseClaw()
        {
            try
            {
                _clawInstance.SendCommand(CarrierInfo.ClawId, 255, 255, 128);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        #region Fuction EtherCat


        /// <summary>
        /// 停止运动
        /// </summary>
        protected void StopMove()
        {
            try
            {
                _motion.StopMove(AxisNo);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected void EmgStopMove()
        {
            try
            {
                _motion.Emg_stop(AxisNo);

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
                _motion.ResetAxisAlm(AxisNo);

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
                await RunCommandAsync(() => AbsMoveBusy, async () =>
                {
                    AbsMoveDone = await _motion.P2pMoveWithCheckDone(AxisNo, TargetPos, TargetVel, null);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        protected async void HomeMove()
        {
            try
            {
                switch (AxisNo)
                {
                    case 4:
                    case 8:
                    case 13: //Z相回零
                        await RunCommandOpAsync(() => Homing, () =>
                        {
                            return _motion.GohomeWithCheckDone(AxisNo, 33, null);
                        });
                        break;
                    case 6:
                    case 14:
                    case 15://原点回零
                        await RunCommandOpAsync(() => Homing, () =>
                        {
                            return _motion.GohomeWithCheckDone(AxisNo, 21, null);
                        });
                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        protected void EnableMotion()
        {
            try
            {
                _motion.ServoOn(AxisNo);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected void DisableMotion()
        {
            try
            {
                _motion.ServoOff(AxisNo);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected void JogF()
        {
            try
            {
                double jogVel = TargetVel > 100 ? 100 : TargetVel;
                _motion.JogMove(AxisNo, jogVel, 1);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected void JogR()
        {
            try
            {
                double jogVel = TargetVel > 100 ? 100 : TargetVel;
                _motion.JogMove(AxisNo, jogVel, 0);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected void StopJog()
        {
            try
            {
                _motion.JogStop(AxisNo);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected void ClearPosOffset()
        {
            try
            {
                _motion.AbsSysClear(AxisNo);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        #endregion

        public override void Cleanup()
        {
            _stopRefresh = true;
            base.Cleanup();
        }

    }
}
