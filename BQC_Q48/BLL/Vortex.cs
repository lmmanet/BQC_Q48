using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
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

        private readonly ILS_Motion _motion;

        private readonly IGlobalStatus _globalStauts;

        private readonly IVortexPosDataAccess _dataAccess;

        #endregion

        #region Variable

        protected double _yMoveVel = 500;

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

        public Vortex(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, IVortexPosDataAccess dataAccess, ILogger logger)
        {
            this._io = io;
            this._motion = motion;
            this._logger = logger;
            this._globalStauts = globalStatus;
            this._dataAccess = dataAccess;
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
            try
            {
                //下压气缸上升
                PressUp();

                //使能Y轴
                await _motion.ServoOn(_axisY).ConfigureAwait(false);
                //Y轴回零
                var result = await _motion.GoHomeWithCheckDone(_axisY, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //Y轴移动到上下料位
                result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 涡旋
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> StartVortexAsync(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                var result = await StartVortex(30, 2000, cts).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        #endregion


        #region Protected Methods

        /// <summary>
        /// 开始涡旋
        /// </summary>
        /// <param name="time"></param>
        /// <param name="vel">30~3000</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> StartVortex(int time,int vel,CancellationTokenSource cts)
        {
            //Y轴移动到涡旋位置
            var result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.VortexPos, _yMoveVel, cts).ConfigureAwait(false);
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
            result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _yMoveVel, cts).ConfigureAwait(false);
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

