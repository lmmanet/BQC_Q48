using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class Vortex : IVortex
    {

        #region Private Members

        private readonly IIoDevice _io;

        private readonly ILogger _logger;

        private readonly ILS_Motion _stepMotion;

        private readonly ICarrierOne _carrier;

        private readonly IGlobalStatus _globalStauts;

        private readonly IVortexPosDataAccess _dataAccess;

        private readonly static object _lockObj = new object();

        #endregion

        #region Variable

        protected double _stepMoveVel = 500;

        protected ushort _axisY = 8;  //Y轴
        protected ushort _vortexMotion1 =32;
        protected ushort _vortexMotion2 =33;
        protected ushort _vortexMotionVel1 = 0;
        protected ushort _vortexMotionVel2 = 1;

        protected ushort _press = 15;  //下压气缸
        protected ushort _pressUpSensor = 17;    //下压气缸上感应
        protected ushort _pressDownSensor =18;  //下压气缸下感应

        protected double _xOffset = 50;    //涡旋X偏移量

        protected VortexPosData _posData;

        #endregion

        #region Constructors

        public Vortex(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, IVortexPosDataAccess dataAccess,ICarrierOne carrier)
        {
            this._io = io;
            this._stepMotion = motion;
            this._logger = new MyLogger(typeof(Vortex));
            this._globalStauts = globalStatus;
            this._dataAccess = dataAccess;

            this._carrier = carrier;

            _posData = dataAccess.GetPosData();
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> GoHome(CancellationTokenSource cts)
        {
            _logger.Info("涡旋回零");
            try
            {
                //下压气缸上升
                PressUp();

                //使能Y轴
                await _stepMotion.ServoOn(_axisY).ConfigureAwait(false);
                //Y轴回零
                var result = await _stepMotion.GoHomeWithCheckDone(_axisY, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Y轴回零失败");
                }

                //Y轴移动到上下料位
                result = await _stepMotion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _stepMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Y轴到上下料位失败");
                }
                return true;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    return false;
                }
                _logger?.Warn($"{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 涡旋
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool StartVortex(Sample sample, CancellationTokenSource cts)
        {
            int time = 30;
            int vel = 1000;
         
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"{sample.Id}样品涡旋-{time}s-{vel}rpm");
                    //到上下料位
                    var result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("涡旋到上下料位失败");
                    }

                    //搬运
                    result = _carrier.GetSampleToVortex(sample, cts);
                    if (!result)
                    {
                        throw new Exception("搬运样品到涡旋失败");
                    }

                    //开始涡旋

                    result = StartVortexAsync(time, vel, cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("涡旋失败");
                    }

                    //搬运到试管架
                    result = _carrier.GetSampleToMaterial(sample, cts);
                    if (!result)
                    {
                        throw new Exception("搬运样品到试管架失败");
                    }

                    //完成

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"{sample.Id}样品涡旋-{time}s-{vel}rpm 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        #endregion


        #region Protected Methods

        /// <summary>
        /// 移动到上下料位
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> MovePutGetPos(CancellationTokenSource cts)
        {
            PressUp();

            //步进Y轴移动到原点位置
            var result = await _stepMotion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _stepMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }
          
            return true;

        }


        /// <summary>
        /// 开始涡旋
        /// </summary>
        /// <param name="time"></param>
        /// <param name="vel">30~3000</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> StartVortexAsync(int time,int vel,CancellationTokenSource cts)
        {
            //Y轴移动到涡旋位置
            var result = await _stepMotion.P2pMoveWithCheckDone(_axisY, _posData.VortexPos, _stepMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }

            //下压气缸动作
            PressDown();

            //启动涡旋电机
            _io.WriteByte_DA(_vortexMotionVel1, vel*10);
            _io.WriteByte_DA(_vortexMotionVel2, vel*10);

            _io.WriteBit_DO(_vortexMotion1,true);
            _io.WriteBit_DO(_vortexMotion2,true);
            //开始定时
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(time);
            do
            {
                Thread.Sleep(1000);
                if (DateTime.Now > end)
                {
                    break;
                }
                if (cts?.IsCancellationRequested == true)
                {
                    _io.WriteBit_DO(_vortexMotion1, false);
                    _io.WriteBit_DO(_vortexMotion2, false);
                    await Task.Delay(500).ConfigureAwait(false);
                    //下压气缸上升
                    PressUp();
                    return false;
                }
            } while (true);

            //停止涡旋电机
            _io.WriteBit_DO(_vortexMotion1, false);
            _io.WriteBit_DO(_vortexMotion2, false);
            //延时
            await Task.Delay(1000).ConfigureAwait(false);
            //下压气缸上升
            PressUp();
            //Y轴移动到上下料位置
            result = await _stepMotion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _stepMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }

            return true;
        }


        /// <summary>
        /// 下压气缸动作
        /// </summary>
        protected virtual void PressDown(bool checkSensor = true)
        {
            //下压气缸向下
            var result = _io.WriteBit_DO(_press, true);
            if (!result)
            {
                throw new Exception("PressDown Err!");
            }
            if (!checkSensor)
            {
                return;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_pressDownSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new Exception("PressDown 超时");
                }
            } while (!result);



        }

        /// <summary>
        /// 下压气缸释放
        /// </summary>
        protected virtual void PressUp(bool checkSensor = true)
        {
            //下压气缸向上
            var result = _io.WriteBit_DO(_press, false);
            if (!result)
            {
                throw new Exception("PressUp Err!");
            }

            if (!checkSensor)
            {
                return;
            }

            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_pressUpSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new Exception("PressUp 超时");
                }
            } while (!result);

        }




        #endregion







    }

}

