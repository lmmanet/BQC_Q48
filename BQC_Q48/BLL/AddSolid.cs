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
    public class AddSolid : IAddSolid
    {

        #region Private Members

        private readonly IEtherCATMotion _motion;
        private readonly IIoDevice _io;
        private readonly ILS_Motion _stepMotion;
        private readonly ILogger _logger;
        private readonly IAddSolidPosDataAccess _dataAccess;
        private readonly IWeight _weight;

        private readonly ICarrierOne _carrier;
        #endregion

        #region Private Variant

        private readonly ushort _axisY1 = 5;    //伺服Y轴
        private readonly ushort _axisY2 = 1;    //步进Y轴
        private readonly ushort _axisC1 = 3;    //步进旋转轴
        private readonly ushort _axisC2 = 2;    //步进旋转轴
        private readonly ushort _XCylinderControl = 9; //X气缸控制
        private readonly ushort _Z1CylinderControl = 11; //Z1气缸控制
        private readonly ushort _Z2CylinderControl = 13; //Z2气缸控制
        private readonly ushort _XCylinderRight = 12;    //X气缸右感应
        private readonly ushort _XCylinderLeft = 13;     //X气缸左感应
        private readonly ushort _Z1CylinderUp;           //Z1气缸上感应（无）
        private readonly ushort _Z1CylinderDown = 9;     //Z1气缸下感应
        private readonly ushort _Z2CylinderUp;           //Z2气缸上感应（无）
        private readonly ushort _Z2CylinderDown = 11;    //Z2气缸下感应
        private double _stepMoveVel = 30;                //步进电机移动速度
        private double _sevorMoveVel = 60;               //伺服电机移动速度
        private double _rotateVel = 10;                  //旋转加固旋转速度
        private ushort _weightId1 = 1;                   //称台1
        private ushort _weightId2 = 2;                   //称台2

        private AddSolidPosData _posData;                //位置数据
        private IGlobalStatus _globalStatus;
        private int _step;
        private bool _isAddSolidDone;
         
        #endregion

        #region Properties

        #endregion

        #region Construtors

        public AddSolid(IEtherCATMotion motion, IIoDevice io, ILS_Motion stepMotion, IAddSolidPosDataAccess dataAccess, IWeight weight, IGlobalStatus globalStatus,ICarrierOne carrier)
        {
            
            this._motion = motion;
            this._io = io;
            this._stepMotion = stepMotion;
            this._logger = new MyLogger(typeof(AddSolid));
            this._dataAccess = dataAccess;
            this._weight = weight;
            this._globalStatus = globalStatus;

            this._carrier = carrier;

            _posData = _dataAccess.GetPosData();
            _globalStatus.StopProgramEventArgs += StopAddSolid;
            _globalStatus.PauseProgramEventArgs += StopAddSolid;
        }

        public void UpdatePosData()
        {
            _posData = _dataAccess.GetPosData();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 模块回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> GoHome(CancellationTokenSource cts)
        {
            try
            {
                _logger?.Info("加盐模块回零");
                Z1_Cylinder_Up();
                Z2_Cylinder_Up();

                //使能Y步进轴
                await _stepMotion.ServoOn(_axisY2).ConfigureAwait(false);
                var result = await _stepMotion.GoHomeWithCheckDone(_axisY2, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //使能Y伺服轴
                if (!_motion.IsServeOn(_axisY1))
                {
                    _motion.ServoOn(_axisY1);
                }

                //使能旋转轴
                await _stepMotion.ServoOn(_axisC1).ConfigureAwait(false);
                await _stepMotion.ServoOn(_axisC2).ConfigureAwait(false);

                X_Cylinder_Right();
                _step = 0;
                return true;
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped)
                {
                    return false;
                }
                if (cts?.IsCancellationRequested == true)
                {
                    _logger?.Info("加盐模块回零,停止");
                    return false;
                }
                _logger?.Warn($"GoHome err:{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止加盐
        /// </summary>
        /// <returns></returns>
        public bool StopAddSolid()
        {
            _motion.StopMove(_axisY1);
            _stepMotion.StopMove(_axisY2);
            _stepMotion.StopMove(_axisC1);
            _stepMotion.StopMove(_axisC2);
            Z1_Cylinder_Up();
            Z2_Cylinder_Up();
            return true;
        }

        /// <summary>
        /// 加盐提取
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> AddSaltExtract(Sample sample, Func<Sample, CancellationTokenSource, Task<bool>> addSolveFunc, Func<bool> func1, Func<bool> func2, CancellationTokenSource cts)
        {
            //判断是否有加盐工艺
            if ((sample.TechParams.Tech & 0x1e00) == 0)
            {
                sample.SubStep = 2;
                return true;
            }

            try
            {
                bool result;

                //加溶剂 
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSolve2) && !_globalStatus.IsPause && sample.SubStep == 0)
                {
                    result = await addSolveFunc(sample, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    sample.SubStep++;
                }

                //加盐
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSalt2) && !_globalStatus.IsPause && sample.SubStep == 1)
                {  
                    //加固重量
                    var weight = new double[] { sample.TechParams.AddHomo[2], sample.TechParams.Solid_B[2], sample.TechParams.Solid_C[2],
                        sample.TechParams.Solid_D[2], sample.TechParams.Solid_E[2],sample.TechParams.Solid_F[2] };

                    result = await AddSolidAsync(sample, weight,func1,func2, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    sample.SubStep = 2;
                }

                //完成
                return true;

            }
            catch (Exception ex)
            {
                if (!_globalStatus.IsStopped)
                {
                    while (_globalStatus.IsPause)
                    {
                        Thread.Sleep(2000);
                        if (_globalStatus.IsStopped)
                        {
                            return false;
                        }
                        if (!_globalStatus.IsStopped && !_globalStatus.IsPause)
                        {
                            return await AddSaltExtract(sample, addSolveFunc, func1, func2, cts).ConfigureAwait(false);
                        }
                    }
                }
                _logger?.Warn(ex.Message);
                return false;
            }


        }

        /// <summary>
        /// 加固
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> AddSolidAsync(Sample sample,double[] weights, Func<bool> func1, Func<bool> func2, CancellationTokenSource cts)
        {
            try
            {
                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                }
                _logger?.Info($"样品{sample.Id}加固");
                if (_isAddSolidDone)
                {
                    goto Done;
                }
                //加固到接驳位
                var result = await MovePutGetPos(cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //从拧盖1处搬试管(搬运到加固位)
                if (!SampleStatusHelper.BitIsOn(sample,SampleStatus.IsInAddSolid))
                {
                    result = _carrier.GetSampleFromCapperOneToAddSolid(sample, func1, func2, cts);
                    if (!result)
                    {
                        return false;
                    }
                }

                //加固   判断加固种类
                if (!_isAddSolidDone)
                {
                    result = await Add_Solid(weights, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    _isAddSolidDone = true;
                }
              

                //完成后加固到接驳位
            Done: result = await MovePutGetPos(cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //从加固搬运试管到拧盖1位
                result = _carrier.GetSampleFromAddSolidToCapperOne(sample, cts);
                if (!result)
                {
                    return false;
                }

                //完成
                _isAddSolidDone = false;
                return true;
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped)
                {
                    return false;
                }
                _logger?.Error(ex.Message);
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
            try
            {
                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                }
                _logger?.Debug($"MovePutGetPos");
                //步进Y轴移动到原点位置
                var result = await _stepMotion.P2pMoveWithCheckDone(_axisY2, 0, _stepMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("步进Y轴移动到指定位出错");
                }
                X_Cylinder_Right();

                return true;
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped)
                {
                    return false;
                }

                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause)
                    {
                        Thread.Sleep(2000);
                    }

                    if (!_globalStatus.IsStopped)
                    {
                        return await MovePutGetPos(cts);
                    }

                    return false;
                }
                _logger.Error(ex.Message);
                return false;
            }
          

        }

        /// <summary>
        /// 加固
        /// </summary>
        /// <param name="weights">加固重量</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> Add_Solid(double[] weights, CancellationTokenSource cts)
        {
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            weights.ToList().ForEach(w=>_logger.Debug($"Add_Solid Weight {w}"));

            while (_step < 6)
            {
                if (weights[_step] <= 0)
                {
                    _step++;
                    continue;
                }
                var result = await Add_SolidSub(_step+1, weights[_step], cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("加固失败!");
                }
                _step++;
            }
            _step = 0;
            return true;
        }

        /// <summary>
        /// 加固
        /// </summary>
        /// <param name="solid">添加种类1~6</param>
        /// <param name="weight">加固重量</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> Add_SolidSub(int solid, double weight, CancellationTokenSource cts)
        {
            try
            {
                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
                }
                _logger?.Debug($"Add_SolidSub-{solid}-{weight} ");
                double[] pos = GetSolidCoordinate(solid);
                await Task.Delay(1000).ConfigureAwait(false);
                //获取毛重
                var gross1 = await _weight.ReadWeight(_weightId1).ConfigureAwait(false);
                var gross2 = await _weight.ReadWeight(_weightId2).ConfigureAwait(false);
                //X气缸移动到加固位
                X_Cylinder_Left();
                //伺服Y轴移动到指定位置
                var result11 =  _motion.P2pMoveWithCheckDone(_axisY1, pos[0], _sevorMoveVel, _globalStatus).ConfigureAwait(false);
                var result22 =  _stepMotion.P2pMoveWithCheckDone(_axisY2, pos[1], _stepMoveVel, cts).ConfigureAwait(false);

                if (!await result11)
                {
                    throw new Exception("伺服Y轴移动到指定位置出错");
                }
                //步进Y轴移动到指定位置
                if (!await result22)
                {
                    throw new Exception("步进Y轴移动到指定位置出错");
                }

                //C1 C2低速旋转
                result11 = _stepMotion.VelocityMove(_axisC1, _rotateVel).ConfigureAwait(false);
                result22 = _stepMotion.VelocityMove(_axisC2, _rotateVel).ConfigureAwait(false);

                //检测旋转轴是否有报警
                if (!await result11)
                {
                    throw new Exception("加盐旋转电机1报警");
                }
                if (!await result22)
                {
                    throw new Exception("加盐旋转电机2报警");
                }

                //Z1 Z2气缸下降 
                Z1_Cylinder_Down();
                Z2_Cylinder_Down();

                //检测称台数据判断是否完成
                var result1 = CheckDone1(weight + gross1).ConfigureAwait(false);
                var result2 = CheckDone2(weight + gross2).ConfigureAwait(false);
                await result1;
                await result2;
                   
                //Z1 Z2气缸上升
                Z1_Cylinder_Up();
                Z2_Cylinder_Up();

                return true;
            }
            catch (Exception ex)
            {
                if (_globalStatus.IsStopped)
                {
                    return false;
                }

                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause)
                    {
                        Thread.Sleep(2000);
                    }

                    if (!_globalStatus.IsStopped)
                    {
                        return await Add_SolidSub(solid, weight, cts);
                    }

                    return false;
                }
                _logger.Error(ex.Message);
                return false;
            }
        }


        /// <summary>
        /// 检测称台1重量数据
        /// </summary>
        /// <param name="weight"></param>
        /// <param name="timeout">加固超时时间</param>
        /// <returns></returns>
        protected async Task<bool> CheckDone1(double weight,int timeout = 300)
        {
            _logger?.Debug($"CheckDone1-{weight}-{timeout}" );
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(timeout);
            while (true)
            {
                await Task.Delay(500).ConfigureAwait(false);
                var value = await _weight.ReadWeight(_weightId1).ConfigureAwait(false);
                if (value >= weight)
                {
                    await _stepMotion.StopMove(_axisC1).ConfigureAwait(false);
                    break;
                }
                if (DateTime.Now>end)
                {
                    throw new TimeoutException($"CheckDone1 加固超时");
                }
            }
            return true;
        }

        /// <summary>
        /// 检测称台2重量数据
        /// </summary>
        /// <param name="weight"></param>
        /// <param name="timeout">加固超时时间</param>
        /// <returns></returns>
        protected async Task<bool> CheckDone2(double weight, int timeout = 300)
        {
            _logger?.Debug($"CheckDone2-{weight}-{timeout}");
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(timeout);
            while (true)
            {
                await Task.Delay(500).ConfigureAwait(false);
                var value = await _weight.ReadWeight(_weightId2).ConfigureAwait(false);
                if (value >= weight)
                {
                    await _stepMotion.StopMove(_axisC2).ConfigureAwait(false);
                    break;
                }
                if (DateTime.Now > end)
                {
                    throw new TimeoutException($"CheckDone1 加固超时");
                }
            }
            return true;
        }

        protected void Z1_Cylinder_Up(bool checkSensor = false)
        {
            _logger?.Debug($"Z1_Cylinder_Up-{checkSensor}");
            var result = _io.WriteBit_DO(_Z1CylinderControl, false);
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
                result = _io.ReadBit_DI(_Z1CylinderUp);
                Thread.Sleep(500);
                temp++;
                if (temp > 12)
                {
                    throw new TimeoutException("Z1_Cylinder_Up 超时");
                }
            } while (!result);
        }

        protected void Z1_Cylinder_Down(bool checkSensor = true)
        {
            _logger?.Debug($"Z1_Cylinder_Down-{checkSensor}");
            var result = _io.WriteBit_DO(_Z1CylinderControl, true);
            if (!result)
            {
                throw new Exception("Z1_Cylinder_Down Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }

            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_Z1CylinderDown);
                Thread.Sleep(500);
                temp++;
                if (temp > 12)
                {
                    throw new TimeoutException("Z1_Cylinder_Down 超时");
                }
            } while (!result);
        }

        protected void Z2_Cylinder_Up(bool checkSensor = false)
        {
            _logger?.Debug($"Z2_Cylinder_Up-{checkSensor}");
            var result = _io.WriteBit_DO(_Z2CylinderControl, false);
            if (!result)
            {
                throw new Exception("Z2_Cylinder_Up Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }

            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_Z2CylinderUp);
                Thread.Sleep(500);
                temp++;
                if (temp > 12)
                {
                    throw new TimeoutException("Z2_Cylinder_Up 超时");
                }
            } while (!result);
        }

        protected void Z2_Cylinder_Down(bool checkSensor = true)
        {
            _logger?.Debug($"Z2_Cylinder_Down-{checkSensor}");
            var result = _io.WriteBit_DO(_Z2CylinderControl, true);
            if (!result)
            {
                throw new Exception("Z2_Cylinder_Down Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }

            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_Z2CylinderDown);
                Thread.Sleep(500);
                temp++;
                if (temp > 12)
                {
                    throw new TimeoutException("Z2_Cylinder_Down 超时");
                }
            } while (!result);
        }

        protected void X_Cylinder_Left(bool checkSensor = true)
        {
            _logger?.Debug($"X_Cylinder_Left-{checkSensor}");
            var result = _io.WriteBit_DO(_XCylinderControl, false);
            if (!result)
            {
                throw new Exception("X_Cylinder_Left Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }

            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_XCylinderLeft);
                Thread.Sleep(500);
                temp++;
                if (temp > 12)
                {
                    throw new TimeoutException("X_Cylinder_Left 超时");
                }
            } while (!result);
        }

        protected void X_Cylinder_Right(bool checkSensor = true)
        {
            _logger?.Debug($"X_Cylinder_Right-{checkSensor}");
            var result = _io.WriteBit_DO(_XCylinderControl, true);
            if (!result)
            {
                throw new Exception("X_Cylinder_Right Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }

            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_XCylinderRight);
                Thread.Sleep(500);
                temp++;
                if (temp > 12000)
                {
                    throw new TimeoutException("X_Cylinder_Right 超时");
                }
            } while (!result);
        }


        #endregion

        #region Private Methods

        /// <summary>
        /// 获取加固坐标A~F 对应 1~6
        /// </summary>
        /// <param name="solid"></param>
        /// <returns></returns>
        private double[] GetSolidCoordinate(int solid)
        {
            switch (solid)
            {
                case 1:
                    return _posData.Solid_A;
                case 2:
                    return _posData.Solid_B;
                case 3:
                    return _posData.Solid_C;
                case 4:
                    return _posData.Solid_D;
                case 5:
                    return _posData.Solid_E;
                case 6:
                    return _posData.Solid_F;
                default:
                    throw new Exception($"参数错误：{solid}");
            }

        }


        #endregion


    }
}
