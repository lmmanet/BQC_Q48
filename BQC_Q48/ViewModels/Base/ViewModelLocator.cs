using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;
using Q_Platform.ViewModels.Windows;
using Q_Platform.ViewModels.Page;
using Q_Platform.ViewModels.UC;

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

            //SimpleIoc.Default.Register<SettingWindowViewModel>();
            //SimpleIoc.Default.Register<NewTechWindowViewModel>();
            //SimpleIoc.Default.Register<CarrierUCViewModel>();
            //SimpleIoc.Default.Register<CentrifugalUCViewModel>();
            //SimpleIoc.Default.Register<HomoUCViewModel>();
            //SimpleIoc.Default.Register<VortexUCViewModel>();
            //SimpleIoc.Default.Register<PipettorUCViewModel>();
            SimpleIoc.Default.Register<AxisTestUCViewModel>();

            SimpleIoc.Default.Register<StepAxisTestUCViewModel>();
            SimpleIoc.Default.Register<ClawTestUCViewModel>();

            SimpleIoc.Default.Register<FieldBusTestUCViewModel>();
            SimpleIoc.Default.Register<IoTestUCViewModel>();
            SimpleIoc.Default.Register<BalanceTestUCViewModel>();

            SimpleIoc.Default.Register<SampleStatusMonitorViewModel>();
        }

        #endregion

        #region Properties

        public MainWindowViewModel MainWindowViewModel => ServiceLocator.Current.GetInstance<MainWindowViewModel>();

        public MainPageViewModel MainPageViewModel => ServiceLocator.Current.GetInstance<MainPageViewModel>();
        public AlarmPageViewModel AlarmPageViewModel => ServiceLocator.Current.GetInstance<AlarmPageViewModel>();
        public DeviceManagePageViewModel DeviceManagePageViewModel => ServiceLocator.Current.GetInstance<DeviceManagePageViewModel>();
        public SampleManagePageViewModel SampleManagePageViewModel => ServiceLocator.Current.GetInstance<SampleManagePageViewModel>();
        public TechManagePageViewModel TechManagePageViewModel => ServiceLocator.Current.GetInstance<TechManagePageViewModel>();


        public AxisTestUCViewModel AxisTestUCViewModel => ServiceLocator.Current.GetInstance<AxisTestUCViewModel>();

        public StepAxisTestUCViewModel StepAxisTestUCViewModel => ServiceLocator.Current.GetInstance<StepAxisTestUCViewModel>();



        public ClawTestUCViewModel ClawTestUCViewModel => ServiceLocator.Current.GetInstance<ClawTestUCViewModel>();


        public FieldBusTestUCViewModel FieldBusTestUCViewModel => ServiceLocator.Current.GetInstance<FieldBusTestUCViewModel>();

        public IoTestUCViewModel IoTestUCViewModel => ServiceLocator.Current.GetInstance<IoTestUCViewModel>();

        public BalanceTestUCViewModel BalanceTestUCViewModel => ServiceLocator.Current.GetInstance<BalanceTestUCViewModel>();



        public SampleStatusMonitorViewModel SampleStatusMonitorViewModel => ServiceLocator.Current.GetInstance<SampleStatusMonitorViewModel>();


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
