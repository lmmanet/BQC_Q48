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
        private readonly ushort _axisZ1 = 30;    //步进Z1轴
        private readonly ushort _axisZ2 = 31;    //步进Z2轴
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
        //private double _rotateVel = 60;                  //旋转加固旋转速度
        private ushort _weightId1 = 1;                   //称台1
        private ushort _weightId2 = 2;                   //称台2

        private AddSolidPosData _posData;                //位置数据
        private IGlobalStatus _globalStatus;
        private int _step;
        private bool _isAddSolidDone;


        private double AddPreWeight1 = 0;
        private double AddPreWeight2 = 0;

        private bool _isCanPressDown = true;  //Z气缸是否允许下压
        #endregion

        #region Properties

        #endregion

        #region Construtors

        public AddSolid(IEtherCATMotion motion, IIoDevice io, ILS_Motion stepMotion, IAddSolidPosDataAccess dataAccess, IWeight weight, IGlobalStatus globalStatus,ICarrierOne carrier,IRunService runService)
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
            runService.AddSaltPauseEventHandler += RunService_AddSaltPauseEventHandler;
            runService.AddSaltContinueEventHandler += RunService_AddSaltContinueEventHandler;
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

                //使能Z1Z2轴
                //await _stepMotion.ServoOn(_axisZ1).ConfigureAwait(false);
                //await _stepMotion.ServoOn(_axisZ2).ConfigureAwait(false);
                //result = await _stepMotion.DM2C_GoHomeWithCheckDone(_axisZ1,cts).ConfigureAwait(false);
                //if (!result)
                //{
                //    return false;
                //}
                //result = await _stepMotion.DM2C_GoHomeWithCheckDone(_axisZ2,cts).ConfigureAwait(false); 
                //if (!result)
                //{
                //    return false;
                //}


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
        public async Task<bool> AddSaltExtract(Sample sample, Func<Sample, bool,CancellationTokenSource, Task<bool>> addSolveFunc, Func<bool> func1, Func<bool> func2, CancellationTokenSource cts)
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
                if ( !_globalStatus.IsStopped && sample.SubStep == 0)
                {
                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSolve2) || TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSalt2))
                    {
                        bool add = TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSolve2);
                        result = await addSolveFunc(sample, add, cts).ConfigureAwait(false);
                        if (!result)
                        {
                            return false;
                        }
                    }
                   
                    sample.SubStep++;
                }

                //加盐
                if (!_globalStatus.IsStopped && sample.SubStep == 1)
                {
                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSalt2))
                    {
                        //加固重量
                        var weight = new double[] { sample.TechParams.AddHomo[2], sample.TechParams.Solid_B[2], sample.TechParams.Solid_C[2],
                        sample.TechParams.Solid_D[2], sample.TechParams.Solid_E[2],sample.TechParams.Solid_F[2] };

                        result = await AddSolidAsync(sample, weight, func1, func2, cts).ConfigureAwait(false);
                        if (!result)
                        {
                            return false;
                        }
                    }
                   
                    sample.SubStep = 2;
                }

                //完成
                if (sample.SubStep == 2)
                {
                    return true;
                }
                return false;

            }
            catch (Exception ex)
            {
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
                bool result;
                _logger?.Info($"样品{sample.Id}加固");
                if (_isAddSolidDone)
                {
                    goto Done;
                }
                //加固到接驳位   内部已有暂停判断
                if (!_globalStatus.IsStopped)
                {
                    result = await MovePutGetPos(cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                }
               
                //从拧盖1处搬试管(搬运到加固位)
                if (!SampleStatusHelper.BitIsOn(sample,SampleStatus.IsInAddSolid) && !_globalStatus.IsStopped)
                {
                    //由内部判断暂停
                    result = _carrier.GetSampleFromCapperOneToAddSolid(sample, func1, func2, cts);
                    if (!result)
                    {
                        return false;
                    }
                }

                //加固   判断加固种类
                if (!_isAddSolidDone && !_globalStatus.IsStopped)
                {
                    //由内部判断暂停
                    result = await Add_Solid(weights, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    _isAddSolidDone = true;
                }


                //完成后加固到接驳位
            Done: if (!_globalStatus.IsStopped)
                {
                    result = await MovePutGetPos(cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                }

                //从加固搬运试管到拧盖1位
                if (!_globalStatus.IsStopped)
                {
                    result = _carrier.GetSampleFromAddSolidToCapperOne(sample, cts);
                    if (!result)
                    {
                        return false;
                    }
                    //完成
                    _isAddSolidDone = false;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
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
                _logger?.Debug($"MovePutGetPos");
                //步进Y轴移动到原点位置
               s1: var result = await _stepMotion.P2pMoveWithCheckDone(_axisY2, 0, _stepMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }
                        if (!_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                    }
                    throw new Exception("步进Y轴移动到指定位出错");
                }
                X_Cylinder_Right();

                return true;
            }
            catch (Exception ex)
            {
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
            weights.ToList().ForEach(w=>_logger.Debug($"Add_Solid Weight {w}"));
            double rotateVel = 10;

            while (_step < 6)
            {
                if (_step > 0)
                {
                    rotateVel = 50;
                }
                else
                {
                    rotateVel = 10;
                }
                if (weights[_step] <= 0)
                {
                    _step++;
                    continue;
                }
                var result = await Add_SolidSub(_step+1, weights[_step], rotateVel, cts).ConfigureAwait(false);
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
        protected async Task<bool> Add_SolidSub(int solid, double weight,double rotateVel, CancellationTokenSource cts)
        {
            try
            {
                _logger?.Debug($"Add_SolidSub-{solid}-{weight} ");
                double[] pos = GetSolidCoordinate(solid);
                await Task.Delay(1000).ConfigureAwait(false);
                //获取毛重
                double gross1, gross2;
                if (AddPreWeight1 == 0)
                {
                    gross1 = await _weight.ReadWeight(_weightId1).ConfigureAwait(false);
                    AddPreWeight1 = gross1;
                }
                else
                {
                    gross1 = AddPreWeight1;
                }

                if (AddPreWeight2 == 0)
                {
                    gross2 = await _weight.ReadWeight(_weightId2).ConfigureAwait(false);
                    AddPreWeight2 = gross2;
                }
                else
                {
                    gross2 = AddPreWeight2;
                }
                //X气缸移动到加固位
                X_Cylinder_Left();
                //伺服Y轴移动到指定位置
               s1: var result11 =  _motion.P2pMoveWithCheckDone(_axisY1, pos[0], _sevorMoveVel, _globalStatus).ConfigureAwait(false);
                var result22 =  _stepMotion.P2pMoveWithCheckDone(_axisY2, pos[1], _stepMoveVel, cts).ConfigureAwait(false);

                if (!await result11)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }
                        if (!_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                    }
                
                    throw new Exception("伺服Y轴移动到指定位置出错");
                }
                //步进Y轴移动到指定位置
                if (!await result22)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }
                        if (!_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                    }

                    throw new Exception("步进Y轴移动到指定位置出错");
                }


                //Z1 Z2气缸下降 
               s3:
                if (_isCanPressDown)
                {
                    Z1_Cylinder_Down();
                    Z2_Cylinder_Down();
                }
                else
                {
                    while (!_isCanPressDown)
                    {
                        Thread.Sleep(1000);
                        if (_globalStatus.IsStopped)
                        {
                            return false;
                        }
                    }
                    goto s3;
                }


            //C1 C2低速旋转
            s2: if (!CheckWeight1(weight + gross1))
                {
                    result11 = _stepMotion.VelocityMove(_axisC1, rotateVel).ConfigureAwait(false);
                }

               if (!CheckWeight2(weight + gross2))
                {
                    result22 = _stepMotion.VelocityMove(_axisC2, rotateVel).ConfigureAwait(false);
                }
                //检测旋转轴是否有报警
                if (!await result11)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }
                        if (!_globalStatus.IsStopped)
                        {
                            goto s2;
                        }
                    }

                    throw new Exception("加盐旋转电机1报警");
                }
                if (!await result22)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                        {
                            Thread.Sleep(1000);
                        }
                        if (!_globalStatus.IsStopped)
                        {
                            goto s2;
                        }
                    }
                    throw new Exception("加盐旋转电机2报警");
                }

                //检测称台数据判断是否完成
                var result1 = CheckDone1(weight + gross1).ConfigureAwait(false);
                var result2 = CheckDone2(weight + gross2).ConfigureAwait(false);
                await result1;
                await result2;

       
                //回转1/8
                var ret3 = _stepMotion.RelativeMoveWithCheckDone(_axisC1, -0.12, 50, cts);
                var ret4 = _stepMotion.RelativeMoveWithCheckDone(_axisC2, -0.12, 50, cts);
                await ret3;
                await ret4;

                //Z1 Z2气缸上升
                Z1_Cylinder_Up();
                Z2_Cylinder_Up();
                AddPreWeight1 = 0;
                AddPreWeight2 = 0;
                return true;
            }
            catch (Exception ex)
            {
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
            await Task.Delay(500).ConfigureAwait(false);
            while (true)
            {
                Thread.Sleep(300);
                var value = _weight.ReadWeight(_weightId1).GetAwaiter().GetResult();
                if (value >= weight)
                {
                    _stepMotion.StopMove(_axisC1).GetAwaiter().GetResult();
                    var res = Rotate1HomePos(null).GetAwaiter().GetResult();
                    if (!res)
                    {
                        throw new Exception("旋转到原位出错!");
                    }
                    break;
                }
                var ret = _stepMotion.ReadAlmCode(_axisC1).GetAwaiter().GetResult();
                if (ret != 0)
                {
                    throw new Exception("加盐旋转电机1报警");
                }
                if (DateTime.Now>end)
                {
                    _stepMotion.StopMove(_axisC1).GetAwaiter().GetResult();
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
            await Task.Delay(500).ConfigureAwait(false);
            while (true)
            {
                Thread.Sleep(300);
                var value = _weight.ReadWeight(_weightId2).GetAwaiter().GetResult();
                if (value >= weight)
                {
                    _stepMotion.StopMove(_axisC2).GetAwaiter().GetResult();
                    var res = Rotate2HomePos(null).GetAwaiter().GetResult();
                    if (!res)
                    {
                        throw new Exception("旋转到原位出错!");
                    }
                    break;
                }
                var ret = _stepMotion.ReadAlmCode(_axisC2).GetAwaiter().GetResult();
                if (ret != 0)
                {
                    throw new Exception("加盐旋转电机2报警");
                }
                if (DateTime.Now > end)
                {
                    _stepMotion.StopMove(_axisC2).GetAwaiter().GetResult();
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


        protected async Task<bool> Rotate1HomePos(CancellationTokenSource cts)
        {
            var t1 = _stepMotion.GetCurrentPos(_axisC1).ConfigureAwait(false);
         
            var pos1 = await t1;
         
            double offset1 = 0.5 - pos1 % 0.5;
         
            var t3 = _stepMotion.RelativeMoveWithCheckDone(_axisC1, offset1, 50, cts);
     

            if (!await t3 )
            {
                return false;
            }
            return true;
        }
        protected async Task<bool> Rotate2HomePos(CancellationTokenSource cts)
        {
          
            var t2 = _stepMotion.GetCurrentPos(_axisC2).ConfigureAwait(false);
           
            var pos2 = await t2;
         
            double offset2 = 0.5 - pos2 % 0.5;

            var t4 =  _stepMotion.RelativeMoveWithCheckDone(_axisC2, offset2, 50, cts);

            if ( !await t4)
            {
                return false;
            }
            return true;
        }

        protected bool CheckWeight1(double weight)
        {
            var value =  _weight.ReadWeight(_weightId1).GetAwaiter().GetResult();
            return value >= weight;
        } 
        
        protected bool CheckWeight2(double weight)
        {
            var value =  _weight.ReadWeight(_weightId2).GetAwaiter().GetResult();
            return value >= weight;
        }


        protected async Task<bool> Z1MoveTo(double offset,CancellationTokenSource cts)
        {
           return await _stepMotion.P2pMoveWithCheckDone(_axisZ1, offset, 20, cts).ConfigureAwait(false);
        }

        protected async Task<bool> Z2MoveTo(double offset,CancellationTokenSource cts)
        {
            return await _stepMotion.P2pMoveWithCheckDone(_axisZ2, offset, 20, cts).ConfigureAwait(false);
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


        /// <summary>
        /// 推拉感应回调
        /// </summary>
        /// <param name="obj"></param>
        private void RunService_AddSaltContinueEventHandler(string obj)
        {
            _isCanPressDown = true;
        }

        /// <summary>
        /// 推拉感应回调
        /// </summary>
        /// <param name="obj"></param>
        private void RunService_AddSaltPauseEventHandler(string obj)
        {
            _isCanPressDown = false;
        }

        #endregion


    }
}
