using Q_Platform.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;
using BQJX.Core.Interface;
using System.Threading;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;

namespace Q_Platform.ViewModels.UC
{
    [AddINotifyPropertyChangedInterface]
    public class IoTestUCViewModel :MyViewModelBase
    {
        #region Private Members

        private readonly IIoDevice _io;
        private readonly ILogger _logger;
        private Task _refreshTask;
        private bool _stopRefresh;
        private bool _refresh;

        #endregion

        #region Properties
       
        public List<IoDefine> InputIoSource { get; set; }

        public List<IoDefine> OutputIoSource { get; set; }

        public double AD_Value1 { get; set; }
        public double AD_Value2 { get; set; }
        public double AD_Value3 { get; set; }
        public double AD_Value4 { get; set; }
        public double AD_Value5 { get; set; }
        public double AD_Value6 { get; set; }
        public double AD_Value7 { get; set; }
        public double AD_Value8 { get; set; }

        public double DA_Value1 { get; set; }
        public double DA_Value2 { get; set; }
        public double DA_Value3 { get; set; }
        public double DA_Value4 { get; set; }
        public double DA_Value5 { get; set; }
        public double DA_Value6 { get; set; }
        public double DA_Value7 { get; set; }
        public double DA_Value8 { get; set; }


        #endregion

        #region Commands
        public ICommand CommandForce { get; set; }


        public ICommand WriteCommand1 { get; set; }
        public ICommand WriteCommand2 { get; set; }
        public ICommand WriteCommand3 { get; set; }
        public ICommand WriteCommand4 { get; set; }
        public ICommand WriteCommand5 { get; set; }
        public ICommand WriteCommand6 { get; set; }
        public ICommand WriteCommand7 { get; set; }
        public ICommand WriteCommand8 { get; set; }


        #endregion

        #region Constructor

