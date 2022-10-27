using BQJX.Common.Common;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using PropertyChanged;
using Q_Platform.BLL;
using Q_Platform.Common;
using Q_Platform.DAL;
using Q_Platform.Models;
using Q_Platform.ViewModels.Base;
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

namespace Q_Platform.ViewModels.Module
{
    [AddINotifyPropertyChangedInterface]
    public class CapperOneUCViewModel : CapperViewModelBase
    {
        protected ushort _axisAddLiquid; //加液注射器
        protected ushort _port1; //加液口1
        protected ushort _port2; //加液口2
        protected ushort _port3; //加液口3
        protected ushort _port4; //加液口4
        protected ushort _port5; //加液口5
        protected ushort _port6; //加液口6
        protected ushort _port7; //加液口7
        protected ushort _port8; //加液口8
        private readonly ICapperOne _capper;

        #region Properties

        public double SyringTargetVel { get; set; } = 20;
        public double SyringTargetPos { get; set; }

        public bool cb1 { get; set; }
        public bool cb2 { get; set; }
        public bool cb3 { get; set; }
        public bool cb4 { get; set; }
        public bool cb5 { get; set; }
        public bool cb6 { get; set; }
        public bool cb7 { get; set; }
        public bool cb8 { get; set; }

        #endregion

        #region Commands

        public ICommand SyringAbsMoveCommand { get; set; }
        public ICommand SyringStopMoveCommand { get; set; }
        public ICommand SyringHomeMoveCommand { get; set; }
        public ICommand SyringObsorbCommand { get; set; }
        public ICommand SyringInjectCommand { get; set; }
        public ICommand SyringResetAxisAmlCommand { get; set; }
        public ICommand ForceCommand { get; set; }

        #endregion

        public CapperOneUCViewModel(ILS_Motion iLS_Motion, IIoDevice io, ICapperPosDataAccess dataAccess, ICapperOne capper) :base(iLS_Motion,io,dataAccess)
        {

            CapperInfo = SimpleIoc.Default.GetInstance<ICapperOne>().GetCapperInfo();
            SyringVisibility = Visibility.Visible;
            this._capper = capper;
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
                   catch (Exception)
                   {
                        //logger?.Error($"_refreshTask err:{ex.Message}");
                    }

               }
           });

            _axisAddLiquid = 4;
            _port1 = 24;
            _port2 = 25;
            _port3 = 26;
            _port4 = 27;
            _port5 = 28;
            _port6 = 29;
            _port7 = 30;
            _port8 = 31;

        }


        protected override void RegisterCommand()
        {
            SyringAbsMoveCommand = new RelayCommand(SyringAbsMove);
            SyringStopMoveCommand = new RelayCommand(SyringStopMove);
            SyringHomeMoveCommand = new RelayCommand(SyringHomeMove);
            SyringObsorbCommand = new RelayCommand(SyringObsorb);
            SyringInjectCommand = new RelayCommand(SyringInject);
            SyringResetAxisAmlCommand = new RelayCommand(SyringResetAxisAml);
            ForceCommand = new RelayCommand<object>(ForceValve);
            base.RegisterCommand();
        }

        protected override void SaveAxisPos()
        {
            base.SaveAxisPos();
            _capper.UpdatePosData();
        }

        protected override void RefreshIoStatus()
        {
            cb1 = _io.ReadBit_DO(_port1);
            cb2 = _io.ReadBit_DO(_port2);
            cb3 = _io.ReadBit_DO(_port3);
            cb4 = _io.ReadBit_DO(_port4);
            cb5 = _io.ReadBit_DO(_port5);
            cb6 = _io.ReadBit_DO(_port6);
            cb7 = _io.ReadBit_DO(_port7);
            cb8 = _io.ReadBit_DO(_port8);
            base.RefreshIoStatus();
        }

        private void SyringAbsMove()
        {
            RunCommandSync(() =>
            {
                _iLS_Motion.P2pMoveWithCheckDone(_axisAddLiquid,SyringTargetPos,SyringTargetVel,null);
            });
        }

        private void SyringStopMove()
        {
            RunCommandSync(() =>
            {
                _iLS_Motion.StopMove(_axisAddLiquid);
            });
        }

        private void SyringHomeMove()
        {
            RunCommandSync(() =>
            {
                _iLS_Motion.GoHomeWithCheckDone(_axisAddLiquid, null);
            });
        }

        private void SyringObsorb()
        {
            RunCommandSync(() =>
            {
                _iLS_Motion.P2pMoveWithCheckDone(_axisAddLiquid, 10,50,null);
            });
        }

        private void SyringInject()
        {
            RunCommandSync(() =>
            {
                _iLS_Motion.P2pMoveWithCheckDone(_axisAddLiquid, 0, 50, null);
            });
        }

        private void SyringResetAxisAml()
        {
            RunCommandSync(() =>
            {
                _iLS_Motion.ResetAxisAlm(_axisAddLiquid);
            });
        }

        private void ForceValve(object obj)
        {
            string str = obj.ToString();
            if (int.TryParse(str, out int port))
            {
                switch (port)
                {
                    case 1:
                        _io.WriteBit_DO(_port1, !cb1);
                        break;
                    case 2:
                        _io.WriteBit_DO(_port2, !cb2);
                        break;
                    case 3:
                        _io.WriteBit_DO(_port3, !cb3);
                        break;
                    case 4:
                        _io.WriteBit_DO(_port4, !cb4);
                        break;
                    case 5:
                        _io.WriteBit_DO(_port5, !cb5);
                        break;
                    case 6:
                        _io.WriteBit_DO(_port6, !cb6);
                        break;
                    case 7:
                        _io.WriteBit_DO(_port7, !cb7);
                        break;
                    case 8:
                        _io.WriteBit_DO(_port8, !cb8);
                        break;
                    default:
                        break;
                }
            }


        }
    }
}
