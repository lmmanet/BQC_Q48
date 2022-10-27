using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;
using Q_Platform.ViewModels.Windows;
using Q_Platform.ViewModels.Page;
using Q_Platform.ViewModels.UC;
using Q_Platform.ViewModels.Module;

namespace BQC_Q48.ViewModels
{
    public class ViewModelLocator
    {

        #region Constructor

        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            //SimpleIoc.Default.Register<LoginWindowViewModel>();
            SimpleIoc.Default.Register<MainWindowViewModel>();


            SimpleIoc.Default.Register<MainPageViewModel>();
            SimpleIoc.Default.Register<AlarmPageViewModel>();
            SimpleIoc.Default.Register<DeviceManagePageViewModel>();
            SimpleIoc.Default.Register<SampleManagePageViewModel>();
            SimpleIoc.Default.Register<TechManagePageViewModel>();

            SimpleIoc.Default.Register<AxisTestUCViewModel>();

            SimpleIoc.Default.Register<StepAxisTestUCViewModel>();
            SimpleIoc.Default.Register<ClawTestUCViewModel>();

            SimpleIoc.Default.Register<FieldBusTestUCViewModel>();
            SimpleIoc.Default.Register<IoTestUCViewModel>();
            SimpleIoc.Default.Register<BalanceTestUCViewModel>();

            SimpleIoc.Default.Register<SampleStatusMonitorViewModel>();

            SimpleIoc.Default.Register<CapperOneUCViewModel>();
            SimpleIoc.Default.Register<CapperTwoUCViewModel>();
            SimpleIoc.Default.Register<CapperThreeUCViewModel>();
            SimpleIoc.Default.Register<CapperFourUCViewModel>();
            SimpleIoc.Default.Register<CapperFiveUCViewModel>();

            SimpleIoc.Default.Register<CarrierOneUCViewModel>();
            SimpleIoc.Default.Register<CarrierTwoUCViewModel>();
            SimpleIoc.Default.Register<VortexViewModel>();
            SimpleIoc.Default.Register<CentrifugalViewModel>();
            SimpleIoc.Default.Register<CenCarrierViewModel>();
            SimpleIoc.Default.Register<ConcentrationViewModel>();
            SimpleIoc.Default.Register<VibrationOneViewModel>();
            SimpleIoc.Default.Register<VibrationTwoViewModel>();
            SimpleIoc.Default.Register<AddSaltUCViewModel>();


            SimpleIoc.Default.Register<AddSampleWinViewModel>();

        }

        #endregion

        #region Properties

        public MainWindowViewModel MainWindowViewModel => ServiceLocator.Current.GetInstance<MainWindowViewModel>();
        public MainPageViewModel MainPageViewModel => ServiceLocator.Current.GetInstance<MainPageViewModel>();
        public AlarmPageViewModel AlarmPageViewModel => ServiceLocator.Current.GetInstance<AlarmPageViewModel>();
        public DeviceManagePageViewModel DeviceManagePageViewModel => ServiceLocator.Current.GetInstance<DeviceManagePageViewModel>();
        public SampleManagePageViewModel SampleManagePageViewModel => ServiceLocator.Current.GetInstance<SampleManagePageViewModel>();
        public TechManagePageViewModel TechManagePageViewModel => ServiceLocator.Current.GetInstance<TechManagePageViewModel>();

        public SampleStatusMonitorViewModel SampleStatusMonitorViewModel => ServiceLocator.Current.GetInstance<SampleStatusMonitorViewModel>();

       


      


        //=============================================DevicePage========================================================//
        public CarrierOneUCViewModel CarrierOneUCViewModel => ServiceLocator.Current.GetInstance<CarrierOneUCViewModel>();
        public CarrierTwoUCViewModel CarrierTwoUCViewModel => ServiceLocator.Current.GetInstance<CarrierTwoUCViewModel>();
        public CapperOneUCViewModel CapperOneUCViewModel => ServiceLocator.Current.GetInstance<CapperOneUCViewModel>();
        public CapperTwoUCViewModel CapperTwoUCViewModel => ServiceLocator.Current.GetInstance<CapperTwoUCViewModel>();
        public CapperThreeUCViewModel CapperThreeUCViewModel => ServiceLocator.Current.GetInstance<CapperThreeUCViewModel>();
        public CapperFourUCViewModel CapperFourUCViewModel => ServiceLocator.Current.GetInstance<CapperFourUCViewModel>();
        public CapperFiveUCViewModel CapperFiveUCViewModel => ServiceLocator.Current.GetInstance<CapperFiveUCViewModel>();
        public VortexViewModel VortexViewModel => ServiceLocator.Current.GetInstance<VortexViewModel>();
        public CentrifugalViewModel CentrifugalViewModel => ServiceLocator.Current.GetInstance<CentrifugalViewModel>();
        public CenCarrierViewModel CenCarrierViewModel => ServiceLocator.Current.GetInstance<CenCarrierViewModel>();
        public ConcentrationViewModel ConcentrationViewModel => ServiceLocator.Current.GetInstance<ConcentrationViewModel>();
        public VibrationOneViewModel VibrationOneViewModel => ServiceLocator.Current.GetInstance<VibrationOneViewModel>();
        public VibrationTwoViewModel VibrationTwoViewModel => ServiceLocator.Current.GetInstance<VibrationTwoViewModel>();
        public AddSaltUCViewModel AddSaltUCViewModel => ServiceLocator.Current.GetInstance<AddSaltUCViewModel>();
        //*************************************************************************************************************************//
        public AxisTestUCViewModel AxisTestUCViewModel => ServiceLocator.Current.GetInstance<AxisTestUCViewModel>();
        public StepAxisTestUCViewModel StepAxisTestUCViewModel => ServiceLocator.Current.GetInstance<StepAxisTestUCViewModel>();
        public ClawTestUCViewModel ClawTestUCViewModel => ServiceLocator.Current.GetInstance<ClawTestUCViewModel>();
        public FieldBusTestUCViewModel FieldBusTestUCViewModel => ServiceLocator.Current.GetInstance<FieldBusTestUCViewModel>();
        public IoTestUCViewModel IoTestUCViewModel => ServiceLocator.Current.GetInstance<IoTestUCViewModel>();
        public BalanceTestUCViewModel BalanceTestUCViewModel => ServiceLocator.Current.GetInstance<BalanceTestUCViewModel>();


        public AddSampleWinViewModel AddSampleWinViewModel => ServiceLocator.Current.GetInstance<AddSampleWinViewModel>();

        #endregion

        #region Public Methods

        public static void Cleanup<T>() where T : ViewModelBase
        {
            if (SimpleIoc.Default.IsRegistered<T>())
            {
                ServiceLocator.Current.GetInstance<T>().Cleanup();

                SimpleIoc.Default.Unregister<T>();
                SimpleIoc.Default.Register<T>();
            }

        }

        #endregion
    
    
    }

}
