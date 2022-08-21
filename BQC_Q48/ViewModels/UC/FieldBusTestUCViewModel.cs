using System;
using System.Windows.Input;
using PropertyChanged;
using Q_Platform.ViewModels.Base;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;

namespace Q_Platform.ViewModels.UC
{
    [AddINotifyPropertyChangedInterface]
    public class FieldBusTestUCViewModel : MyViewModelBase
    {

        #region Private Members

        private readonly ICardBase _card;
        private readonly ILogger _logger;

        #endregion

        #region Properties

        public ushort CardId { get; set; } = 0;
        public ushort SlaveId { get; set; }
        public string MainIndex { get; set; }
        public string SubIndex { get; set; }
        public ushort DataLen { get; set; }
        public int DataValue { get; set; }


        #endregion

        #region Commands

        /// <summary>
        /// 读
        /// </summary>
        public ICommand ReadCommand { get; set; }

        /// <summary>
        /// 停止
        /// </summary>
        public ICommand WriteCommand { get; set; }

       
        #endregion

        #region Construtors

        public FieldBusTestUCViewModel(ICardBase card, IEtherCATMotion motion, ILogger logger)
        {
            _logger = logger;
            _card = card;
            RegisterCommand();
        }

        #endregion

        #region Private Methods

        private void RegisterCommand()
        {
            ReadCommand = new RelayCommand(ReadSlave);
            WriteCommand = new RelayCommand(WriteSlave);
            
        }

        private void ReadSlave()
        {
            DataValue = _card.GetPDO(CardId, SlaveId, HexToUshort(MainIndex), HexToUshort(SubIndex), DataLen);
        }

        private void WriteSlave()
        {
            _card.SetPDO(CardId, SlaveId, HexToUshort(MainIndex), HexToUshort(SubIndex), DataLen, DataValue);
        }




        private ushort HexToUshort(string hex)
        {
            var result = Convert.ToInt32(hex, 16);
            return (ushort)result;
        }


        #endregion

    }
}
