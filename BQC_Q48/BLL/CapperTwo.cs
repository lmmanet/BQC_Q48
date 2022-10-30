using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.Common;
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
            _capperSensor = 24;        //I2.0
            _xOffset = 60;    //拧盖X偏移量

            _posData = _dataAccess.GetCapperPosData(2);
        }

        public override void UpdatePosData()
        {
            _posData = _dataAccess.GetCapperPosData(2); ;
        }

        public override CapperInfo GetCapperInfo()
        {
            var cpInfo = base.GetCapperInfo();
            cpInfo.CapperId = 2;
            cpInfo.CapperName = "ICapperTwo";
            cpInfo.CapperOffDistance = -0.85;
            cpInfo.CapperOnTorque = 80;
            return cpInfo;
        }

        #endregion


        //================================================移液部分 兽药=================================================//

        /// <summary>
        /// 从试管架2搬运无盖萃取管到移栽移液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetPolishFromMaterialToTransfer(Sample sample, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs)
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
                        result = MovePutGetPos(gs).GetAwaiter().GetResult();
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
                            result = _carrier.GetPolishFromMaterialToCapperTwo(sample, gs);
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
                            result = CapperOffAsync(sample, gs).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品萃取管拆盖 失败!");
                            }
                            SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishUnCapped);
                            if (_unCapFalt == true)
                            {
                                throw new Exception("拧盖2检测有盖,请确认拆盖成功后继续程序!");
                            }
                        }
                    }
                    

                    //移动到上下料位
                    if (!_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(gs).GetAwaiter().GetResult();
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
                            result = _carrier.GetPolishFromCapperTwoToTransfer(sample, func, gs);
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
                if (_globalStatus.IsStopped || _globalStatus.IsPause)
                {
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
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetPolishFromTransferToCapperTwo(Sample sample, Func<ushort, IGlobalStatus, Task<bool>> func, IGlobalStatus gs)
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
                        result = MovePutGetPos(gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                    }
                 

                    //搬运萃取管到拧盖2
                    if (!_globalStatus.IsStopped)
                    {
                        result = _carrier.GetPolishFromTransferToCapperTwo(sample, func, gs);
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
                if (_globalStatus.IsStopped || _globalStatus.IsPause)
                {
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
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetPolishFromCapperTwoToMaterial(Sample sample, IGlobalStatus gs)
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
                            result = CapperOnAsync(sample, gs).GetAwaiter().GetResult();
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
                        result = MovePutGetPos(gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }

                    }


                    //搬运到移栽
                    if (!_globalStatus.IsStopped)
                    {
                        result = _carrier.GetPolishFromCapperTwoToMaterial(sample, gs);
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
                if (_globalStatus.IsStopped || _globalStatus.IsPause)
                {
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
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetPolishFromCapperTwoToCold(Sample sample, IGlobalStatus gs)
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
                            result = CapperOnAsync(sample, gs).GetAwaiter().GetResult();
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
                        result = MovePutGetPos(gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                    }

                    //搬运到冰浴
                    if (!_globalStatus.IsStopped)
                    {
                        result = _carrier.GetPolishFromCapperTwoToCold(sample, gs);
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
                if (_globalStatus.IsStopped || _globalStatus.IsPause)
                {
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
        /// <param name="gs"></param>
        /// <returns></returns>
        public bool GetPolishFromMaterialToCapperTwo(Sample sample, IGlobalStatus gs)
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
                        result = MovePutGetPos(gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("拧盖移动到上下料位 出错");
                        }
                    }

                    //搬运萃取管到拧盖2
                    if (!_globalStatus.IsStopped)
                    {
                        result = _carrier.GetPolishFromMaterialToCapperTwo(sample, gs);
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
                            result = CapperOffAsync(sample, gs).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"{sample.Id}样品萃取管拆盖 失败!");
                            }
                            SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishUnCapped);
                            if (_unCapFalt == true)
                            {
                                throw new Exception("拧盖2检测有盖,请确认拆盖成功后继续程序!");
                            }
                        }
                    }

                    //移动到上下料位
                    if (!_globalStatus.IsStopped)
                    {
                        result = MovePutGetPos(gs).GetAwaiter().GetResult();
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
                if (_globalStatus.IsStopped || _globalStatus.IsPause)
                {
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
        /// <param name="gs"></param>
        /// <returns></returns>
        public async Task<bool> GetSampleFromMaterialToCapperTwo(Sample sample, IGlobalStatus gs)
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
                            result = MovePutGetPos(gs).GetAwaiter().GetResult();
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
                                result = _carrier.GetSampleFromMaterialToCapperTwo(sample, gs);
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
                                result = CapperOffAsync(sample, gs).GetAwaiter().GetResult();
                                if (!result)
                                {
                                    throw new Exception($"{sample.Id}样品管拆盖 失败!");
                                }
                                SampleStatusHelper.SetBitOn(sample, SampleStatus.IsUnCapped);
                                if (_unCapFalt == true)
                                {
                                    throw new Exception("拧盖2检测有盖,请确认拆盖成功后继续程序!");
                                }
                            }
                        }


                        //移动到上下料位
                        if (!_globalStatus.IsStopped)
                        {
                            result = MovePutGetPos(gs).GetAwaiter().GetResult();
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
                    if (_globalStatus.IsStopped || _globalStatus.IsPause)
                    {
                        return false;
                    }
                    _logger?.Warn(ex.Message);
                    return false;
                }

            }).ConfigureAwait(false);

          
        }
      
        /// <summary>
        /// 从拧盖2取回移液完后的样品管到试管架1
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public async Task<bool> GetSampleFromCapperTwoToMaterial(Sample sample, IGlobalStatus gs)
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
                                result = CapperOnAsync(sample, gs).GetAwaiter().GetResult();
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
                            result = MovePutGetPos(gs).GetAwaiter().GetResult();
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
                                result = _carrier.GetSampleFromCapperTwoToMaterial(sample, gs);
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
                if (_globalStatus.IsStopped || _globalStatus.IsPause)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        //====================模块内部===============================//

        /// <summary>
        /// 拆盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOffAsync(Sample sample, IGlobalStatus gs)
        {
            //判断样品是否有盖
            s1: var result = await CapperOff(gs, -0.85).ConfigureAwait(false);

            if (!result)
            {
                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause && !_globalStatus.IsStopped)
                    {
                        Thread.Sleep(2000);
                    }

                    if (!_globalStatus.IsStopped)
                    {
                        goto s1;
                    }
                }
            }

        //    //检测是否拆盖成功
        //    result = CheckUnCapper(gs);
        //    if (!result)
        //    {
        //        return false;
        //    }

        //s5: result = await _motion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _yMoveVel, gs).ConfigureAwait(false);
        //    if (!result)
        //    {
        //        if (_globalStatus.IsPause)
        //        {
        //            while (_globalStatus.IsPause && !_globalStatus.IsStopped)
        //            {
        //                Thread.Sleep(1000);
        //            }
        //            if (!_globalStatus.IsStopped)
        //            {
        //                goto s5;
        //            }
        //        }
        //        throw new Exception("Y轴运动出错！");
        //    }


            return result;
        }

        /// <summary>
        /// 装盖
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public override async Task<bool> CapperOnAsync(Sample sample, IGlobalStatus gs)
        {
            //判断样品是否有盖
            s1: var result = await base.CapperOn(80, 40, gs).ConfigureAwait(false);

            if (!result)
            {
                if (_globalStatus.IsPause)
                {
                    while (_globalStatus.IsPause && !_globalStatus.IsStopped)
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
