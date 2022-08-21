using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Q_Platform.ViewModels.Page
{
    public class SampleManagePageViewModel : MyViewModelBase
    {
        #region Properties

       
        #endregion

        #region Commands

     
        #endregion

        #region Construtors

        public SampleManagePageViewModel()
        {
            RegisterCommnand();
        }

        #endregion

        #region Private Methods
        private void RegisterCommnand()
        {

        }


        #endregion


        public override void Cleanup()
        {
            base.Cleanup();
        }

    }
}
