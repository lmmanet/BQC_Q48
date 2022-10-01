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
    public class CapperTwo : CapperBase, ICapperTwo
    {

        private static ILogger logger = new MyLogger(typeof(CapperTwo));

        private readonly ICarrierOne _carrier;

        private readonly static object _lockObj = new object();

        #region Construtors

        public CapperTwo(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess, ICarrierOne carrier) : base(io, motion, globalStatus, dataAccess, logger)
        {
            this._carrier = carrier;
            _axisY = 9;
            _axisC1 = 10;
            _axisC2 = 11;
            _axisZ = 13;
            _holding = 19;
            _claw = 20;
            _holdingCloseSensor = 22;  //I1.6
            _holdingOpenSensor = 23;   //I1.7

            _xOffset = 60;    //拧盖X偏移量

            _posData = _dataAccess.GetCapperPosData(2);
        }

        public override void UpdatePosData()
        {
            _posData = _dataAccess.GetCapperPosData(2); ;
        }

        #endregion

        //===============================================================模块内部==================================================================//

        /// <summary>
        /// 拆盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOffAsync(Sample sample, CancellationTokenSource cts)
        {
            //判断样品是否有盖
            var result = await CapperOff(cts, -0.75).ConfigureAwait(false);

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

        /// <summary>
        /// 装盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOnAsync(Sample sample, CancellationTokenSource cts)
        {  
            //判断样品是否有盖
            var result = await base.CapperOn(80, 40, cts).ConfigureAwait(false);

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



        //===============================================================离心移栽==================================================================//



        //================================================移液部分 兽药=================================================//

        /// <summary>
        /// 从试管架2搬运无盖萃取管到移栽移液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromMaterialToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从试管架2搬运{sampleId}萃取管到移栽移液");

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //搬运萃取管到拧盖2
                    result = _carrier.GetPolishFromMaterialToCapperTwo(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"从试管架2搬运{ sampleId }萃取管到拧盖2失败!");
                    }

                    //拆盖
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishUnCapped))
                    {
                        result = CapperOffAsync(sample,cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品萃取管拆盖 失败!");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //搬运到移栽
                    result = _carrier.GetPolishFromCapperTwoToTransfer(sample, func, cts);
                    if (!result)
                    {
                        throw new Exception($"从拧盖2搬运{ sampleId }萃取管到移栽失败!");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从试管架2搬运{sampleId}萃取管到移栽移液 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从移栽搬运萃取空管到拧盖2   
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromTransferToCapperTwo(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从移栽搬运{sampleId}萃取管到拧盖2");

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //搬运萃取管到拧盖2
                    result = _carrier.GetPolishFromTransferToCapperTwo(sample,func, cts);
                    if (!result)
                    {
                        throw new Exception($"从移栽搬运{ sampleId }萃取管到拧盖2失败!");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从试管架2搬运{sampleId}萃取管到移栽移液 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从拧盖2搬运萃取空管到试管架2  拧盖2内部（装盖，下料到试管架） 或搬运取完上清液的萃取管等待振荡先到冰浴
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromCapperTwoToMaterial(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从拧盖2搬运{sampleId}萃取管到试管架");

                    //装盖
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCapper))
                    {
                        result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品萃取管装盖 失败！ PolishStatus-{sample.PolishStatus}");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //搬运到移栽
                    result = _carrier.GetPolishFromCapperTwoToMaterial(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"从拧盖2搬运{ sampleId }萃取管到试管架2失败!");
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从拧盖2搬运{sampleId}萃取管到试管架 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从拧盖2搬运萃取管到冰浴 
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromCapperTwoToCold(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从拧盖2搬运{sampleId}萃取管到冰浴");

                    //装盖
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCapper))
                    {
                        result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品萃取管装盖 失败！ PolishStatus-{sample.PolishStatus}");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //搬运到冰浴
                    result = _carrier.GetPolishFromCapperTwoToCold(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"从拧盖2搬运{sampleId}萃取管到冰浴失败!");
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从拧盖2搬运{sampleId}萃取管到冰浴 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从试管架2搬运萃取管到拧盖2 接受上清液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromMaterialToCapperTwo(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从试管架2搬运{sampleId}萃取管到拧盖2");

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //搬运萃取管到拧盖2
                    result = _carrier.GetPolishFromMaterialToCapperTwo(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"从试管架2搬运{ sampleId }萃取管到拧盖2失败!");
                    }  
                    
                    //拆盖
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishUnCapped))
                    {
                        result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品萃取管拆盖 失败!");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从试管架2搬运{sampleId}萃取管到拧盖2 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }


        //================================================移液部分 =================================================//
      
        /// <summary>
        /// 从试管架1取样品管到拧盖2移去上清液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromMaterialToCapperTwo(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从试管架1搬运{sampleId}样品管到拧盖2");

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //搬运样品管到拧盖2
                    result = _carrier.GetSampleFromMaterialToCapperTwo(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"从试管架2搬运{ sampleId }样品管到拧盖2失败!");
                    }

                    //拆盖
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped))
                    {
                        result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品管拆盖 失败!");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从试管架1搬运{sampleId}样品管到拧盖2 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }
      
        /// <summary>
        /// 从拧盖2取回移液完后的样品管到试管架1
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> GetSampleFromCapperTwoToMaterial(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                return await Task.Run(() =>
                {
                    lock (_lockObj)
                    {
                        _logger?.Info($"从拧盖2搬运{sampleId}样品管到试管架1");

                        //装盖
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperTwo))
                        {
                            result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品管装盖 失败！ SampleTubeStatus-{sample.SampleTubeStatus}");
                            }
                            SampleStatusHelper.ResetBit(sample, SampleStatus.IsUnCapped);
                        }

                        //移动到上下料位
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }

                        //搬运到试管架1
                        result = _carrier.GetSampleFromCapperTwoToMaterial(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"从拧盖2搬运{ sampleId }样品管到试管架1失败!");
                        }
                        return true;
                    }
                });
              
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从拧盖2搬运{sampleId}样品管到试管架1 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        public bool DoPipettingOne(Sample sample, CancellationTokenSource cts)
        {
            //开始移液
            var result = _carrier.DoPipetting(sample, true, cts);
            if (!result)
            {
                throw new Exception("样品离心管提取上清液 出错");
            }
            return result;
        }

        public bool DoPipettingTwo(Sample sample, CancellationTokenSource cts)
        {
            //开始移液
            var result = _carrier.DoPipetting(sample, false, cts);
            if (!result)
            {
                throw new Exception("样品离心管提取上清液 出错");
            }
            return result;
        }
        //=************************************************************************************************************************=//



        public bool GetSampleFromMaterialAndPipettingSmallToBig(Sample sample, CancellationTokenSource cts)
        {
            var result = GetSampleFromMaterialAndPipetting(sample, false, cts);
            if (!result)
            {
                throw new Exception("移液失败!");
            }
            ///加入到振荡列表
            result = _carrier.GetSampleFromCapperTwoToMaterial(sample, cts);
            if (!result)
            {
                throw new Exception("搬运到试管架失败!");
            }

            return result;
        }

        public bool GetTubeFromMaterialToCapperTwo(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从试管架2取{sample.Id}离心空管到拧盖2");

                    //拧盖移动到上下料位
                    var result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //样品离心管到拧盖2
                    result = _carrier.GetPolishFromMaterialToCapperTwo(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"从试管架2取{sample.Id}离心空管 失败！ PolishStatus-{sample.PolishStatus}");
                    }

                    //拆盖
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped))
                    {
                        result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}离心空管拆盖 失败！ PolishStatus-{sample.PolishStatus}");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }


                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperTwo))
                    {
                        return true;
                    }

                    throw new Exception($"从试管架2取{sample.Id}离心空管到拧盖2失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从试管架2取{sample.Id}离心空管到拧盖2 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }




        /// <summary>
        /// 从冰浴取试管到移栽  拧盖无需锁 只做中转作用
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromColdToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            //无需锁 只做中转作用
            return _carrier.GetSampleFromColdToTransfer(sample,func, cts);
        }

        /// <summary>
        /// 从冰浴取萃取管到移栽  拧盖无需锁 只做中转作用
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromColdToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            //无需锁 只做中转作用
            return _carrier.GetPolishFromColdToTransfer(sample, func, cts);
        }


        /// <summary>
        /// 离心完成后从移栽中取出试管
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromTransferToMaterial(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            return _carrier.GetSampleFromTransferToMaterial(sample, func, cts);
        }

        /// <summary>
        /// 离心完后从移栽中取出萃取管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromTransferToMaterial(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            return _carrier.GetPolishFromTransferToMaterial(sample, func, cts);
        }








        private bool GetSampleFromMaterialAndPipetting(Sample sample, bool isBigToSmall, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从{sample.Id}样品离心管取上清液到净化管");

                    //拧盖移动到上下料位
                    var result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //样品离心管到拧盖2
                    result = _carrier.GetSampleFromMaterialToCapperTwo(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"从试管架取{sample.Id}样品离心管 失败！ SampleTubeStatus-{sample.SampleTubeStatus}");
                    }

                    //拆盖
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped))
                    {
                        result = CapperOffAsync(sample,cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品离心管拆盖 失败！ SampleTubeStatus-{sample.SampleTubeStatus}");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

              

                    //装盖
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped))
                    {
                        result = CapperOnAsync(sample,cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品离心管装盖 失败！ SampleTubeStatus-{sample.SampleTubeStatus}");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }




                    return true;


                    throw new Exception($"从{sample.Id}样品离心管取上清液到净化管失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从{sample.Id}样品离心管取上清液到净化管 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }





        /// <summary>
        /// 从拧盖2取无盖试管到移栽      移液 =》 CentrifugalCarrier => CapperTwo（拆盖完）(传入动作) => CarrierOne(传入动作)
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func">移栽旋转动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromCapperTwoToTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从试管架取{sample.Id}样品管（待浓缩）");

                    //拧盖移动到上下料位
                    var result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //取离心完成后除脂萃取试管
                    result = _carrier.GetPolishFromMaterialToCapperTwo(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"从试管架2取{sample.Id}样品萃取管 失败！ PolishStatus-{sample.PolishStatus}");
                    }

                    //拆盖
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishUnCapped))
                    {
                        result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品萃取管拆盖 失败！ PolishStatus-{sample.PolishStatus}");
                        }
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //搬运到移栽
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCapper))
                    {
                        result = _carrier.GetPolishFromCapperTwoToTransfer(sample, func, cts);
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品萃取管搬运到移栽 失败！ PolishStatus-{sample.PolishStatus}");
                        }
                    }

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInTransfer))
                    {
                        return true;
                    }

                    throw new Exception($"从试管架2取{sample.Id}样品萃取管到移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从试管架2取{sample.Id}样品萃取管到移栽 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }


        /// <summary>
        /// 从移栽回收萃取管（提取浓缩液后）
        /// </summary>
        /// <returns></returns>
        private bool RecylePolishFromTransfer(Sample sample, Func<ushort, CancellationTokenSource, Task<bool>> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从移栽取{sampleId}样品萃取管到试管架");
                    bool result;

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //从离心移栽搬运无盖试管到拧盖2
                    result = _carrier.GetPolishFromTransferToCapperTwo(sample, func, cts);
                    if (!result)
                    {
                        throw new Exception("从移栽取萃取管到拧盖2 出错");
                    }

                    //装盖
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishUnCapped))
                    {
                        result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品萃取管装盖 失败!");
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //从拧盖2取萃取管到试管架2
                    result = _carrier.GetPolishFromCapperTwoToMaterial(sample, cts);
                    if (!result)
                    {
                        throw new Exception("从拧盖2取萃取管到试管架2 出错");
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽取{sampleId}样品萃取管到试管架 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
           
        }


        /// <summary>
        /// 从拧盖1回收样品管
        /// </summary>
        /// <returns></returns>
        private bool RecycleSampleFromCapperTwo(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从拧盖2取{sampleId}样品离心管到试管架1");
                    bool result;
                    //装盖
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped))
                    {
                        result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"{sample.Id}样品离心管装盖 失败!");
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsUnCapped);
                    }

                    //移动到上下料位
                    result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("拧盖移动到上下料位 出错");
                    }

                    //搬运试管到试管架
                    result = _carrier.GetSampleFromCapperTwoToMaterial(sample, cts);
                    if (!result)
                    {
                        throw new Exception("从拧盖2取离心管到试管架 出错");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从拧盖2取{sampleId}样品离心管到试管架1 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

        }



    }
}
