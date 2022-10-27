using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Q_Platform.ViewModels.Base
{
    public abstract class VibrationViewModelBase : MyViewModelBase
    {
        protected readonly IEtherCATMotion _motion;
        protected readonly ILogger _logger;
        protected readonly IIoDevice _io;

        protected ushort _axis;
        protected ushort _holding;
        protected ushort _holdingOpenSensor; //原位
        protected ushort _holdingCloseSensor; //到位

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

        public double WCurrentVel { get; set; }

        public double TargetVel { get; set; } = 200;


        #endregion

        #region Commands

        public ICommand VelMoveCommand { get; set; }
        public ICommand StopMoveCommand { get; set; }
        public ICommand EnableMotionCommand { get; set; }
        public ICommand DisableMotionCommand { get; set; }
        public ICommand ResetAxisAmlCommand { get; set; }
        public ICommand HomeMoveCommand { get; set; }
        public ICommand ExtendCommand { get; set; }
        public ICommand RetrieveCommand { get; set; }

        #endregion

        public VibrationViewModelBase(IEtherCATMotion motion,IIoDevice io, ILogger logger)
        {
            this._motion = motion;
            this._logger = logger;
            this._io = io;
            RegisterCommand();
        }


        protected void RefreshStatus()
        {
            WCurrentVel = _motion.GetCurrentVel(_axis) *60;
            MotionIoStatus = _motion.GetMotionIoStatus(_axis);
            if (_motion.GetMotionStatus(_axis) == 4)
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
            VelMoveCommand = new RelayCommand(VelMove);
            StopMoveCommand = new RelayCommand(StopMove);
            EnableMotionCommand = new RelayCommand(EnableMotion);
            DisableMotionCommand = new RelayCommand(DisableMotion);
            ResetAxisAmlCommand = new RelayCommand(ResetAxisAml);
            HomeMoveCommand = new RelayCommand(HomeMove);
            ExtendCommand = new RelayCommand(Extend);
            RetrieveCommand = new RelayCommand(Retrieve);
        }

        private void VelMove()
        {
            RunCommandSync(() =>
            {
                _motion.VelocityMove(_axis, TargetVel/60, 1);
            });
        }

        private void StopMove()
        {
            RunCommandSync(() =>
            {
                _motion.StopMove(_axis);
            });
        }

        private void EnableMotion()
        {
            RunCommandSync(() =>
            {
                _motion.ServoOn(_axis);
            });
        }

        private void DisableMotion()
        {
            RunCommandSync(() =>
            {
                _motion.ServoOff(_axis);
            });
        }

        private void ResetAxisAml()
        {
            RunCommandSync(() =>
            {
                _motion.ResetAxisAlm(_axis);
            });
        }

        private void HomeMove()
        {
            RunCommandAsync(() =>
            {
                _io.WriteBit_DO(_holding, false);
                Thread.Sleep(1000);
                _motion.GohomeWithCheckDone(_axis, 33, null);
            });
        }

        private void Extend()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_holding, true);
            });
        }

        private void Retrieve()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_holding, false);
            });
        }
    }

}
