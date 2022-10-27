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

        public int MotionStatus { get; set; }

        public uint MotionIoStatus { get; set; }

        public string AlarmMessage { get; set; }

        public Visibility ShowAlarmMsg { get; set; }

        public double WCurrentVel { get; set; }

        public double TargetVel { get; set; }

        /// <summary>
        /// 离心机门开关状态 0： 开 1：关 2：中间位
        /// </summary>
        public int ShadowStatus { get; set; }

        /// <summary>
        /// 在工位1
        /// </summary>
        public bool InStation1 { get; set; }
        public bool InStation2 { get; set; }
        public bool InStation3 { get; set; }
        public bool InStation4 { get; set; }


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
            MotionIoStatus = _motion.GetMotionIoStatus(4);
            if (_motion.GetMotionStatus(_axis) == 4)
            {
                MotionStatus = 1;
            }
            else
            {
                MotionStatus = 0;
            }
            WCurrentVel = _motion.GetCurrentVel(_axis) * 60;
            if (_io.ReadBit_DI(_shadowOpenSensor) && !_io.ReadBit_DI(_shadowCloseSensor))
            {
                ShadowStatus = 0;
            }
            else if (!_io.ReadBit_DI(_shadowOpenSensor) && _io.ReadBit_DI(_shadowCloseSensor))
            {
                ShadowStatus = 1;
            }
            else
            {
                ShadowStatus = 2;
            }

        }

        private void RegisterCommand()
        {
            VelMoveCommand = new RelayCommand(VelMove);
            StopMoveCommand = new RelayCommand(StopMove);
            EnableMotionCommand = new RelayCommand(EnableMotion);
            DisableMotionCommand = new RelayCommand(DisableMotion);
            ResetAxisAmlCommand = new RelayCommand(ResetAxisAml);
            HomeMove1Command = new RelayCommand(async()=> await HomeMove1());
            HomeMove2Command = new RelayCommand(async () => await HomeMove2());
            HomeMove3Command = new RelayCommand(async () => await HomeMove3());
            HomeMove4Command = new RelayCommand(async () => await HomeMove4());
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

        private async Task HomeMove1()
        {
            await RunCommandAsync(() => InStation1, () =>
            {
                return _motion.GohomeWithCheckDone(_axis, _homeMode, 0.055, null);
            });
        }

        private async Task HomeMove2()
        {
            await RunCommandAsync(() => InStation2, () =>
            {
                return _motion.GohomeWithCheckDone(_axis, _homeMode, 0.305, null);
            });
        }

        private async Task HomeMove3()
        {
            await RunCommandAsync(() => InStation3, () =>
            {
                return _motion.GohomeWithCheckDone(_axis, _homeMode, 0.555, null);
            });
        }

        private async Task HomeMove4()
        {
            await RunCommandAsync(() => InStation4, () =>
            {
                return _motion.GohomeWithCheckDone(_axis, _homeMode, 0.805, null);
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
