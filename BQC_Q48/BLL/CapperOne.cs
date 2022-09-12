using BQJX.Common.Interface;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using BQJX.Common;
using Q_Platform.DAL;
using BQJX.Common.Common;

namespace Q_Platform.BLL
{
    public class CapperOne : CapperBase
    {

        private readonly SyringBase _syring;

        #region Constructors

        public CapperOne(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess, ILogger logger) : base(io, motion, globalStatus,dataAccess, logger)
        {
            _axisY = 5;
            _axisC1 = 6;
            _axisC2 = 7;
            _axisZ = 12;
            _holding = 16;
            _claw = 17;
            _holdingCloseSensor = 19;  //I1.3
            _holdingOpenSensor = 20;   //I1.4

            _xOffset = 68;    //加液X偏移量

            _posData = GetPosData();
            _syring = new SyringOne(io,motion);
        }

        #endregion
                
        #region Public Methods

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public override async Task<bool> GoHome(CancellationTokenSource cts)
        {
            try
            {
                //注射器回零
                var result1 = _syring.GoHome(cts).ConfigureAwait(false);
                //拧盖回零
                var result2 = base.GoHome(cts).ConfigureAwait(false);
                if (!await result1 || !await result2)
                {
                    throw new Exception($"回零出错result1:{result1.GetAwaiter().GetResult()}，result2:{result2.GetAwaiter().GetResult()}");
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
        /// 加液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> AddSolve(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                //判断样品是否需要加液

                //判断样品是否完成加液

                //判断样品是否有盖

                //加溶剂A
                if (sample.TechParams.Solvent_A != 0)
                {
                    double volume = sample.TechParams.Solvent_A;
                    var result = await AddSolve(0x01, volume, cts).ConfigureAwait(false);
                }
                //加溶剂B
                if (sample.TechParams.Solvent_B != 0)
                {
                    double volume = sample.TechParams.Solvent_B;
                    var result = await AddSolve(0x02, volume, cts).ConfigureAwait(false);
                }
                //加溶剂C
                if (sample.TechParams.Solvent_C != 0)
                {
                    double volume = sample.TechParams.Solvent_C;
                    var result = await AddSolve(0x04, volume, cts).ConfigureAwait(false);
                }
                //加溶剂D
                if (sample.TechParams.Solvent_D != 0)
                {
                    double volume = sample.TechParams.Solvent_D;
                    var result = await AddSolve(0x08, volume, cts).ConfigureAwait(false);
                }


                return true;
            }
            catch (Exception ex)
            {

                throw ex;
            }
          
        }


        #endregion


        #region Protected Methods

        /// <summary>
        /// 加液
        /// </summary>
        /// <param name="solve"></param>
        /// <param name="volume"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> AddSolve(byte solve,double volume, CancellationTokenSource cts)
        {
            try
            {
                //判断试管是否有盖子
                if (!_haveCapper)
                {
                    //试管有盖   先拆盖
                    if (!await base.CapperOff(cts).ConfigureAwait(false))
                    {
                        //试管有盖拆盖出错
                        return false;
                    }
                }

                //抱夹夹紧
                _io.WriteBit_DO(_holding, true);

                //Y轴移动到加液位
                var result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.AddLiquidPos, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //开始加液
                result = await _syring.AddSolve(solve, volume, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                return true;

            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        #endregion


        #region Private Methods



        #endregion


        protected CapperPosData GetPosData()
        {
           return  _dataAccess.GetCapperPosData(1);
        }




    }
}