        public IoTestUCViewModel(IIoDevice io, IEtherCATMotion motion, ILogger logger)
        {
            _logger = logger;
            _io = io;

            RegisterCommand();
            InitIoDefineData();

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
                        while (_refresh)
                        {
                            Thread.Sleep(1000);
                        }
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        logger?.Error($"_refreshTask err:{ex.Message}");
                    }

                }

            });

           



        } 
        #endregion

        #region Private Methods

        /// <summary>
        /// 注册命令
        /// </summary>
        private void RegisterCommand()
        {
            CommandForce = new RelayCommand<object>(ForceOutput);
            WriteCommand1 = new RelayCommand<object>(WriteChannel1);
            WriteCommand2 = new RelayCommand<object>(WriteChannel2);
            WriteCommand3 = new RelayCommand<object>(WriteChannel3);
            WriteCommand4 = new RelayCommand<object>(WriteChannel4);
            WriteCommand5 = new RelayCommand<object>(WriteChannel5);
            WriteCommand6 = new RelayCommand<object>(WriteChannel6);
            WriteCommand7 = new RelayCommand<object>(WriteChannel7);
            WriteCommand8 = new RelayCommand<object>(WriteChannel8);
        }

        private void WriteChannel1(object obj)
        {
            double value = double.Parse(obj.ToString());
            _io.WriteByte_DA(0, value);
        }

        private void WriteChannel2(object obj)
        {
            double value = double.Parse(obj.ToString());
            _io.WriteByte_DA(1, value);
        }

        private void WriteChannel3(object obj)
        {
            double value = double.Parse(obj.ToString());
            _io.WriteByte_DA(2, value);
        }

        private void WriteChannel4(object obj)
        {
            double value = double.Parse(obj.ToString());
            _io.WriteByte_DA(3, value);
        }

        private void WriteChannel5(object obj)
        {
            double value = double.Parse(obj.ToString());
            _io.WriteByte_DA(4, value);
        }

        private void WriteChannel6(object obj)
        {
            double value = double.Parse(obj.ToString());
            _io.WriteByte_DA(5, value);
        }

        private void WriteChannel7(object obj)
        {
            double value = double.Parse(obj.ToString());
            _io.WriteByte_DA(6, value);
        }

        private void WriteChannel8(object obj)
        {
            double value = double.Parse(obj.ToString());
            _io.WriteByte_DA(7, value);
        }

        /// <summary>
        /// 强制Io输出
        /// </summary>
        /// <param name="obj"></param>
        private void ForceOutput(object obj)
        {
            var ioInfo = obj as IoDefine;
            if (ioInfo == null)
            {
                return;
            }
            var bitNo =OutputIoSource.IndexOf(ioInfo);
            _io.WriteBit_DO((ushort)bitNo, !ioInfo.Value);
        }

        /// <summary>
        /// 初始化Io信息
        /// </summary>
        private void InitIoDefineData()
        {

            InputIoSource = new List<IoDefine>()
            {
                new IoDefine{IoDefineName="离心门开到位",IoAddress="IN0",Value=false},
                new IoDefine{IoDefineName="离心门关到位",IoAddress="IN1",Value=false},
                new IoDefine{IoDefineName="离心搬运Y气缸原位",IoAddress="IN2",Value=false},
                new IoDefine{IoDefineName="离心搬运Y气缸到位",IoAddress="IN3",Value=false},
                new IoDefine{IoDefineName="急停",IoAddress="IN4",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="IN5",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="IN6",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="IN7",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I0.0",Value=false},
                new IoDefine{IoDefineName="加盐Z1气缸下位",IoAddress="I0.1",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I0.2",Value=false},
                new IoDefine{IoDefineName="加盐Z2气缸下位",IoAddress="I0.3",Value=false},
                new IoDefine{IoDefineName="X气缸到位",IoAddress="I0.4",Value=false},
                new IoDefine{IoDefineName="X气缸原位",IoAddress="I0.5",Value=false},
                new IoDefine{IoDefineName="料仓到位感应",IoAddress="I0.6",Value=false},
                new IoDefine{IoDefineName="振荡抱夹气缸到位",IoAddress="I0.7",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I1.0",Value=false},
                new IoDefine{IoDefineName="涡旋气缸上位",IoAddress="I1.1",Value=false},
                new IoDefine{IoDefineName="涡旋气缸下位",IoAddress="I1.2",Value=false},
                new IoDefine{IoDefineName="拆盖1抱夹到位",IoAddress="I1.3",Value=false},
                new IoDefine{IoDefineName="拆盖1抱夹原位",IoAddress="I1.4",Value=false},
                new IoDefine{IoDefineName="拆盖1检盖光纤",IoAddress="I1.5",Value=false},
                new IoDefine{IoDefineName="拆盖2抱夹到位",IoAddress="I1.6",Value=false},
                new IoDefine{IoDefineName="拆盖2抱夹原位",IoAddress="I1.7",Value=false},
                new IoDefine{IoDefineName="拆盖2检盖光纤",IoAddress="I2.0",Value=false},
                new IoDefine{IoDefineName="试管架1检测",IoAddress="I2.1",Value=false},
                new IoDefine{IoDefineName="试管架2检测",IoAddress="I2.2",Value=false},
                new IoDefine{IoDefineName="试管架3检测",IoAddress="I2.3",Value=false},
                new IoDefine{IoDefineName="试管架4检测",IoAddress="I2.4",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I2.5",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I2.6",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I2.7",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I3.0",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I3.1",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I3.2",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I3.3",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I3.4",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I3.5",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I3.6",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="I3.7",Value=false},
                new IoDefine{IoDefineName="振荡抱夹到位", IoAddress="I0.0",Value=false},
                new IoDefine{IoDefineName="备用", IoAddress="I0.1",Value=false},
                new IoDefine{IoDefineName="拆盖3抱夹到位",      IoAddress="I0.2",Value=false},
                new IoDefine{IoDefineName="拆盖3抱夹原位",  IoAddress="I0.3",Value=false},
                new IoDefine{IoDefineName="拆盖3检盖光纤",  IoAddress="I0.4",Value=false},
                new IoDefine{IoDefineName="拆盖4抱夹到位",IoAddress="I0.5",Value=false},
                new IoDefine{IoDefineName="拆盖4抱夹原位",IoAddress="I0.6",Value=false},
                new IoDefine{IoDefineName="拆盖4检盖光纤", IoAddress="I0.7",Value=false},
                new IoDefine{IoDefineName="拆盖5抱夹到位", IoAddress="I1.0",Value=false},
                new IoDefine{IoDefineName="拆盖5抱夹原位", IoAddress="I1.1",Value=false},
                new IoDefine{IoDefineName="拆盖5检盖光纤", IoAddress="I1.2",Value=false},
                new IoDefine{IoDefineName="注射器气缸上位", IoAddress="I1.3",Value=false},
                new IoDefine{IoDefineName="备用", IoAddress="I1.4",Value=false},
                new IoDefine{IoDefineName="备用",   IoAddress="I1.5",Value=false},
                new IoDefine{IoDefineName="备用",   IoAddress="I1.6",Value=false},
                new IoDefine{IoDefineName="浓缩气缸下位",   IoAddress="I1.7",Value=false},
                new IoDefine{IoDefineName="浓缩气缸上位",   IoAddress="I2.0",Value=false},
                new IoDefine{IoDefineName="浓缩真空1",   IoAddress="I2.1",Value=false},
                new IoDefine{IoDefineName="浓缩真空2",   IoAddress="I2.2",Value=false},
                new IoDefine{IoDefineName="备用",   IoAddress="I2.3",Value=false},
                new IoDefine{IoDefineName="试管架1检测",   IoAddress="I2.4",Value=false},
                new IoDefine{IoDefineName="试管架2检测",   IoAddress="I2.5",Value=false},
                new IoDefine{IoDefineName="试管架3检测",   IoAddress="I2.6",Value=false},
                new IoDefine{IoDefineName="试管架4检测",   IoAddress="I2.7",Value=false},
                new IoDefine{IoDefineName="备用",   IoAddress="I3.0",Value=false},
                new IoDefine{IoDefineName="备用",   IoAddress="I3.1",Value=false},
                new IoDefine{IoDefineName="备用",   IoAddress="I3.2",Value=false},
                new IoDefine{IoDefineName="备用",   IoAddress="I3.3",Value=false},
                new IoDefine{IoDefineName="备用",   IoAddress="I3.4",Value=false},
                new IoDefine{IoDefineName="备用",   IoAddress="I3.5",Value=false},
                new IoDefine{IoDefineName="备用",   IoAddress="I3.6",Value=false},
                new IoDefine{IoDefineName="备用",   IoAddress="I3.7",Value=false}
               

            };

            OutputIoSource = new List<IoDefine>()
            {
                new IoDefine{IoDefineName="离心机门开控制" ,IoAddress="OUT0",Value=false},
                new IoDefine{IoDefineName="离心机门关控制" ,IoAddress="OUT1",Value=false},
                new IoDefine{IoDefineName="Y气缸控制",IoAddress="OUT2",Value=false},
                new IoDefine{IoDefineName="照明" ,IoAddress="OUT3",Value=false},
                new IoDefine{IoDefineName="备用" ,IoAddress="OUT4",Value=false},
                new IoDefine{IoDefineName="备用" ,IoAddress="OUT5",Value=false},
                new IoDefine{IoDefineName="备用" ,IoAddress="OUT6",Value=false},
                new IoDefine{IoDefineName="备用" ,IoAddress="OUT7",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="Q0.0",Value=false},
                new IoDefine{IoDefineName="加盐X气缸控制",IoAddress="Q0.1",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="Q0.2",Value=false},
                new IoDefine{IoDefineName="加盐Z1气缸控制"        ,IoAddress="Q0.3",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="Q0.4",Value=false},
                new IoDefine{IoDefineName="加盐Z2气缸控制"         ,IoAddress="Q0.5",Value=false},
                new IoDefine{IoDefineName="振荡抱夹控制" ,IoAddress="Q0.6",Value=false},
                new IoDefine{IoDefineName="涡旋下压气缸控制",IoAddress="Q0.7",Value=false},
                new IoDefine{IoDefineName="拆盖1抱夹控制",IoAddress="Q1.0",Value=false},
                new IoDefine{IoDefineName="拆盖1手爪控制",IoAddress="Q1.1",Value=false},
                new IoDefine{IoDefineName="备用"         ,IoAddress="Q1.2",Value=false},
                new IoDefine{IoDefineName="拆盖2抱夹控制",IoAddress="Q1.3",Value=false},
                new IoDefine{IoDefineName="拆盖2手爪控制",IoAddress="Q1.4",Value=false},
                new IoDefine{IoDefineName="备用"         ,IoAddress="Q1.5",Value=false},
                new IoDefine{IoDefineName="冰浴水冷控制",IoAddress="Q1.6",Value=false},
                new IoDefine{IoDefineName="备用",IoAddress="Q1.7",Value=false},
                new IoDefine{IoDefineName="加液阀1"      ,IoAddress="Q2.0",Value=false},
                new IoDefine{IoDefineName="加液阀2"      ,IoAddress="Q2.1",Value=false},
                new IoDefine{IoDefineName="加液阀3"      ,IoAddress="Q2.2",Value=false},
                new IoDefine{IoDefineName="加液阀4"      ,IoAddress="Q2.3",Value=false},
                new IoDefine{IoDefineName="加液阀5"      ,IoAddress="Q2.4",Value=false},
                new IoDefine{IoDefineName="加液阀6"      ,IoAddress="Q2.5",Value=false},
                new IoDefine{IoDefineName="加液阀7"      ,IoAddress="Q2.6",Value=false},
                new IoDefine{IoDefineName="加液阀8"      ,IoAddress="Q2.7",Value=false},
                new IoDefine{IoDefineName="涡旋1"         ,IoAddress="Q3.0",Value=false},
                new IoDefine{IoDefineName="涡旋2"         ,IoAddress="Q3.1",Value=false},
                new IoDefine{IoDefineName="照明"         ,IoAddress="Q3.2",Value=false},
                new IoDefine{IoDefineName="备用"              ,IoAddress="Q3.3",Value=false},
                new IoDefine{IoDefineName="备用"   ,IoAddress="Q3.4",Value=false},
                new IoDefine{IoDefineName="备用" ,IoAddress="Q3.5",Value=false},
                new IoDefine{IoDefineName="备用" ,IoAddress="Q3.6",Value=false},
                new IoDefine{IoDefineName="备用" , IoAddress="Q3.7",Value=false},
                new IoDefine{IoDefineName="振荡抱夹控制" , IoAddress="Q0.0",Value=false},
                new IoDefine{IoDefineName="拆盖3抱夹控制" ,      IoAddress="Q0.1",Value=false},
                new IoDefine{IoDefineName="拆盖3手爪控制" ,  IoAddress="Q0.2",Value=false},
                new IoDefine{IoDefineName="备用",  IoAddress="Q0.3",Value=false},
                new IoDefine{IoDefineName="拆盖4抱夹控制" ,IoAddress="Q0.4",Value=false},
                new IoDefine{IoDefineName="拆盖4手爪控制" ,IoAddress="Q0.5",Value=false},
                new IoDefine{IoDefineName="备用" , IoAddress="Q0.6",Value=false},
                new IoDefine{IoDefineName="拆盖5抱夹控制" , IoAddress="Q0.7",Value=false},
                new IoDefine{IoDefineName="拆盖5手爪控制" , IoAddress="Q1.0",Value=false},
                new IoDefine{IoDefineName="备用" , IoAddress="Q1.1",Value=false},
                new IoDefine{IoDefineName="备用", IoAddress="Q1.2",Value=false},
                new IoDefine{IoDefineName="搬运Z气缸控制", IoAddress="Q1.3",Value=false},
                new IoDefine{IoDefineName="浓缩1真空控制" ,   IoAddress="Q1.4",Value=false},
                new IoDefine{IoDefineName="浓缩2真空控制" ,   IoAddress="Q1.5",Value=false},
                new IoDefine{IoDefineName="浓缩吹气控制" ,   IoAddress="Q1.6",Value=false},
                new IoDefine{IoDefineName="浓缩Z气缸上控制" ,   IoAddress="Q1.7",Value=false},
                new IoDefine{IoDefineName="浓缩Z气缸下控制" ,   IoAddress="Q2.0",Value=false},
                new IoDefine{IoDefineName="加标蠕动泵控制" ,   IoAddress="Q2.1",Value=false},
                new IoDefine{IoDefineName="加液阀1" ,   IoAddress="Q2.2",Value=false},
                new IoDefine{IoDefineName="加液阀2" ,   IoAddress="Q2.3",Value=false},
                new IoDefine{IoDefineName="加液阀3" ,   IoAddress="Q2.4",Value=false},
                new IoDefine{IoDefineName="加液阀4" ,   IoAddress="Q2.5",Value=false},
                new IoDefine{IoDefineName="加液阀5" ,   IoAddress="Q2.6",Value=false},
                new IoDefine{IoDefineName="加液阀6" ,   IoAddress="Q2.7",Value=false},  
                new IoDefine{IoDefineName="加液阀7" ,   IoAddress="Q3.0",Value=false},
                new IoDefine{IoDefineName="加液阀8" ,   IoAddress="Q3.1",Value=false},
                new IoDefine{IoDefineName="浓缩电机1" ,   IoAddress="Q3.2",Value=false},
                new IoDefine{IoDefineName="浓缩电机2" ,   IoAddress="Q3.3",Value=false},
                new IoDefine{IoDefineName="照明" ,   IoAddress="Q3.4",Value=false},
                new IoDefine{IoDefineName="样品冷却蠕动泵" ,   IoAddress="Q3.5",Value=false},
                new IoDefine{IoDefineName="标准冷却蠕动泵" ,   IoAddress="Q3.6",Value=false},
                new IoDefine{IoDefineName="备用" ,   IoAddress="Q3.7",Value=false},
            };

            _io.Config_AD(1008, 2, 1, 5, 10);
            _io.Config_AD(1008, 2, 2, 5, 10);
            _io.Config_AD(1008, 2, 3, 5, 10);
            _io.Config_AD(1008, 2, 4, 5, 10);
            _io.Config_AD(1011, 2, 1, 5, 10);
            _io.Config_AD(1011, 2, 2, 5, 10);
            _io.Config_AD(1011, 2, 3, 5, 10);
            _io.Config_AD(1011, 2, 4, 5, 10);

            _io.Config_DA(1008, 3, 1, 0, 1);
            _io.Config_DA(1008, 3, 2, 0, 1);
            _io.Config_DA(1008, 3, 3, 0, 1);
            _io.Config_DA(1008, 3, 4, 0, 1);

            _io.Config_DA(1011, 3, 1, 0, 1);
            _io.Config_DA(1011, 3, 2, 0, 1);
            _io.Config_DA(1011, 3, 3, 0, 1);
            _io.Config_DA(1011, 3, 4, 0, 1);




        }

        /// <summary>
        /// 刷新Io状态
        /// </summary>
        private void RefreshIoStatus()
        {
            var io1 = _io.ReadByte_DI(0);
            var io2 = _io.ReadByte_DI(1);
            var io3 = _io.ReadByte_DI(2);

            var o0 = _io.ReadByte_DO(0);
            var o1 = _io.ReadByte_DO(1);
            var o2 = _io.ReadByte_DO(2);

            for (int i = 0; i < InputIoSource.Count; i++)
            {
                uint bitR = (uint)(Math.Pow(2, i%32));
                if (i<32)
                {
                    InputIoSource[i].Value = (io1 & bitR) != bitR;
                }
                else if(i>=32 && i<64)
                {
                    InputIoSource[i].Value = (io2 & bitR) != bitR;
                }
                else if(i>=64 && i<96)
                {
                    InputIoSource[i].Value = (io3 & bitR) != bitR;
                }
            }

            for (int i = 0; i < OutputIoSource.Count; i++)
            {
                uint bitR = (uint)(Math.Pow(2, i % 32));
                if (i < 32)
                {
                    OutputIoSource[i].Value = (o0 & bitR) != bitR;
                }
                else if (i >= 32 && i < 64)
                {
                    OutputIoSource[i].Value = (o1 & bitR) != bitR;
                }
                else if (i >= 64 && i < 96)
                {
                    OutputIoSource[i].Value = (o2 & bitR) != bitR;
                }
            }

            AD_Value1 = _io.ReadByte_AD(0) * 0.00468 -50;
            //AD_Value2 = _io.ReadByte_AD(1) * 0.00468 -50;
            //AD_Value3 = _io.ReadByte_AD(2) * 0.00468 -50;
            //AD_Value4 = _io.ReadByte_AD(3) * 0.00468 -50;
            AD_Value5 = _io.ReadByte_AD(4) * 0.00468 -50;
            AD_Value6 = _io.ReadByte_AD(5) * 0.00468 -50;
            //AD_Value7 = _io.ReadByte_AD(6) * 0.00468 -50;
            //AD_Value8 = _io.ReadByte_AD(7) * 0.00468 - 50;

            DA_Value1 = _io.ReadDoubleDA(0);
            DA_Value2 = _io.ReadDoubleDA(1);
            DA_Value3 = _io.ReadDoubleDA(2);
            DA_Value4 = _io.ReadDoubleDA(3);
            DA_Value5 = _io.ReadDoubleDA(4);
            DA_Value6 = _io.ReadDoubleDA(5);
            DA_Value7 = _io.ReadDoubleDA(6);
            DA_Value8 = _io.ReadDoubleDA(7);

        }

        #endregion

        public override void Cleanup()
        {
            _stopRefresh = true;
            base.Cleanup();

        }




    }

    [AddINotifyPropertyChangedInterface]
    public class IoDefine
    {

        [DoNotNotify]
        public string IoAddress { get; set; }
        
        public bool Value { get; set; }

        [DoNotNotify]
        public string IoDefineName { get; set; }
    }


}
