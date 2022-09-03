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
    public class AddSolid
    {

        #region Private Members

        private readonly IEtherCATMotion _motion;
        private readonly IIoDevice _io;
        private readonly ILS_Motion _stepMotion;
        private readonly ILogger _logger;
        private readonly IAddSolidPosDataAccess _dataAccess;
        private readonly IWeight _weight;

        #endregion

        #region Private Variant

        private ushort _axisY1 = 0;    //伺服Y轴
        private ushort _axisY2 = 0;    //步进Y轴
        private ushort _axisC1 = 0;    //步进旋转轴
        private ushort _axisC2 = 0;    //步进旋转轴
        private string _XCylinderControl = "Q0.1";
        private string _Z1CylinderControl = "Q0.3";
        private string _Z2CylinderControl = "Q0.5";

        private string _XCylinderHP = "I0.5";
        private string _XCylinderWP = "I0.4";
        private string _Z1CylinderWP = "I0.1";
        private string _Z2CylinderWP = "I0.3";

        private double _stepMoveVel = 30;
        private double _sevorMoveVel = 40;

        #endregion

        private AddSolidPosData _posData;
        private ushort _weightId1 = 1;
        private ushort _weightId2 = 2;

        private IGlobalStatus _globalStatus;

        #region Properties

        #endregion

        #region Construtors

        public AddSolid(IEtherCATMotion motion, IIoDevice io, ILS_Motion stepMotion, IAddSolidPosDataAccess dataAccess,IWeight weight,IGlobalStatus globalStatus, ILogger logger)
        {
            this._motion = motion;
            this._io = io;
            this._stepMotion = stepMotion;
            this._logger = logger;
            this._dataAccess = dataAccess;
            this._weight = weight;
            this._globalStatus = globalStatus;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 加固
        /// </summary>
        /// <param name="solid">种类</param>
        /// <param name="weight">数量</param>
        /// <returns></returns>
        public async Task<bool> AddSolidAsync(int solid,double weight,CancellationTokenSource cts)
        {
            //伺服Y轴移动到指定位置
            var result1 = _motion.P2pMoveWithCheckDone(_axisY1, _posData.Solid_A[0], _sevorMoveVel,cts); 
            var result = await X_Left().ConfigureAwait(false);
            if (!result)
            {
                return false;
            }
            //步进Y轴移动到指定位置
            result = await _stepMotion.P2pMoveWithCheckDone(_axisY2, _posData.Solid_A[1], _stepMoveVel,cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }


            //C1 C2低速旋转
            _stepMotion.VelocityMove(_axisC1, 10);
            _stepMotion.VelocityMove(_axisC2, 10);

            result =  result1.GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //Z1 Z2气缸下降 


            //C1 C2转相对定位


            //检测称台数据判断是否完成



            throw new NotImplementedException();
        }






        /////////////////////////////////////////////////////////////////////
        public async Task<bool> Z1_Low()
        {
            if (_io.ReadBit_DI(_Z1CylinderWP))
            {
                return true;
            }

            var ret = _io.SetBit_DO(_Z1CylinderControl);
            if (!ret)
            {
                _logger?.Error($"AddSolid 点位输出失败");
                return false;
            }
            return await Task.Run(() => 
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    ret = _io.ReadBit_DI(_Z1CylinderWP);
                    if (ret)
                    {
                        break;
                    }
                    if (_globalStatus.IsStopped)
                    {
                        return false;
                    }
                }
                return true;
            }).ConfigureAwait(false);
        }
        public async Task<bool> Z1_High()
        {
            var ret = _io.ResetBit_DO(_Z1CylinderControl);
            if (!ret)
            {
                _logger?.Error($"AddSolid 点位输出失败");
                return false;
            }
            return await Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(2000);
                    ret = !_io.ReadBit_DI(_Z1CylinderWP);
                    if (ret)
                    {
                        break;
                    }
                    if (_globalStatus.IsStopped)
                    {
                        return false;
                    }
                }
                return true;
            }).ConfigureAwait(false);
        }
        public async Task<bool> Z2_Low()
        {
            if (_io.ReadBit_DI(_Z2CylinderWP))
            {
                return true;
            }

            var ret = _io.SetBit_DO(_Z2CylinderControl);
            if (!ret)
            {
                _logger?.Error($"AddSolid 点位输出失败");
                return false;
            }
            return await Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    ret = _io.ReadBit_DI(_Z2CylinderWP);
                    if (ret)
                    {
                        break;
                    }
                    if (_globalStatus.IsStopped)
                    {
                        return false;
                    }
                }
                return true;
            }).ConfigureAwait(false);
        }
        public async Task<bool> Z2_High()
        {
            var ret = _io.ResetBit_DO(_Z2CylinderControl);
            if (!ret)
            {
                _logger?.Error($"AddSolid 点位输出失败");
                return false;
            }
            return await Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    ret = !_io.ReadBit_DI(_Z2CylinderWP);
                    if (ret)
                    {
                        break;
                    }
                    if (_globalStatus.IsStopped)
                    {
                        return false;
                    }
                }
                return true;
            }).ConfigureAwait(false);
        }
        public async Task<bool> X_Right()
        {
            if (_io.ReadBit_DI(_XCylinderWP))
            {
                return true;
            }

            var ret = _io.SetBit_DO(_XCylinderControl);
            if (!ret)
            {
                return false;
            }
            return await Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    ret = _io.ReadBit_DI(_XCylinderWP);
                    if (ret)
                    {
                        break;
                    }
                    if (_globalStatus.IsStopped)
                    {
                        return false;
                    }
                }
                return true;
            }).ConfigureAwait(false);
        }
        public async Task<bool> X_Left()
        {
            if (_io.ReadBit_DI(_XCylinderHP))
            {
                return true;
            }

            var ret = _io.ResetBit_DO(_XCylinderControl);
            if (!ret)
            {
                _logger?.Error($"AddSolid 点位输出失败");
                return false;
            }
            return await Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1000);
                    ret = _io.ReadBit_DI(_XCylinderHP);
                    if (ret)
                    {
                        break;
                    }
                    if (_globalStatus.IsStopped)
                    {
                        return false;
                    }
                }
                return true;
            }).ConfigureAwait(false);
        }

        #endregion


        protected void IntialPosData()
        {
            _posData = _dataAccess.GetPosData();
        }




    }
}
