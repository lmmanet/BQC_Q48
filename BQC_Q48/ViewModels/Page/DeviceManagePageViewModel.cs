using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using Q_Platform.ViewModels.Base;
using Q_Platform.Views.UC;
using System;
using System.Windows;
using System.Windows.Input;

namespace Q_Platform.ViewModels.Page
{
    public class DeviceManagePageViewModel : MyViewModelBase
    {

        #region Properties

        public FrameworkElement CurrentContent { get; set; } = new AxisTestUC1();

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
            var page = GetPage(o.ToString());
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
            Type type = this.GetType().Assembly.GetType($"Q_Platform.Views.UC.{pageName}");
            if (type == null)
            {
                return null;
            }
            return (FrameworkElement)Activator.CreateInstance(type);
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
            base.Cleanup();
        }

        #endregion


    }
}
