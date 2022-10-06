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
    public class Concentration : IConcentration
    {

        #region Private Members

        private readonly IIoDevice _io;

        private readonly ILogger _logger;

        private readonly ILS_Motion _motion;

        private readonly IGlobalStatus _globalStatus;

        private readonly ISyringTwo _syringTwo;

        private ConcentrationPosData _posData;

        private readonly IConcentrationPosDataAccess _dataAccess;

        #endregion

        #region Variable

        protected double _yMoveVel = 500;


        protected ushort _axisY = 25;

        protected ushort _zCylinderDown = 56;      //Q2.0
        protected ushort _zCylinderUp = 55;        //Q1.7
        protected ushort _zCylinderHigh = 56;      //I2.0
        protected ushort _zCylinderLow = 55;       //I1.7
        protected ushort _vaccumSensor1 = 57;      //I2.1
        protected ushort _vaccumSensor2 = 58;      //I2.2
        protected ushort _vaccum1 = 52;            //Q1.4
        protected ushort _vaccum2 = 53;            //Q1.5
        protected ushort _rotateMotion1 = 66;      //Q3.2
        protected ushort _rotateMotion2 = 67;      //Q3.3


        protected double _xOffset = 50;    //浓缩X偏移量

        #endregion

        #region Constructors

        public Concentration(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ISyringTwo syringTwo, IConcentrationPosDataAccess dataAccess)
        {
            this._io = io;
            this._motion = motion;
            _syringTwo = syringTwo;

            this._logger = new MyLogger(typeof(Concentration));
            this._globalStatus = globalStatus;
            this._dataAccess = dataAccess;

            _posData = dataAccess.GetPosData();
        }

        public void UpdatePosData()
        {
            _posData = _dataAccess.GetPosData();
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
                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                }
                _logger?.Info("浓缩回零");

                //Z轴上升
                //_io.WriteBit_DO(_claw, true);

                //使能Y轴
                await _motion.ServoOn(_axisY).ConfigureAwait(false);
                //Y轴回零
                var result = await _motion.GoHomeWithCheckDone(_axisY, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }


                //复位真空

                //Y轴移动到上下料位置
                result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
                //回零完成
                return true;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    _logger?.Info("浓缩回零 停止");
                    return false;
                }
                _logger?.Warn($"浓缩回零 err:{ex.Message}");
                return false;
            }

        }

        /// <summary>
        /// 样品浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool DoConcentration(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                bool result;
                int vel = sample.TechParams.ConcentrationVel;
                int time = sample.TechParams.ConcentrationTime;

                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Concentration))
                {
                    result = DoConcentration(vel, time, cts);
                    if (!result)
                    {
                        throw new Exception($"样品{sample.Id}浓缩失败!");
                    }
                }
                return true;
                
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped)
                {
                    _logger?.Error(ex.Message);
                    return false;
                }
                _logger.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 样品复溶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool Redissolve(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                bool result;
                double volume = sample.TechParams.Redissolve; //1ml乙酸乙酯  或  0.95ml乙腈水溶液
                int vel = 3000;
                int time = 30;
                
                if (TechStatusHelper.BitIsOn(sample.TechParams,TechStatus.Redissolve))
                {
                    result = Redissolve(2,volume, vel, time, cts);
                    if (!result)
                    {
                        throw new Exception($"样品{sample.Id}复溶失败!");
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped)
                {
                    _logger?.Error(ex.Message);
                    return false;
                }
                _logger.Warn(ex.Message);
                return false;
            }

        }


        #endregion

        #region Protected Methods

        /// <summary>
        /// 复溶
        /// </summary>
        /// <param name="var">复溶溶剂种类1：DMSO  2：乙酸乙酯</param>
        /// <param name="volume">复溶加液量</param>
        /// <param name="vel">涡旋速度</param>
        /// <param name="time">涡旋时间</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool Redissolve(byte var,double volume, int vel, int time, CancellationTokenSource cts)
        {
            byte port = 0x04;
            if (var == 2)
            {
                port = 0x08;
            }

            try
            {
                //Z气缸上升
                Z_Cylinder_Up();

                //Y轴移动到加液
                s1: var result = _motion.P2pMoveWithCheckDone(_axisY, GetAddLiquidPos(), _yMoveVel, cts).GetAwaiter().GetResult();
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                    }
                    throw new Exception($"浓缩Y轴移动到复溶位失败!");
                }

                //注射器加液
               s2: result = _syringTwo.AddSolve(port, volume, cts).GetAwaiter().GetResult(); //乙酸乙酯
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsStopped)
                        {
                            goto s2;
                        }
                    }
                    throw new Exception($"复溶加液失败!");
                }

                //Y轴移动到浓缩位
               s3: result = _motion.P2pMoveWithCheckDone(_axisY, GetConcenPos(), _yMoveVel, cts).GetAwaiter().GetResult();
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsStopped)
                        {
                            goto s3;
                        }
                    }
                    throw new Exception($"浓缩Y轴移动到浓缩位失败!");
                }

                //Z气缸下降
                Z_Cylinder_Down();

                //涡旋  //旋转
                StartRotate(vel);

                //延时
                DateTime end = DateTime.Now + TimeSpan.FromSeconds(time);
                while (true)
                {
                    Thread.Sleep(1000);
                    if (DateTime.Now > end)
                    {
                        break; 
                    }
                }
               

                //停止
                StopRotate();
                Thread.Sleep(500);
                //Z气缸上升
                Z_Cylinder_Up();

                //完成
                //Y轴移动到上下位
               s4: result = _motion.P2pMoveWithCheckDone(_axisY, GetPutGetPos(), _yMoveVel, cts).GetAwaiter().GetResult();
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsStopped)
                        {
                            goto s4;
                        }
                    }
                    throw new Exception("浓缩Y轴移动到上下料位失败!");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return false;
            }

        }

        /// <summary>
        /// 开始浓缩
        /// </summary>
        /// <param name="vel"></param>
        /// <param name="time"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool DoConcentration(int vel, int time, CancellationTokenSource cts)
        {
            try
            {
                //Z气缸上升
                Z_Cylinder_Up();

                //Y轴移动到浓缩位
               s1: var result = _motion.P2pMoveWithCheckDone(_axisY, GetConcenPos(), _yMoveVel, cts).GetAwaiter().GetResult();
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                    }
                    throw new Exception("浓缩Y轴移动到浓缩位失败!");
                }

                //Z气缸下降
                Z_Cylinder_Down();

                //旋转
                StartRotate(vel);

                //打开真空
                OpenVaccume();

                //检查真空
               s4: result = CheckVaccumeSensor(30);
                if (!result)
                {
                    _globalStatus.PauseProgram();
                    _logger?.Warn("浓缩真空度未到达！");
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsStopped)
                        {
                            goto s4;
                        }
                    }
                    throw new Exception("浓缩真空度未到达！");
                }

                ///延时
                DateTime end = DateTime.Now + TimeSpan.FromMinutes(time);
                while (true)
                {
                    Thread.Sleep(10000);
                    if (DateTime.Now >= end)
                    {
                        break;
                    }
                    if (cts?.IsCancellationRequested == true)
                    {
                        throw new TaskCanceledException();
                    }
                }

                CloseVaccume();
                StopRotate();
                Thread.Sleep(1000);
                //Z气缸上升
                Z_Cylinder_Up();

                //Y轴移动一小步 
              s2: result = _motion.P2pMoveWithCheckDone(_axisY, GetConcenPos() + 5, _yMoveVel, cts).GetAwaiter().GetResult();
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsStopped)
                        {
                            goto s2;
                        }
                    }
                    throw new Exception("浓缩Y轴移动到浓缩位失败!");
                }

                //等待1S
                Thread.Sleep(1000);

                //Y轴移动到上下位
               s3: result = _motion.P2pMoveWithCheckDone(_axisY, GetPutGetPos(), _yMoveVel, cts).GetAwaiter().GetResult();
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsStopped)
                        {
                            goto s3;
                        }
                    }
                    throw new Exception("浓缩Y轴移动到上下料位失败!");
                }

                return true;

            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return false;
            }


        }


        #endregion

        #region Private Methods

        private void StartRotate(int vel)
        {
            double value = vel * 3;
            _io.WriteByte_DA(4, value);
            _io.WriteByte_DA(5, value);

            s1: var result1 = _io.WriteBit_DO(_rotateMotion1, true);
            var result2= _io.WriteBit_DO(_rotateMotion2, true);
            if (!result1)
            {
                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause)
                    {
                        Thread.Sleep(1000);
                    }

                    if (!_globalStatus.IsStopped)
                    {
                        goto s1;
                    }
                }
                throw new Exception("浓缩电机1启动出错！");
            }
            if (!result2)
            {
                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause)
                    {
                        Thread.Sleep(1000);
                    }

                    if (!_globalStatus.IsStopped)
                    {
                        goto s1;
                    }
                }
                throw new Exception("浓缩电机2启动出错！");
            }
        }

        private void StopRotate()
        {
            var result1 = _io.WriteBit_DO(_rotateMotion1, false); 
            var result2 = _io.WriteBit_DO(_rotateMotion2, false);
            if (!result1)
            {
                throw new Exception("浓缩电机1停止出错！");
            }
            if (!result2)
            {
                throw new Exception("浓缩电机2停止出错！");
            }
        }
        

        /// <summary>
        /// Z气缸上
        /// </summary>
        /// <param name="checkSensor"></param>
        private void Z_Cylinder_Up(bool checkSensor = true)
        {
            _logger?.Debug($"Z_Cylinder_Up-{checkSensor}");
            var result = _io.WriteBit_DO(_zCylinderDown, false);
            result = _io.WriteBit_DO(_zCylinderUp, true);
            if (!result)
            {
                throw new Exception("Z1_Cylinder_Up Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }

            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_zCylinderHigh);
                Thread.Sleep(500);
                temp++;
                if (temp > 12)
                {
                    throw new TimeoutException("Z_Cylinder_Up 超时");
                }
            } while (!result);
        }

        /// <summary>
        /// Z气缸下
        /// </summary>
        /// <param name="checkSensor"></param>
        private void Z_Cylinder_Down(bool checkSensor = true)
        {
            _logger?.Debug($"Z_Cylinder_Down-{checkSensor}");
            var result = _io.WriteBit_DO(_zCylinderUp, false);
            result = _io.WriteBit_DO(_zCylinderDown, true);
            if (!result)
            {
                throw new Exception("Z_Cylinder_Down Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }

            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_zCylinderLow);
                Thread.Sleep(500);
                temp++;
                if (temp > 12)
                {
                    throw new TimeoutException("Z_Cylinder_Down 超时");
                }
            } while (!result);
        }

        /// <summary>
        /// 打开真空
        /// </summary>
        private void OpenVaccume()
        {
            _logger?.Debug($"OpenVaccume");
            var result = _io.WriteBit_DO(_vaccum1, true);
            if (!result)
            {
                throw new Exception("打开真空1失败!");
            }
            result = _io.WriteBit_DO(_vaccum2, true);
            if (!result)
            {
                throw new Exception("打开真空2失败!");
            }
        }

        /// <summary>
        /// 关闭真空
        /// </summary>
        private void CloseVaccume()
        {
            _logger?.Debug($"ClosVaccume");
            var result = _io.WriteBit_DO(_vaccum1, false);
            if (!result)
            {
                throw new Exception("关闭真空1失败!");
            }
            result = _io.WriteBit_DO(_vaccum2, false);
            if (!result)
            {
                throw new Exception("关闭真空2失败!");
            }
        }

        /// <summary>
        /// 检查真空是否完好
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private bool CheckVaccumeSensor(int timeout)
        {
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(timeout);
            while (true)
            {
                var result1 = _io.ReadBit_DI(_vaccumSensor1);
                var result2 = _io.ReadBit_DI(_vaccumSensor2);
                Thread.Sleep(500);
                if (result1 && result2)
                {
                    return true;
                }

                if (DateTime.Now > end)
                {
                    return false;
                }
            }
        }

        private double GetConcenPos()
        {
            return _posData.ConcPos;
        }

        private double GetPutGetPos()
        {
            return _posData.PutGetPos;
        }
        
        private double GetAddLiquidPos()
        {
            return _posData.ConcPos + 30;
        }



        #endregion




    }
}
