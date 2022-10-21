using BQJX.Common.Common;
using BQJX.Core;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Q_Platform.ViewModels.Base
{
    public abstract class MyViewModelBase :ViewModelBase
    {
        private object mPropertyValueCheckLock = new object();

        protected Task _refreshTask;
        protected bool _stopRefresh;

        #region Properties

        public DateTime DateTimeNow { get; set; }

        #endregion


        protected async Task RunCommandAsync(Expression<Func<bool>> updatingFlag, Func<Task> action)
        {
            // Lock to ensure single access to check
            lock (mPropertyValueCheckLock)
            {
                // Check if the flag property is true (meaning the function is already running)
                if (updatingFlag.GetPropertyValue())
                    return;

                // Set the property flag to true to indicate we are running
                updatingFlag.SetPropertyValue(true);
            }

            try
            {
                // Run the passed in action
                await action();
            }
            finally
            {
                // Set the property flag back to false now it's finished
                updatingFlag.SetPropertyValue(false);
            }
        }

        protected async Task RunCommandOpAsync(Expression<Func<bool>> updatingFlag, Func<Task> action)
        {
            // Lock to ensure single access to check
            lock (mPropertyValueCheckLock)
            {
                // Check if the flag property is true (meaning the function is already running)
                if (!updatingFlag.GetPropertyValue())
                    return;

                // Set the property flag to true to indicate we are running
                updatingFlag.SetPropertyValue(false);
            }

            try
            {
                // Run the passed in action
                await action();
            }
            finally
            {
                // Set the property flag back to false now it's finished
                updatingFlag.SetPropertyValue(true);
            }
        }



        protected void RunCommandSync(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected async Task RunCommandAsync(Action action)
        {
            await Task.Run(() =>
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(ex.Message);
                    });
                }
            }).ConfigureAwait(false);
           
        }







        public override void Cleanup()
        {
            _stopRefresh = true;
            base.Cleanup();
        }


    }

}
