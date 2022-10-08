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

        private bool _isOccupy;

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
                    _isOccupy = true;
                    _logger?.Info($"从试管架2搬运{sampleId}萃取管到移栽移液");

                    //移动到上下料位
                    if (!_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                    }
                  

                    //搬运萃取管到拧盖2 
                    if (!_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInShelf))
                        {
                            result = _carrier.GetPolishFromMaterialToCapperTwo(sample, cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架2搬运{ sampleId }萃取管到拧盖2失败!");
                            }
                        }
                    }
                    

                    //拆盖
                    if (!_globalStatus.IsStopped)
                    {
                        if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishUnCapped))
                        {
                            result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品萃取管拆盖 失败!");
                            }
                            SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishUnCapped);
                        }
                    }
                    

                    //移动到上下料位
                    if (!_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                    }
                 

                    //搬运到移栽
                    if (!_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCapper))
                        {
                            result = _carrier.GetPolishFromCapperTwoToTransfer(sample, func, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖2搬运{ sampleId }萃取管到移栽失败!");
                            }
                        }
                    }
                   

                    if (SampleStatusHelper.BitIsOn(sample,SampleStatus.IsPolishInTransfer))
                    {
                        _isOccupy = false;
                        return true;
                    }
                    throw new Exception("试管状态错误！");
                   
                }
            }
            catch (Exception ex)
            {
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
                    _isOccupy = true;
                    _logger?.Info($"从移栽搬运{sampleId}萃取管到拧盖2");

                    //移动到上下料位
                    if (!_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                    }
                 

                    //搬运萃取管到拧盖2
                    if (!_globalStatus.IsStopped)
                    {
                        result = _carrier.GetPolishFromTransferToCapperTwo(sample, func, cts);
                        if (!result)
                        {
                            throw new Exception($"从移栽搬运{ sampleId }萃取管到拧盖2失败!");
                        }
                        _isOccupy = false;
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
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
                    _isOccupy = true;
                    _logger?.Info($"从拧盖2搬运{sampleId}萃取管到试管架");

                    //装盖
                    if (!_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCapper))
                        {
                            result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品萃取管装盖 失败！ PolishStatus-{sample.PolishStatus}");
                            }
                            SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishUnCapped);
                        }
                    }


                    //移动到上下料位
                    if (!_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }

                    }


                    //搬运到移栽
                    if (!_globalStatus.IsStopped)
                    {
                        result = _carrier.GetPolishFromCapperTwoToMaterial(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"从拧盖2搬运{ sampleId }萃取管到试管架2失败!");
                        }
                        _isOccupy = false;
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
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
                    _isOccupy = true;
                    _logger?.Info($"从拧盖2搬运{sampleId}萃取管到冰浴");

                    //装盖
                    if (!_globalStatus.IsStopped)
                    {
                        if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCapper))
                        {
                            result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品萃取管装盖 失败！ PolishStatus-{sample.PolishStatus}");
                            }
                            SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishUnCapped);
                        }

                    }

                    //移动到上下料位
                    if (!_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                    }

                    //搬运到冰浴
                    if (!_globalStatus.IsStopped)
                    {
                        result = _carrier.GetPolishFromCapperTwoToCold(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"从拧盖2搬运{sampleId}萃取管到冰浴失败!");
                        }
                        _isOccupy = false;
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
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
                    _isOccupy = true;
                    _logger?.Info($"从试管架2搬运{sampleId}萃取管到拧盖2");

                    //移动到上下料位
                    if (!_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                    }

                    //搬运萃取管到拧盖2
                    if (!_globalStatus.IsStopped)
                    {
                        result = _carrier.GetPolishFromMaterialToCapperTwo(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"从试管架2搬运{ sampleId }萃取管到拧盖2失败!");
                        }

                    }

                    //拆盖
                    if (!_globalStatus.IsStopped)
                    {
                        if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishUnCapped))
                        {
                            result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品萃取管拆盖 失败!");
                            }
                            SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishUnCapped);
                        }
                    }

                    //移动到上下料位
                    if (!_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                        _isOccupy = false;
                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
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
        public async Task<bool> GetSampleFromMaterialToCapperTwo(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lockObj)
                    {
                        _isOccupy = true;
                        _logger?.Info($"从试管架1搬运{sampleId}样品管到拧盖2");

                        //移动到上下料位
                        if (!_globalStatus.IsStopped)
                        {
                            result = MovePutGetPos(cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception("拧盖移动到上下料位 出错");
                            }

                        }

                        //搬运样品管到拧盖2
                        if (!_globalStatus.IsStopped)
                        {
                            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                            {
                                result = _carrier.GetSampleFromMaterialToCapperTwo(sample, cts);
                                if (!result)
                                {
                                    throw new Exception($"从试管架2搬运{ sampleId }样品管到拧盖2失败!");
                                }
                            }
                        }


                        //拆盖
                        if (!_globalStatus.IsStopped)
                        {
                            if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped))
                            {
                                result = CapperOffAsync(sample, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"{sample.Id}样品管拆盖 失败!");
                                }
                                SampleStatusHelper.SetBitOn(sample, SampleStatus.IsUnCapped);
                            }
                        }


                        //移动到上下料位
                        if (!_globalStatus.IsStopped)
                        {
                            result = MovePutGetPos(cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception("拧盖移动到上下料位 出错");
                            }
                            _isOccupy = false;
                            return true;
                        }

                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Warn(ex.Message);
                    return false;
                }

            }).ConfigureAwait(false);

          
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
                        _isOccupy = true;
                        _logger?.Info($"从拧盖2搬运{sampleId}样品管到试管架1");

                        //装盖
                        if (!_globalStatus.IsStopped)
                        {
                            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsUnCapped) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperTwo))
                            {
                                result = CapperOnAsync(sample, cts).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"{sample.Id}样品管装盖 失败！ SampleTubeStatus-{sample.SampleTubeStatus}");
                                }
                                SampleStatusHelper.ResetBit(sample, SampleStatus.IsUnCapped);
                            }
                        }

                        //移动到上下料位
                        if (!_globalStatus.IsStopped)
                        {
                            result = MovePutGetPos(cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception("拧盖移动到上下料位 出错");
                            }
                        }

                        //搬运到试管架1
                        if (!_globalStatus.IsStopped)
                        {
                            if (SampleStatusHelper.BitIsOn(sample,SampleStatus.IsInCapperTwo))
                            {
                                result = _carrier.GetSampleFromCapperTwoToMaterial(sample, cts);
                                if (!result)
                                {
                                    throw new Exception($"从拧盖2搬运{ sampleId }样品管到试管架1失败!");
                                }
                            }
                            _isOccupy = false;
                            return true;

                        }

                        return false;
                    }
                });
              
            }
            catch (Exception ex)
            {
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        //====================模块内部===============================//

        /// <summary>
        /// 拆盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOffAsync(Sample sample, CancellationTokenSource cts)
        {
            //判断样品是否有盖
            s1: var result = await CapperOff(cts, -0.85).ConfigureAwait(false);

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
        /// 装盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOnAsync(Sample sample, CancellationTokenSource cts)
        {
            //判断样品是否有盖
            s1: var result = await base.CapperOn(80, 40, cts).ConfigureAwait(false);

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



    }
}
