using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Q_Platform.ViewModels.Module
{
    public class CentrifugalViewModel : MyViewModelBase
    {

        protected readonly IIoDevice _io;
        protected readonly IEtherCATMotion _motion;
        protected readonly ILogger _logger;
        private readonly ushort _axis = 8;
        private ushort _homeMode = 33; //离心机回零模式

        private ushort _shadowOpen = 0; //离心机门打开控制
        private ushort _shadowClose = 1; //离心机门关闭控制
        private ushort _shadowOpenSensor = 0; //离心机门打开感应
        private ushort _shadowCloseSensor = 1; //离心机门关闭感应

        #region Properties


        public string AlarmMessage { get; set; }

        public Visibility ShowAlarmMsg { get; set; }

        public double WCurrentVel { get; set; }

        public double TargetVel { get; set; }


        #endregion

        #region Commands

        public ICommand VelMoveCommand { get; set; }
        public ICommand StopMoveCommand { get; set; }
        public ICommand EnableMotionCommand { get; set; }
        public ICommand DisableMotionCommand { get; set; }
        public ICommand ResetAxisAmlCommand { get; set; }
        public ICommand HomeMove1Command { get; set; }
        public ICommand HomeMove2Command { get; set; }
        public ICommand HomeMove3Command { get; set; }
        public ICommand HomeMove4Command { get; set; }

        public ICommand OpenShadowCommand { get; set; }
        public ICommand CloseShadowCommand { get; set; }

        #endregion
        public CentrifugalViewModel(IEtherCATMotion motion, IIoDevice io, ILogger logger)
        {
            this._motion = motion;
            this._io = io;
            this._logger = logger;

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
            WCurrentVel = _motion.GetCurrentVel(_axis)*60;
        }

        private void RegisterCommand()
        {
            VelMoveCommand = new RelayCommand(VelMove);
            StopMoveCommand = new RelayCommand(StopMove);
            EnableMotionCommand = new RelayCommand(EnableMotion);
            DisableMotionCommand = new RelayCommand(DisableMotion);
            ResetAxisAmlCommand = new RelayCommand(ResetAxisAml);
            HomeMove1Command = new RelayCommand(HomeMove1);
            HomeMove2Command = new RelayCommand(HomeMove2);
            HomeMove3Command = new RelayCommand(HomeMove3);
            HomeMove4Command = new RelayCommand(HomeMove4);
            OpenShadowCommand = new RelayCommand(OpenShadow);
            CloseShadowCommand = new RelayCommand(CloseShadow);
        }

        private void VelMove()
        {
            RunCommandSync(() =>
            {
                _motion.VelocityMove(_axis,TargetVel/60,1);
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

        private void HomeMove1()
        {
            RunCommandAsync(() =>
            {
                _motion.GohomeWithCheckDone(_axis, _homeMode, 0.05, null);
            });
        }

        private void HomeMove2()
        {
            RunCommandAsync(() =>
            {
                _motion.GohomeWithCheckDone(_axis, _homeMode, 0.3, null);
            });
        }

        private void HomeMove3()
        {
            RunCommandAsync(() =>
            {
                _motion.GohomeWithCheckDone(_axis, _homeMode, 0.55, null);
            });
        }

        private void HomeMove4()
        {
            RunCommandAsync(() =>
            {
                _motion.GohomeWithCheckDone(_axis, _homeMode, 0.8, null);
            });
        }

        private void OpenShadow()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_shadowClose, false); 
                _io.WriteBit_DO(_shadowOpen, true);
            });
        }

        private void CloseShadow()
        {
            RunCommandSync(() =>
            {
                _io.WriteBit_DO(_shadowClose, true);
                _io.WriteBit_DO(_shadowOpen, false);
            });
        }
    }


}
