using BQJX.Common.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BQJX.Common.Common
{
    public class GlobalStatus : IGlobalStatus
    {
        private bool _stop;
        private bool _pause;
        private bool _emgStop;
        /// <summary>
        /// 状态机 bit0:初始化中 bit1:急停中 2:自动运行中 3:待机中  //互斥
        /// 8:故障 9:已回零 10:停机中 11:暂停中                       //叠加
        /// </summary>
        private int _machineStatus = 0x08; //待机

        #region Events

        public event Func<bool> StopProgramEventArgs;

        public event Func<bool> PauseProgramEventArgs;

        public event Func<bool> ContinueProgramEventArgs;

        /// <summary>
        /// 状态机改变事件
        /// </summary>
        public event Action MachineStatusChangedEventArgs;

        #endregion

        #region Properties

        public bool IsStopped => _stop;
        public bool IsPause => _pause;
        public int MachineStatus => _machineStatus;

        public bool IsEmgStop => _emgStop;

        #endregion

        /// <summary>
        /// 启动程序
        /// </summary>
        public bool StartProgram()
        {
            //在待机状态且回零状态且无报警且暂停中且无故障 才可以切换到运行状态
            if ((_machineStatus & 0x30f) == 0x208 && !_emgStop)
            {
                _stop = false;
                _pause = false;
                _machineStatus = 0x204; //运行状态 无故障 已回零 无停机 无暂停
                MachineStatusChangedEventArgs?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 停止程序
        /// </summary>
        /// <param name="stopDoneFunc"></param>
        public void StopProgram(Func<bool> stopDoneFunc)
        {
            _stop = true;
            _pause = true;
            StopProgramEventArgs?.Invoke();

            Task.Run(() =>
            {
                var result = stopDoneFunc?.Invoke() == true;
                if (result)
                {
                    _stop = false;
                    _pause = false;
                    _machineStatus = _machineStatus | 0x408; //停机置1
                    _machineStatus = _machineStatus & 0xfff8; //低3位置零
                    MachineStatusChangedEventArgs?.Invoke();
                }
            });
          
        }

        /// <summary>
        /// 暂停程序
        /// </summary>
        public bool PauseProgram()
        {
            //在自动运行状态 才可以切换到暂停状态
            if ((_machineStatus & 0x04) != 0x04 || _emgStop) 
            {
                return false; 
            }

            _pause = true;
            PauseProgramEventArgs?.Invoke();

            _machineStatus = _machineStatus | 0x800; //暂停置1
            MachineStatusChangedEventArgs?.Invoke();
            return true;
        }

        /// <summary>
        /// 继续程序
        /// </summary>
        public bool ContinueProgram()
        {
            //在待机状态且回零状态且无报警且暂停中且无故障 才可以切换到运行状态
            if ((_machineStatus & 0x30F) != 0x204 || _emgStop)
            {
                return false;
            }

            _stop = false;
            _pause = false;
            ContinueProgramEventArgs?.Invoke();

            _machineStatus = _machineStatus | 0x4; //自动运行置1
            _machineStatus = _machineStatus & 0xf3ff; //复位暂停中 复位停机中
            MachineStatusChangedEventArgs?.Invoke();
            return true;
        }

        /// <summary>
        /// 回零初始化
        /// </summary>
        /// <param name="stopDoneFunc"></param>
        /// <returns></returns>
        public bool InitStatus(Func<bool> stopDoneFunc, Func<Task<bool>> initFunc)
        {
            //不在待机状态不可进行初始化操作
            if ((_machineStatus & 0xfdff) != 0x08 || _emgStop) 
            {
                return false;
            }


            var stopDone = stopDoneFunc?.Invoke() == true;
            if (stopDone)
            {
                _stop = false;
                _pause = false;
                _machineStatus = _machineStatus & 0xfdf0; //复位低4位状态 和回零状态
                _machineStatus = _machineStatus | 0x1; // 初始化置1
                MachineStatusChangedEventArgs?.Invoke();
                bool result = false;

                //执行回零程序
                result = initFunc().GetAwaiter().GetResult();
                //回零成功
                if (result)
                {
                    _machineStatus = _machineStatus & 0xfff0; //复位低4位状态 
                    _machineStatus = _machineStatus | 0x208; // 待机置1  回零置1
                    MachineStatusChangedEventArgs?.Invoke();
                    return true;
                }
                //回零失败

                //检查回零程序是否停止后
                _machineStatus = _machineStatus & 0xfdf0; //复位低4位状态 和回零状态
                _machineStatus = _machineStatus | 0x8; // 待机置1 
                MachineStatusChangedEventArgs?.Invoke();
                return false;
             
            }
            return false;
        }

        /// <summary>
        /// 急停
        /// </summary>
        public void EmgStop()
        {
            _emgStop = true; 
            _machineStatus = _machineStatus | 0x02;
            _machineStatus = _machineStatus & 0xfff2;
            MachineStatusChangedEventArgs?.Invoke();
        }

        /// <summary>
        /// 发生报警
        /// </summary>
        public void AlmOccu()
        {
            _machineStatus = _machineStatus | 0x100; //故障置1
            MachineStatusChangedEventArgs?.Invoke();
        }

        /// <summary>
        /// 复位报警
        /// </summary>
        public void ResetAlm()
        {
            _emgStop = false;
            _machineStatus = _machineStatus & 0xFEFD;   //fuwei 8 bit bao jing
            MachineStatusChangedEventArgs?.Invoke();
        }


    }
}
