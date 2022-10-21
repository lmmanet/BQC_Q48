using BQJX.Common;
using BQJX.Core.Interface;
using BQJX.Models;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Q_Platform.DAL;
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

        private readonly ITechParamsDataAccess _dataAccess;
        private readonly ILogger _logger;
        private int pageIndex = 0;


        #region Properties

        public ObservableCollection<TechParamsModel> TechList { get; set; } = new ObservableCollection<TechParamsModel>();

        /// <summary>
        /// 详细参数显示
        /// </summary>
        public TechParamsInfo SelectedTechInfo { get; set; }
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

        public TechManagePageViewModel(ITechParamsDataAccess dataAccess,ILogger logger)
        {
            this._dataAccess = dataAccess;
            this._logger = logger;
            RegisterCommnand();
        }

        #endregion

        #region Private Methods
        private void RegisterCommnand()
        {
            GetTechInfoByIdFromDataBase(0, 10);


            SearchCommand = new RelayCommand(SearchTech);
            DeleteTechCommand = new RelayCommand<object>(DeleteTech);
            ForwardPageCommand = new RelayCommand(ForwardPage);
            NextPageCommand = new RelayCommand(NextPage);

            AddTechCommand = new RelayCommand(AddTech);
            DetailCommand = new RelayCommand<object>(TechDetail);
            DetailCloseCommand = new RelayCommand(DetailClose);


            Messenger.Default.Register<TechParamsInfo>(this, "AddTech", OnAddTechCallBack);
        }



        private void SearchTech()
        {
            if (!string.IsNullOrEmpty(SearchName))
            {
                GetTechInfoByNameFromDataBase(SearchName);
                return;
            }
            if (SearchStartTime != null && SearchEndTime != null)
            {
                GetTechInfoByTimeFromDataBase((DateTime)SearchStartTime, SearchEndTime);
            }

        }

        private void DeleteTech(object obj)
        {
            var tech = obj as TechParamsInfo;
            if (tech == null)
            {
                return;
            }

            var dr = MessageBox.Show("确认删除该工艺", "警告", MessageBoxButton.YesNo);
            if (dr == MessageBoxResult.Yes)
            {
                try
                {
                    //var result = _dataAccess.DeleteTechParamsInfo(tech);
                    //if (result)
                    //{
                    //    TechList.Remove(tech);
                    //}
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex.Message);

                }
            }
        }

        private void ForwardPage()
        {
            pageIndex--;
            if (pageIndex < 0)
            {
                pageIndex = 0;
            }
            GetTechInfoByIdFromDataBase(10 * pageIndex, 10 * (pageIndex + 1));
        }

        private void NextPage()
        {
            pageIndex++;
            GetTechInfoByIdFromDataBase(10 * pageIndex, 10 * (pageIndex + 1));
            if (TechList.Count == 0)
            {
                MessageBox.Show("没有更多的工艺信息", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddTech()
        {
           // new NewTechWindow().ShowDialog();
        }

        private void TechDetail(object obj)
        {
            var tech = obj as TechParamsInfo;
            if (tech == null)
            {
                return;
            }

            SelectedTechInfo = tech;
            DetailVisibility = Visibility.Visible;
        }

        private void DetailClose()
        {
            DetailVisibility = Visibility.Collapsed;
        }

        private void OnAddTechCallBack(TechParamsInfo techParamsInfo)
        {
            //保存工艺参数到数据库  并更新显示
            techParamsInfo.Createtime = DateTime.Now;

            var result = SaveTechParamsInfo(techParamsInfo).GetAwaiter().GetResult();
            if (!result)
            {
                MessageBox.Show("保存到数据库失败! 数据库存在重名工艺名称", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }//

           // TechList.Add(techParamsInfo);


        }


        private void GetTechInfoByIdFromDataBase(int start, int end)
        {
            try
            {
                TechList.Clear();
                var techParamsList = _dataAccess.GetTechParamsInfoById(start, end - start);

                //foreach (TechParamsInfo item in techParamsList)
                //{
                //    TechList.Add(item);
                //}
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
            }

        }

        private void GetTechInfoByNameFromDataBase(string techName)
        {
            try
            {
                TechList.Clear();
                var techParamsList = _dataAccess.GetTechParamsInfoByTechName(techName);

                //foreach (TechParamsInfo item in techParamsList)
                //{
                //    TechList.Add(item);
                //}
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
            }

        }

        private void GetTechInfoByTimeFromDataBase(DateTime start, DateTime? end)
        {
            try
            {
                TechList.Clear();
                var techParamsList = _dataAccess.GetTechParamsInfoByTime(start, end == null ? DateTime.Now : (DateTime)end);

                //foreach (TechParamsInfo item in techParamsList)
                //{
                //    TechList.Add(item);
                //}
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
            }

        }


        private Task<bool> SaveTechParamsInfo(TechParamsInfo techParamsInfo)
        {
            return Task.Run(() =>
            {
                var list = _dataAccess.GetTechParamsInfoById(0, 1000);

                //var tech = list.FirstOrDefault(t => t.TechName == techParamsInfo.TechName);
                //if (tech == null)
                //{
                //    //_dataAccess.InsertTechParamsInfo(techParamsInfo);
                //    return true;
                //}
                //else
                //{
                //    return false;
                //}
                return false;
            });
        }







        #endregion

        public override void Cleanup()
        {
            base.Cleanup();
        }








    }
}
