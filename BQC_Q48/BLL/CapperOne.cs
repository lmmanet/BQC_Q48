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
using Q_Platform.Logger;

namespace Q_Platform.BLL
{
    public class CapperOne : CapperBase, ICapperOne
    {

        #region Private Members

        private static ILogger logger = new MyLogger(typeof(CapperOne));

        private readonly SyringBase _syring;

        private readonly ICarrierOne _carrier; 

        #endregion

        #region Constructors

        public CapperOne(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess, ICarrierOne carrier, IAddSolid addSolid) : base(io, motion, globalStatus,dataAccess, logger)
        {
            this._carrier = carrier;


            _axisY = 5;
            _axisC1 = 6;
            _axisC2 = 7;
            _axisZ = 12;
            _holding = 16;
            _claw = 17;
            _holdingCloseSensor = 19;  //I1.3
            _holdingOpenSensor = 20;   //I1.4

            _xOffset = 68;    //加液X偏移量
            _capperTorque = 80;  //拧盖力度
            _capperTimeout = 40;  //拧盖超时时间 S 

            _posData = _dataAccess.GetCapperPosData(1);
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
            _logger?.Info($"拧盖模块1回零");
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
                    _logger?.Info($"拧盖模块1回零 暂停");
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
            _logger?.Info($"样品{sampleId}加液");
            try
            {
                //拧盖移动到上下料位
                var result = await MovePutGetPos(cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("拧盖移动到上下料位 出错") ;
                }

                //搬运试管到拧盖1  内部判断试管位置
                result = _carrier.GetSampleToCapperOne(sample,cts);
                if (!result)
                {
                    throw new Exception("搬运试管到拧盖1 出错");
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
                if (sample.TechParams.Solvent_A != 0)
                {
                    double volume = sample.TechParams.Solvent_A;
                    result = await AddSolve(0x01, volume, cts).ConfigureAwait(false);
                }
                //加溶剂B
                if (sample.TechParams.Solvent_B != 0)
                {
                    double volume = sample.TechParams.Solvent_B;
                    result = await AddSolve(0x02, volume, cts).ConfigureAwait(false);
                }
                //加溶剂C
                if (sample.TechParams.Solvent_C != 0)
                {
                    double volume = sample.TechParams.Solvent_C;
                    result = await AddSolve(0x04, volume, cts).ConfigureAwait(false);
                }
                //加溶剂D
                if (sample.TechParams.Solvent_D != 0)
                {
                    double volume = sample.TechParams.Solvent_D;
                    result = await AddSolve(0x08, volume, cts).ConfigureAwait(false);
                }
                //完成
                return true;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"样品{sample.Id}加液,暂停");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
          
        }

        /// <summary>
        /// 装盖及下料
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="getToMaterial"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> MoveOut(Sample sample,CancellationTokenSource cts)
        {
            try
            {
                bool result;

                //判断是否在拧盖1
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperOne))
                {
                    return true;
                }

                //判断试管是否有盖 装盖
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped))
                {
                    result = await CapperOn(_capperTorque, 40, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsUnCapped);
                }

                ////判断是否要  下料到试管架
                //if (getToMaterial)
                //{
                //    // 下料到试管架
                //    result = _carrier.GetSampleFromCapperOneToMaterial(sample, CloseHold, OpenHold, cts);
                //    if (!result)
                //    {
                //        return false;
                //    }
                //}
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
        protected async Task<bool> AddSolve(byte solve,double volume, CancellationTokenSource cts)
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

        #endregion

        #region Private Methods

        

        #endregion


        /// <summary>
        /// 定时器
        /// </summary>
        /// <param name="time">分钟</param>
        /// <param name="taskCallBack"></param>
        public void Delay(int time,Func<Sample, CancellationTokenSource,Task<bool>> func,Sample sample ,CancellationTokenSource cts)
        {
            DateTime end = DateTime.Now + TimeSpan.FromMinutes(time);
            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(5000);
                    if (cts?.IsCancellationRequested == true)
                    {
                        return;
                    }
                    //时间到
                    if (DateTime.Now > end)
                    {
                        if (cts?.IsCancellationRequested == true)
                        {
                            throw new TaskCanceledException();
                        }

                        func?.Invoke(sample,cts);
                        return;
                    }

                    return;
                }
            });
        }



     

    }
}
