using BQJX.Common.Interface;
using BQJX.Core.Interface;
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

        #endregion

        #region Private Members

        private readonly IIoDevice _io;
        private readonly IEtherCATMotion _motion;
        private readonly ILS_Motion _iLsMotion;
        private readonly IGlobalStatus _globalStatus;
        private Task _task;
        private Task _almTask;
        private bool _runFlag;

        private ushort pump1 = 22;
        private ushort pump2 = 69;
        private ushort pump3 = 70;


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
        private bool _shelfSensor1Temp;     //试管架1到位检测   I21
        private bool _shelfSensor2Temp;     //试管架2到位检测   I22
        private bool _shelfSensor3Temp;     //试管架3到位检测   I23
        private bool _tipShelfTemp;         //枪头架到位检测    I25
        private bool _shelfSensor4Temp;     //试管架2-1到位检测   I24 
        private bool _shelfSensor5Temp;     //试管架2-2到位检测   I25 
        private bool _shelfSensor6Temp;     //试管架2-3到位检测   I26 
        private bool _shelfSensor7Temp;     //试管架2-4到位检测   I27 



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
                    AD_Value1 = _io.ReadByte_AD(0) * 0.00468 - 50;
                    AD_Value5 = _io.ReadByte_AD(4) * 0.00468 - 50;
                    AD_Value6 = _io.ReadByte_AD(5) * 0.00468 - 50;
                    if (AD_Value1 > SetTemperature1 && !pumpRunning1 && IsTemperatureCtl)
                    {
                        _io.WriteBit_DO_Delay_Reverse(pump1, 60);
                    }
                    if (AD_Value5 > SetTemperature2 && !pumpRunning2 && IsTemperatureCtl)
                    {
                        _io.WriteBit_DO_Delay_Reverse(pump2, 60);
                    }
                    if (AD_Value6 > SetTemperature3 && !pumpRunning3 && IsTemperatureCtl)
                    {
                        _io.WriteBit_DO_Delay_Reverse(pump3, 60);
                    }

                    pumpRunning1 = _io.ReadBit_DO(pump1);
                    pumpRunning2 = _io.ReadBit_DO(pump2);
                    pumpRunning3 = _io.ReadBit_DO(pump3);

                    //报警程序
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
                    //Thread.Sleep(1000);
                }
                foreach (var item in list2)
                {
                    var status = _iLsMotion.ReadAlmCode(item.SlaveId).GetAwaiter().GetResult();
                    if (status != 0)
                    {
                        IsAlmOccu = true;
                        _almCode = $"步进电机{item.AxisName} err:{status.ToString()}";
                        _almAxisType = 1;
                        _almServoAxisNo = item.SlaveId;
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
                    AddSaltContinueEventHandler?.Invoke("盐料仓到位,程序继续!");
                }
                else if (!b && _saltSensorTemp) //检测下降沿
                {
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
                var b = !_io.ReadBit_DI(_shelfSensor1) || !_io.ReadBit_DI(_shelfSensor2) || !_io.ReadBit_DI(_shelfSensor3)
                    || !_io.ReadBit_DI(_shelfSensor4) || !_io.ReadBit_DI(_tipShelf);

                var b2 = _io.ReadBit_DI(_shelfSensor1) && _io.ReadBit_DI(_shelfSensor2) && _io.ReadBit_DI(_shelfSensor3)
                    && _io.ReadBit_DI(_shelfSensor4) && _io.ReadBit_DI(_tipShelf);

                if (b2 && !_shelfSensor1Temp) //检测上升沿
                {
                    CarrierOneContinueEventHandler?.Invoke("试管料仓到位,程序继续!");
                    _shelfSensor1Temp = b2;
                }
                else if (!b && _shelfSensor1Temp) //检测下降沿
                {
                    CarrierOnePauseEventHandler?.Invoke("试管料仓未到位,程序暂停!");
                    _shelfSensor1Temp = b;
                }
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
                var b = !_io.ReadBit_DI(_shelf2Sensor1) || !_io.ReadBit_DI(_shelf2Sensor2) || !_io.ReadBit_DI(_shelf2Sensor3)
                    || !_io.ReadBit_DI(_shelf2Sensor4) ;

                var b2 = _io.ReadBit_DI(_shelf2Sensor1) && _io.ReadBit_DI(_shelf2Sensor2) && _io.ReadBit_DI(_shelf2Sensor3)
                    && _io.ReadBit_DI(_shelf2Sensor4);

                if (b2 && !_shelfSensor1Temp) //检测上升沿
                {
                    CarrierTwoContinueEventHandler?.Invoke("试管料仓到位,程序继续!");
                    _shelfSensor1Temp = b2;
                }
                else if (!b && _shelfSensor1Temp) //检测下降沿
                {
                    CarrierTwoPauseEventHandler?.Invoke("试管料仓未到位,程序暂停!");
                    _shelfSensor1Temp = b;
                }
            }
            catch (Exception)
            {
            }
        }


        #endregion

    }
}
