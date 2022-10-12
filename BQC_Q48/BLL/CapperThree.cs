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

        //private readonly ICapperFour _capperFour;

        //private readonly ICapperFive _capperFive;

        private readonly static object _lockObj = new object(); 

        #endregion

        #region Construtors

        public CapperThree(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess,ICarrierTwo carrier, 
            IVibrationTwo vibration, ISyringTwo syring) : base(io, motion, globalStatus, dataAccess, logger) //, ICapperFour capperFour,ICapperFive capperFive
        {
            this._carrier = carrier;
            this._vibration = vibration;
            //this._capperFour = capperFour;
            //this._capperFive = capperFive;
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

           s1: var result = await CapperOn(50, 40, cts).ConfigureAwait(false);

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
                        goto s1;
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
            s1: var result = await CapperOff(cts, -1.3).ConfigureAwait(false);

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
                        goto s1;
                    }
                }
            }

            return result;
        }

        //==================================================================移液部分======================================================================================//

        /// <summary>
        /// 从拧盖3取净化管到移栽  移液 =》 CentrifugalCarrier => CapperThree => CarrierTwo  根据工艺判断是否加液  
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperThreeToTransfer(Sample sample ,Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                lock (_lockObj)
                {
                    GlobalCache.Instance.IsCapperThreeOccupy = true;
                    _logger?.Info($"从试管架取{sample.Id}样品移液管");
                    bool result;

                    //拧盖移动到上下料位
                    if (sample.SubStep == 0 && !_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                        sample.SubStep++;
                    }

                    //从试管架取试管到拧盖3
                    if (sample.SubStep == 1 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                        {
                            result = _carrier.GetSampleFromMaterialToCapperThree(sample, cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架取{sample.Id}样品移液管 失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.SubStep++;
                        }
                        else
                        {
                            throw new Exception($"从拧盖3搬运净化管到移栽步骤号错误 subStep-{sample.SubStep}!");
                        }
                       
                    }

                    //拆盖
                    if (sample.SubStep == 2 && !_globalStatus.IsStopped)
                    {
                        if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                        {
                            result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品移液管拆盖 失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyUnCapped);
                            sample.SubStep++;
                        }
                        else
                        {
                            throw new Exception($"从拧盖3搬运净化管到移栽步骤号错误 subStep-{sample.SubStep}!");
                        }
                    }

                    //是否加液
                    if (sample.SubStep == 3 && !_globalStatus.IsStopped)
                    {
                        if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSolveAcetic))
                        {
                            result = AddSolve(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}净化管加醋酸铵水溶液 失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                        }
                        sample.SubStep++;
                    }

                    //是否振荡  ==》 下料到拧盖3
                    if (sample.SubStep == 4 && !_globalStatus.IsStopped)
                    {
                        if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.VibrationBeforePurify))
                        {
                            //装盖
                            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                            {
                                result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"{sample.Id}样品移液管装盖 失败！ PurifyStatus-{sample.PurifyStatus}");
                                }
                                SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyUnCapped);
                            }
                        }
                        sample.SubStep++;
                    }

                    //振荡
                    if (sample.SubStep >= 5 && sample.SubStep < 8 && !_globalStatus.IsStopped)
                    {
                        if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.VibrationBeforePurify))
                        {
                            //振荡
                            result = _vibration.StartVibrationTwo(sample, cts); // 5 6 7
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}净化管加醋酸铵水溶液振荡 失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                        }
                        sample.SubStep = 8;
                    }

                    //二次拆盖
                    if (sample.SubStep == 8 && !_globalStatus.IsStopped)
                    {
                        if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                        {
                            result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品移液管拆盖 失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyUnCapped);
                        }
                        sample.SubStep++;
                    }

                    //移动到上下料位
                    if (sample.SubStep == 9 && !_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                        sample.SubStep++;
                    }
                    
                    //判断完成
                    if (sample.SubStep == 10 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                        {
                            return true;
                        }
                    }
                 
                    throw new Exception($"从试管架取{sample.Id}样品移液管到移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从拧盖3取净化管到移栽  移液 =》 CentrifugalCarrier => CapperThree => CarrierTwo  根据工艺判断是否加液  
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperThreeToTransferWithoutVibration(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                lock (_lockObj)
                {
                    GlobalCache.Instance.IsCapperThreeOccupy = true;
                    _logger?.Info($"从试管架取{sample.Id}样品移液管");
                    bool result;

                    //拧盖移动到上下料位
                    if (sample.SubStep == 0 && !_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                        sample.SubStep++;
                    }

                    //从试管架取试管到拧盖3
                    if (sample.SubStep == 1 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                        {
                            result = _carrier.GetSampleFromMaterialToCapperThree(sample, cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架取{sample.Id}样品移液管 失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.SubStep++;
                        }
                    }

                    //拆盖
                    if (sample.SubStep == 2 && !_globalStatus.IsStopped)
                    {
                        if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                        {
                            result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品移液管拆盖 失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyUnCapped);
                            sample.SubStep++;
                        }
                        else
                        {
                            throw new Exception($"从拧盖3搬运净化管到振荡步骤号错误 subStep-{sample.SubStep}!");
                        }
                    }

                    //搬运到移栽
                    if (sample.SubStep == 3 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                        {
                            result = _carrier.GetSampleFromCapperThreeToTransfer(sample, func, cts);
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品移液管搬运到移栽 失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.SubStep++;
                        }
                        else
                        {
                            throw new Exception($"从拧盖3搬运净化管到振荡步骤号错误 subStep-{sample.SubStep}!");
                        }
                    }

                    //判断完成
                    if (sample.SubStep == 4 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                        {
                            sample.SubStep = 11;
                            return true;
                        }
                    }
                 
                    throw new Exception($"从试管架取{sample.Id}样品移液管到移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
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
                    GlobalCache.Instance.IsCapperThreeOccupy = true;
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
                    GlobalCache.Instance.IsCapperThreeOccupy = true;
                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.PurifyVibration))//有净化振荡过程
                    {
                        _logger?.Info($"从拧盖3取{sampleId}样品净化管到振荡 振荡");
                    }
                    else
                    {
                        _logger?.Info($"从拧盖3取{sampleId}样品净化空管到试管架");//油脂管
                    }
                  
                    //装盖
                    if (!_globalStatus.IsStopped && sample.SubStep == 16)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                        {
                            result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品净化管装盖 失败!");
                            }
                            SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyUnCapped);
                        }
                        sample.SubStep++;
                    }

                    //移动到上下料位
                    if (!_globalStatus.IsStopped && sample.SubStep == 17)
                    {
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                        sample.SubStep++;
                    }
                   
                }

                //开始振荡
                if (!_globalStatus.IsStopped && sample.SubStep >= 18 && sample.SubStep < 22) //18~22
                {
                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.PurifyVibration) && sample.MainStep == 5)
                    {
                        result = _vibration.StartVibrationOne(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"净化管{sampleId}振荡失败");
                        }
                    }
                    sample.SubStep = 22;
                }

                //在试管架
                if (!_globalStatus.IsStopped && sample.SubStep == 22)
                {
                    GlobalCache.Instance.IsCapperThreeOccupy = false;  // 释放占用拧盖3资源
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        return true;
                    }
                }
               
                throw new Exception($"从振荡取{sampleId}样品净化管到试管架失败,SampleStatus-{sample.Status}");

            }
            catch (Exception ex)
            {

                _logger?.Warn(ex.Message);
                return false;
            }
        }


        public bool GetSampleFromCapperThreeToMaterial(Sample sample,CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                bool result;
                lock (_lockObj)
                {
                    //装盖
                    if (!_globalStatus.IsStopped && sample.SubStep == 16)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                        {
                            result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品净化管装盖 失败!");
                            }
                            SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyUnCapped);
                        }
                     
                        sample.SubStep++;
                    }

                    //移动到上下料位
                    if (!_globalStatus.IsStopped && sample.SubStep == 17)
                    {
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                        sample.SubStep++;
                    }

                    //下料到试管架
                    if (!_globalStatus.IsStopped && sample.SubStep == 18)
                    {
                        result = _carrier.GetSampleFromCapperThreeToMaterial(sample, cts);
                        if (!result)
                        {
                            throw new Exception("从拧盖3取试管到试管架 出错");
                        }
                        GlobalCache.Instance.IsCapperThreeOccupy = false; //释放占用拧盖3资源
                        sample.SubStep++;
                        return true;
                    }


                    throw new Exception($"从振荡取{sampleId}样品净化管到试管架失败,SampleStatus-{sample.Status}");
                }

            }
            catch (Exception ex)
            {

                _logger?.Warn(ex.Message);
                return false;
            }
        }


        //=====================================================================================================================================//


        /// <summary>
        /// 拧盖3移液   由拧盖4移液调用
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移液动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool DoPipetting(Sample sample, Func<Sample, CancellationTokenSource,bool> func, CancellationTokenSource cts)
        {
            s1: ushort sampleId = sample.Id;
            Thread.Sleep(1000); //等待获取锁间隔
            try
            {
                if (GlobalCache.Instance.IsCapperThreeOccupy)
                {
                    if (_globalStatus.IsStopped)
                    {
                        return false;
                    }
                    goto s1;
                }

                lock (_lockObj)
                {
                    _logger?.Info($"从试管架取{sampleId}净化管");
                    bool result;

                    //拧盖移动到上下料位
                    if (sample.SubStep == 0 && !_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                        sample.SubStep++;
                    }

                    //从试管架取试管到拧盖3
                    if (sample.SubStep == 1 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                        {
                            result = _carrier.GetSampleFromMaterialToCapperThree(sample, cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架取{sample.Id}净化管 失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            sample.SubStep++;
                        }
                    }

                    //拆盖
                    if (sample.SubStep == 2 && !_globalStatus.IsStopped)
                    {
                        if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                        {
                            result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品移液管拆盖 失败！ PurifyStatus-{sample.PurifyStatus}");
                            }
                            SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyUnCapped);
                            sample.SubStep++;
                        }
                    }

                    //移液
                    if (sample.SubStep == 3 && !_globalStatus.IsStopped)
                    {
                        result = func?.Invoke(sample, cts) != false;
                        if (!result)
                        {
                            throw new Exception("拧盖3移液失败!");
                        }
                        sample.SubStep++;
                    }

                    //装盖
                    if (sample.SubStep == 4 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyUnCapped))
                        {
                            result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品净化管装盖 失败!");
                            }
                            SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyUnCapped);
                        }
                        sample.SubStep++;
                    }

                    //搬运到试管架
                    if (sample.SubStep == 5 && !_globalStatus.IsStopped)
                    {
                        result = _carrier.GetSampleFromCapperThreeToMaterial(sample, cts);
                        if (!result)
                        {
                            throw new Exception("从拧盖3搬运净化管到试管架失败!");
                        }
                        sample.SubStep = 0;
                        return true;
                    }


                    throw new Exception($"从试管架取{sample.Id}样品净化管移液失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Warn(ex.Message);
                return false;
            }
        }



        #endregion

       
        /// <summary>
        /// 加液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private async Task<bool> AddSolve(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            double volume = sample.TechParams.cusuanan;
            _logger?.Info($"{sampleId}净化管加液-{volume}ml");
            //加入醋酸铵水溶液
            return await AddSolve(volume, cts);
     
        }

        /// <summary>
        /// 加液
        /// </summary>
        /// <param name="solve"></param>
        /// <param name="volume"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private async Task<bool> AddSolve(double volume, CancellationTokenSource cts,byte solve = 0x01)
        {
            _logger?.Debug($"AddSolve-{solve}-{volume}");
            try
            {
                //抱夹夹紧
                _io.WriteBit_DO(_holding, true);

                //Y轴移动到加液位
               s1: var result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.AddLiquidPos, _yMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause)
                        {
                            Thread.Sleep(1000);
                        }
                        if (!_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                    }
                    throw new Exception("Y轴移动到加液位失败!");
                }

                //开始加液
                s2: result = await _syring.AddSolve(solve, volume, cts).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause)
                        {
                            Thread.Sleep(1000);
                        }
                        if (!_globalStatus.IsStopped)
                        {
                            goto s2;
                        }
                    }
                    throw new Exception("加液失败!");
                }

                return true;

            }
            catch (Exception ex)
            {
                _logger?.Warn(ex.Message);
                return false; ;
            }
        }

    }
}
