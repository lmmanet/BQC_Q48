using BQJX.Core.Common;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using PropertyChanged;
using System.Windows;
using Q_Platform.Models;
using Q_Platform.DAL;
using BQJX.Common.Common;
using System.Reflection;
using System.Collections.ObjectModel;

namespace Q_Platform.ViewModels.UC
{
    [AddINotifyPropertyChangedInterface]
    public class AxisTestUCViewModel : MyViewModelBase
    {
        #region Private Members

        private readonly ICardBase _card;
        private readonly IEtherCATMotion _motion;
        private readonly ILogger _logger;
        private Task _refreshTask;
        private bool _stopRefresh;
        private bool _refresh;

        #endregion

        #region Properties

        public int MotionStatus { get; set; }

        public double CurrentPos { get; set; }

        public double CurrentVel { get; set; }

        public double TargetPos { get; set; }

        public double TargetVel { get; set; } = 5;

        public bool AbsMoveBusy { get; set; }

        public bool AbsMoveDone { get; set; }

        public bool RelativeMoveBusy { get; set; }

        public bool RelativeMoveDone { get; set; }

        public bool ResetFieldBusBusy { get; set; }

        public bool ResetFieldBusDone { get; set; }

        /// <summary>
        /// 速度运动按钮使能
        /// </summary>

        public bool EnableVelMove { get; set; } = false;

        /// <summary>
        /// 回零按钮使能
        /// </summary>
        public Visibility HomeMoveVisibility { get; set; } = Visibility.Hidden;

        /// <summary>
        /// 回零中
        /// </summary>
        public bool Homing { get; set; } = true;

        /// <summary>
        /// 轴点位数据信息
        /// </summary>
        public ObservableCollection<AxisPosInfo> AxisPosInfos { get; set; }

        [DoNotNotify]
        public List<AxisEleGear> ListAxisInfo { get; set; }

        [DoNotNotify]
        public ushort AxisNo { get; set; }



        #endregion

        #region Commands

        /// <summary>
        /// 热复位
        /// </summary>
        public ICommand ResetFieldBusCommand { get; set; }

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
        /// 相对运动
        /// </summary>
        public ICommand RelativeMoveCommand { get; set; }

        /// <summary>
        /// 速度运动
        /// </summary>
        public ICommand VelocityMoveCommand { get; set; }

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
        /// 配置限位
        /// </summary>
        public ICommand ConfiguraLimitCommand { get; set; }

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


        #endregion

        #region Constructors

