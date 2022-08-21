using BQJX.Models;
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
    public class TechManagePageViewModel : MyViewModelBase
    {

        #region Properties

        public ObservableCollection<TechParamsModel> TechList { get; set; }

        public string SearchName { get; set; }
        public DateTime? SearchStartTime { get; set; }
        public DateTime? SearchEndTime { get; set; }

        public Visibility DetailVisibility { get; set; }

        #endregion

        #region Commands

        public ICommand SearchCommand { get; set; }
        public ICommand AddTechCommand { get; set; }
        public ICommand DetailCommand { get; set; }
        public ICommand DeleteTechCommand { get; set; }
        public ICommand ForwardPageCommand { get; set; }
        public ICommand NextPageCommand { get; set; }
        public ICommand DetailCloseCommand { get; set; }



        #endregion

        #region Construtors

        public TechManagePageViewModel()
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
