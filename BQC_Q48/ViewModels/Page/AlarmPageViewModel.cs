using BQJX.Models;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Q_Platform.ViewModels.Page
{
    public class AlarmPageViewModel : MyViewModelBase
    {

        #region Properties

        public ObservableCollection<AlarmMessage> AlarmList { get; set; } = new ObservableCollection<AlarmMessage>();

        #endregion

        #region Commands

        public ICommand CancelAlarmCommand { get; set; }

        #endregion

        public AlarmPageViewModel()
        {
            CancelAlarmCommand = new RelayCommand<object>(CancelAlarm);
            Messenger.Default.Register<AlarmMessage>(this, "AlarmNotification", OnAlarmNotification);
        }

        /// <summary>
        /// 报警通知
        /// </summary>
        /// <param name="obj"></param>
        private void OnAlarmNotification(AlarmMessage obj)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                obj.Id+= AlarmList.Count;
                AlarmList.Add(obj);

            });
         
        }

        /// <summary>
        /// 确认报警信息
        /// </summary>
        /// <param name="obj"></param>
        private void CancelAlarm(object obj)
        {
            var alarm = obj as AlarmMessage;
            if (alarm != null)
            {
                AlarmList.Remove(alarm);
            }
        }
    }
}
