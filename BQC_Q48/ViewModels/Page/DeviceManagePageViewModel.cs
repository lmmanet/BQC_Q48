using BQC_Q48.ViewModels;
using BQJX.Core.Interface;
using CommonServiceLocator;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using Q_Platform.BLL;
using Q_Platform.ViewModels.Base;
using Q_Platform.ViewModels.Module;
using Q_Platform.ViewModels.UC;
using Q_Platform.Views.Module;
using Q_Platform.Views.UC;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace Q_Platform.ViewModels.Page
{
    public class DeviceManagePageViewModel : MyViewModelBase
    {

        #region Properties

        public FrameworkElement CurrentContent { get; set; } = new SampleStatusMonitor();

        public string PageTitle { get; set; }


        #endregion

        #region Commands

        public ICommand SwichCommand { get; set; }

        public ICommand PageCloseCommand { get; set; }

        #endregion

        #region Constructor

        public DeviceManagePageViewModel()
        {
            RegisterCommnand();
        }

        #endregion

        #region Private Methods

        private void RegisterCommnand()
        {
            SwichCommand = new RelayCommand<object>(SwichContent);
            PageCloseCommand = new RelayCommand(PageClose);
        }

        /// <summary>
        /// 切换画面内容
        /// </summary>
        /// <param name="o"></param>
        private void SwichContent(object o)
        {
            string str = o.ToString();
            string[] strs = str.Split('@');
            //SampleStatusMonitor
            var page = GetPage(strs[0]);
            
            if (page != null)
            {
                if (o.ToString() == "AxisTestUC1")
                {
                    PageTitle = "伺服轴测试";
                }
                if (o.ToString() == "IoTestUC")
                {
                    PageTitle = "提取Io测试";

                }
                if (o.ToString() == "ClawTestUC")
                {
                    PageTitle = "电爪测试";

                }
                if (o.ToString() == "StepAxisTestUC")
                {
                    PageTitle = "步进一体机测试";
                }
                if (o.ToString() == "PipettorUC")
                {
                    PageTitle = "移液模块";
                }

                if (strs[0] == "CapperUC")
                {
                    if (strs[1] == "1")
                    {
                        page.DataContext = GetViewModelLocator().CapperOneUCViewModel; 
                    }
                    if (strs[1] == "2")
                    {
                        page.DataContext = GetViewModelLocator().CapperTwoUCViewModel;  
                    }
                    if (strs[1] == "3")
                    {
                        page.DataContext = GetViewModelLocator().CapperThreeUCViewModel;
                    }
                    if (strs[1] == "4")
                    {
                        page.DataContext = GetViewModelLocator().CapperFourUCViewModel; 
                    }
                    if (strs[1] == "5")
                    {
                        page.DataContext = GetViewModelLocator().CapperFiveUCViewModel; 
                    }
                }

                if (strs[0]== "CarrierUC")
                {
                    if (strs[1] == "1")
                    {
                        page.DataContext = GetViewModelLocator().CarrierOneUCViewModel;
                    }
                    if (strs[1] == "2")
                    {
                        page.DataContext = GetViewModelLocator().CarrierTwoUCViewModel;
                    }
                }

                if (strs[0] == "VibrationUC")
                {
                    if (strs[1] == "1")
                    {
                        page.DataContext = GetViewModelLocator().VibrationOneViewModel;
                    }
                    if (strs[1] == "2")
                    {
                        page.DataContext = GetViewModelLocator().VibrationTwoViewModel;
                    }
                }



                CurrentContent = page;

            }
        }

        /// <summary>
        /// 获取实例页面
        /// </summary>
        /// <param name="pageName"></param>
        /// <returns></returns>
        private FrameworkElement GetPage(string pageName)
        {
            Type t = CurrentContent.GetType();
            if (t.Name == pageName)
            {
                return CurrentContent;
            }
            //Q_Platform.Views.Module.CapperUC
            Type type = this.GetType().Assembly.GetType($"Q_Platform.Views.UC.{pageName}");
           
            if (type == null)
            {
                type = this.GetType().Assembly.GetType($"Q_Platform.Views.Module.{pageName}");
                if (type == null)
                {
                    type = this.GetType().Assembly.GetType($"Q_Platform.Views.UC.Base.{pageName}");
                    if (type == null)
                    {
                        return null;
                    }
                }
            }
            return (FrameworkElement)Activator.CreateInstance(type);
        }


        private ViewModelLocator GetViewModelLocator()
        {
            var res =  Application.Current.Resources["Locator"];
            var locator = res as ViewModelLocator;
            
            return locator;
        }


        /// <summary>
        /// 切换到主画面
        /// </summary>
        private void PageClose()
        {
            //PageTitle =;
            //CurrentContent = new DeviceLayout();
        }

        #endregion

        #region Public Methods

        public override void Cleanup()
        {
            ViewModelLocator.Cleanup<CarrierOneUCViewModel>();
            ViewModelLocator.Cleanup<CarrierTwoUCViewModel>();
            ViewModelLocator.Cleanup<CapperOneUCViewModel>();
            ViewModelLocator.Cleanup<CapperTwoUCViewModel>();
            ViewModelLocator.Cleanup<CapperThreeUCViewModel>();
            ViewModelLocator.Cleanup<CapperFourUCViewModel >();
            ViewModelLocator.Cleanup<CapperFiveUCViewModel >();
            ViewModelLocator.Cleanup<VortexViewModel>();
            ViewModelLocator.Cleanup<CentrifugalViewModel>();
            ViewModelLocator.Cleanup<CenCarrierViewModel>();
            ViewModelLocator.Cleanup<ConcentrationViewModel>();
            ViewModelLocator.Cleanup<VibrationOneViewModel >();
            ViewModelLocator.Cleanup<VibrationTwoViewModel >();
            ViewModelLocator.Cleanup<AddSaltUCViewModel>();
            ViewModelLocator.Cleanup<AxisTestUCViewModel>();
            ViewModelLocator.Cleanup<StepAxisTestUCViewModel>();
            ViewModelLocator.Cleanup<ClawTestUCViewModel>();
            ViewModelLocator.Cleanup<FieldBusTestUCViewModel>();
            ViewModelLocator.Cleanup<IoTestUCViewModel>();
            ViewModelLocator.Cleanup<BalanceTestUCViewModel>();

            base.Cleanup();
        }

        #endregion


    }
}
