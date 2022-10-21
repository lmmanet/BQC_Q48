using BQJX.Common;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
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
    public class SampleManagePageViewModel : MyViewModelBase
    {
        private readonly ISampleDataAccess _dataAccess;
        private readonly ILogger _logger;
        private int pageIndex = 0;



        #region Properties

        public ObservableCollection<SampleInfo> SampleList { get; set; } = new ObservableCollection<SampleInfo>();

        private string _searchName;

        public string SearchName
        {
            get { return _searchName; }
            set { _searchName = value; RaisePropertyChanged(); }
        }

        private string _searchSnNum;

        public string SearchSnNum
        {
            get { return _searchSnNum; }
            set { _searchSnNum = value; RaisePropertyChanged(); }
        }

        private DateTime? _searchStartTime;

        public DateTime? SearchStartTime
        {
            get { return _searchStartTime; }
            set { _searchStartTime = value; RaisePropertyChanged(); }
        }

        private DateTime? _searchEndTime;

        public DateTime? SearchEndTime
        {
            get { return _searchEndTime; }
            set { _searchEndTime = value; RaisePropertyChanged(); }
        }


        #endregion

        #region Commands

        public ICommand SearchCommand { get; set; }

        public ICommand DeleteSampeCommand { get; set; }

        public ICommand ForwardPageCommand { get; set; }

        public ICommand NextPageCommand { get; set; }

        #endregion

        #region Construtors

        public SampleManagePageViewModel(ISampleDataAccess dataAccess, ILogger logger)
        {
            this._dataAccess = dataAccess;
            this._logger = logger;
            RegisterCommnand();
            GetSampleInfoByIdFromDataBase(0, 10);
        }

        #endregion

        #region Private Methods
        private void RegisterCommnand()
        {
            SearchCommand = new RelayCommand(SearchSample);
            DeleteSampeCommand = new RelayCommand<object>(DeleteSample);
            ForwardPageCommand = new RelayCommand(ForwardPage);
            NextPageCommand = new RelayCommand(NextPage);
        }

        private void SearchSample()
        {
            if (!string.IsNullOrEmpty(SearchName))
            {
                GetSampleInfoByNameFromDataBase(SearchName);
                return;
            }
            if (!string.IsNullOrEmpty(SearchSnNum))
            {
                GetSampleInfoBySnNumFromDataBase(SearchSnNum);
                return;
            }
            if (SearchStartTime != null && SearchEndTime != null)
            {
                GetSampleInfoByTimeFromDataBase((DateTime)SearchStartTime, SearchEndTime);
            }

        }

        private void DeleteSample(object obj)
        {
            var sample = obj as SampleInfo;
            if (sample == null)
            {
                return;
            }

            var dr = MessageBox.Show("确认删除该样品", "警告", MessageBoxButton.YesNo);
            if (dr == MessageBoxResult.Yes)
            {
                try
                {
                    var result = _dataAccess.DeleteSampleInfo(sample);
                    if (result)
                    {
                        SampleList.Remove(sample);
                    }
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
            GetSampleInfoByIdFromDataBase(10 * pageIndex, 10 * (pageIndex + 1));
        }

        private void NextPage()
        {
            pageIndex++;
            GetSampleInfoByIdFromDataBase(10 * pageIndex, 10 * (pageIndex + 1));
            if (SampleList.Count == 0)
            {
                MessageBox.Show("没有更多的样品信息", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private void GetSampleInfoByIdFromDataBase(int start, int end)
        {
            try
            {
                SampleList.Clear();
                var samplelist = _dataAccess.GetSampleInfoById(start, end - start);

                foreach (SampleInfo item in samplelist)
                {
                    SampleList.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger? .Error(ex.Message);
            }

        }

        private void GetSampleInfoByNameFromDataBase(string name)
        {
            try
            {
                SampleList.Clear();
                var samplelist = _dataAccess.GetSampleInfoByName(name);

                foreach (SampleInfo item in samplelist)
                {
                    SampleList.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
            }

        }

        private void GetSampleInfoBySnNumFromDataBase(string sn)
        {
            try
            {
                SampleList.Clear();
                var samplelist = _dataAccess.GetSampleInfoBySnNum(sn);

                foreach (SampleInfo item in samplelist)
                {
                    SampleList.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
            }

        }

        private void GetSampleInfoByTimeFromDataBase(DateTime start, DateTime? end)
        {
            try
            {
                SampleList.Clear();
                var samplelist = _dataAccess.GetSampleInfoByTime(start, end == null ? DateTime.Now : (DateTime)end);

                foreach (SampleInfo item in samplelist)
                {
                    SampleList.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
            }

        }
        #endregion


        public override void Cleanup()
        {
            base.Cleanup();
        }

    }
}