        public AxisTestUCViewModel(ICardBase card,IEtherCATMotion motion,ILogger logger)
        {
            _logger = logger;
            _card = card;
            _motion = motion;
            
            RegisterCommand();
            ListAxisInfo = _motion?.GetAxisInfos();

            _refreshTask = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var motionIo = _motion.GetMotionIoStatus(AxisNo);
                        var status = _motion.GetMotionStatus(AxisNo);
                        MotionStatus =(int) (motionIo + (status == 4 ? 128 : 0));
                        
                        CurrentPos = _motion.GetCurrentPos(AxisNo);
                        CurrentVel = _motion.GetCurrentVel(AxisNo);
                        if (_stopRefresh)
                        {
                            break;
                        }
                        while (_refresh)
                        {
                            Thread.Sleep(1000);
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


        #region Private Methods

        private void RegisterCommand()
        {
            ResetFieldBusCommand = new RelayCommand(ResetFieldBus);
            StopMoveCommand = new RelayCommand(StopMove);
            EmgStopMoveCommand = new RelayCommand(EmgStopMove);
            ResetAxisAmlCommand = new RelayCommand(ResetAxisAml);
            AbsMoveCommand = new RelayCommand(AbsMove);
            RelativeMoveCommand = new RelayCommand(RelativeMove);
            VelocityMoveCommand = new RelayCommand(VelocityMove);
            HomeMoveCommand = new RelayCommand(HomeMove);
            EnableMotionCommand = new RelayCommand(EnableMotion);
            DisableMotionCommand = new RelayCommand(DisableMotion);
            JogFCommand = new RelayCommand(JogF);
            JogRCommand = new RelayCommand(JogR);
            StopJogCommand = new RelayCommand(StopJog);
            ConfiguraLimitCommand = new RelayCommand(ConfiguraLimit);
            ClearPosOffsetCommand = new RelayCommand(ClearPosOffset);
            ComboxSelectChangedCommand = new RelayCommand<object>(ComboxSelectChanged);
            AxisPosInfoChangedCommand = new RelayCommand<object>(AxisPosInfoChanged);

            TechCommand = new RelayCommand<object>(UpdateAxisPos);
            SavePosDataCommand = new RelayCommand(SaveAxisPos);
        }

        /// <summary>
        /// 保存点位数据
        /// </summary>
        private void SaveAxisPos()
        {
            var list = AxisPosInfos.ToList();
            bool result = false;

            if (AxisNo <= 3)
            {
                ushort id = AxisNo;
                if (AxisNo == 3)
                {
                    id = 2;
                }
                result = SimpleIoc.Default.GetInstance<ICarrierOneDataAccess>().UpdatePosDataByAxisPosInfo(id, list);

            }
            if (AxisNo == 5)
            {
                ushort id = 0;
                result = SimpleIoc.Default.GetInstance<IAddSolidPosDataAccess>().UpdatePosDataByAxisPosInfo(id, list);
            }
            if (AxisNo == 7)
            {
                ushort id = 0;
                result = SimpleIoc.Default.GetInstance<ICentrifugalCarrierPosDataAccess>().UpdatePosDataByAxisPosInfo(id, list);
            }
            if (AxisNo >= 9 && AxisNo <= 12)
            {
                ushort id = 0;
                if (AxisNo == 10)
                {
                    id = 1;
                }
                if (AxisNo == 11)
                {
                    id = 2;
                }
                if (AxisNo == 12)
                {
                    id = 2;
                }
                result = SimpleIoc.Default.GetInstance<ICarrierTwoDataAccess>().UpdatePosDataByAxisPosInfo(id, list);
            }

        }

        private void ComboxSelectChanged(object obj)
        {
            var axis = obj as AxisEleGear;
            if (axis == null)
            {
                return;
            }

            AxisNo = axis.AxisNo;
            if (axis.AxisName == "离心机" || axis.AxisName == "提取振荡" || axis.AxisName == "净化振荡")
            {
                EnableVelMove = true;
            }
            else
            {
                EnableVelMove = false;
            }

            if (axis.AxisName == "离心机" || axis.AxisName == "提取移液器" || axis.AxisName == "净化移液器" || axis.AxisName == "净化注射器" || axis.AxisName == "提取振荡" || axis.AxisName == "净化振荡")
            {
                HomeMoveVisibility = Visibility.Visible;
            }
            else
            {
                HomeMoveVisibility = Visibility.Hidden;
            }

            //更新轴点位信息
            AxisPosInfos = new ObservableCollection<AxisPosInfo>();
            GetAxisPosInfo(axis);

        }


        /// <summary>
        /// 获取轴点位信息
        /// </summary>
        /// <param name="axis"></param>
        private void GetAxisPosInfo(AxisEleGear axis)
        {

            #region 获取CarrierOnePosData

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
                    AxisPosInfos.Add(new AxisPosInfo() { AxisName = axis.AxisName,MemberName = item.Name, AxisNo = axis.AxisNo, PosName = posName, PosData = values[index] });
                }
            }

            #endregion

            //加盐Y轴  5
            if (axis.AxisNo == 5)
            {
                AddSolidPosData data = SimpleIoc.Default.GetInstance<IAddSolidPosDataAccess>().GetPosData();
                Type type = typeof(AddSolidPosData);
                PropertyInfo[] propertyInfos = type.GetProperties();

                foreach (var item in propertyInfos)
                {
                    var values = (double[])item.GetValue(data);
                    string posName = item.Name;
                    if (item.IsDefined(typeof(PosNameAttribute)))
                    {
                        var posNameAtt = item.GetCustomAttribute(typeof(PosNameAttribute)) as PosNameAttribute;
                        posName = posNameAtt.PosName;

                    }
                    AxisPosInfos.Add(new AxisPosInfo() { AxisName = axis.AxisName, MemberName = item.Name, AxisNo = axis.AxisNo, PosName = posName, PosData = values[0] });
                }

            }

            //离心Z轴  7
            if (axis.AxisNo == 7)
            {
                CentrifugalCarrierPosData  data = SimpleIoc.Default.GetInstance<ICentrifugalCarrierPosDataAccess>().GetPosData();
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
        /// 更新点位数据
        /// </summary>
        /// <returns></returns>
        private void UpdateAxisPos(object obj)
        {
            var posInfo = obj as AxisPosInfo;
            if (posInfo == null)
            {
                //return false;
                return;
            }
            bool result = false;

            if (AxisNo <= 3)
            {
                ushort id = AxisNo;
                result = SimpleIoc.Default.GetInstance<ICarrierOneDataAccess>().UpdatePosDataByAxisPosInfo(id, posInfo); 

            }
            if (AxisNo == 5)
            {
                ushort id = 0;
                result = SimpleIoc.Default.GetInstance<IAddSolidPosDataAccess>().UpdatePosDataByAxisPosInfo(id, posInfo);
            }
            if (AxisNo == 7)
            {
                ushort id = 0;
                result = SimpleIoc.Default.GetInstance<ICentrifugalCarrierPosDataAccess>().UpdatePosDataByAxisPosInfo(id, posInfo);
            }
            if (AxisNo >= 9 && AxisNo <= 12)
            {
                ushort id = 0;
                if (AxisNo == 10)
                {
                    id = 1;
                }
                if (AxisNo == 11)
                {
                    id = 2;
                }
                if (AxisNo == 12)
                {
                    id = 3;
                }
                result = SimpleIoc.Default.GetInstance<ICarrierTwoDataAccess>().UpdatePosDataByAxisPosInfo(id, posInfo);
            }
           // return result;
        }


        private void AxisPosInfoChanged(object obj)
        {
            var axisPosInfo = obj as AxisPosInfo;
            if (axisPosInfo != null)
            {
                TargetPos = axisPosInfo.PosData;
            }
        }

        private async void ResetFieldBus()
        {
            //停止状态刷新
            _refresh = true;
            Thread.Sleep(500);
            //开始复位
            await RunCommandAsync(() => RelativeMoveBusy, async () =>
            {
                ResetFieldBusDone = await _card.ResetFieldBus(0);
            });
            //弹出窗口提示正在复位...

            //启动刷新
            _refresh = false;
        }

        private void StopMove()
        {
            _motion.StopMove(AxisNo);
        }

        private void EmgStopMove()
        {
            _motion.Emg_stop(AxisNo);
        }

        private void ResetAxisAml()
        {
            _motion.ResetAxisAlm(AxisNo);
        }

        private async void AbsMove()
        {
            await RunCommandAsync(()=> AbsMoveBusy, async()=>
            {
                AbsMoveDone = await _motion.P2pMoveWithCheckDone(AxisNo, TargetPos, TargetVel, null);
            });
        }

        private async void RelativeMove()
        {
            await RunCommandAsync(() => RelativeMoveBusy, async () =>
            {
                RelativeMoveDone = await _motion.RelativeMoveWithCheckDone(AxisNo, TargetPos, TargetVel, null);
            });

        }

        private void VelocityMove()
        {
            _motion.VelocityMove(AxisNo, TargetVel, 1);
        }

        private async void HomeMove()
        {
            switch (AxisNo)
            {
                case 4:
                case 8:
                case 13: //Z相回零
                    await RunCommandOpAsync(()=>Homing,()=>
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

        private void EnableMotion()
        {
            _motion.ServoOn(AxisNo);
        }

        private void DisableMotion()
        {
            _motion.ServoOff(AxisNo);
        }

        private void JogF()
        {
            double jogVel = TargetVel > 100 ? 100: TargetVel;
            _motion.JogMove(AxisNo, jogVel,1);
        }

        private void JogR()
        {
            double jogVel = TargetVel > 100 ? 100 : TargetVel;
            _motion.JogMove(AxisNo, jogVel, 0);
        }

        private void StopJog()
        {
            _motion.JogStop(AxisNo);
        }

        private void ConfiguraLimit()
        {
            //_motion.ConfigSoftLimit(AxisNo, 1, 0, 50);
        }

        private void ClearPosOffset()
        {
            _motion.AbsSysClear(AxisNo);
            
        }


        #endregion

        public override void Cleanup()
        {
            base.Cleanup();
            _stopRefresh = true;

        }



    }

}
