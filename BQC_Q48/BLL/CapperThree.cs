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
    public class CapperThree : CapperBase, ICapperThree
    {

        private static ILogger logger = new MyLogger(typeof(CapperThree));

        private readonly SyringBase _syring;

        private readonly ICarrierTwo _carrier;


        #region Construtors

        public CapperThree(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess,ICarrierTwo carrier) : base(io, motion, globalStatus, dataAccess, logger)
        {
            this._carrier = carrier;
            _axisY = 16;
            _axisC1 = 17;
            _axisC2 = 18;
            _axisZ = 29;
            _holding = 41;
            _claw = 42;
            _holdingCloseSensor = 42;  //I0.2
            _holdingOpenSensor = 43;   //I0.3

            _xOffset = 60;    //拧盖X偏移量
            _capperTorque = 50;  //拧盖力度
            _capperTimeout = 40;  //拧盖超时时间 S 

            _posData = _dataAccess.GetCapperPosData(3);
            _syring = new SyringTwo(io, motion);
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
            _logger?.Info($"拧盖模块3回零");
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
                if (cts?.IsCancellationRequested == true)
                {
                    return false;
                }
                _logger?.Warn($"{ex.Message}");
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
            ushort sampleId = sample.Id;
            _logger?.Info($"净化管{sampleId}加液");
            try
            {
                //拧盖移动到上下料位
                var result = await MovePutGetPos(cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("拧盖移动到上下料位 出错");
                }

                //搬运试管到拧盖1  内部判断试管位置
                result = _carrier.GetSampleToCapperThree(sample, cts);
                if (!result)
                {
                    throw new Exception("搬运净化管到拧盖3 出错");
                }

                //判断试管是否有盖 拆盖
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped))
                {
                    result = await CapperOff(cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception("拆盖 出错");
                    }
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsUnCapped);
                }

                //加溶剂A   内部判断是否有盖  需要修改
                //if (sample.TechParams.Solvent_A != 0)
                //{
                //    double volume = sample.TechParams.Solvent_A;
                //    result = await AddSolve(0x01, volume, cts).ConfigureAwait(false);
                //}
                ////加溶剂B
                //if (sample.TechParams.Solvent_B != 0)
                //{
                //    double volume = sample.TechParams.Solvent_B;
                //    result = await AddSolve(0x02, volume, cts).ConfigureAwait(false);
                //}
                ////加溶剂C
                //if (sample.TechParams.Solvent_C != 0)
                //{
                //    double volume = sample.TechParams.Solvent_C;
                //    result = await AddSolve(0x04, volume, cts).ConfigureAwait(false);
                //}
                ////加溶剂D
                //if (sample.TechParams.Solvent_D != 0)
                //{
                //    double volume = sample.TechParams.Solvent_D;
                //    result = await AddSolve(0x08, volume, cts).ConfigureAwait(false);
                //}
                //完成
                return true;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

        }



        public bool GetSampleToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            //搬运试管到拧盖3
            _carrier.GetSampleToCapperThree( sample,cts);


            //拆盖  + 加液 + 振荡

            //搬运试管到移栽
            _carrier.GetSampleToTransfer(sample, func, cts);

            return false;
        }






        public bool GetSampleFromMarterialToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            throw new NotImplementedException();
        }

        public bool GetSampleFromTransferToMarterial(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            throw new NotImplementedException();
        }

        public bool GetSampleFromCapperThreeToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            throw new NotImplementedException();
        }

        public bool GetSampleFromTransferToMarterialPiperttor(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            throw new NotImplementedException();
        }





        #endregion



        public async Task<bool> MovePutGetPos(CancellationTokenSource cts)
        {
            //复位抱夹
            OpenHolding();

            //Y轴移动到上下料位置
            var result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _yMoveVel, cts).ConfigureAwait(false);
            if (!result)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// 加液
        /// </summary>
        /// <param name="solve"></param>
        /// <param name="volume"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> AddSolve(byte solve, double volume, CancellationTokenSource cts)
        {
            _logger?.Debug($"AddSolve-{solve}-{volume}");
            try
            {
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
                _logger?.Error(ex.Message);
                throw ex;
            }
        }



        protected CapperPosData GetPosData()
        {
            return _dataAccess.GetCapperPosData(3);
        }

    }
}
