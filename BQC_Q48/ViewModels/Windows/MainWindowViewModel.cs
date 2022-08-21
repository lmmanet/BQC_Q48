using BQC_Q48.Views;
using GalaSoft.MvvmLight.Command;
using Q_Platform.Logger;
using Q_Platform.ViewModels.Base;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Q_Platform.ViewModels.Windows
{
    public class MainWindowViewModel : MyViewModelBase
    {
        #region Private Members

        private Task refreshTimeTask;

        private bool refreshTimeFlag = true;

        #endregion

        #region Properties

        public FrameworkElement CurrentPage { get; set; }



        #endregion

        #region Commands

        public ICommand WindowMinCommand { get; set; }
        public ICommand WindowMaxCommand { get; set; }
        public ICommand WindowCloseCommand { get; set; }
        public ICommand SwichPageCommand { get; set; }


        #endregion

        #region Constructors

        public MainWindowViewModel()
        {
            RegisterCommnand();
            refreshTimeTask = Task.Run(async () =>
            {
                while (refreshTimeFlag)
                {
                    DateTimeNow = DateTime.Now;
                    await Task.Delay(1000);
                }
            });
        }

        #endregion


        #region Private Methods

        private void RegisterCommnand()
        {
            WindowMaxCommand = new RelayCommand<Object>(WindowMax);
            WindowCloseCommand = new RelayCommand<Object>(WindowClose);
            WindowMinCommand = new RelayCommand<Object>(WindowMin);
            SwichPageCommand = new RelayCommand<Object>(SwichPage);
        }

        private void WindowMin(object o)
        {
            (o as MainWindow).WindowState = WindowState.Minimized;
            LoggerHelper.Logger.Debug("=======================++++++++++++++=====================");
        }

        private void WindowMax(object o)
        {
            if ((o as MainWindow).WindowState == WindowState.Maximized)
                (o as MainWindow).WindowState = WindowState.Normal;
            else
                (o as MainWindow).WindowState = WindowState.Maximized;
        }

        private void WindowClose(object o)
        {
            var result = MessageBox.Show("确认是否退出程序", "提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                //停止时间刷新线程
                refreshTimeFlag = false;
                refreshTimeTask.Wait();

                try
                {
                    // MainServer.GetCardInstance()?.Close();
                }
                catch
                {

                }

                (o as MainWindow).Close();
                var p = System.Diagnostics.Process.GetCurrentProcess();
                p.Kill();
            }
        }

        private void SwichPage(object o)
        {
            var page = GetPage(o.ToString());
            if (page != null)
            {
                CurrentPage = page;
            }
        }

        /// <summary>
        /// 获取实例页面
        /// </summary>
        /// <param name="pageName"></param>
        /// <returns></returns>
        private FrameworkElement GetPage(string pageName)
        {
            //BQC_TCM.Views.AlarmPage
            Type type = this.GetType().Assembly.GetType($"BQC_Q48.Views.{pageName}");
            if (type == null)
            {
                return null;
            }
            return (FrameworkElement)Activator.CreateInstance(type);
        }



        #endregion


        #region Public Methods


        #endregion

    }

}
