using BQJX.Common.Common;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using PropertyChanged;
using Q_Platform.BLL;
using Q_Platform.Common;
using Q_Platform.DAL;
using Q_Platform.Models;
using Q_Platform.ViewModels.Base;
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

namespace Q_Platform.ViewModels.Module
{
    [AddINotifyPropertyChangedInterface]
    public class CapperTwoUCViewModel : CapperViewModelBase
    {
        private readonly ICapperTwo _capper;

        #region Construtors

        public CapperTwoUCViewModel(ILS_Motion iLS_Motion, IIoDevice io, ICapperPosDataAccess dataAccess, ICapperTwo capper) :base(iLS_Motion,io,dataAccess)
        {
     
            CapperInfo = SimpleIoc.Default.GetInstance<ICapperTwo>().GetCapperInfo();
            this._capper = capper;
            _refreshTask = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        RefreshIoStatus();

                        if (_stopRefresh)
                        {
                            break;
                        }

                        Thread.Sleep(1000);
                    }
                    catch (Exception)
                    {
                        //logger?.Error($"_refreshTask err:{ex.Message}");
                    }

                }
            });


        }

        #endregion


        protected override void SaveAxisPos()
        {
            base.SaveAxisPos();
            _capper.UpdatePosData();
        }

    }
}
