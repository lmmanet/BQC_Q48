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

        private readonly ICarrierOne _carrier;

        #region Private Members

        private readonly IEtherCATMotion _motion;
        private readonly IIoDevice _io;
        private readonly ILS_Motion _stepMotion;
        private readonly ILogger _logger;
        private readonly IAddSolidPosDataAccess _dataAccess;
        private readonly IWeight _weight;

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

        private double _xOffset = 50;                    //工位1 2 X方向坐标偏移

        private AddSolidPosData _posData;                //位置数据
        private IGlobalStatus _globalStatus;
        private int _step = -1;
         
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
                if (cts?.IsCancellationRequested == true)
                {
                    return false;
                }
                _logger?.Error($"GoHome err:{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 加固
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> AddSolidAsync(Sample sample,int[] solids,double[] weights, CancellationTokenSource cts)
        {
            try
            {
                //加固到接驳位
                var result = await MovePutGetPos(cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //从拧盖1处搬试管(搬运到加固位)
                result = _carrier.GetSampleToAddSolid(sample, cts);
                if (!result)
                {
                    return false;
                }
               
                //加固   判断加固种类
                result = await Add_Solid(solids, weights, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //完成后加固到接驳位
                result = await MovePutGetPos(cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //搬运试管到拧盖1位
                result = _carrier.GetSampleToCapperOne(sample, cts);
                if (!result)
                {
                    return false;
                }

                //完成
                return true;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        #endregion

        #region Protected Methods

        protected async Task<bool> MovePutGetPos(CancellationTokenSource cts)
        {
            //步进Y轴移动到原点位置
            var result = await _stepMotion.P2pMoveWithCheckDone(_axisY2, 0, _stepMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception("步进Y轴移动到指定位出错");
            }
            X_Cylinder_Right();

            return true;

        }


        /// <summary>
        /// 加固
        /// </summary>
        /// <param name="solids">添加种类1~6 </param>
        /// <param name="weights">加固重量</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> Add_Solid(int[] solids,double[] weights, CancellationTokenSource cts)
        {
            int count = weights.Length;
            while (_step < count)
            {
                if (solids[_step] <= 0 || weights[_step] <= 0)
                {
                    _step++;
                    continue;
                }
                var result = await Add_SolidSub(solids[_step], weights[_step], cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
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
                double[] pos = GetSolidCoordinate(solid);
                await Task.Delay(1000).ConfigureAwait(false);
                //获取毛重
                var gross1 = await _weight.ReadWeight(_weightId1).ConfigureAwait(false);
                var gross2 = await _weight.ReadWeight(_weightId2).ConfigureAwait(false);
                //X气缸移动到加固位
                X_Cylinder_Left();
                //伺服Y轴移动到指定位置
                var result = await _motion.P2pMoveWithCheckDone(_axisY1, pos[0], _sevorMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("伺服Y轴移动到指定位置出错");
                }
                //步进Y轴移动到指定位置
                result = await _stepMotion.P2pMoveWithCheckDone(_axisY2, pos[1], _stepMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("步进Y轴移动到指定位置出错");
                }

                //C1 C2低速旋转
                _stepMotion.VelocityMove(_axisC1, _rotateVel);
                _stepMotion.VelocityMove(_axisC2, _rotateVel);

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
                //Z1 Z2气缸上升
                Z1_Cylinder_Up();
                Z2_Cylinder_Up();

                //停止电机旋转
                await _stepMotion.StopMove(_axisC1).ConfigureAwait(false);
                await _stepMotion.StopMove(_axisC2).ConfigureAwait(false);

                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                _logger?.Debug($"Add_SolidSub err:{ex.Message}");
                throw ex;
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
                if (temp > 6)
                {
                    throw new TimeoutException("Z1_Cylinder_Up 超时");
                }
            } while (!result);
        }

        protected void Z1_Cylinder_Down(bool checkSensor = true)
        {
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
                if (temp > 6)
                {
                    throw new TimeoutException("Z1_Cylinder_Down 超时");
                }
            } while (!result);
        }

        protected void Z2_Cylinder_Up(bool checkSensor = false)
        {
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
                if (temp > 6)
                {
                    throw new TimeoutException("Z2_Cylinder_Up 超时");
                }
            } while (!result);
        }

        protected void Z2_Cylinder_Down(bool checkSensor = true)
        {
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
                if (temp > 6)
                {
                    throw new TimeoutException("Z2_Cylinder_Down 超时");
                }
            } while (!result);
        }

        protected void X_Cylinder_Left(bool checkSensor = true)
        {
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
                if (temp > 6)
                {
                    throw new TimeoutException("X_Cylinder_Left 超时");
                }
            } while (!result);
        }

        protected void X_Cylinder_Right(bool checkSensor = true)
        {
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
                if (temp > 6)
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
