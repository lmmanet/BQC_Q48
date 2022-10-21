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
    public class CapperThreeUCViewModel : CapperViewModelBase
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

        private readonly ICapperThree _capper;

        #region Properties

        public double SyringTargetVel { get; set; } = 20;
        public double SyringTargetPos { get; set; }

        #endregion

        #region Commands

        public ICommand SyringAbsMoveCommand { get; set; }
        public ICommand SyringStopMoveCommand { get; set; }
        public ICommand SyringHomeMoveCommand { get; set; }
        public ICommand SyringObsorbCommand { get; set; }
        public ICommand SyringInjectCommand { get; set; }
        public ICommand SyringResetAxisAmlCommand { get; set; }


        #endregion

        #region Construtors

        public CapperThreeUCViewModel(ILS_Motion iLS_Motion, IIoDevice io, ICapperPosDataAccess dataAccess, ICapperThree capper) :base(iLS_Motion,io,dataAccess)
        {
            SyringVisibility = Visibility.Visible;

            CapperInfo = SimpleIoc.Default.GetInstance<ICapperThree>().GetCapperInfo();
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

            _axisAddLiquid = 26;
            _port1 = 58;    //Q2.2
            _port2 = 59;
            _port3 = 60;
            _port4 = 61;
            _port5 = 62;
            _port6 = 63;
            _port7 = 64;
            _port8 = 65;

        }

        #endregion

        protected override void RegisterCommand()
        {
            SyringAbsMoveCommand = new RelayCommand(SyringAbsMove);
            SyringStopMoveCommand = new RelayCommand(SyringStopMove);
            SyringHomeMoveCommand = new RelayCommand(SyringHomeMove);
            SyringObsorbCommand = new RelayCommand(SyringObsorb);
            SyringInjectCommand = new RelayCommand(SyringInject);
            SyringResetAxisAmlCommand = new RelayCommand(SyringResetAxisAml);

            base.RegisterCommand();
        }

        protected override void SaveAxisPos()
        {
            base.SaveAxisPos();
            _capper.UpdatePosData();
        }

        private void SyringAbsMove()
        {
            RunCommandSync(() =>
            {
                _iLS_Motion.P2pMoveWithCheckDone(_axisAddLiquid, SyringTargetPos, SyringTargetVel, null);
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
                _iLS_Motion.P2pMoveWithCheckDone(_axisAddLiquid, 10, 50, null);
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



    }
}
