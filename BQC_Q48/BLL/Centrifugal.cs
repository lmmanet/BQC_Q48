using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Communication;
using BQJX.Core;
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
    public class Centrifugal : ICentrifugal
    {

        #region Private Members

        private readonly IEtherCATMotion _motion;
        private readonly IIoDevice _io;
        private readonly ILS_Motion _stepMotion;
        private readonly IEPG26 _claw;
        private readonly ILogger _logger;
        private readonly ICentrifugalCarrierPosDataAccess _dataAccess;
        private CentrifugalCarrierPosData _posData;
        #endregion

        #region Variants

        private ushort _axisCentrigugal = 8; //离心机轴
        private ushort _axisZ = 7;  //搬运Z轴
        private ushort _axisX = 14;  //搬运X轴
        private ushort _axisC = 15;  //搬运旋转轴
        private ushort _homeMode = 33; //离心机回零模式

        private ushort _shadowOpen = 0; //离心机门打开控制
        private ushort _shadowClose = 1; //离心机门关闭控制
        private ushort _shadowOpenSensor = 0; //离心机门打开感应
        private ushort _shadowCloseSensor = 1; //离心机门关闭感应

        private ushort _y_Ctr = 2; //Y气缸控制
        private ushort _y_HP = 2; //Y气缸缩回感应
        private ushort _y_WP = 3; //Y气缸伸出感应

        private ushort _clawId = 2;  //电爪485地址
        private byte _clawOpen = 0;  //电爪打开位置
        private byte _clawClose = 255; //电爪关闭位置
        private double _stepMoveVel = 30;                //步进电机移动速度
        private double _sevorMoveVel = 60;               //伺服电机移动速度
        #endregion

        #region Construtors

        public Centrifugal(IEtherCATMotion motion, IIoDevice io, ILS_Motion stepMotion, IEPG26 claw, ICentrifugalCarrierPosDataAccess dataAccess, ILogger logger)
        {
            this._motion = motion;
            this._io = io;
            this._stepMotion = stepMotion;
            this._dataAccess = dataAccess;
            this._claw = claw;
            this._logger = logger;

            _posData = _dataAccess.GetPosData();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 模块回零
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GoHome()
        {
            try
            {
                //Z轴回零
                var result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, 10, null).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
                //X C 离心机轴回零
                var ret1 = _stepMotion.GoHomeWithCheckDone(_axisX, null).ConfigureAwait(false);
                var ret2 = _stepMotion.GoHomeWithCheckDone(_axisC, null).ConfigureAwait(false);
                var ret3 = Centri_GoHome().ConfigureAwait(false);
                if (!ret1.GetAwaiter().GetResult() || !ret2.GetAwaiter().GetResult() || !ret3.GetAwaiter().GetResult())
                {
                    return false;
                }
                //气缸回零
                Y_Cylinder_Put();
                OpenShadow();

                return true;
            }
            catch (CommunicationException cmex)
            {
                return false;
            }
            catch (EtherCATMotionException ecex)
            {
                return false;
            }
            catch (Exception ex)
            {
                throw ex;
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

        protected async Task<bool> DoCentrifugal(int time,int vel, CancellationTokenSource cts)
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
            await Task.Delay(10000).ConfigureAwait(false);


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

        protected async Task<bool> GetTubeIn(int tube,CancellationTokenSource cts)
        {
            //Z轴上升到0位 
            var result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }

            //X轴移动到位置1  //C轴旋转到大小试管位   //离心机旋转到指定位1 
            var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, GetXCenter1Coordinate(),_stepMoveVel ,cts).ConfigureAwait(false);
            var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, GetCCenterBigCoordinate(), _stepMoveVel, cts).ConfigureAwait(false);
            var result3 = GoStation(1).ConfigureAwait(false);

            //Y气缸移动到位
            Y_Cylinder_Get();

            //Z轴下降抓取第一支试管
            if (!await result1||!await result2)
            {
                return false;
            }
            result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZGetCoordinate(), _sevorMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }

            //手爪夹紧
            result = await _claw.SendCommand(_clawId, _clawClose, 255, 255).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }
            //判断手爪夹紧
            await _claw.CheckDone(_clawId,30).ConfigureAwait(false);

            var status = await _claw.ClawGetchStatus(_clawId).ConfigureAwait(false);
            if (status != 2)
            {
                return false;
            }

            //Z轴上升到0位 
            result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }

            //Y轴移动到离心机位
            Y_Cylinder_Put();

            //判断离心机旋转到指定位1 //打开离心机门
            if (!await result3)
            {
                return false;
            }
            OpenShadow();

            //Z轴下降到放料位
            result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZPutCoordinate(), _sevorMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }

            //手爪打开
            result = await _claw.SendCommand(_clawId, _clawOpen, 255, 255).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }

            //判断手爪打开
            await _claw.CheckDone(_clawId, 30).ConfigureAwait(false);

            status = await _claw.ClawGetchStatus(_clawId).ConfigureAwait(false);
            if (status != 3)
            {
                return false;
            }

            //Z轴上升到0位
            result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }

            return true;
        }

        protected async Task<bool> GetTubeOut(int tube,CancellationTokenSource cts)
        {
            //Z轴上升到0位 
            var result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }
            //X轴移动到位置1 //C轴旋转到大小试管位  //离心机旋转到指定位1 
            var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, GetXCenter1Coordinate(), _stepMoveVel, cts).ConfigureAwait(false);
            var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, GetCCenterBigCoordinate(), _stepMoveVel, cts).ConfigureAwait(false);
            var result3 = GoStation(1).ConfigureAwait(false);


            //Y气缸移动到位
            Y_Cylinder_Put();

            //离心机门打开
            OpenShadow();

            //Z轴下降到放料位
            result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZPutCoordinate(), _sevorMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }
            //手爪夹紧

            //Z轴上升到0位 

            //Y轴移动到离心机位

            //离心机旋转到指定位1

            //Z轴下降到放料位

            //手爪打开

            //Z轴上升到0位
            result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }
            return true;
        }










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
                    throw new ActionTimeoutException("离心机护罩打开超时");
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
                    throw new ActionTimeoutException("离心机护罩关闭超时");
                }
            } while (!result);
        }

        /// <summary>
        /// Y气缸取放料位
        /// </summary>
        protected void Y_Cylinder_Get(bool checkSensor = true)
        {
            var result = _io.WriteBit_DO(_y_Ctr, true);
            if (!result)
            {
                throw new Exception("Y_Cylinder_Get Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_y_WP);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new ActionTimeoutException("Y气缸到取放料位超时");
                }
            } while (!result);
        }

        /// <summary>
        /// Y气缸离心机位
        /// </summary>
        protected void Y_Cylinder_Put(bool checkSensor = true)
        {
            var result = _io.WriteBit_DO(_y_Ctr, false);
            if (!result)
            {
                throw new Exception("Y_Cylinder_Put Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_y_HP);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new ActionTimeoutException("Y气缸到离心机位超时");
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



        private double GetXRightCoordinate()
        {
            return _posData.RightPos;
        }
        private double GetCRightCoordinate()
        {
            return 0;
        }

        private double GetXLeftCoordinate()
        {
            return _posData.LeftPos;
        }
        
        /// <summary>
        /// 获取中间取料位置1
        /// </summary>
        /// <returns></returns>
        private double GetXCenter1Coordinate()
        {
            return 0;
        }

        /// <summary>
        /// 获取中间取料位置2
        /// </summary>
        /// <returns></returns>
        private double GetXCenter2Coordinate()
        {
            return 0;
        }

        /// <summary>
        /// 获取中间大管旋转位置
        /// </summary>
        /// <returns></returns>
        private double GetCCenterBigCoordinate()
        {
            return 0;
        }

        /// <summary>
        /// 获取中间小管旋转位置
        /// </summary>
        /// <returns></returns>
        private double GetCCenterSmallCoordinate()
        {
            return 0;
        }


        /// <summary>
        /// 获取Z取试管坐标
        /// </summary>
        /// <returns></returns>
        private double GetZGetCoordinate()
        {
            return _posData.ZGetPos;
        }

        /// <summary>
        /// 获取Z离心机放料坐标
        /// </summary>
        /// <returns></returns>
        private double GetZPutCoordinate()
        {
            return _posData.ZPutPos;
        }

    }
}
