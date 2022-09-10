using BQJX.Common.Common;
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
using System.Collections.ObjectModel;
using Q_Platform.Models;
using Q_Platform.DAL;
using System.Reflection;

namespace Q_Platform.ViewModels.UC
{
    [AddINotifyPropertyChangedInterface]
    public class StepAxisTestUCViewModel : MyViewModelBase
    {
        #region Private Members

        private readonly ILS_Motion iLS_Motion;
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

        public double TargetVel { get; set; }

        public bool AbsMoveBusy { get; set; }

        public bool AbsMoveDone { get; set; }

        public bool RelativeMoveBusy { get; set; }

        public bool RelativeMoveDone { get; set; }

        [DoNotNotify]
        public List<StepAxisEleGear> ListAxisInfo { get; set; }

        [DoNotNotify]
        public ushort AxisNo { get; set; }


        /// <summary>
        /// 轴点位数据信息
        /// </summary>
        public ObservableCollection<AxisPosInfo> AxisPosInfos { get; set; }


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
        /// 编码器回读数据偏移清零
        /// </summary>
        public ICommand ClearPosOffsetCommand { get; set; }

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


        #endregion

        #region Constructors

        public StepAxisTestUCViewModel()
        {
            iLS_Motion = SimpleIoc.Default.GetInstance<ILS_Motion>();
            AxisNo = 1;
            ListAxisInfo = iLS_Motion?.GetAxisInfos();
            _refreshTask = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        MotionStatus = iLS_Motion.GetMotionIoStatus(AxisNo).GetAwaiter().GetResult();
                        CurrentPos = iLS_Motion.GetCurrentPos(AxisNo).GetAwaiter().GetResult();
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
                        _logger?.Error($"_refreshTask err:{ex.Message}");
                    }

                }

            });
            RegisterCommand();
        }

        #endregion

        #region Private Methods

        private void RegisterCommand()
        {
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
   
            ClearPosOffsetCommand = new RelayCommand(ClearPosOffset);
            GetCapperOnCommand = new RelayCommand(GetCapperOn);
            PutCapperOffCommand = new RelayCommand(PutCapperOff);

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

            if (AxisNo == 1) //加盐Y轴  1
            {
                ushort id = 1;
                result = SimpleIoc.Default.GetInstance<IAddSolidPosDataAccess>().UpdatePosDataByAxisPosInfo(id, list);
            }

            #endregion

            //拧盖Y Z轴
            if (AxisNo == 5 || AxisNo == 9 || AxisNo == 16 || AxisNo == 19 || AxisNo == 22 //Y 轴
                || AxisNo == 12 || AxisNo == 13 || AxisNo == 29 || AxisNo == 28 || AxisNo == 27)  //拧盖Z轴
            {
                ushort id = 1;
                if (AxisNo == 9 || AxisNo == 13)
                {
                    id = 2;
                }
                if (AxisNo == 16 || AxisNo == 29)
                {
                    id = 3;
                }
                if (AxisNo == 19 || AxisNo == 28)
                {
                    id = 4;
                }
                if (AxisNo == 22 || AxisNo == 27)
                {
                    id = 5;
                }
                result = SimpleIoc.Default.GetInstance<ICapperPosDataAccess>().UpdatePosDataByAxisPosInfo(id, list);

            }

            //涡旋 Y轴  8
            if (AxisNo == 8)
            {
                ushort id = 1;
                result = SimpleIoc.Default.GetInstance<IVortexPosDataAccess>().UpdatePosDataByAxisPosInfo(id, list);

            }

            //浓缩Y轴 25
            if (AxisNo == 25)
            {
                ushort id = 1;
                result = SimpleIoc.Default.GetInstance<IConcentrationPosDataAccess>().UpdatePosDataByAxisPosInfo(id, list);
            }

            //离心X轴 14
            if (AxisNo == 14)
            {
                ushort id = 1;
                result = SimpleIoc.Default.GetInstance<ICentrifugalCarrierPosDataAccess>().UpdatePosDataByAxisPosInfo(id, list);
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
        
        
        }

        private void AxisPosInfoChanged(object obj)
        {
            var axisPosInfo = obj as AxisPosInfo;
            if (axisPosInfo != null)
            {
                TargetPos = axisPosInfo.PosData;
            }
        }

        private void PutCapperOff()
        {
            iLS_Motion.RelativeMoveWithCheckDone(AxisNo, -2, 50, null);
        }

        private void GetCapperOn()
        {
            if (AxisNo == 17 || AxisNo == 18 )  //拧盖3
            {
                iLS_Motion.TorqueMoveWithCheckDone(AxisNo, 30, 30,0, null);
            }
            else if(AxisNo == 20 || AxisNo == 21 ) //拧盖4
            {
                iLS_Motion.TorqueMoveWithCheckDone(AxisNo, 30, 30,0, null);
            }
            else if(AxisNo == 23 || AxisNo == 24 ) //拧盖5
            {
                iLS_Motion.TorqueMoveWithCheckDone(AxisNo, 30, 30,0, null); 
            }
            else
            {
                iLS_Motion.TorqueMoveWithCheckDone(AxisNo, 50, 50,0, null);
            }
            
        }

        private void ComboxSelectChanged(object obj)
        {
            StepAxisEleGear stepAxisEleGear = obj as StepAxisEleGear;
            if (stepAxisEleGear!=null)
            {
                AxisNo = stepAxisEleGear.SlaveId;
 
                //更新轴点位信息
                AxisPosInfos = new ObservableCollection<AxisPosInfo>();
                GetAxisPosInfo(stepAxisEleGear);
            }

            

        
        }

        /// <summary>
        /// 获取轴点位信息
        /// </summary>
        /// <param name="axis"></param>
        private void GetAxisPosInfo(StepAxisEleGear axis)
        {
            #region 获取CarrierOnePosData

            if (axis.SlaveId == 1) //加盐Y轴  1
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
                    AxisPosInfos.Add(new AxisPosInfo() { AxisName = axis.AxisName, MemberName = item.Name, AxisNo = axis.SlaveId, PosName = posName, PosData = values[1] });
                }
            }

            #endregion

            //拧盖Y Z轴
            if (axis.SlaveId == 5 || axis.SlaveId == 9 || axis.SlaveId == 16 || axis.SlaveId == 19 || axis.SlaveId == 22 //Y 轴
                || axis.SlaveId == 12 || axis.SlaveId == 13 || axis.SlaveId == 29 || axis.SlaveId == 28 || axis.SlaveId == 27)  //拧盖Z轴
            {
                int slave = 1;
                if (axis.SlaveId == 9 || axis.SlaveId == 13)
                {
                    slave = 2;
                }
                if (axis.SlaveId == 16 || axis.SlaveId == 29)
                {
                    slave = 3;
                }
                if (axis.SlaveId == 19 || axis.SlaveId == 28)
                {
                    slave = 4;
                }
                if (axis.SlaveId == 22 || axis.SlaveId == 27)
                {
                    slave = 5;
                }
                CapperPosData data = SimpleIoc.Default.GetInstance<ICapperPosDataAccess>().GetCapperPosData(slave);
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

            //涡旋 Y轴  8
            if (axis.SlaveId == 8)
            {
                VortexPosData data = SimpleIoc.Default.GetInstance<IVortexPosDataAccess>().GetPosData();
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

            //浓缩Y轴 25
            if (axis.SlaveId ==25)
            {
                ConcentrationPosData data = SimpleIoc.Default.GetInstance<IConcentrationPosDataAccess>().GetPosData();
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
                    AxisPosInfos.Add(new AxisPosInfo() { AxisName = axis.AxisName, MemberName = item.Name, AxisNo = axis.SlaveId, PosName = posName, PosData = values});
                }
            }

            //离心X轴 14
            if (axis.SlaveId == 14)
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
                        posName = posNameAtt.PosName;

                    }
                    AxisPosInfos.Add(new AxisPosInfo() { AxisName = axis.AxisName, MemberName = item.Name, AxisNo = axis.SlaveId, PosName = posName, PosData = values });
                }
            }


        }

        private void StopMove()
        {
            iLS_Motion.StopMove(AxisNo);
        }

        private void EmgStopMove()
        {
            iLS_Motion.Emg_stop(AxisNo);
        }

        private void ResetAxisAml()
        {
            iLS_Motion.ResetAxisAlm(AxisNo);
        }

        private async void AbsMove()
        {
            await RunCommandAsync(() => AbsMoveBusy, async () =>
            {
                AbsMoveDone = await iLS_Motion.P2pMoveWithCheckDone(AxisNo, TargetPos, TargetVel, null);
            });
        }

        private async void RelativeMove()
        {
            await RunCommandAsync(() => RelativeMoveBusy, async () =>
            {
                RelativeMoveDone = await iLS_Motion.RelativeMoveWithCheckDone(AxisNo, TargetPos, TargetVel, null);
            });

        }

        private void VelocityMove()
        {
            iLS_Motion.VelocityMove(AxisNo, TargetVel);
        }

        private void HomeMove()
        {
            if (AxisNo == 12 || AxisNo == 13 || AxisNo == 27 || AxisNo == 28 || AxisNo == 29)
            {//12 13 27 28 29
                iLS_Motion.DM2C_GoHomeWithCheckDone(AxisNo, null);
            }
            else
            {

                iLS_Motion.GoHomeWithCheckDone(AxisNo, null);
            }

        }

        private void EnableMotion()
        {
            iLS_Motion.ServoOn(AxisNo);
        }

        private void DisableMotion()
        {
            iLS_Motion.ServeOff(AxisNo);
        }

        private void JogF()
        {
            double jogVel = TargetVel > 100 ? 100 : TargetVel;
            iLS_Motion.JogF(AxisNo);
        }

        private void JogR()
        {
            double jogVel = TargetVel > 100 ? 100 : TargetVel;
            iLS_Motion.JogR(AxisNo);
        }

        private void StopJog()
        {
            iLS_Motion.StopMove(AxisNo);
        }



        private void ClearPosOffset()
        {
           iLS_Motion.GoHomeWithCheckDone(AxisNo,32,null);
        }


        public override void Cleanup()
        {
            base.Cleanup();
            _stopRefresh = true;

        }




    }
}
