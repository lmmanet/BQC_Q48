using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Core.Interface;
using Q_Platform.DAL;
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
        private readonly ILS_Motion _stepMotion;
        private readonly IEPG26 _claw;
        private readonly ILogger _logger;
        private readonly ICentrifugalCarrierPosDataAccess _dataAccess;
        private CentrifugalCarrierPosData _posData;

        private readonly ICarrierOne _carrierOne;
        private readonly ICarrierTwo _carrierTwo;
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
        private ushort _y_HP = 3; //Y气缸缩回感应
        private ushort _y_WP = 2; //Y气缸伸出感应

        private ushort _clawId = 2;  //电爪485地址
        private byte _clawOpen = 0;  //电爪打开位置
        private byte _clawClose = 255; //电爪关闭位置
        private double _stepMoveVel = 30;                //步进电机移动速度
        private double _sevorMoveVel = 50;               //伺服电机移动速度
        #endregion

        #region Construtors

        public Centrifugal(IEtherCATMotion motion, IIoDevice io, ILS_Motion stepMotion, IEPG26 claw,ICarrierOne carrierOne,ICarrierTwo carrierTwo, ICentrifugalCarrierPosDataAccess dataAccess)
        {
            this._motion = motion;
            this._io = io;
            this._stepMotion = stepMotion;
            this._dataAccess = dataAccess;
            this._claw = claw;

            this._carrierOne = carrierOne;
            this._carrierTwo = carrierTwo;

            this._logger = new MyLogger(typeof(Centrifugal));

            _posData = _dataAccess.GetPosData();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 模块回零
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GoHome(CancellationTokenSource cts)
        {
            try
            {
                //使能夹爪
                var result = await _claw.Enable(_clawId).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //Z轴回零
                result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, 10, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
                //X C 离心机轴回零
                await _stepMotion.ServoOn(_axisX).ConfigureAwait(false);
                await _stepMotion.ServoOn(_axisC).ConfigureAwait(false);
                var ret1 = _stepMotion.GoHomeWithCheckDone(_axisX, cts).ConfigureAwait(false);
                var ret2 = _stepMotion.GoHomeWithCheckDone(_axisC, cts).ConfigureAwait(false);
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
                _logger?.Error(ex.Message);
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

        /// <summary>
        /// 离心机搬运搬运试管到离心机
        /// </summary>
        /// <param name="sampleId">试管编号</param>
        /// <param name="isBig">是否是大管</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> GetTubeIn(Sample sample, bool isBig,CancellationTokenSource cts)
        {
            byte clawOpen = 110;
            ushort sampleId = sample.Id;

            //判断大小管
            ushort pos1 = 2,pos2 = 4;
            if (isBig)
            {
                clawOpen = 0;
                pos1 = 1;
                pos2 = 3;
            }

            if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCentrifugal))
            {
                if (sample.TubeStatus == 0)
                {
                    //移栽上取料
                    var result = await GetTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId - 1), isBig), clawOpen, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }

                    //离心机放料
                    result = await PutTubeAtCentrifugal( pos1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    sample.TubeStatus = 1;
                }

                if (sample.TubeStatus == 1)
                {
                    //移栽上取料
                    var result = await GetTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId), isBig), clawOpen, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }

                    //离心机放料
                    result = await PutTubeAtCentrifugal(pos2, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    sample.TubeStatus = 0;
                }

                SampleStatusHelper.ResetBit(sample, SampleStatus.IsInTransfer);
                SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCentrifugal);
            }

            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCentrifugal))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 从离心机取出试管
        /// </summary>
        /// <param name="tube"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> GetTubeOut(Sample sample, bool isBig, CancellationTokenSource cts)
        {
            byte clawOpen = 110;
            ushort sampleId = sample.Id;

            //判断大小管
            ushort pos1 = 2, pos2 = 4;
            if (isBig)
            {
                clawOpen = 0;
                pos1 = 1;
                pos2 = 3;
            }

            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCentrifugal))
            {
                if (sample.TubeStatus == 0)
                {
                    //离心机取料
                    var result = await GetTubeAtCentrifugal(pos1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }

                    //移栽放料
                    result = await PutTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId - 1), isBig), clawOpen, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    sample.TubeStatus = 1;
                }

                if (sample.TubeStatus == 1)
                {
                    //离心机取料
                    var result = await GetTubeAtCentrifugal(pos2, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }

                    //移栽放料
                    result = await PutTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId ), isBig), clawOpen, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    sample.TubeStatus = 0;
                }

                SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCentrifugal);
                SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
            }

            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
            {
                return true;
            }

            return false;

        }

        ///////==========================================================================================================================////

        /// <summary>
        /// 在移栽上取料
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="clawOpenByte"></param>
        /// <param name="cts"></param>
        /// <param name="clawCloseByte"></param>
        /// <returns></returns>
        protected async Task<bool> GetTubeAtTransfer(double[] pos, byte clawOpenByte, CancellationTokenSource cts, byte clawCloseByte = 255)
        {
            try
            {
                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece())
                {
                    //打开手爪到指定位置
                    var ret = await OpenClaw(clawOpenByte).ConfigureAwait(false);
                    if (!ret)
                    {
                        throw new Exception("手爪打开出错");
                    }
                }
                else //手爪有抓取物件
                {
                    return true;
                }

                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);
           
                //X轴移动到位置1  //C轴旋转到大小试管位   //离心机旋转到指定位1 
                var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, pos[0], _stepMoveVel, cts).ConfigureAwait(false);
                var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, pos[1], _stepMoveVel, cts).ConfigureAwait(false);
                //Y气缸移动到位
                Y_Cylinder_Get();

                //Z轴下降抓取第一支试管
                if (!await result1 || !await result2)
                {
                    throw new Exception("离心移栽运动出错") ;
                }
                var result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZTransferCoordinate(true), _sevorMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Z轴运动到取料位出错");
                }

                //手爪夹紧
                result = await CloseClaw(clawCloseByte).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪夹紧出错");
                }

                return true;

            }
            catch (Exception ex)
            {
                await OpenClaw(clawOpenByte).ConfigureAwait(false);
                await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }

        }

        /// <summary>
        /// 在移栽上放料放料
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="clawOpenByte"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> PutTubeAtTransfer(double[] pos, byte clawOpenByte, CancellationTokenSource cts)
        {
            try
            {
                //如果手爪打开 判定放料完成
                if (await _claw.ClawGetchStatus(_clawId) == 1)
                {
                    return true;

                }

                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece())
                {
                    throw new Exception("手爪上无试管");
                }

                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);
            

                //X轴移动到位置1  //C轴旋转到大小试管位   //离心机旋转到指定位1 
                var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, pos[0], _stepMoveVel, cts).ConfigureAwait(false);
                var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, pos[1], _stepMoveVel, cts).ConfigureAwait(false);
                //Y气缸移动到位
                Y_Cylinder_Get();

                //Z轴下降抓取第一支试管
                if (!await result1 || !await result2)
                {
                    throw new Exception("离心移栽运动出错");
                }
                var result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZTransferCoordinate(false), _sevorMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Z轴运动到放料位出错");
                }

                //手爪松开
                result = await OpenClaw(clawOpenByte).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪打开出错");
                }

                //判断Z轴是否在原点
                if (!await CheckAxisZInSafePos(cts))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                await OpenClaw(clawOpenByte).ConfigureAwait(false);
                await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }
        }

        /// <summary>
        /// 离心机取料
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> GetTubeAtCentrifugal(ushort pos, CancellationTokenSource cts, byte clawCloseByte = 255, byte clawOpenByte = 0)
        {
            try
            {
                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece())
                {
                    //打开手爪到指定位置
                    var ret = await OpenClaw(clawOpenByte).ConfigureAwait(false);
                    if (!ret)
                    {
                        throw new Exception("手爪打开出错");
                    }
                }
                else //手爪有抓取物件
                {
                    return true;
                }

                //离心机转到指定位置
                var result3 = GoStation(pos).ConfigureAwait(false);

                //Y轴移动到离心机位
                Y_Cylinder_Put();

                //判断离心机旋转到指定位1 //打开离心机门
                if (!await result3)
                {
                    throw new Exception("离心机转到指定位出错");
                }
                OpenShadow();

                //Z轴下降到取料位
                var result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZCentrifugalCoordinate(true), _sevorMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Z轴到取料位出错！");
                }

                //手爪夹紧
                result = await CloseClaw(clawCloseByte).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪夹紧出错");
                }

                return true;
            }
            catch (Exception ex)
            {
                await OpenClaw(clawOpenByte).ConfigureAwait(false);
                await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }
        }

        /// <summary>
        /// 离心机放料
        /// </summary>
        /// <param name="clawOpenByte">手爪打开位置</param>
        /// <param name="pos">离心机旋转位</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> PutTubeAtCentrifugal(ushort pos, CancellationTokenSource cts, byte clawOpenByte = 0)
        {
            try
            {
                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);


                //如果手爪打开 判定放料完成
                if (await _claw.ClawGetchStatus(_clawId) == 1)
                {
                    return true;

                }

                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece())
                {
                    throw new Exception("手爪上无试管");
                }

                //离心机转到指定位置
                var result3 = GoStation(pos).ConfigureAwait(false);

                //Y轴移动到离心机位
                Y_Cylinder_Put();

                //判断离心机旋转到指定位1 //打开离心机门
                if (!await result3)
                {
                    throw new Exception("离心机转到指定位出错");
                }
                OpenShadow();

                //Z轴下降到放料位
                var result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZCentrifugalCoordinate(false), _sevorMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Z轴到放料位出错！");
                }

                //手爪打开
                result = await OpenClaw(clawOpenByte).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪打开出错！");
                }

                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                await OpenClaw(clawOpenByte).ConfigureAwait(false);
                await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }
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
                    throw new TimeoutException("Y气缸到取放料位超时");
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
                    throw new TimeoutException("Y气缸到离心机位超时");
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

        /// <summary>
        /// 检查Z轴是否在安全位置
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> CheckAxisZInSafePos(CancellationTokenSource cts)
        {
            if (!AxisIsInSafePos(_axisZ))
            {
                var result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"Z轴移动到安全位置失败!");
                }
            }
            return true;
        }

        /// <summary>
        /// 打开手爪到指定位置
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> OpenClaw(byte openPos)
        {
            var result = await _claw.SendCommand(_clawId, openPos, 255, 255).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception("手爪打开失败！");
            }
            int status = 0;
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(10);
            do
            {
                status = _claw.ClawGetchStatus(_clawId).GetAwaiter().GetResult();
                if (status == 3)
                {
                    return true;
                }
                if (status == 1)
                {
                    throw new Exception("手爪打开受阻！");
                }
                Thread.Sleep(300);
                if (DateTime.Now > end)
                {
                    throw new TimeoutException("离心机手爪动作超时！");
                }

            } while (status == 0);

            throw new Exception($"手爪状态错误 err{status}");
        }

        /// <summary>
        /// 关闭手爪
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> CloseClaw(byte closePos)
        {
            var result = await _claw.SendCommand(_clawId, closePos, 255, 255).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception("手爪抓取物件失败！");
            }
            int status = 0;
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(10);
            do
            {
                status = _claw.ClawGetchStatus(_clawId).GetAwaiter().GetResult();
                if (status == 2)
                {
                    return true;
                }
                if (status == 3)
                {
                    throw new Exception("手爪未抓取到物料！");
                }
                Thread.Sleep(300);
                if (DateTime.Now > end)
                {
                    throw new TimeoutException("离心机手爪动作超时！");
                }

            } while (status == 0);
            throw new Exception($"手爪状态错误 err{status}");
        }

        /// <summary>
        /// 手爪是否抓取到物件
        /// </summary>
        /// <returns>true:手爪上有物件 false:手爪上无物件</returns>
        protected async Task<bool> ClawIsGetchPiece()
        {
            var status = await _claw.ClawGetchStatus(_clawId).ConfigureAwait(false);
            return status == 2;
        } 

        #endregion

        #region Private Methods

        /// <summary>
        /// 判断轴是否在安全位置
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        private bool AxisIsInSafePos(ushort axisNo)
        {
            var currentPos = _motion.GetCurrentPos(axisNo);
            return Math.Round(currentPos, 1) == 0;
        }

        /// <summary>
        /// 获取右侧坐标
        /// </summary>
        /// <param name="num">试管编号</param>
        /// <param name="isBig">是否是大管</param>
        /// <returns></returns>
        private double[] GetRightCoordinate(ushort num, bool isBig)
        {
            bool b = num % 2 == 0;
            if (isBig)
            {
                if (b)
                {
                    return new double[] { _posData.RightPos, _posData.CRightPos1 };
                }
                return new double[] { _posData.RightPos, _posData.CRightPos2 };
            }
            else
            {
                if (b)
                {
                    return new double[] { _posData.RightPos, _posData.CRightPos3 };
                }
                return new double[] { _posData.RightPos, _posData.CRightPos4 };
            }
        }

        /// <summary>
        /// 获取左侧坐标
        /// </summary>
        /// <param name="num">试管编号</param>
        /// <param name="isBig">是否是大管</param>
        /// <returns></returns>
        private double[] GetLeftCoordinate(ushort num, bool isBig)
        {
            bool b = num % 2 == 0;
            if (isBig)
            {
                if (b)
                {
                    return new double[] { _posData.LeftPos, _posData.CLeftPutPos1 };
                }
                return new double[] { _posData.LeftPos, _posData.CLeftPutPos2 };
            }
            else
            {
                if (b)
                {
                    return new double[] { _posData.LeftPos, _posData.CLeftPutPos3 };
                }
                return new double[] { _posData.LeftPos, _posData.CLeftPutPos4 };
            }
        }

         /// <summary>
         /// 获取中间取料点位
         /// </summary>
         /// <param name="num">试管编号</param>
         /// <param name="isBig">是否是大管</param>
         /// <returns></returns>
        private double[] GetCenterCoordinate(ushort num,bool isBig)
        {
            bool b = num % 2 == 0;
            if (isBig)
            {
                if (!b)
                {
                    return new double[] { _posData .XCentPos1,_posData.CCentPos1};
                }
                return new double[] { _posData.XCentPos2, _posData.CCentPos1 };
            }
            else
            {
                if (!b)
                {
                    return new double[] { _posData.XCentPos1, _posData.CCentPos2 };
                }
                return new double[] { _posData.XCentPos2, _posData.CCentPos2 };
            }
           
        }

        /// <summary>
        /// 获取Z取试管坐标
        /// </summary>
        /// <param name="isGet">是否获取取料位/放料位</param>
        /// <returns></returns>
        private double GetZTransferCoordinate(bool isGet = true)
        {
            if (!isGet)
            {
                return _posData.ZGetPos - 3;
            }
            return _posData.ZGetPos;
        }

        /// <summary>
        /// 获取Z离心机放料坐标
        /// </summary>
        /// <param name="isGet">是否获取取料位/放料位</param>
        /// <returns></returns>
        private double GetZCentrifugalCoordinate(bool isGet = true)
        {
            if (!isGet)
            {
                return _posData.ZPutPos - 3;
            }
            return _posData.ZPutPos;
        }

        #endregion


    }
}
