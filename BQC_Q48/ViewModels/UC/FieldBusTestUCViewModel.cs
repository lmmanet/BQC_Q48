using System;
using System.Windows.Input;
using PropertyChanged;
using Q_Platform.ViewModels.Base;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using Q_Platform.BLL;
using System.Threading.Tasks;
using System.Threading;
using BQJX.Common;
using System.Collections.Generic;

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


        public ICommand Command1 { get; set; }
        public ICommand Command2 { get; set; }
        public ICommand Command3 { get; set; }
        public ICommand Command4 { get; set; }
        public ICommand Command5 { get; set; }
        public ICommand Command6 { get; set; }

       


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

            Command1 = new RelayCommand(async()=> await DoFuc1() );
            Command2 = new RelayCommand(async()=> await DoFuc2());
            Command3 = new RelayCommand(async()=> await DoFuc3());
            Command4 = new RelayCommand(async()=> await DoFuc4());
            Command5 = new RelayCommand(async()=> await DoFuc5());
            Command6 = new RelayCommand(async() => await DoFuc6());

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

    
        /// <summary>
        /// 回零测试
        /// </summary>
        /// <returns></returns>
        private async Task DoFuc1()
        {
            
           

        }

        private async Task DoFuc2()
        {
            //SimpleIoc.Default.GetInstance<IVortex>().StartVortexAsync(new BQJX.Common.Sample(),null);  //开始涡旋
            //SimpleIoc.Default.GetInstance<VibrationBase>().StartVibrationAsync(new BQJX.Common.Sample(), null);  //振荡开始

            SimpleIoc.Default.GetInstance<IMainPro>().StartPro();

            //var intance = SimpleIoc.Default.GetInstance<CarrierBase>() as CarrierOne;
            //var ret = await intance.GetSampleFromCapperOneToMaterial(id1, null).ConfigureAwait(false);
            //if (!ret)
            //{
            //    return;
            //}
            //await intance.GetSampleFromCapperOneToMaterial(id2, null).ConfigureAwait(false);          

            //var intance = SimpleIoc.Default.GetInstance<CarrierBase>() as CarrierOne;
            //var ret = await intance.GetSampleFromVortexToMaterial(id1, null).ConfigureAwait(false);
            //if (!ret)
            //{
            //    return;
            //}
            //await intance.GetSampleFromVortexToMaterial(id2, null).ConfigureAwait(false);

            //var intance = SimpleIoc.Default.GetInstance<CarrierBase>() as CarrierOne;
            //var ret = await intance.GetSampleFromVibrationToCold(id1, null).ConfigureAwait(false);
            //if (!ret)
            //{
            //    return;
            //}
            //await intance.GetSampleFromVibrationToCold(id2, null).ConfigureAwait(false);

        }
        private async Task DoFuc3()
        {
            //Sample sample1 = new Sample() { Id = 1, Status = 1, TechParams = new TechParams() { Solvent_A = 10, Solvent_C = 10 } };
            // Sample sample3 = new Sample() { Id = 1, Status = 0x80, TechParams = new TechParams() { Solvent_A = 10, Solvent_C = 10 } };
            //Sample sample4 = new Sample() { Id = 1, Status = 256, TechParams = new TechParams() { Solvent_A = 10, Solid_B = 10, Solvent_C = 10 } };


            //Task.Run(() =>
            //{
            //    SimpleIoc.Default.GetInstance<ICentrifugalCarrier>().GetSampleFromCapperTwoToTransfer(sample1, null);
            //    SimpleIoc.Default.GetInstance<ICentrifugalCarrier>().GetSampleFromTransferToCapperTwo(sample1, null);

            //    SimpleIoc.Default.GetInstance<ICentrifugalCarrier>().GetSampleFromColdToTransfer(sample3, null);

            //});


            //SimpleIoc.Default.GetInstance<ICentrifugalCarrier>().GetSampleFromMarterialToTransfer(sample4, null);
            //SimpleIoc.Default.GetInstance<ICentrifugalCarrier>().GetSampleFromTransferToMarterial(sample4,true, null);







        }  
        private async Task DoFuc4()
        {
            //var intance = SimpleIoc.Default.GetInstance<CarrierBase>() as CarrierOne;
            //var ret = await intance.GetSampleFromTransferToMaterial(id1, null).ConfigureAwait(false);
            //if (!ret)
            //{
            //    return;
            //}
            //await intance.GetSampleFromTransferToMaterial(id2, null).ConfigureAwait(false);

        }
        private async Task DoFuc5()
        {
            SimpleIoc.Default.GetInstance<IMainPro>().ContinuePro();
            //var intance = SimpleIoc.Default.GetInstance<CarrierBase>() as CarrierOne;
            //var ret = await intance.GetSampleFromTransferToCapperTwo(id1, null).ConfigureAwait(false);
            //if (!ret)
            //{
            //    return;
            //}
            //await intance.GetSampleFromTransferToCapperTwo(id2, null).ConfigureAwait(false);

            //var intance = SimpleIoc.Default.GetInstance<CarrierBase>() as CarrierOne;
            //var ret = await intance.GetSampleFromMaterialToTransfer(id1, null).ConfigureAwait(false);
            //if (!ret)
            //{
            //    return;
            //}
            //await intance.GetSampleFromMaterialToTransfer(id2, null).ConfigureAwait(false);
        }
        private async Task DoFuc6()
        {
            SimpleIoc.Default.GetInstance<IMainPro>().StopPro();
        }









        #endregion

    }
}
