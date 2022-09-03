using BQJX.Communication;
using BQJX.Core;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class Centrifugal
    {

        #region Private Members

        private readonly IEtherCATMotion _motion;
        private readonly IIoDevice _io;
        private readonly ILS_Motion _stepMotion;
        private readonly IEPG26 _claw;
        private readonly ILogger _logger;

        #endregion

        #region Variants

        private ushort _axisCentrigugal = 8; //离心机轴
        private ushort _axisZ = 7;  //搬运Z轴
        private ushort _axisX = 14;  //搬运X轴
        private ushort _axisC = 15;  //搬运旋转轴
        private ushort _homeMode = 33; //离心机回零模式

        private ushort _shadowOpen = 0; //离心机门打开控制
        private ushort  _shadowClose = 1; //离心机门关闭控制
        private ushort _shadowOpenSensor = 0; //离心机门打开感应
        private ushort _shadowCloseSensor = 1; //离心机门关闭感应

        private ushort _y_Ctr = 2; //Y气缸控制
        private ushort _y_HP = 2; //Y气缸缩回感应
        private ushort _y_WP = 3; //Y气缸伸出感应

        private byte _clawOpen = 0;  //电爪打开位置
        private byte _clawClose = 0; //电爪关闭位置

        #endregion

        #region Construtors

        public Centrifugal(IEtherCATMotion motion, IIoDevice io, ILS_Motion stepMotion, IEPG26 claw, ILogger logger)
        {
            this._motion = motion;
            this._io = io;
            this._stepMotion = stepMotion;
            this._claw = claw;
            this._logger = logger;
        }

        #endregion





        /// <summary>
        /// 模块回零
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GoHome()
        {
            try
            {
                //Z轴回零
                var result = await Z_GoHome().ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
                //X C 离心机轴回零
                var ret1 = X_GoHome().ConfigureAwait(false);
                var ret2 = C_GoHome().ConfigureAwait(false);
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




        #region Protected Methods


        /// <summary>
        /// 打开离心机护罩
        /// </summary>
        public void OpenShadow()
        {
            _io.WriteBit_DO(_shadowClose,false);
            _io.WriteBit_DO(_shadowOpen,true);
            bool result = false;
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_shadowOpenSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new Exception("离心机护罩打开超时");
                }
            } while (!result);
        }

        /// <summary>
        /// 关闭离心机护罩
        /// </summary>
        public void CloseShadow()
        {
            _io.WriteBit_DO(_shadowOpen,false);
            _io.WriteBit_DO(_shadowClose,true);
            bool result = false;
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_shadowCloseSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new Exception("离心机护罩关闭超时");
                }
            } while (!result);
        }

        /// <summary>
        /// Y气缸取放料位
        /// </summary>
        public void Y_Cylinder_Get()
        {
            _io.WriteBit_DO(_y_Ctr, true);
            bool result = false;
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_y_WP);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new Exception("Y气缸到取放料位超时");
                }
            } while (!result);
        }
           
        /// <summary>
        /// Y气缸离心机位
        /// </summary>
        public void Y_Cylinder_Put()
        {
            _io.WriteBit_DO(_y_Ctr, false);
            bool result = false;
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_y_HP);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new Exception("Y气缸到离心机位超时");
                }
            } while (!result);
        }



        /// <summary>
        /// 旋转到指定工位
        /// </summary>
        /// <param name="num">1:初始位 2：90°位 3：180°位 4：270°位</param>
        public void GoStation(ushort num)
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

            var result = _motion.GohomeWithCheckDone(_axisCentrigugal, _homeMode, null).GetAwaiter().GetResult();
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

                result = _motion.P2pMoveWithCheckDone(_axisCentrigugal, offset, 1, null).GetAwaiter().GetResult();
                if (!result)
                {
                    _logger?.Error($"离心机转到指定位置出错");
                    throw new Exception("离心机转到指定位置出错");
                }
            }
            else
            {
                return;
            }
        }


        /// <summary>
        /// 旋转轴回零
        /// </summary>
        /// <returns></returns>
        public async Task<bool> C_GoHome()
        {
            return await _stepMotion.GoHomeWithCheckDone(_axisC,null).ConfigureAwait(false);
        }

        /// <summary>
        /// X轴回零
        /// </summary>
        /// <returns></returns>
        public async Task<bool> X_GoHome()
        {
            return await _stepMotion.GoHomeWithCheckDone(_axisX, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Z轴回零
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Z_GoHome()
        {
            return await _motion.P2pMoveWithCheckDone(_axisZ, 0, 10, null).ConfigureAwait(false);
        }

        /// <summary>
        /// 离心机回零
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Centri_GoHome()
        {
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
            return true;
        }

        #endregion










    }
}
