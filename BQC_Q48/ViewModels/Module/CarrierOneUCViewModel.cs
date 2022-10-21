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
using BQJX.Common.Common;
using BQJX.Core.Common;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using PropertyChanged;
using Q_Platform.BLL;
using Q_Platform.Common;
using Q_Platform.DAL;
using Q_Platform.Models;
using Q_Platform.ViewModels.Base;

namespace Q_Platform.ViewModels.Module
{
    [AddINotifyPropertyChangedInterface]
    public class CarrierOneUCViewModel : CarrierViewModelBase
    {

        public CarrierOneUCViewModel(ICarrierOneDataAccess dataAccess, IEtherCATMotion motion, IEPG26 clawInstance, ILogger logger) :base(dataAccess,motion,clawInstance,logger)
        {
            CarrierInfo = SimpleIoc.Default.GetInstance<ICarrierOne>().GetCarrierInfo();
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
        }


        /// <summary>
        /// 保存点位数据
        /// </summary>
        protected override void SaveAxisPos()
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
                SimpleIoc.Default.GetInstance<ICarrierOne>().UpdatePosData();

            }

          

        }

     




    }
}
