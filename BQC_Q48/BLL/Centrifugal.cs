using BQJX.Common;
using BQJX.Core.Interface;
using Q_Platform.Logger;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class Centrifugal : ICentrifugal
    {

        #region Private Members

        private readonly IEtherCATMotion _motion;
        private readonly IIoDevice _io;
        private readonly ILogger _logger;
        private readonly ICentrifugalCarrier _carrier;

        #endregion

        #region Variants

        private ushort _axisCentrigugal = 8; //离心机轴
        private ushort _homeMode = 33; //离心机回零模式

        private ushort _shadowOpen = 0; //离心机门打开控制
        private ushort _shadowClose = 1; //离心机门关闭控制
        private ushort _shadowOpenSensor = 0; //离心机门打开感应
        private ushort _shadowCloseSensor = 1; //离心机门关闭感应

        #endregion

        #region Construtors

        public Centrifugal(IEtherCATMotion motion, IIoDevice io, ICentrifugalCarrier carrier)
        {
            this._motion = motion;
            this._io = io;
            this._carrier = carrier; 
            this._logger = new MyLogger(typeof(Centrifugal));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 模块回零
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GoHome(CancellationTokenSource cts)
        {
            _logger?.Info("离心机回零");
            try
            {
                var ret3 = Centri_GoHome().ConfigureAwait(false);
                if (!ret3.GetAwaiter().GetResult())
                {
                    return false;
                }
                OpenShadow();

                return true;
            }
            //catch (CommunicationException cmex)
            //{
            //    return false;
            //}
            //catch (EtherCATMotionException ecex)
            //{
            //    return false;
            //}
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

        }

        /// <summary>
        /// 样品离心
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> CentrifugalAsync(Sample sample,CancellationTokenSource cts)
        {
            throw new NotImplementedException();
        }

      
        #endregion

        #region Protected Methods

        /// <summary>
        /// 离心机开始离心
        /// </summary>
        /// <param name="time"></param>
        /// <param name="vel"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> DoCentrifugal(int time,int vel, CancellationTokenSource cts)
        {
            //关闭门
            CloseShadow();

            //开始离心
            double velocity = vel / 60;
            var result = _motion.VelocityMove(_axisCentrigugal, velocity, 1);
            if (!result)
            {
                return false;
            }
            //延时
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(time);
            bool isDone = false;
            while (true)
            {
                Thread.Sleep(1000);
                if (DateTime.Now >= end)
                {
                    isDone = true;
                    break;
                }
                if (cts?.IsCancellationRequested == true)
                {
                    isDone = false;
                    break;
                }
            }
            //停止电机
            _motion.StopMove(_axisCentrigugal);

            //等待停止  
            DateTime timeout = DateTime.Now + TimeSpan.FromSeconds(30);
            while (true)
            {
                var cv = Math.Abs(Math.Round(_motion.GetCurrentVel(_axisCentrigugal), 1));
                if (cv < 0.2)
                {
                    break;
                }
                if (DateTime.Now > timeout)
                {
                    throw new TimeoutException("检测离心机停止超时");
                }
                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException();
                }
            }
          

            //离心机回零
            result = await Centri_GoHome().ConfigureAwait(false);
            if (!result)
            {
                return false;
            }

            //打开门
            OpenShadow();

            return isDone;
        }




        #endregion


        #region 底层方法

        /// <summary>
        /// 打开离心机护罩
        /// </summary>
        protected void OpenShadow(bool checkSensor = true)
        {
            var result = _io.WriteBit_DO(_shadowClose, false);
            if (!result)
            {
                throw new Exception("OpenShadow Err!");
            }
            result = _io.WriteBit_DO(_shadowOpen, true);
            if (!result)
            {
                throw new Exception("OpenShadow Err!");
            }
            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_shadowOpenSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new TimeoutException("离心机护罩打开超时");
                }
            } while (!result);
        }

        /// <summary>
        /// 关闭离心机护罩
        /// </summary>
        protected void CloseShadow(bool checkSensor = true)
        {
            var result = _io.WriteBit_DO(_shadowOpen, false);
            if (!result)
            {
                throw new Exception("CloseShadow Err!");
            }
            result = _io.WriteBit_DO(_shadowClose, true);
            if (!result)
            {
                throw new Exception("CloseShadow Err!");
            }
            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_shadowCloseSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new TimeoutException("离心机护罩关闭超时");
                }
            } while (!result);
        }

   
        /// <summary>
        /// 旋转到指定工位
        /// </summary>
        /// <param name="num">1:初始位 2：90°位 3：180°位 4：270°位</param>
        protected async Task<bool> GoStation(ushort num)
        {
            //判断离心机是否停止
            var cv = Math.Abs(Math.Round(_motion.GetCurrentVel(_axisCentrigugal), 1));
            if (cv > 0.2)
            {
                _logger?.Error($"离心机未停止 速度：{cv}");
                throw new Exception("离心机未停止");
            }

            //打开护罩
            OpenShadow();


            //离心机回零

            var status = _motion.GetMotionStatus(_axisCentrigugal);
            if ((status & 4) != 4)
            {
                _motion.ServoOn(_axisCentrigugal);
            }

            var result = await _motion.GohomeWithCheckDone(_axisCentrigugal, _homeMode, null).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Error($"离心机回零出错");
                throw new Exception("离心机回零出错");
            }


            //离心机移动到指定位置
            if (num != 1)
            {
                double offset = 0;
                if (num == 2)
                {
                    offset = 0.25;
                }
                else if (num == 3)
                {
                    offset = 0.5;
                }
                else if (num == 4)
                {
                    offset = 0.75;
                }
                else
                {
                    throw new Exception($"指定位置编号不存在 num：{num}");
                }

                result = await _motion.P2pMoveWithCheckDone(_axisCentrigugal, offset, 1, null).ConfigureAwait(false);
                if (!result)
                {
                    _logger?.Error($"离心机转到指定位置出错");
                    throw new Exception("离心机转到指定位置出错");
                }

                return true;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 离心机回零
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> Centri_GoHome()
        {
            //检查是否使能
            if (!_motion.IsServeOn(_axisCentrigugal))
            {
                _motion.ServoOn(_axisCentrigugal);
            }

            var result = await _motion.GohomeWithCheckDone(_axisCentrigugal, _homeMode, null).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Error($"离心机回零出错");
                throw new Exception("离心机回零出错");
            }
            return true;
        }

     
        #endregion



    }
}
