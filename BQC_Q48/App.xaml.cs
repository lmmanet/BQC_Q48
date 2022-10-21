using BQC_Q48.Views;
using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Communication.Balance;
using BQJX.Communication.CL2C;
using BQJX.Communication.JoDell;
using BQJX.Communication.Modbus;
using BQJX.Core;
using BQJX.Core.Common;
using BQJX.Core.Interface;
using BQJX.DAL.Base;
using GalaSoft.MvvmLight.Ioc;
using Q_Platform.BLL;
using Q_Platform.DAL;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace BQC_Q48
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private string _axisDataFilePath;
        public string _sampleFile;
        public App()
        {
            string currentPath = Environment.CurrentDirectory;
            _axisDataFilePath = "C:\\Program Files\\AppConfig" + "\\AxisData.ini";
            _sampleFile = currentPath + "\\Sample.xml";

            string connString = ConfigurationManager.ConnectionStrings["ConnStr"].ConnectionString;
            //加载Log配置

            SimpleIoc.Default.Register<ILogger, LoggerHelper>();
            var logger = SimpleIoc.Default.GetInstance<ILogger>();
         
            #region 注册数据业务

            SimpleIoc.Default.Register<IDataAccessBase>(() => new MySqlDataAccessBase(connString));
            SimpleIoc.Default.Register<ICarrierOneDataAccess, CarrierOneDataAccess>();
            SimpleIoc.Default.Register<ICarrierTwoDataAccess, CarrierTwoDataAccess>();
            SimpleIoc.Default.Register<IAddSolidPosDataAccess, AddSolidPosDataAccess>();
            SimpleIoc.Default.Register<ICentrifugalCarrierPosDataAccess, CentrifugalCarrierPosDataAccess>();
            SimpleIoc.Default.Register<IVortexPosDataAccess, VortexPosDataAccess>();
            SimpleIoc.Default.Register<IConcentrationPosDataAccess, ConcentrationPosDataAccess>();
            SimpleIoc.Default.Register<ICapperPosDataAccess, CapperPosDataAccess>();


            SimpleIoc.Default.Register<ITechParamsDataAccess, TechParamsDataAccess>();
            SimpleIoc.Default.Register<ISampleDataAccess, SampleDataAccess>();



            #endregion

            #region 注册基础业务

            SimpleIoc.Default.Register<IGlobalStatus, GlobalStatus>();

            SimpleIoc.Default.Register<ICardBase, CardBase>();
            SimpleIoc.Default.Register<IEtherCATMotion>(() => new EtherCATMotion(GetSevorAxisInfos(), logger));
            SimpleIoc.Default.Register<IIoDevice, IoDevice>();

            SimpleIoc.Default.Register<IModbusBase>(() => new ModbusRtu(GetStepMotionSerialPortInfo(), null), "StepMotion");
            SimpleIoc.Default.Register<IModbusBase>(() => new ModbusRtu(GetClawSerialPortInfo(), null), "Claw");
            SimpleIoc.Default.Register<IModbusBase>(() => new ModbusRtu(GetBalanceSerialPortInfo(), null), "Balance");

            SimpleIoc.Default.Register<ILS_Motion>(() => new LS_Motion(SimpleIoc.Default.GetInstance<IModbusBase>("StepMotion"), logger, GetStepAxisInfos()));
            SimpleIoc.Default.Register<IEPG26>(() => new EPG26(SimpleIoc.Default.GetInstance<IModbusBase>("Claw"), logger));
            SimpleIoc.Default.Register<IWeight>(() => new Weight(SimpleIoc.Default.GetInstance<IModbusBase>("Balance"), logger));

            SimpleIoc.Default.Register<ISyringOne,SyringOne>();
            SimpleIoc.Default.Register<ISyringTwo,SyringTwo>();

          

            #endregion

            #region 注册业务

            //搬运

            SimpleIoc.Default.Register<ICarrierOne, CarrierOne>();  //搬运1
            SimpleIoc.Default.Register<ICarrierTwo, CarrierTwo>();  //搬运2
            SimpleIoc.Default.Register<ICentrifugalCarrier, CentrifugalCarrier>();  //离心搬运
            //加固

            SimpleIoc.Default.Register<IAddSolid, AddSolid>();     //加固
            //拧盖

            SimpleIoc.Default.Register<ICapperOne, CapperOne>();
            SimpleIoc.Default.Register<ICapperTwo, CapperTwo>();
            SimpleIoc.Default.Register<ICapperThree, CapperThree>();
            SimpleIoc.Default.Register<ICapperFour, CapperFour>();
            SimpleIoc.Default.Register<ICapperFive, CapperFive>();
           ////振荡

            SimpleIoc.Default.Register<IVibrationOne, VibrationOne>();
            SimpleIoc.Default.Register<IVibrationTwo, VibrationTwo>();

            //离心

            SimpleIoc.Default.Register<ICentrifugal, Centrifugal>();

            //浓缩

            SimpleIoc.Default.Register<IConcentration, Concentration>();
            //涡旋

            SimpleIoc.Default.Register<IVortex, Vortex>();

            #endregion

            SimpleIoc.Default.Register<IMainPro,MainPro>();
            SimpleIoc.Default.Register<IRunService,RunService>();
        }

      
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ///初始化卡
            SimpleIoc.Default.GetInstance<ICardBase>().Initialize(_axisDataFilePath);

            // if (new LoginWindow().ShowDialog() == true)
            //  {
            new MainWindow().ShowDialog();
            // }

            //保存数据
            GlobalCache.Save();

            //var p = System.Diagnostics.Process.GetCurrentProcess();
            //p.Kill();
            Application.Current.Shutdown(0);

        }

        #region Private Methods


        /// <summary>
        /// 伺服轴齿轮比
        /// </summary>
        /// <returns></returns>
        private List<AxisEleGear> GetSevorAxisInfos()
        {
            return new List<AxisEleGear>()
            {
                new AxisEleGear{ AxisName="提取搬运X轴",AxisNo=0,EleGear = 0.1},
                new AxisEleGear{ AxisName="提取搬运Y轴",AxisNo=1,EleGear = 0.1},
                new AxisEleGear{ AxisName="提取搬运Z1轴",AxisNo=2,EleGear = 0.0787},
                new AxisEleGear{ AxisName="提取搬运Z2轴",AxisNo=3,EleGear = 0.0787},
                new AxisEleGear{ AxisName="提取振荡",AxisNo=4,EleGear = 1,HomeOffset = 0.35},
                new AxisEleGear{ AxisName="加盐Y轴",AxisNo=5,EleGear = 0.1},
                new AxisEleGear{ AxisName="提取移液器",AxisNo=6,EleGear = 2},          //1r == 0.5ml   5000p/r
                new AxisEleGear{ AxisName="离心Z轴",AxisNo=7,EleGear = 0.0787},
                new AxisEleGear{ AxisName="离心机",AxisNo=8,EleGear = 1,Tacc = 5, Tdec = 5,HomeOffset = 0.05},
                new AxisEleGear{ AxisName="净化搬运X轴",AxisNo=9,EleGear = 0.1},
                new AxisEleGear{ AxisName="搬运Y轴 ",AxisNo=10,EleGear = 0.1},
                new AxisEleGear{ AxisName="搬运Z1轴",AxisNo=11,EleGear = 0.0787},
                new AxisEleGear{ AxisName="搬运Z2轴",AxisNo=12,EleGear = 0.0787},
                new AxisEleGear{ AxisName="净化振荡",AxisNo=13,EleGear = 1,HomeOffset = 0.75},
                new AxisEleGear{ AxisName="净化移液器",AxisNo=14,EleGear = 9.45},         //1r  == 0.1058ml   1058p/r
                new AxisEleGear{ AxisName="净化注射器",AxisNo=15,EleGear = 9.45,HomeOffset = -23}        //50ul  == 60mm  ==

            };

        }

        /// <summary>
        /// 步进轴齿轮比
        /// </summary>
        /// <returns></returns>
        private List<StepAxisEleGear> GetStepAxisInfos()
        {
            return new List<StepAxisEleGear>()
            {
                new StepAxisEleGear{ AxisName="加盐Y轴",SlaveId=1,EleGear = 88.2,HomeHigh = 10 ,UpdateParams = true,JogVel =10,JogAccDec =50},   //1r ==113.097 //88.42
                new StepAxisEleGear{ AxisName="加盐C1轴",SlaveId=3,EleGear = 10000},
                new StepAxisEleGear{ AxisName="加盐C2轴",SlaveId=2,EleGear = 10000},
                new StepAxisEleGear{ AxisName="提取加液轴",SlaveId=4,EleGear = 12000},         //1r =5/6ml    10000p/r //HomeOffset =-3000,HomeMode = 2
                new StepAxisEleGear{ AxisName="拧盖1 Y轴",SlaveId=5,EleGear = 787.40},         //1r == 12.7mm   10000p/r
                new StepAxisEleGear{ AxisName="拧盖1 C1轴",SlaveId=6,EleGear = 10000},
                new StepAxisEleGear{ AxisName="拧盖1 C2轴",SlaveId=7,EleGear = 10000},
                new StepAxisEleGear{ AxisName="涡旋Y轴",SlaveId=8,EleGear = 787.40},
                new StepAxisEleGear{ AxisName="拧盖2 Y轴",SlaveId=9,EleGear = 787.40},
                new StepAxisEleGear{ AxisName="拧盖2 C1轴",SlaveId=10,EleGear = 10000},
                new StepAxisEleGear{ AxisName="拧盖2 C2轴",SlaveId=11,EleGear = 10000},
                new StepAxisEleGear{ AxisName="拧盖Z1轴",SlaveId=12,EleGear = 10000,HomeMode = 2},
                new StepAxisEleGear{ AxisName="拧盖Z2轴",SlaveId=13,EleGear = 10000},
                new StepAxisEleGear{ AxisName="离心搬运X轴",SlaveId=14,EleGear = 71.426},
                new StepAxisEleGear{ AxisName="离心搬运C轴",SlaveId=15,EleGear = 10000,HomeMode =6},
                new StepAxisEleGear{ AxisName="拧盖3 Y轴",SlaveId=16,EleGear = 787.40},
                new StepAxisEleGear{ AxisName="拧盖3 C1轴",SlaveId=17,EleGear = 10000},
                new StepAxisEleGear{ AxisName="拧盖3 C2轴",SlaveId=18,EleGear = 10000},
                new StepAxisEleGear{ AxisName="拧盖4 Y轴",SlaveId=19,EleGear = 787.40},
                new StepAxisEleGear{ AxisName="拧盖4 C1轴",SlaveId=20,EleGear = 10000},
                new StepAxisEleGear{ AxisName="拧盖4 C2轴",SlaveId=21,EleGear = 10000},
                new StepAxisEleGear{ AxisName="拧盖5 Y轴",SlaveId=22,EleGear = 787.40},
                new StepAxisEleGear{ AxisName="拧盖5 C1轴",SlaveId=23,EleGear = 10000},
                new StepAxisEleGear{ AxisName="拧盖5 C2轴",SlaveId=24,EleGear = 10000},
                new StepAxisEleGear{ AxisName="浓缩Y轴",SlaveId=25,EleGear = 787.40,HomeMode = 14,HomeHigh = 50,HomeLow = 50},
                new StepAxisEleGear{ AxisName="C加液轴",SlaveId=26,EleGear = 12000},
                new StepAxisEleGear{ AxisName="拧盖Z3轴",SlaveId=29,EleGear = 10000},
                new StepAxisEleGear{ AxisName="拧盖Z4轴",SlaveId=28,EleGear = 10000},
                new StepAxisEleGear{ AxisName="拧盖Z5轴",SlaveId=27,EleGear = 10000}

            };
        }

        /// <summary>
        /// 获取步进一体机串口信息
        /// </summary>
        /// <returns></returns>
        private SerialPortParams GetStepMotionSerialPortInfo()
        {
            return new SerialPortParams()
            {
                PortName = "COM7",
                BaudRate = 115200,
                DataBits = 8,
                StopBits = 1,
                Parity = 0,
            };
        }

        /// <summary>
        /// 获取手爪串口信息
        /// </summary>
        /// <returns></returns>
        private SerialPortParams GetClawSerialPortInfo()
        {
            return new SerialPortParams()
            {
                PortName = "COM8",
                BaudRate = 115200,
                DataBits = 8,
                StopBits = 1,
                Parity = 0,
            };
        }

        /// <summary>
        /// 获取称台串口信息
        /// </summary>
        /// <returns></returns>
        private SerialPortParams GetBalanceSerialPortInfo()
        {
            return new SerialPortParams()
            {
                PortName = "COM9",
                BaudRate = 38400,
                DataBits = 8,
                StopBits = 1,
                Parity = 0,
            };
        }


        #endregion


    }

}
