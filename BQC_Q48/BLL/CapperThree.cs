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

        #region Private Members

        private static ILogger logger = new MyLogger(typeof(CapperThree));

        private readonly ISyringTwo _syring;

        private readonly ICarrierTwo _carrier;

        private readonly IVibrationTwo _vibration;

        private readonly ICapperFour _capperFour;

        private readonly static object _lockObj = new object(); 

        #endregion

        #region Construtors

        public CapperThree(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess,ICarrierTwo carrier, IVibrationTwo vibration, ISyringTwo syring, ICapperFour capperFour) : base(io, motion, globalStatus, dataAccess, logger)
        {
            this._carrier = carrier;
            this._vibration = vibration;
            this._capperFour = capperFour;
            _axisY = 16;
            _axisC1 = 17;
            _axisC2 = 18;
            _axisZ = 29;
            _holding = 41;
            _claw = 42;
            _holdingCloseSensor = 42;  //I0.2
            _holdingOpenSensor = 43;   //I0.3

            _xOffset = 60;    //拧盖X偏移量

            _posData = _dataAccess.GetCapperPosData(3);
            _syring = syring;
        }

        public override void UpdatePosData()
        {
            _posData = _dataAccess.GetCapperPosData(3);
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
        /// 装盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOnAsync(Sample sample, CancellationTokenSource cts)
        {
            //判断样品是否有盖

            var result = await CapperOn(50, 40, cts).ConfigureAwait(false);

            if (!result)
            {
                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause)
                    {
                        Thread.Sleep(2000);
                    }

                    if (!_globalStatus.IsStopped)
                    {
                        return await CapperOnAsync(sample, cts);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 拆盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOffAsync(Sample sample, CancellationTokenSource cts)
        {
            //判断样品是否有盖
            var result = await CapperOff(cts, -1.3).ConfigureAwait(false);

            if (!result)
            {
                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause)
                    {
                        Thread.Sleep(2000);
                    }

                    if (!_globalStatus.IsStopped)
                    {
                        return await CapperOffAsync(sample, cts);
                    }
                }
            }

            return result;
        }

        //==================================================================离心部分======================================================================================//

        /// <summary>
        /// 从净化管架取试管到移栽（离心） 
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <returns></returns>
        public bool GetSampleFromMarterialToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
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
        public bool GetSampleToBottleOrToSeiling(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    bool result;
                    _logger?.Info($"从试管架取{sample.Id}样品净化管到拧盖3");

                    //从试管架取净化管到拧盖3
                    if (SampleStatusHelper.BitIsOn(sample,SampleStatus.IsPurfyInShelf))
                    {
                        result = _carrier.GetSampleFromMaterialToCapperThree(sample, cts);
                        if (!result)
                        {
                            return false;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInShelf);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInCapper);
                    }

                    //拆盖
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                    {
                        result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品移液管拆盖 失败！ PurifyStatus-{sample.PurifyStatus}");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyUnCapped);
                    }

                    //提取样品液 无浓缩  ==》工艺完成   调用两次
                    if (!TechStatusHelper.BitIsOn(sample.TechParams,TechStatus.ExtractPurify))
                    {
                        //直接提取净化液 需要锁定
                        if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractSample))
                        {
                            result = _capperFour.GetSampleFromPurify(sample, cts);
                            if (!result)
                            {
                                throw new Exception($"从净化管{sample.Id}提取样品失败!");
                            }
                            //程序完成
                            TechStatusHelper.ResetBit(sample.TechParams, TechStatus.ExtractSample);
                        }
                        
                    }
                    //移液到浓缩西林瓶
                    else
                    {
                        //拧盖4动作  搬运西林瓶到试管架

                        result = PipettingFromCapperThreeToCapperFour(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"从净化管{sample.Id}提取浓缩液体失败!");
                        }
                    }

                    //装盖
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                    {
                        result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品净化管装盖 失败!");
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyUnCapped);
                    }

                    //搬回空净化管到试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        result = _carrier.GetSampleFromCapperThreeToMaterial(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"从拧盖3搬运净化管{sample.Id}到试管架失败!");
                        }
                    }

                    //在试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        return true;
                    }

                    throw new Exception($"从移栽取{sample.Id}样品净化管到拧盖3 失败,PurifyStatus-{sample.PurifyStatus}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    _logger?.Error(ex.Message);
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 离心完成后从移栽取出净化管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromTransferToMaterial(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            return _carrier.GetSampleFromTransferToMarterial(sample, func, cts);
        }

        //==================================================================移液部分======================================================================================//

        /// <summary>
        /// 从拧盖3取净化管到移栽  移液 =》 CentrifugalCarrier => CapperThree => CarrierTwo  根据工艺判断是否加液  
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperThreeToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
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
                    if (SampleStatusHelper.BitIsOn(sample,SampleStatus.IsPurfyInShelf))
                    {
                        result = _carrier.GetSampleFromMaterialToCapperThree(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"从试管架取{sample.Id}样品移液管 失败！ PurifyStatus-{sample.PurifyStatus}");
                        }
                    }
                  

                    //拆盖
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                    {
                        result = CapperOffAsync(sample,cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品移液管拆盖 失败！ PurifyStatus-{sample.PurifyStatus}");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyUnCapped);
                    }

                    //是否加液
                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSolveAcetic))
                    {
                        result = AddSolve(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}净化管加醋酸铵水溶液 失败！ PurifyStatus-{sample.PurifyStatus}");
                        }
                        TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddSolveAcetic);
                    }

                    //是否振荡  ==》 下料到拧盖3
                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.VibrationBeforePurify))
                    {
                        //装盖
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                        {
                            result = CapperOffAsync(sample,cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品移液管装盖 失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyUnCapped);
                        }

                        //振荡
                        result = _vibration.StartVibrationTwo(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}净化管加醋酸铵水溶液振荡 失败！ PurifyStatus-{sample.PurifyStatus}");
                        }

                        TechStatusHelper.ResetBit(sample.TechParams, TechStatus.VibrationBeforePurify);
                    }

                    //二次拆盖
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                    {
                        result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品移液管拆盖 失败！ PurifyStatus-{sample.PurifyStatus}");
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
                            throw new Exception($"{sample.Id}样品移液管搬运到移栽 失败！ PurifyStatus-{sample.PurifyStatus}");
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
                if (_globalStatus.IsStopped)
                {
                    _logger?.Info($"从移栽取出{sample.Id}样品（50ml）空管 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从移栽搬运净化管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromTransferToCapperThree(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
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
        /// 从拧盖3搬运净化管到振荡或者搬运净化管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperThreeToVibration(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                bool result;
                lock (_lockObj)
                {
                    //&& sample.TechParams.TechStep == 6
                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.PurifyVibration))//有净化振荡过程
                    {
                        _logger?.Info($"从拧盖3取{sampleId}样品净化管到振荡 振荡");
                    }
                    else
                    {
                        _logger?.Info($"从拧盖3取{sampleId}样品净化空管到试管架");//油脂管
                    }
                  

                    //装盖
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                    {
                        result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
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

                }

                //开始振荡
                if (TechStatusHelper.BitIsOn(sample.TechParams,TechStatus.PurifyVibration))
                {
                    result = _vibration.StartVibrationOne(sample ,cts);
                    if (!result)
                    {
                        throw new Exception($"净化管{sampleId}振荡失败");
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.PurifyVibration);
                }

                //在试管架
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                {
                    return true;
                }

                throw new Exception($"从振荡取{sampleId}样品净化管到试管架失败,SampleStatus-{sample.Status}");

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



        //==================================================================浓缩部分======================================================================================//













        #endregion

        /// <summary>
        /// 从净化管移液到西林瓶浓缩
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool PipettingFromCapperThreeToCapperFour(Sample sample,CancellationTokenSource cts)
        {
            if (SampleStatusHelper.BitIsOn(sample,SampleStatus.IsSelingInCapper)&& SampleStatusHelper.BitIsOn(sample, SampleStatus.IsSelingUnCapped))
            {
                var result = _carrier.DoPipettingTwo(sample,1, cts);
                if (!result)
                {
                    throw new Exception($"西林瓶{sample.Id}移取待浓缩液失败!");
                }
            }
            if (!TechStatusHelper.BitIsOn(sample.TechParams,TechStatus.ExtractPurify))
            {
                return true;
            }
            return false;
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
            double volume = 5;
            _logger?.Info($"净化管{sampleId}加液-{volume}ml");
            try
            {
                //拧盖移动到加液位
                var result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.AddLiquidPos, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("拧盖移动到上下料位 出错");
                }

                //搬运试管到拧盖1  内部判断试管位置
                result = _carrier.GetSampleFromMaterialToCapperThree(sample, cts);
                if (!result)
                {
                    throw new Exception("搬运净化管到拧盖3 出错");
                }

                //判断试管是否有盖 拆盖
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                {
                    result = await CapperOffAsync(sample,cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception("拆盖 出错");
                    }
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyUnCapped);
                }

                //加入醋酸铵水溶液
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSolveAcetic))
                {
                    //加入醋酸铵水溶液
                    result = AddSolve(volume, cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception($"净化管{sampleId}加液 出错");
                    }

                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddSolveAcetic);
                }

                //完成
                if (!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSolveAcetic))
                {
                    return true;
                }

                throw new Exception($"净化管{sampleId}加液-{volume}ml 失败! 状态错误");

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
        protected async Task<bool> AddSolve(double volume, CancellationTokenSource cts,byte solve = 0x01)
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
