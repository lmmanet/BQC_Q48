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

        private readonly static object _lockObj = new object();

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
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                {
                    result = await CapperOff(cts, -1.3).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception("拆盖 出错");
                    }
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyUnCapped);
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

        //==================================================================离心部分======================================================================================//

        /// <summary>
        /// 从净化管架取试管到移栽（离心） 
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <returns></returns>
        public bool GetSampleFromMarterialToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            //从试管架取振荡完的净化管去离心   
            return _carrier.GetSampleFromMaterialToTransfer(sample, func, cts);
        }

        /// <summary>
        /// 离心完成后从移栽中取出试管 
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromTransferToMarterial(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            //从移栽取下试管到试管架

            return _carrier.GetSampleFromTransferToMarterial(sample, func, cts);
        }


        //==================================================================移液部分======================================================================================//

        /// <summary>
        /// 根据工艺判断是否加液  为实现
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperThreeToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从试管架取{sample.Id}样品移液管");

                    //拧盖移动到上下料位
                    var result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //从试管架取试管到拧盖3
                    result = _carrier.GetSampleFromMaterialToCapperThree(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"从试管架取{sample.Id}样品移液管 失败！ TubeStatus-{sample.TubeStatus}");
                    }

                    //拆盖
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                    {
                        result = CapperOff(cts, -1.3).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品移液管拆盖 失败！ TubeStatus-{sample.TubeStatus}");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //搬运到移栽
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        result = _carrier.GetSampleFromCapperThreeToTransfer(sample, func, cts);
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品移液管搬运到移栽 失败！ TubeStatus-{sample.TubeStatus}");
                        }
                    }

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                    {
                        return true;
                    }

                    throw new Exception($"从试管架取{sample.Id}样品移液管到移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽取出{sample.Id}样品（50ml）空管 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从移栽搬运无盖净化管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromTransferToCapperThree(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从移栽取{sample.Id}样品净化管到拧盖3");

                    //拧盖移动到上下料位
                    var result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //从移栽取试管到拧盖3
                    result = _carrier.GetSampleFromTransferToCapperThree(sample, func, cts);
                    if (!result)
                    {
                        throw new Exception($"从移栽取{sample.Id}样品净化管到拧盖3 失败!");
                    }

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        return true;
                    }

                    throw new Exception($"从移栽取{sample.Id}样品净化管到拧盖3失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽取{sample.Id}样品净化管到拧盖3 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从拧盖3搬运有盖净化盖到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperThreeToMaterial(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                bool result;
                lock (_lockObj)
                {
                    _logger?.Info($"从拧盖3取{sampleId}样品净化管到试管架");

                    //拆盖
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                    {
                        result = CapperOn(30, 40, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品净化管装盖 失败!");
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //搬运到移栽
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        result = _carrier.GetSampleFromCapperThreeToMaterial(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"从拧盖3取{sampleId}样品净化管到试管架 失败！ TubeStatus-{sample.TubeStatus}");
                        }
                    }

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        return true;
                    }

                    throw new Exception($"从拧盖3取{sampleId}样品净化管到试管架失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从拧盖3取{sampleId}样品净化管到试管架 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }



     

        public bool GetSampleFromTransferToMarterialPiperttor(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            //从移栽取下完成移液的净化管   空管或者需要振荡的净化管

            //拧盖3装盖

            //搬运到振荡2  或直接下料到试管架

            //完成振荡后下料到试管架
            ushort sampleId = sample.Id;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从试管架取{sample.Id}样品移液管");

                    //拧盖移动到上下料位
                    var result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //从试管架取试管到拧盖3
                    result = _carrier.GetSampleFromTransferToCapperThree(sample, func, cts);
                    if (!result)
                    {
                        throw new Exception($"从移栽取{sample.Id}样品净化管 失败！ TubeStatus-{sample.TubeStatus}");
                    }

                    //拆盖
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                    {
                        result = CapperOn(30,40, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品移液管装盖 失败！ TubeStatus-{sample.TubeStatus}");
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //搬运到移栽
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        result = _carrier.GetSampleFromCapperThreeToMaterial(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品移液管搬运到移栽 失败！ TubeStatus-{sample.TubeStatus}");
                        }
                    }

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                    {
                        return true;
                    }

                    throw new Exception($"从试管架取{sample.Id}样品移液管到移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽取出{sample.Id}样品（50ml）空管 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
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
