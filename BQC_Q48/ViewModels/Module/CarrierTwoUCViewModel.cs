using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using Q_Platform.BLL;
using Q_Platform.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Q_Platform.ViewModels.Module
{
    public class CarrierTwoUCViewModel : CarrierOneUCViewModel
    {

        private readonly IIoDevice _io;

        #region Commands

        /// <summary>
        /// 加标回零
        /// </summary>
        public ICommand Syring2HomeCommand { get; set; }

        /// <summary>
        /// 注射器注射命令
        /// </summary>
        public ICommand Syring2Command { get; set; }

        /// <summary>
        /// 注射器吸液命令
        /// </summary>
        public ICommand Obsorb2Command { get; set; }

        /// <summary>
        /// 加标Z气缸动作
        /// </summary>
        public ICommand Syring2DownCommand { get; set; }

        /// <summary>
        /// 加标Z气缸动作
        /// </summary>
        public ICommand Syring2UpCommand { get; set; }

        #endregion

        #region Constructors

        public CarrierTwoUCViewModel(ICarrierOneDataAccess dataAccess, IEtherCATMotion motion, IEPG26 clawInstance, IIoDevice io, ILogger logger) : base(dataAccess, motion, clawInstance, logger)
        {
            this._io = io;
            CarrierInfo = SimpleIoc.Default.GetInstance<ICarrierTwo>().GetCarrierInfo();

        }


        #endregion

        protected override void RegisterCommand()
        {
            Syring2DownCommand = new RelayCommand(SyringDown);
            Syring2UpCommand = new RelayCommand(SyringUp);
            Syring2HomeCommand = new RelayCommand(Syring2Home);
            Syring2Command = new RelayCommand(Syring2Func);
            Obsorb2Command = new RelayCommand(Obsorb2Func);

            base.RegisterCommand();
        }

        /// <summary>
        /// 保存点位数据
        /// </summary>
        protected override void SaveAxisPos()
        {
            var list = AxisPosInfos.ToList();
            bool result = false;


            if (SelectedAxis.AxisNo >= 9 && SelectedAxis.AxisNo <= 12)
            {
                ushort id = 0;
                if (SelectedAxis.AxisNo == 10)
                {
                    id = 1;
                }
                if (SelectedAxis.AxisNo == 11)
                {
                    id = 2;
                }
                if (SelectedAxis.AxisNo == 12)
                {
                    id = 2;
                }
                result = SimpleIoc.Default.GetInstance<ICarrierTwoDataAccess>().UpdatePosDataByAxisPosInfo(id, list);
                SimpleIoc.Default.GetInstance<ICarrierTwo>().UpdatePosData();
            }

        }




        //==============================================================================================//

        private void SyringDown()
        {
            try
            {
                _io.WriteBit_DO(51,true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void SyringUp()
        {
            try
            {
                _io.WriteBit_DO(51, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void Syring2Home()
        {
            try
            {
                _motion.GohomeWithCheckDone(15, 21, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
          
        }

        private void Syring2Func()
        {
            try
            {
                double offset = 0;
                _motion.P2pMoveWithCheckDone(15, offset,1, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Obsorb2Func()
        {
            try
            {
                double offset = 50;
                _motion.P2pMoveWithCheckDone(15, offset, 1, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



    }
}
