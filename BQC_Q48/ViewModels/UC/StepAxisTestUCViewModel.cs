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
        }

        private void PutCapperOff()
        {
            iLS_Motion.RelativeMoveWithCheckDone(AxisNo, -2, 50, null);
        }

        private void GetCapperOn()
        {
            if (AxisNo == 17 || AxisNo == 18 )  //拧盖3
            {
                iLS_Motion.TorqueMoveWithCheckDone(AxisNo, 30, 30, null);
            }
            else if(AxisNo == 20 || AxisNo == 21 ) //拧盖4
            {
                iLS_Motion.TorqueMoveWithCheckDone(AxisNo, 30, 30, null);
            }
            else if(AxisNo == 23 || AxisNo == 24 ) //拧盖5
            {
                iLS_Motion.TorqueMoveWithCheckDone(AxisNo, 30, 30, null); 
            }
            else
            {
                iLS_Motion.TorqueMoveWithCheckDone(AxisNo, 50, 50, null);
            }
            
        }

        private void ComboxSelectChanged(object obj)
        {
            StepAxisEleGear stepAxisEleGear = obj as StepAxisEleGear;
            if (stepAxisEleGear!=null)
            {
               AxisNo = stepAxisEleGear.SlaveId;
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


        #endregion

        public override void Cleanup()
        {
            base.Cleanup();
            _stopRefresh = true;

        }




    }
}
