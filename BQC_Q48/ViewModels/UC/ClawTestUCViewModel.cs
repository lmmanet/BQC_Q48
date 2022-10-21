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
using BQJX.Common.Common;

namespace Q_Platform.ViewModels.UC
{
    [AddINotifyPropertyChangedInterface]
    public class ClawTestUCViewModel :MyViewModelBase
    {
        #region Private Members

        private readonly IEPG26 _clawInstance;
        private readonly ILogger _logger;

        #endregion

        #region Properties
        
        public int ClawStatus { get; set; }

        public byte TargetPos { get; set; } = 128;
        public byte TargetVel { get; set; } = 128;
        public byte TargetTorque { get; set; } = 128;

        public List<ClawInfo> ListClawInfo { get; set; }

        [DoNotNotify]
        public int Id { get; set; } = 1;



        #endregion

        #region Commands

        /// <summary>
        /// 下拉选框
        /// </summary>
        public ICommand ComboxSelectChangedCommand { get; set; }

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
        /// 执行自定义命令
        /// </summary>
        public ICommand ExcuteUserCommand { get; set; }

        #endregion


        #region Constructors

        public ClawTestUCViewModel()
        {
            _clawInstance = SimpleIoc.Default.GetInstance<IEPG26>();
            _logger = SimpleIoc.Default.GetInstance<ILogger>();
            ListClawInfo = new List<ClawInfo>() 
            {
                new ClawInfo{ ClawId = 1 ,ClawName="提取手爪" },
                new ClawInfo{ ClawId = 2 ,ClawName="离心机手爪"},
                new ClawInfo{ ClawId = 3 ,ClawName="净化手爪"}
            
            };
            RegisterCommnand();
            _refreshTask = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var result = _clawInstance.GetClawStatus(Id).GetAwaiter().GetResult();
                        if (result != null)
                        {
                            ClawStatus = result.ClawStatus;
                        }
                       
                        if (_stopRefresh)
                        {
                            break;
                        }
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error($"_refreshTask err:{ex.Message}");
                    }

                }

            });
        }

        #endregion

        #region Private Methods

        private void RegisterCommnand()
        {
            ComboxSelectChangedCommand = new RelayCommand<object>(ComboxSelectChanged);
            EnableCommand = new RelayCommand(EnableClaw);
            DisableCommand = new RelayCommand(DisableClaw);
            OpenClawCommand = new RelayCommand(OpenClaw);
            CloseClawCommand = new RelayCommand(CloseClaw);
            ExcuteUserCommand = new RelayCommand(ExcuteCommand);
        }

        private void ExcuteCommand()
        {
            _clawInstance.SendCommand(Id, TargetPos, TargetVel, TargetTorque);
        }

        private void CloseClaw()
        {
            _clawInstance.SendCommand(Id, 255, 255, 128);
        }

        private void OpenClaw()
        {
            _clawInstance.SendCommand(Id, 80, 255, 128);
        }

        private void DisableClaw()
        {
            _clawInstance.Disable(Id);
        }

        private void EnableClaw()
        {
            _clawInstance.Enable(Id);
        }

        private void ComboxSelectChanged(object obj)
        {
            ClawInfo claw = obj as ClawInfo;
            if (claw == null)
            {
                return;
            }
            Id = claw.ClawId;
        }


        #endregion


        #region Public Methods

        public override void Cleanup()
        {
            _stopRefresh = true;
            base.Cleanup();
        }

        #endregion







    }
}
