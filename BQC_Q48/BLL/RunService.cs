using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class RunService : IRunService
    {
        #region Events

        public event Action<string> AlmOccuCallBack;
        public event Action<string> AddSaltPauseEventHandler;
        public event Action<string> AddSaltContinueEventHandler;
        public event Action<string> CarrierOnePauseEventHandler;
        public event Action<string> CarrierOneContinueEventHandler;
        public event Action<string> CarrierTwoPauseEventHandler;
        public event Action<string> CarrierTwoContinueEventHandler;
        public event Action EmgStopOccuEventArgs;

        public event Action<double[]> TemperatureChangedEventHandler;

        #endregion

        #region Private Members

        private readonly IIoDevice _io;
        private readonly IEtherCATMotion _motion;
        private readonly ILS_Motion _iLsMotion;
        private readonly IGlobalStatus _globalStatus;
        private readonly ILogger _logger;
        private Task _task;
        private Task _almTask;
        private bool _runFlag;

        private ushort pump1 = 22;
        private ushort pump2 = 70;
        private ushort pump3 = 69;
        private ushort _emgSersor = 4;


        private bool pumpRunning1;
        private bool pumpRunning2;
        private bool pumpRunning3;

        private bool IsAlmOccu;
        private int _almDealStatus;

        private string _almCode;

        private int _almAxisType;
        private ushort _almServoAxisNo;
        private ushort _almStepAxisNo;

        //感应器临时变量
        private bool _saltSensorTemp;       //盐料仓到位检测    I06 
        private bool _shelfSensor1Temp = true;     //试管架1到位检测   I21
        private bool _shelfSensor2Temp = true;      //试管架2到位检测   I22
        private bool _shelfSensor3Temp = true;      //试管架3到位检测   I23
        private bool _shelfSensor4Temp = true;     //试管架2-1到位检测   I24 
        private bool _tipShelfTemp = true;         //枪头架到位检测    I25
        private bool _shelfSensor5Temp = true;     //试管架2-2到位检测   I25 
        private bool _shelfSensor6Temp = true;     //试管架2-3到位检测   I26 
        private bool _shelfSensor7Temp = true;     //试管架2-4到位检测   I27 
        private bool _shelfSensor8Temp = true;     

        private bool _emgOccuTemp = true; //急停发生

        #endregion

        #region Private Variant

        private ushort _saltSensor = 14;       //盐料仓到位检测    I06 
        private ushort _shelfSensor1 = 25;     //试管架1到位检测   I21
        private ushort _shelfSensor2 = 26;     //试管架2到位检测   I22
        private ushort _shelfSensor3 = 27;     //试管架3到位检测   I23
        private ushort _shelfSensor4 = 28;     //试管架3到位检测   I24
        private ushort _tipShelf = 29;         //枪头架到位检测    I25
        private ushort _shelf2Sensor1 = 60;     //试管架2-1到位检测   I24 
        private ushort _shelf2Sensor2 = 61;     //试管架2-2到位检测   I25 
        private ushort _shelf2Sensor3 = 62;     //试管架2-3到位检测   I26 
        private ushort _shelf2Sensor4 = 63;     //试管架2-4到位检测   I27 

        #endregion

        #region Properties

        public double AD_Value1 { get; set; }
        public double AD_Value5 { get; set; }
        public double AD_Value6 { get; set; }

        public double SetTemperature1 { get; set; } = 0;
        public double SetTemperature2 { get; set; } = 0;
        public double SetTemperature3 { get; set; } = 0;

        public bool IsTemperatureCtl { get; set; } = true;

        /// <summary>
        /// 使能试管架到位检测暂停
        /// </summary>
        public bool EnableCheckSensor { get; set; } = true;

        #endregion

        #region Construtors

        public RunService(IEtherCATMotion motion, ILS_Motion iLsMotion, IIoDevice io, IGlobalStatus globalStatus)
        {
            this._motion = motion;
            this._iLsMotion = iLsMotion;
            this._io = io;
            this._globalStatus = globalStatus;
            this._logger = new MyLogger(typeof(RunService));
        }


        #endregion

        #region Public Methods

        /// <summary>
        /// 启动监控程序
        /// </summary>
        /// <returns></returns>
        public Task Run()
        {
            InitDA();
            if (_task != null)
            {
                if (!_task.IsCompleted)
                {
                    return _task;
                }
            }

            _task = Task.Run(() =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                
                while (!_runFlag)
                {
                    //温控程序
                    TemperatrueControl();

                    //急停
                    EmgMonitor();

                    //报警监控程序  异步执行
                    ReadAlm();

                    if (IsAlmOccu && _almDealStatus == 0)
                    {
                        AlmOccuCallBack?.Invoke(_almCode);
                        _almDealStatus = 1;
                        _globalStatus.PauseProgram();
                    }

                    //检测试管架到位情况
                    if (EnableCheckSensor)
                    {
                        CheckAddSaltSensor();
                        CheckShelf1Sensor();
                        CheckShelf2Sensor();
                    }

                    Thread.Sleep(0);
                }


            });
           
            return _task;
        }

        private void EmgMonitor()
        {
            bool emg = _io.ReadBit_DI(_emgSersor);
            if (_emgOccuTemp && !emg)
            {
                EmgStopOccuEventArgs?.Invoke();
                _emgOccuTemp = false;
            }
            if (!_emgOccuTemp && emg)
            {
                _emgOccuTemp = true;
            }
        }

        private void TemperatrueControl()
        {
            var t1 = Math.Round(_io.ReadByte_AD(0) * 0.00468 - 50, 1);
            var t2 = Math.Round(_io.ReadByte_AD(4) * 0.00468 - 50, 1);
            var t3 = Math.Round(_io.ReadByte_AD(5) * 0.00468 - 50, 1);
            if (t1 != AD_Value1 || t2 != AD_Value5 || t3 != AD_Value6)
            {
                AD_Value1 = t1;
                AD_Value5 = t2;
                AD_Value6 = t3;
                double[] tArr = new double[] { t1, t2, t3 };
                TemperatureChangedEventHandler?.Invoke(tArr);
            }
            if (AD_Value1 > SetTemperature1 && !pumpRunning1 && IsTemperatureCtl)
            {
                _io.WriteBit_DO_Delay_Reverse(pump1, 20);
                pumpRunning1 = true;
            }
            if (AD_Value5 > SetTemperature2 && !pumpRunning2 && IsTemperatureCtl)
            {
                _io.WriteBit_DO_Delay_Reverse(pump2, 20);
                pumpRunning2 = true;
            }
            if (AD_Value6 > SetTemperature3 && !pumpRunning3 && IsTemperatureCtl)
            {
                _io.WriteBit_DO_Delay_Reverse(pump3, 20);
                pumpRunning3 = true;
            }

            pumpRunning1 = _io.ReadBit_DO(pump1);
            pumpRunning2 = _io.ReadBit_DO(pump2);
            pumpRunning3 = _io.ReadBit_DO(pump3);

            if (pumpRunning1 && AD_Value1 + 2 < SetTemperature1)
            {
                _io.WriteBit_DO(pump1, false);
            }
            if (pumpRunning2 && AD_Value5 + 2 < SetTemperature2)
            {
                _io.WriteBit_DO(pump2, false);
            }
            if (pumpRunning3 && AD_Value6 + 2 < SetTemperature3)
            {
                _io.WriteBit_DO(pump3, false);
            }

        }

        /// <summary>
        /// 停止监控程序
        /// </summary>
        public void StopPro()
        {
            _runFlag = true;
        }

        /// <summary>
        /// 复位报警
        /// </summary>
        public void ResetAlm()
        {
            if (_almAxisType == 0)
            {
                _motion.ResetAxisAlm(_almServoAxisNo);
            }
            else
            {
                if (_almStepAxisNo<1 || _almStepAxisNo >32)
                {
                    return;
                }
                _iLsMotion.ResetAxisAlm(_almStepAxisNo);
            }
            IsAlmOccu = false;
            _almDealStatus = 0;
        }



        #endregion

        #region Private Methods

        private void InitDA()
        {
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


        private Task ReadAlm()
        {
            if (_almTask != null)
            {
                if (!_almTask.IsCompleted)
                {
                    return _almTask;
                }
            }
            _almTask = Task.Run(() =>
            {
                var list1 = _motion.GetAxisInfos();
                var list2 = _iLsMotion.GetAxisInfos();
                //伺服报警检测
                foreach (var item in list1)
                {
                    var status = _motion.GetAxisAlmCode(item.AxisNo);
                    if (status != 0)
                    {
                        IsAlmOccu = true;
                        _almCode = $"伺服电机{item.AxisName} err:{status.ToString()}";
                        _almAxisType = 0;
                        _almServoAxisNo = item.AxisNo;
                        return;
                    }
                }
                //步进报警监测
                foreach (var item in list2)
                {
                    var status = _iLsMotion.ReadAlmCode(item.SlaveId).GetAwaiter().GetResult();
                    if (status != 0)
                    {
                        IsAlmOccu = true;
                        _almCode = $"步进电机{item.AxisName} err:{status.ToString()}";
                        _almAxisType = 1;
                        _almStepAxisNo = item.SlaveId;
                        return;
                    }
                    Thread.Sleep(1000);
                }

            });
            return _almTask;
        }

        /// <summary>
        /// 加盐料仓检测到位
        /// </summary>
        private void CheckAddSaltSensor()
        {
            try
            {
                var b = _io.ReadBit_DI(_saltSensor);
                if (b && !_saltSensorTemp) //检测上升沿
                {
                    //AddSaltContinueEventHandler?.Invoke("盐料仓到位,程序继续!");
                }
                else if (!b && _saltSensorTemp) //检测下降沿
                {
                    _logger?.Warn("加盐料仓未到位!");
                    AddSaltPauseEventHandler?.Invoke("盐料仓未到位,程序暂停!");
                }
                _saltSensorTemp = b;
            }
            catch (Exception)
            {
            }
          
        }  
        
        /// <summary>
        /// 试管1料仓检测到位  搬运1
        /// </summary>
        private void CheckShelf1Sensor()
        {
            try
            {
                var b = _io.ReadBit_DI(_shelfSensor1);
                if (b && !_shelfSensor1Temp)
                {
                    //CarrierOneContinueEventHandler?.Invoke("试管料仓到位,程序继续!");
                }
                else if (!b && _shelfSensor1Temp)
                {
                    _logger?.Warn("试管架1-1未到位!");
                    CarrierOnePauseEventHandler?.Invoke("试管料仓未到位,程序暂停!");
                }
                _shelfSensor1Temp = b;

                var b2 = _io.ReadBit_DI(_shelfSensor2);
                if (b2 && !_shelfSensor2Temp) //检测上升沿
                {
                    //CarrierOneContinueEventHandler?.Invoke("试管料仓到位,程序继续!");
                }
                else if (!b2 && _shelfSensor2Temp) //检测下降沿
                {
                    _logger?.Warn("试管架1-2未到位!");
                    CarrierOnePauseEventHandler?.Invoke("试管料仓未到位,程序暂停!");
                }
                _shelfSensor2Temp = b2;

                var b3 = _io.ReadBit_DI(_shelfSensor3);
                if (b3 && !_shelfSensor3Temp) //检测上升沿
                {
                    // CarrierOneContinueEventHandler?.Invoke("试管料仓到位,程序继续!");
                }
                else if (!b3 && _shelfSensor3Temp) //检测下降沿
                {
                    _logger?.Warn("试管架1-3未到位!");
                    CarrierOnePauseEventHandler?.Invoke("试管料仓未到位,程序暂停!");
                }
                _shelfSensor3Temp = b3;

                var b4 = _io.ReadBit_DI(_shelfSensor4);
                if (b4 && !_shelfSensor4Temp) //检测上升沿
                {
                    //CarrierOneContinueEventHandler?.Invoke("试管料仓到位,程序继续!");
                }
                else if (!b4 && _shelfSensor4Temp) //检测下降沿
                {
                    _logger?.Warn("试管架1-4未到位!");
                    CarrierOnePauseEventHandler?.Invoke("试管料仓未到位,程序暂停!");
                }
                _shelfSensor4Temp = b4;

                var b5 = _io.ReadBit_DI(_tipShelf);
                if (b5 && !_tipShelfTemp) //检测上升沿
                {
                    //CarrierOneContinueEventHandler?.Invoke("枪头架到位,程序继续!");
                }
                else if (!b5 && _tipShelfTemp) //检测下降沿
                {
                    _logger?.Warn("枪头架未到位!");
                    CarrierOnePauseEventHandler?.Invoke("枪头架未到位,程序暂停!");
                }
                _tipShelfTemp = b5;

            }
            catch (Exception)
            {
            }
        }
        
        /// <summary>
        /// 试管2料仓检测到位  搬运2
        /// </summary>
        private void CheckShelf2Sensor()
        {
            try
            {
                var b = _io.ReadBit_DI(_shelf2Sensor1);
                var b2 =_io.ReadBit_DI(_shelf2Sensor2);
                var b3 =_io.ReadBit_DI(_shelf2Sensor3);
                var b4 = _io.ReadBit_DI(_shelf2Sensor4);

                if (b && !_shelfSensor5Temp) //检测上升沿
                {
                    //CarrierTwoContinueEventHandler?.Invoke("试管料仓到位,程序继续!");
                }
                else if (!b && _shelfSensor5Temp) //检测下降沿
                {
                    _logger?.Warn("试管架2-1未到位!");
                    CarrierTwoPauseEventHandler?.Invoke("试管料仓未到位,程序暂停!");
                }
                _shelfSensor5Temp = b;

                if (b2 && !_shelfSensor6Temp)
                {
                    //CarrierTwoContinueEventHandler?.Invoke("试管料仓到位,程序继续!");
                }
                else if (!b2 && _shelfSensor6Temp)
                {
                    _logger?.Warn("试管架2-2未到位!");
                    CarrierTwoPauseEventHandler?.Invoke("试管料仓未到位,程序暂停!");
                }
                _shelfSensor6Temp = b2;

                if (b3 && !_shelfSensor7Temp)
                {
                    //CarrierTwoContinueEventHandler?.Invoke("试管料仓到位,程序继续!");
                }
                else if (!b3 && _shelfSensor7Temp)
                {
                    _logger?.Warn("试管架2-3未到位!");
                    CarrierTwoPauseEventHandler?.Invoke("试管料仓未到位,程序暂停!");
                }
                _shelfSensor7Temp = b3;

                if (b4 && !_shelfSensor8Temp)
                {
                    //CarrierTwoContinueEventHandler?.Invoke("试管料仓到位,程序继续!");
                }
                else if (!b4 && _shelfSensor8Temp)
                {
                    _logger?.Warn("试管架2-4未到位!");
                    CarrierTwoPauseEventHandler?.Invoke("试管料仓未到位,程序暂停!");
                }
                _shelfSensor8Temp = b4;



            }
            catch (Exception)
            {
            }
        }


        #endregion

    }
}
