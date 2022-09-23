using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class CarrierOne : CarrierBase, ICarrierOne
    {
        private static ILogger logger = new MyLogger(typeof(CarrierOne));

        private Dictionary<Sample, ushort> _coldDic = new Dictionary<Sample, ushort>(); // //冰浴字典

        #region Private Members

        private readonly static object _lockObj = new object();

        private string _currentMethodName = string.Empty;

        private ICarrierOneDataAccess _dataAccess;

        private CarrierOnePosData _posData;

        private int _pipettingStep =1;

        #endregion

        #region Construtors

        public CarrierOne(IEtherCATMotion motion, IEPG26 claw, IGlobalStatus globalStatus, ICarrierOneDataAccess dataAccess ) : base(motion, claw, globalStatus, logger)
        {
            _axisX = 0;
            _axisY = 1;
            _axisZ1 = 2;
            _axisZ2 = 3;
            _axisP = 6;
            _clawSlaveId = 1;
            _putOffNeedle = -3;
            _absorbVel = 2;
            _syringVel = 5;


            _dataAccess = dataAccess;
            _posData = _dataAccess.GetPosData();
        }

        #endregion

        #region Public Methods   搬运部分

        /// <summary>
        /// 从加固搬运试管到拧盖1
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromAddSolidToCapperOne(Sample sample,CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"搬运{sampleId}样品到拧盖1");
             
                    //试管在加固
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInAddSolid))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromAddSolidToCapperOne((ushort)(2 * sampleId), cts);
                            if (!result)
                            {
                                throw new Exception($"从加固搬运{sampleId}样品到拧盖1失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromAddSolidToCapperOne((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从加固搬运{sampleId}样品到拧盖1失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInAddSolid);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCapperOne);
                    }

                    //试管在拧盖1
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperOne))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}样品到拧盖1失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
          
           
        }       
        
        /// <summary>
        /// 从加固搬运试管到拧盖1
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromMaterialToCapperOne(Sample sample,CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从试管架搬运{sampleId}样品到拧盖1");

                    //试管在试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromMaterialToCapperOne((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}样品到拧盖1失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromMaterialToCapperOne((ushort)(2 * sampleId), cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}样品到拧盖1失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInShelf);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCapperOne);
                    }

                    //试管在拧盖1
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperOne))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}样品到拧盖1失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
          
           
        }

        /// <summary>
        /// 从振荡1搬运样品管到试管架1
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromVibrationToMaterial(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger.Info($"搬运{ sample.Id}样品到试管架");
                
                    //试管在振荡
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInVibrationOne))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToMaterial((ushort)(2 * sampleId - 1), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToMaterial((ushort)(2 * sampleId), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInVibrationOne);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInShelf);
                    }

                    //试管在试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{ sample.Id}样品到试管架失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从涡旋搬运样品管到试管架1
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromVortexToMaterial(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger.Info($"搬运{ sample.Id}样品到试管架");

                    //试管在涡旋
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInVortexed))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVortexToMaterial((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从涡旋处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVortexToMaterial((ushort)(2 * sampleId), cts);
                            if (!result)
                            {
                                throw new Exception($"从涡旋处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInVortexed);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInShelf);
                    }

                    //试管在试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{ sample.Id}样品到试管架失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从拧盖1搬运试管到加固
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperOneToAddSolid(Sample sample, Func<bool> func1, Func<bool> func2, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    //试管在试管架
                    _logger?.Info($"从拧盖1搬运{sampleId}样品到加固");

                    //试管在拧盖1  需要传送气缸动作
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperOne))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperOneToAddSolid((ushort)(2 * sampleId - 1), func1, func2, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖1搬运{sampleId}样品到加固失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperOneToAddSolid((ushort)(2 * sampleId), func1, func2, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖1搬运{sampleId}样品到加固失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCapperOne);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInAddSolid);
                    }

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInAddSolid))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}样品到加固失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }

        }

        /// <summary>
        /// 从振荡1搬运试管到冰浴
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="posNum">冰浴位置代号1~8</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromVibrationToCold(Sample sample,CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            ushort posNum = 0;

            try
            { 
                //试管在冰浴
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCold))
                {
                    return true;
                }

                DateTime end = DateTime.Now + TimeSpan.FromMinutes(10);
         
                lock (_lockObj)
                {
                attemp: for (int i = 1; i < 9; i++)
                    {
                        if (_coldDic.ContainsValue((ushort)i))
                        {
                            continue;
                        }
                        else
                        {
                            posNum = (ushort)i;
                            break;
                        }
                    }
                    if (posNum == 0)
                    {
                        if (DateTime.Now > end)
                        {
                            throw new Exception("冰浴试管已满！ 等待超时");
                        }
                        Thread.Sleep(10000);
                        goto attemp;
                    }

                    _logger.Info($"搬运-{ sample.Id}-样品到冰浴-{posNum}-位");

                    #region MyRegion


                    //试管在拧盖1  需要传送气缸动作
                    //if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperOne))
                    //{
                    //    if (sample.TubeStatus == 0)
                    //    {
                    //        result = GetSampleFromCapperOneToCold((ushort)(2 * sampleId), null, null, cts);
                    //        if (!result)
                    //        {
                    //            throw new Exception($"从拧盖1处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                    //        }
                    //        sample.TubeStatus = 1;
                    //    }
                    //    if (sample.TubeStatus == 1)
                    //    {
                    //        result = GetSampleFromCapperOneToMaterial((ushort)(2 * sampleId - 1), null, null, cts);
                    //        if (!result)
                    //        {
                    //            throw new Exception($"从拧盖1处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                    //        }
                    //        sample.TubeStatus = 0;
                    //    }
                    //    SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCapperOne);
                    //    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCold);
                    //}

                    //试管在涡旋
                    //if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInVortexed))
                    //{
                    //    if (sample.TubeStatus == 0)
                    //    {
                    //        result = GetSampleFromVortexToMaterial((ushort)(2 * sampleId - 1), cts);
                    //        if (!result)
                    //        {
                    //            throw new Exception($"从涡旋处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                    //        }
                    //        sample.TubeStatus = 1;
                    //    }
                    //    if (sample.TubeStatus == 1)
                    //    {
                    //        result = GetSampleFromVortexToMaterial((ushort)(2 * sampleId), cts);
                    //        if (!result)
                    //        {
                    //            throw new Exception($"从涡旋处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                    //        }
                    //        sample.TubeStatus = 0;
                    //    }
                    //    SampleStatusHelper.ResetBit(sample, SampleStatus.IsInVortexed);
                    //    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCold);
                    //}

                    //试管在拧盖2   
                    //if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperTwo))
                    //{
                    //    if (sample.TubeStatus == 0)
                    //    {
                    //        result = GetSampleFromCapperTwoToMaterial((ushort)(2 * sampleId - 1), null, null, cts);
                    //        if (!result)
                    //        {
                    //            throw new Exception($"从拧盖2处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                    //        }
                    //        sample.TubeStatus = 1;
                    //    }
                    //    if (sample.TubeStatus == 1)
                    //    {
                    //        result = GetSampleFromCapperTwoToMaterial((ushort)(2 * sampleId), null, null, cts);
                    //        if (!result)
                    //        {
                    //            throw new Exception($"从拧盖2处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                    //        }
                    //        sample.TubeStatus = 0;
                    //    }
                    //    SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCapperTwo);
                    //    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCold);
                    //}

                    //试管在移栽
                    //if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                    //{
                    //    if (sample.TubeStatus == 0&& cts?.IsCancellationRequested != true)
                    //    {
                    //        result = GetSampleFromTransferToMaterial((ushort)(2 * sampleId - 1), null, cts);
                    //        if (!result)
                    //        {
                    //            throw new Exception($"从移栽处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                    //        }
                    //        sample.TubeStatus = 1;
                    //    }
                    //    if (sample.TubeStatus == 1&& cts?.IsCancellationRequested != true)
                    //    {
                    //        result = GetSampleFromTransferToMaterial((ushort)(2 * sampleId), null, cts);
                    //        if (!result)
                    //        {
                    //            throw new Exception($"从移栽处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                    //        }
                    //        sample.TubeStatus = 0;
                    //    }
                    //    SampleStatusHelper.ResetBit(sample, SampleStatus.IsInTransfer);
                    //    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCold);
                    //}


                    #endregion

                    //试管在振荡
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInVibrationOne))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToCold((ushort)(2 * sampleId - 1), (ushort)(2 * posNum - 1), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToCold((ushort)(2 * sampleId), (ushort)(2 * posNum), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }

                        _coldDic.Add(sample, posNum);

                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInVibrationOne);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCold);
                    }


                    //试管在冰浴
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCold))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{ sample.Id}样品到冰浴失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 搬运样品离心管到拧盖2  移液（一次移液）
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromMaterialToCapperTwo(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                _logger?.Info($"从试管架取{sample.Id}样品离心管到拧盖2");

                //试管在试管架2
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                {
                    if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                    {
                        var result = GetSampleFromMaterialToCapperTwo((ushort)(2 * sampleId -1), cts);
                        if (!result)
                        {
                            throw new Exception($"从试管架取{sample.Id}样品离心管到拧盖2 失败！ TubeStatus-{sample.TubeStatus}");
                        }
                        sample.TubeStatus = 1;
                    }
                    if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                    {
                        var result = GetSampleFromMaterialToCapperTwo((ushort)(2 * sampleId ), cts);
                        if (!result)
                        {
                            throw new Exception($"从试管架取{sample.Id}样品离心管到拧盖2 失败！ TubeStatus-{sample.TubeStatus}");
                        }
                        sample.TubeStatus = 0;
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsInShelf);
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCapperTwo);
                }
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperTwo))
                {
                    return true;
                }
                throw new Exception($"从试管架取{sample.Id}样品样品离心管到拧盖2失败,SampleStatus-{sample.Status}");

            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }

        }

        /// <summary>
        /// 从移栽搬运试管到拧盖2  兽药移液完成
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromTransferToCapperTwo(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从离心移栽搬运{sampleId}试管到拧盖2");
                    //试管在移栽
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromTransferToCapperTwo((ushort)(2 * sampleId - 1), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从离心移栽搬运{sampleId}试管到拧盖2失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromTransferToCapperTwo((ushort)(2 * sampleId ), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从离心移栽搬运{sampleId}试管到拧盖2失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInTransfer);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCapperTwo);
                    }
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperTwo))
                    {
                        return true;
                    }
                    throw new Exception($"从离心移栽搬运{sampleId}试管到拧盖2失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从拧盖2取试管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperTwoToMaterial(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger.Info($"搬运{ sample.Id}样品到试管架");
                   
                    //试管在拧盖2   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperTwo))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperTwoToMaterial((ushort)(2 * sampleId - 1), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖2处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperTwoToMaterial((ushort)(2 * sampleId), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖2处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCapperTwo);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInShelf);
                    }

                    //试管在试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{ sample.Id}样品到试管架失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从拧盖1取样品管到试管架1
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperOneToMaterial(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger.Info($"搬运{ sample.Id}样品到试管架");
                    //试管在拧盖1  需要传送气缸动作
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperOne))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperOneToMaterial((ushort)(2 * sampleId), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖1处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperOneToMaterial((ushort)(2 * sampleId - 1), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖1处搬运试管失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCapperOne);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInShelf);
                    }

                    //试管在试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{ sample.Id}样品到试管架失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }



        /// <summary>
        /// 从冰浴取试管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromColdToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            //查询冰浴字典
            ushort posNum = 0;
            var b = _coldDic.TryGetValue(sample, out posNum);
            if (!b)
            {
                throw new Exception($"冰浴中不存在该样品-{sample.Id}！");
            }

            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"搬运{sampleId}样品到离心移栽");

                    //试管在冰浴
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCold))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromColdToTransfer((ushort)(2 * sampleId),(ushort)(2*posNum -1), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从冰浴搬运{sampleId}样品到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromColdToTransfer((ushort)(2 * sampleId - 1), (ushort)(2 * posNum), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从冰浴搬运{sampleId}样品到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        _coldDic.Remove(sample);
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCold);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                    }

                    //试管在移栽

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}样品到离心移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从振荡1取试管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromVibrationOneToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"搬运{sampleId}样品到离心移栽");

                    //试管在振荡
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInVibrationOne))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToTransfer((ushort)(2 * sampleId), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1搬运{sampleId}样品到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToTransfer((ushort)(2 * sampleId - 1), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1搬运{sampleId}样品到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInVibrationOne);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                    }

                    //试管在移栽

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}样品到离心移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从离心移栽搬运试管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromTransferToMaterial(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从离心移栽搬运{sampleId}样品到试管架");   //试管在试管架 //试管在拧盖1 //试管在加固 //试管在涡旋 //试管在拧盖2   //试管在振荡 //试管在冰浴 
                    //试管在移栽
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromTransferToMaterial((ushort)(2 * sampleId), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从离心移栽搬运{sampleId}样品到试管架失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromTransferToMaterial((ushort)(2 * sampleId - 1), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从离心移栽搬运{sampleId}样品到试管架失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInTransfer);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInShelf);
                    }
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                    {
                        return true;
                    }
                    throw new Exception($"从离心移栽搬运{sampleId}样品到试管架失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

     



        /// <summary>
        /// 搬运试管到涡旋
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleToVortex(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"搬运{sampleId}样品到涡旋");
                    //试管在试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromMaterialToVortex((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}样品到涡旋失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromMaterialToVortex((ushort)(2 * sampleId), cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}样品到涡旋失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInShelf);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInVortexed);
                    }

                    //试管在拧盖1  需要传送气缸动作
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperOne))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperOneToVortex((ushort)(2 * sampleId), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖1搬运{sampleId}样品到涡旋失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperOneToVortex((ushort)(2 * sampleId - 1), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖1搬运{sampleId}样品到涡旋失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCapperOne);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInVortexed);
                    }

                    //试管在拧盖2   

                    //试管在振荡
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInVibrationOne))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToVortex((ushort)(2 * sampleId - 1), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1搬运{sampleId}样品到涡旋失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToVortex((ushort)(2 * sampleId), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1搬运{sampleId}样品到涡旋失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInVibrationOne);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInVortexed);
                    }
                    //试管在冰浴

                    //试管在移栽

                    //试管在涡旋
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInVortexed))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}样品到涡旋失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }

        }

        /// <summary>
        /// 搬运试管到振荡
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleToVibration(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"搬运{sampleId}样品到振荡1");
                    //试管在试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromMaterialToVibration((ushort)(2 * sampleId), cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}样品到振荡1失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromMaterialToVibration((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}样品到振荡1失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInShelf);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInVibrationOne);
                    }

                    //试管在拧盖1  需要传送气缸动作
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperOne))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperOneToVibration((ushort)(2 * sampleId), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖1搬运{sampleId}样品到振荡1失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperOneToVibration((ushort)(2 * sampleId - 1), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖1搬运{sampleId}样品到振荡1失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCapperOne);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInVibrationOne);
                    }

                    //试管在涡旋    
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInVortexed))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVortexToVibration((ushort)(2 * sampleId), cts);
                            if (!result)
                            {
                                throw new Exception($"从涡旋搬运{sampleId}样品到振荡1失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVortexToVibration((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从涡旋搬运{sampleId}样品到振荡1失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInVortexed);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInVibrationOne);
                    }

                    //试管在冰浴
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCold))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromColdToVibration((ushort)(2 * sampleId), cts);
                            if (!result)
                            {
                                throw new Exception($"从冰浴搬运{sampleId}样品到振荡1失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromColdToVibration((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从冰浴搬运{sampleId}样品到振荡1失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCold);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInVibrationOne);
                    }

                    //试管在移栽

                    //试管在振荡
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInVibrationOne))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}样品到振荡1失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }

        }

        /// <summary>
        /// 搬运试管到离心移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleToTransfer(Sample sample,Func<ushort, CancellationTokenSource,bool> func,CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"搬运{sampleId}样品到离心移栽");
                    //试管在试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromMaterialToTransfer((ushort)(2 * sampleId - 1), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}样品到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromMaterialToTransfer((ushort)(2 * sampleId), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}样品到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInShelf);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                    }

                    //试管在拧盖1
       
                    //试管在加固

                    //试管在涡旋
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInVortexed))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVortexToTransfer((ushort)(2 * sampleId),func, cts);
                            if (!result)
                            {
                                throw new Exception($"从涡旋搬运{sampleId}样品到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVortexToTransfer((ushort)(2 * sampleId - 1),func, cts);
                            if (!result)
                            {
                                throw new Exception($"从涡旋搬运{sampleId}样品到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInVortexed);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                    }

                    //试管在拧盖2   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperTwo))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperTwoToTransfer((ushort)(2 * sampleId), func,cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖2搬运{sampleId}样品到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperTwoToTransfer((ushort)(2 * sampleId - 1), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖2搬运{sampleId}样品到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCapperTwo);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                    }

                    //试管在振荡
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInVibrationOne))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToTransfer((ushort)(2 * sampleId), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1搬运{sampleId}样品到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToTransfer((ushort)(2 * sampleId - 1), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1搬运{sampleId}样品到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInVibrationOne);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                    }


                    //试管在移栽

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}样品到离心移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }

        }




        //===================================================================萃取管====================================================================================//

        /// <summary>
        /// 从拧盖2取萃取管到冰浴
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromCapperTwoToCold(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            ushort posNum = 0;

            try
            {
                //萃取管在冰浴
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCold))
                {
                    return true;
                }
                lock (_lockObj)
                {
                    DateTime end = DateTime.Now + TimeSpan.FromMinutes(10);
                attemp: for (int i = 1; i < 9; i++)
                    {
                        if (_coldDic.ContainsValue((ushort)i))
                        {
                            continue;
                        }
                        else
                        {
                            posNum = (ushort)i;
                            break;
                        }
                    }
                    if (posNum == 0)
                    {
                        if (DateTime.Now > end)
                        {
                            throw new Exception("冰浴试管已满！ 等待超时");
                        }
                        Thread.Sleep(10000);
                        goto attemp;
                    }

                    _logger.Info($"从拧盖2搬运{ sample.Id}萃取管到冰浴");

                    //萃取管在拧盖2   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCapper))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperTwoToCold((ushort)(2 * sampleId  +47), (ushort)(2 * posNum - 1), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖2搬运{ sample.Id}萃取管到冰浴失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperTwoToCold((ushort)(2 * sampleId +48), (ushort)(2 * posNum ), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖2搬运{ sample.Id}萃取管到冰浴失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishInCapper);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishInCold);
                    }

                    //萃取管在冰浴
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCold))
                    {
                        return true;
                    }
                    throw new Exception($"从拧盖2搬运{ sample.Id}萃取管到冰浴失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }


        }

        /// <summary>
        /// 从移栽取萃取管到拧盖2
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromTransferToCapperTwo(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从移栽搬运{sampleId}样品萃取管到拧盖2");

                    //萃取管在移栽  
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInTransfer))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromTransferToCapperTwo((ushort)(2 * sampleId +47), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从移栽搬运{sampleId}样品萃取管到拧盖2失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromTransferToCapperTwo((ushort)(2 * sampleId + 48), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从移栽搬运{sampleId}样品萃取管到拧盖2失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishInTransfer);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishInCapper);
                    }

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCapper))
                    {
                        return true;
                    }
                    throw new Exception($"从移栽搬运{sampleId}样品萃取管到拧盖2失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从拧盖2取萃取管到试管架2
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
                    _logger?.Info($"从拧盖2搬运{sampleId}样品萃取管到试管架2");

                    //试管在拧盖2   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCapper))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperTwoToMaterial((ushort)(2 * sampleId + 47), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖2搬运{sampleId}样品萃取管到试管架2失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperTwoToMaterial((ushort)(2 * sampleId + 48), null,null,cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖2搬运{sampleId}样品萃取管到试管架2失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishInCapper);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishInShelf);
                    }

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInShelf))
                    {
                        return true;
                    }
                    throw new Exception($"从拧盖2搬运{sampleId}样品萃取管到试管架2失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 搬运空离心管到拧盖2  兽药移液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromMaterialToCapperTwo(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            try
            {
                _logger?.Info($"从试管架2取{sample.Id}样品萃取管到拧盖2");

                //试管在试管架2
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInShelf))
                {
                    if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                    {
                        var result = GetSampleFromMaterialToCapperTwo((ushort)(2 * sampleId + 47), cts);
                        if (!result)
                        {
                            throw new Exception($"从试管架2取{sample.Id}样品萃取管到拧盖2 失败！ TubeStatus-{sample.TubeStatus}");
                        }
                        sample.TubeStatus = 1;
                    }
                    if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                    {
                        var result = GetSampleFromMaterialToCapperTwo((ushort)(2 * sampleId + 48), cts);
                        if (!result)
                        {
                            throw new Exception($"从试管架2取{sample.Id}样品萃取管到拧盖2 失败！ TubeStatus-{sample.TubeStatus}");
                        }
                        sample.TubeStatus = 0;
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishInShelf);
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishInCapper);
                }
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCapper))
                {
                    return true;
                }
                throw new Exception($"从试管架2取{sample.Id}样品萃取管到拧盖2失败,SampleStatus-{sample.Status}");

            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }

        }

        /// <summary>
        /// 从拧盖2搬运试管到移栽  兽药移液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromCapperTwoToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从拧盖2搬运{sampleId}样品萃取管到离心移栽");

                    //试管在拧盖2   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCapper))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperTwoToTransfer((ushort)(2 * sampleId +47), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖2搬运{sampleId}样品萃取管到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromCapperTwoToTransfer((ushort)(2 * sampleId+48), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖2搬运{sampleId}样品萃取管到离心移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishInCapper);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishInTransfer);
                    }

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInTransfer))
                    {
                        return true;
                    }
                    throw new Exception($"从拧盖2搬运{sampleId}样品萃取管到离心移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从振荡1取萃取管到冰浴
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func1"></param>
        /// <param name="func2"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromVibrationToCold(Sample sample, Func<bool> func1, Func<bool> func2, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            ushort posNum = 0;

            try
            {
                //萃取管在冰浴
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCold))
                {
                    return true;
                }
                lock (_lockObj)
                {
                    DateTime end = DateTime.Now + TimeSpan.FromMinutes(10);
                attemp: for (int i = 1; i < 9; i++)
                    {
                        if (_coldDic.ContainsValue((ushort)i))
                        {
                            continue;
                        }
                        else
                        {
                            posNum = (ushort)i;
                            break;
                        }
                    }
                    if (posNum == 0)
                    {
                        if (DateTime.Now > end)
                        {
                            throw new Exception("冰浴试管已满！ 等待超时");
                        }
                        Thread.Sleep(10000);
                        goto attemp;
                    }

                    _logger.Info($"从振荡1搬运{ sample.Id}萃取管到冰浴");

                    //萃取管在拧盖2   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInVibration))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToCold((ushort)(2 * sampleId + 47), (ushort)(2 * posNum - 1), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1搬运{ sample.Id}萃取管到冰浴失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToCold((ushort)(2 * sampleId + 48), (ushort)(2 * posNum ), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1搬运{ sample.Id}萃取管到冰浴失败！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishInVibration);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishInCold);
                    }

                    //萃取管在冰浴
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCold))
                    {
                        return true;
                    }
                    throw new Exception($"从振荡1搬运{ sample.Id}萃取管到冰浴失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从涡旋取萃取管到冰浴
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromVortexToCold(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            ushort posNum = 0;

            try
            {
                //萃取管在冰浴
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCold))
                {
                    return true;
                }
                lock (_lockObj)
                {
                    DateTime end = DateTime.Now + TimeSpan.FromMinutes(10);
                attemp: for (int i = 1; i < 9; i++)
                    {
                        if (_coldDic.ContainsValue((ushort)i))
                        {
                            continue;
                        }
                        else
                        {
                            posNum = (ushort)i;
                            break;
                        }
                    }
                    if (posNum == 0)
                    {
                        if (DateTime.Now > end)
                        {
                            throw new Exception("冰浴试管已满！ 等待超时");
                        }
                        Thread.Sleep(10000);
                        goto attemp;
                    }

                    _logger.Info($"从涡旋搬运{ sample.Id}萃取管到冰浴");

                    //萃取管在拧盖2   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInVortexed))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVortexToCold((ushort)(2 * sampleId + 47), (ushort)(2 * posNum - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从涡旋搬运{ sample.Id}萃取管到冰浴！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVortexToCold((ushort)(2 * sampleId + 48), (ushort)(2 * posNum ), cts);
                            if (!result)
                            {
                                throw new Exception($"从涡旋搬运{ sample.Id}萃取管到冰浴！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishInVortexed);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishInCold);
                    }

                    //萃取管在冰浴
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCold))
                    {
                        return true;
                    }
                    throw new Exception($"从涡旋搬运{ sample.Id}萃取管到冰浴失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 从振荡1取萃取管到涡旋
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func1"></param>
        /// <param name="func2"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromVibrationToVortex(Sample sample, Func<bool> func1, Func<bool> func2, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            ushort posNum = 0;

            try
            {
                //萃取管在冰浴
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCold))
                {
                    return true;
                }
                lock (_lockObj)
                {
                    DateTime end = DateTime.Now + TimeSpan.FromMinutes(10);
                attemp: for (int i = 1; i < 9; i++)
                    {
                        if (_coldDic.ContainsValue((ushort)i))
                        {
                            continue;
                        }
                        else
                        {
                            posNum = (ushort)i;
                            break;
                        }
                    }
                    if (posNum == 0)
                    {
                        if (DateTime.Now > end)
                        {
                            throw new Exception("冰浴试管已满！ 等待超时");
                        }
                        Thread.Sleep(10000);
                        goto attemp;
                    }

                    _logger.Info($"从振荡1搬运{ sample.Id}萃取管到冰浴");

                    //萃取管在拧盖2   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInVibration))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToCold((ushort)(2 * sampleId + 47), (ushort)(2 * posNum - 1), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1搬运{ sample.Id}萃取管到冰浴！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                            result = GetSampleFromVibrationToCold((ushort)(2 * sampleId + 48), (ushort)(2 * posNum), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡1搬运{ sample.Id}萃取管到冰浴！TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishInVibration);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishInCold);
                    }

                    //萃取管在冰浴
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCold))
                    {
                        return true;
                    }
                    throw new Exception($"从振荡1搬运{ sample.Id}萃取管到冰浴失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }


        #endregion

        #region Public Methods 移液部分


        /// <summary>
        /// 开始移液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="bigToSmall">从大管到小管</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool DoPipetting(Sample sample,bool bigToSmall, CancellationTokenSource cts)
        {
            double volume = sample.TechParams.ExtractVolume;
       
            try
            {
                lock (_lockObj)
                {
                    _logger.Info($"样品{ sample.Id}移液-{volume}ml");
                    if (_pipettingStep == 1 && cts?.IsCancellationRequested != true)
                    {
                        //取枪头
                        var result = base.GetNeedleAsync(GetTipCoordinate(2 * sample.Id - 1), cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                           throw new Exception($"第一管取枪头失败,pipettingStep-{_pipettingStep}");
                        }
                        _pipettingStep++;
                    }

                    if (_pipettingStep == 2 && cts?.IsCancellationRequested != true)
                    {
                        //移液
                        var result = base.DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id - 1, bigToSmall), GetPipettorTargetCoordinate(2 * sample.Id - 1, bigToSmall), volume, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第一管移液失败,pipettingStep-{_pipettingStep}");
                        }
                        _pipettingStep++;
                    }

                    if (_pipettingStep == 3 && cts?.IsCancellationRequested != true)
                    {
                        //退枪头
                        var result = base.PutNeedleAsync(GetTipCoordinate(2 * sample.Id - 1), cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第一管放枪头失败,pipettingStep-{_pipettingStep}");
                        }
                        _pipettingStep++;
                    }

                    if (_pipettingStep == 4 && cts?.IsCancellationRequested != true)
                    {
                        //取枪头
                        var result = base.GetNeedleAsync(GetTipCoordinate(2 * sample.Id), cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第二管取枪头失败,pipettingStep-{_pipettingStep}");
                        }
                        _pipettingStep++;
                    }

                    if (_pipettingStep == 5 && cts?.IsCancellationRequested != true)
                    {
                        //移液
                        var result = base.DoPipettingAsync(GetPipettorSourceCoordinate(2 * sample.Id, bigToSmall), GetPipettorTargetCoordinate(2 * sample.Id, bigToSmall), volume, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第二管移液失败,pipettingStep-{_pipettingStep}");
                        }
                        _pipettingStep++;
                    }

                    if (_pipettingStep == 6 && cts?.IsCancellationRequested != true)
                    {
                        //退枪头
                        var result = base.PutNeedleAsync(GetTipCoordinate(2 * sample.Id), cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception($"第二管放枪头失败,pipettingStep-{_pipettingStep}");
                        }
                        _pipettingStep = 1;
                        return true;
                    }
                    throw new Exception($"样品{ sample.Id}移液-{volume}ml失败,pipettingStep-{_pipettingStep}");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        #endregion

        #region Protected Methods

        //==========================================================================================================//

        /// <summary>
        /// 从试管架到拧盖1
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromMaterialToCapperOne(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            _logger.Debug($"GetSampleFromMaterialToCapperOne-{num},clawOpenByte-{clawOpenByte}");
            //取料
            base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();
      
            //放料
            base.PutTubeAsync(GetCapperOneCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }

        /// <summary>
        /// 从试管架到涡旋
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromMaterialToVortex(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromMaterialToVortex-{num},clawOpenByte-{clawOpenByte}");
            //取料
            base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetVortexCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从试管架到拧盖2
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromMaterialToCapperTwo(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromMaterialToCapperTwo-{num},clawOpenByte-{clawOpenByte}");
            //取料
            base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetCapperTwoCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从试管架到振荡
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromMaterialToVibration(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromMaterialToVibration-{num},clawOpenByte-{clawOpenByte}");
            //取料
            base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //判断X位置
            //if (_motion.GetCurrentPos(_axisY) <100)
            //{
            //移动到安全位置
            var result = CarrierMoveToSafePos(GetSampleTubeCoordinate(76), cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }
            //}

            //放料
            base.PutTubeAsync(GetVibrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }

        /// <summary>
        /// 从试管架到移栽
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromMaterialToTransfer(ushort num, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromMaterialToTransfer-{num},clawOpenByte-{clawOpenByte}");
            //取料
            base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //移动到安全位置
            var result = CarrierMoveToSafePos(GetColdCoordinate(1), cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }

            //旋转到指定位置
            result = func.Invoke(num,cts);
            if (!result)
            {
                throw new Exception("移栽移动到指定位失败!");
            }

            //放料
            base.PutTubeAsync(GetTransferCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }

        //===========================================================================================================//

        /// <summary>
        /// 从加固位到拧盖1处
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromAddSolidToCapperOne(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromAddSolidToCapperOne-{num},clawOpenByte-{clawOpenByte}");
            //取料
            base.GetTubeAsync(GetAddSolidCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetCapperOneCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }

        //===========================================================================================================//

        /// <summary>
        /// 从拧盖1处到加固位
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperOneToAddSolid(ushort num, Func<bool> func1, Func<bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromCapperOneToAddSolid-{num},clawOpenByte-{clawOpenByte}");
            //取料辅助动作
            var result = func1?.Invoke() != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetCapperOneCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            result = func2?.Invoke() != false;
            if (!result)
            {
                return false;
            }

            //放料
            base.PutTubeAsync(GetAddSolidCoordinate(num,false), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }

        /// <summary>
        /// 从拧盖1到试管架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperOneToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromCapperOneToMaterial-{num},clawOpenByte-{clawOpenByte}");
            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetCapperOneCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //放料
            base.PutTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从拧盖1到涡旋
        /// </summary>
        /// <param name="num"></param> 
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperOneToVortex(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromCapperOneToVortex-{num},clawOpenByte-{clawOpenByte}");
            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetCapperOneCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //放料
            base.PutTubeAsync(GetVortexCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }

        /// <summary>
        /// 从拧盖1到振荡
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperOneToVibration(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromCapperOneToVibration-{num},clawOpenByte-{clawOpenByte}");
            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetCapperOneCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //判断X位置
            //if (num % 2 == 0)
            //{
            //    //移动到安全位置
            //    result = CarrierMoveToSafePos(GetSampleTubeCoordinate(76), cts).GetAwaiter().GetResult();
            //    if (!result)
            //    {
            //        return false;
            //    }
            //}

            //放料
            base.PutTubeAsync(GetVibrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }


        //===========================================================================================================//

        /// <summary>
        /// 从涡旋到试管架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromVortexToMaterial(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromVortexToMaterial-{num},clawOpenByte-{clawOpenByte}");
            //取料
            base.GetTubeAsync(GetVortexCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从涡旋到拧盖1
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromVortexToCapperOne(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromVortexToCapperOne-{num},clawOpenByte-{clawOpenByte}");
            //取料
            base.GetTubeAsync(GetVortexCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetCapperOneCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }

        /// <summary>
        /// 从涡旋取试管到振荡
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromVortexToVibration(ushort num,CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromVortexToVibration-{num},clawOpenByte-{clawOpenByte}");
            //取料
            base.GetTubeAsync(GetVortexCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetVibrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }

        /// <summary>
        /// 从涡旋到移栽
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromVortexToTransfer(ushort num, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromVortexToTransfer-{num},clawOpenByte-{clawOpenByte}");
            //取料
            base.GetTubeAsync(GetVortexCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //移动到安全位置
            var result = CarrierMoveToSafePos(GetColdCoordinate(1), cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }

            //旋转到指定位置
            result = func.Invoke(num,cts);
            if (!result)
            {
                throw new Exception("移栽移动到指定位失败!");
            }

            //放料
            base.PutTubeAsync(GetTransferCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }

        /// <summary>
        /// 从涡旋到冰浴
        /// </summary>
        /// <param name="num"></param>
        /// <param name="posNum"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromVortexToCold(ushort num, ushort posNum, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromVortexToCold-{num},clawOpenByte-{clawOpenByte}");

            //取料
            base.GetTubeAsync(GetVortexCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetColdCoordinate(posNum), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        //===========================================================================================================//

        /// <summary>
        /// 从拧盖2到试管架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperTwoToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromCapperTwoToMaterial-{num},clawOpenByte-{clawOpenByte}");
            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetCapperTwoCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //放料
            base.PutTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从拧盖2到移栽
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1"></param>
        /// <param name="func2"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperTwoToTransfer(ushort num, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromCapperTwoToMaterial-{num},clawOpenByte-{clawOpenByte}");
            //取料辅助动作
            //var result = func1?.Invoke(num) != false;
            //if (!result)
            //{
            //    return false;
            //}

            //取料
            base.GetTubeAsync(GetCapperTwoCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            //result = func2?.Invoke(num) != false;
            //if (!result)
            //{
            //    return false;
            //}

            //移动到安全位置
            var result = CarrierMoveToSafePos(GetColdCoordinate(1), cts).GetAwaiter().GetResult();
            if (!result)
            {
                throw new Exception("搬运移动到安全位置失败!");
            }

            //旋转到指定位置
            result = func.Invoke(num, cts);
            if (!result)
            {
                throw new Exception("移栽移动到指定位失败!");
            }
            //放料
            base.PutTubeAsync(GetTransferCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从拧盖2到冰浴
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperTwoToCold(ushort num, ushort posNum, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromCapperTwoToCold-{num},clawOpenByte-{clawOpenByte}");

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetCapperTwoCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

     
            //放料
            base.PutTubeAsync(GetColdCoordinate(posNum), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        //===========================================================================================================//

        /// <summary>
        /// 从振荡到冰浴
        /// </summary>
        /// <param name="num"></param>
        /// <param name="posNum">冰浴位置编号</param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromVibrationToCold(ushort num, ushort posNum, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromVibrationToCold-{num},clawOpenByte-{clawOpenByte}");
            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetVibrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }


            //放料
            base.PutTubeAsync(GetColdCoordinate(posNum), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }

        /// <summary>
        /// 从振荡到涡旋
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromVibrationToVortex(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromVibrationToVortex-{num},clawOpenByte-{clawOpenByte}");
            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetVibrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }


            //放料
            base.PutTubeAsync(GetVortexCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }

        /// <summary>
        /// 从振荡到试管架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromVibrationToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromVibrationToMaterial-{num},clawOpenByte-{clawOpenByte}");
            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetVibrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }


            //放料
            base.PutTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从振荡到移栽
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="func3">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromVibrationToTransfer(ushort num, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromVibrationToTransfer-{num},clawOpenByte-{clawOpenByte}");
            //取料辅助动作
            //var result = func1?.Invoke(num) != false;
            //if (!result)
            //{
            //    return false;
            //}

            //取料
            base.GetTubeAsync(GetVibrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            //result = func2?.Invoke(num) != false;
            //if (!result)
            //{
            //    return false;
            //}

            //移动到安全位置
            var result = CarrierMoveToSafePos(GetColdCoordinate(1), cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }

            //旋转到指定位置
            result = func.Invoke(num,cts);
            if (!result)
            {
                throw new Exception("移栽移动到指定位失败!");
            }


            //放料
            base.PutTubeAsync(GetTransferCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }

        //===========================================================================================================//

        /// <summary>
        /// 从移栽到试管架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromTransferToMaterial(ushort num, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromTransferToMaterial-{num},clawOpenByte-{clawOpenByte}");
            //旋转到指定位置
            var result = func.Invoke(num,cts);
            if (!result)
            {
                throw new Exception("移栽移动到指定位失败!");
            }

            //取料
            base.GetTubeAsync(GetTransferCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //移动到安全位置
            result = CarrierMoveToSafePos(GetColdCoordinate(1), cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }

            //放料
            base.PutTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从移栽到拆盖2
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromTransferToCapperTwo(ushort num, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            _logger.Debug($"GetSampleFromTransferToCapperTwo-{num},clawOpenByte-{clawOpenByte}");

            //旋转到指定位置
            var result = func.Invoke(num, cts);
            if (!result)
            {
                throw new Exception("移栽移动到指定位失败!");
            }

            //取料
            base.GetTubeAsync(GetTransferCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //移动到安全位置
            result = CarrierMoveToSafePos(GetColdCoordinate(1), cts).GetAwaiter().GetResult();
            if (!result)
            {
                return false;
            }

            //放料
            base.PutTubeAsync(GetCapperTwoCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;

        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// 从冰浴到移栽
        /// </summary>
        /// <param name="num"></param>
        /// <param name="posNum">冰浴缓存位置代号</param>
        /// <param name="func">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromColdToTransfer(ushort num,ushort posNum, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;


            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger.Debug($"GetSampleFromColdToTransfer-{num}-{posNum},clawOpenByte-{clawOpenByte}");

            //取料
            base.GetTubeAsync(GetColdCoordinate(posNum), clawOpenByte, cts).GetAwaiter().GetResult();

            //移动到安全位置
            if (num != 1)
            {
                //移动到安全位置
                var ret = CarrierMoveToSafePos(GetColdCoordinate(1), cts).GetAwaiter().GetResult();
                if (!ret)
                {
                    return false;
                }
            }

            //旋转到指定位置
            var result = func.Invoke(num,cts);
            if (!result)
            {
                throw new Exception("移栽移动到指定位失败!");
            }

            //放料
            base.PutTubeAsync(GetTransferCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从冰浴到振荡
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromColdToVibration(ushort num,CancellationTokenSource cts)
        {
            byte clawOpenByte = 40;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            _logger.Debug($"GetSampleFromColdToVibration-{num},clawOpenByte-{clawOpenByte}");
          
            //取料
            base.GetTubeAsync(GetColdCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetVibrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        #endregion

        #region Private Methods


        /// <summary>
        /// 获取50ml试管位置
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] GetSampleTubeCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.SamplePos1;
            if (tubeId > 24 && tubeId <= 48)
            {
                xyz = _posData.SamplePos2;
            }
            if (tubeId > 48 && tubeId <= 72)
            {
                xyz = _posData.SamplePos3;
            }
            if (tubeId > 72 && tubeId <= 96)
            {
                xyz = _posData.SamplePos4;
            }
            //计算偏移
            int id = (tubeId - 1) % 24;

            //计算结果

            return base.GetCoordinate(id + 1, 8, 3, -45, 45, xyz);

        }

        /// <summary>
        /// 获取冷却缓存坐标
        /// </summary>
        /// <param name="tubeId">1-16</param>
        /// <returns></returns>
        private double[] GetColdCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.ColdPos;
            return base.GetCoordinate(tubeId, 8, 2, -45, 45, xyz);
        }

        /// <summary>
        /// 获取加固位置
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetAddSolidCoordinate(int tubeId, bool isGetPos = true)
        {
            double x = _posData.AddSolidPos[0];
            double y = _posData.AddSolidPos[1];
            double z = _posData.AddSolidPos[2];

            int i = (tubeId - 1) % 2;
            if (isGetPos)
            {
                if (i == 1)  //单数
                {
                    return new double[] { x + 50, y, z };
                }
                return new double[] { x, y, z };
            }
            else
            {
                if (i == 1)  //单数
                {
                    return new double[] { x + 50, y, z - 5 };
                }
                return new double[] { x, y, z - 5 };
            }
        }

        /// <summary>
        /// 获取拧盖1位置
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetCapperOneCoordinate(int tubeId)
        {
            double x = _posData.CapperOnePos[0];
            double y = _posData.CapperOnePos[1];
            double z = _posData.CapperOnePos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 60, y , z };
            }
            return new double[] { x, y, z };
        }

        /// <summary>
        /// 获取涡旋位置
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetVortexCoordinate(int tubeId)
        {
            double x = _posData.VortexPos[0];
            double y = _posData.VortexPos[1];
            double z = _posData.VortexPos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 50, y, z };
            } 
            return new double[] { x, y, z };
        }

        /// <summary>
        /// 获取拧盖2位置
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetCapperTwoCoordinate(int tubeId)
        {
            double x = _posData.CapperTwoPos[0];
            double y = _posData.CapperTwoPos[1];
            double z = _posData.CapperTwoPos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 60, y , z };
            }
            return new double[] { x, y, z };
        }

        /// <summary>
        /// 获取振荡位置
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetVibrationCoordinate(int tubeId)
        {
            double x = _posData.VibrationOnePos[0];
            double y = _posData.VibrationOnePos[1];
            double z = _posData.VibrationOnePos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 60, y, z };
            }
            return new double[] { x, y, z };
        }

        /// <summary>
        /// 获取移栽位置
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetTransferCoordinate(int tubeId)
        {
            double x = _posData.TransferLeftPos[0];
            double y = _posData.TransferLeftPos[1];
            double z = _posData.TransferLeftPos[2];

            int i = (tubeId - 1) % 2;
            //if (i == 1)  //单数
            //{
            //    return new double[] { x, y + 50, z };
            //}
            return new double[] { x, y, z };
        }


        //================================================================//

        /// <summary>
        /// 获取Tip头坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] GetTipCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.NeedlePos;
            return base.GetCoordinate(tubeId, 16, 6, -20, 20, xyz);
        }

        /// <summary>
        /// 获取移液取液位坐标
        /// </summary>
        /// <returns></returns>
        private double[] GetPipettorSourceCoordinate(int tubeId,bool bigToSmall)
        {
            bool b = tubeId % 2 == 0;
            if (!b) //第一个
            {
                if (bigToSmall)
                {
                    return _posData.PipettingSourcePos;
                }
                return _posData.PipettingSourcePos2;
            }
            else
            {
                if (bigToSmall)
                {
                    return new double[] 
                    {
                        _posData.PipettingSourcePos[0] +60,
                        _posData.PipettingSourcePos[1],
                        _posData.PipettingSourcePos[2]
                    };
                }
                return new double[]
                   {
                        _posData.PipettingSourcePos2[0] + 44,
                        _posData.PipettingSourcePos2[1],
                        _posData.PipettingSourcePos2[2]
                   };
            }
          
        }

        /// <summary>
        /// 获取移液吐液位坐标
        /// </summary>
        /// <returns></returns>
        private double[] GetPipettorTargetCoordinate(int tubeId, bool bigToSmall)
        {
            bool b = tubeId % 2 == 0;
            if (!b) //第一个
            {
                if (bigToSmall)
                {   //大管到小管
                    return _posData.PipettingTargetPos;
                }
                return new double[]
                    {   //大管取液位置 小管到大管
                        _posData.PipettingSourcePos[0],
                        _posData.PipettingSourcePos[1],
                        _posData.PipettingSourcePos[2] -30,
                    };

            }
            else
            {
                if (bigToSmall)
                {
                    return new double[] 
                    {   //大管到小管
                        _posData.PipettingTargetPos[0] + 44,
                        _posData.PipettingTargetPos[1],
                        _posData.PipettingTargetPos[2],
                    };
                }
                return new double[]
                {   //大管取液位置 小管到大管
                    _posData.PipettingSourcePos[0] + 60,
                    _posData.PipettingSourcePos[1],
                    _posData.PipettingSourcePos[2] -30,
                };
            }
          
        }



        #endregion

        protected override double[] GetPipettingSafePos()
        {
            return GetSampleTubeCoordinate(73);
        }






        /// <summary>
        /// 从拧盖1搬运试管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func1"></param>
        /// <param name="func2"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperOneToMaterial(Sample sample, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            _logger?.Info($"从拧盖1搬运{sampleId}样品到试管架");
            try
            {
                lock (_lockObj)
                {
                    //试管在拧盖1
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperOne))
                    {
                        if (sample.TubeStatus == 0)
                        {
                            result = GetSampleFromCapperOneToMaterial((ushort)(2 * sampleId - 1), func1, func2, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖1搬运{sampleId}样品到试管架失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1)
                        {
                            result = GetSampleFromCapperOneToMaterial((ushort)(2 * sampleId), func1, func2, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖1搬运{sampleId}样品到试管架失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCapperOne);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInShelf);
                    }
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                    {
                        return true;
                    }
                    throw new Exception($"从拧盖1搬运{sampleId}样品到试管架失败,SampleStatus-{sample.Status}");

                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从拧盖1搬运{sampleId}样品到试管架 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

        }






        public bool Test(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
            ushort posNum = 0;

            //试管在冰浴
            if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCold))
            {
                return true;
            }

            DateTime end = DateTime.Now + TimeSpan.FromMinutes(10);
        attemp: for (int i = 1; i < 9; i++)
            {
                if (_coldDic.ContainsValue((ushort)i))
                {
                    continue;
                }
                else
                {
                    posNum = (ushort)i;
                    break;
                }
            }
            if (posNum == 0)
            {
                if (DateTime.Now > end)
                {
                    throw new Exception("冰浴试管已满！ 等待超时");
                }
                Thread.Sleep(10000);
                goto attemp;
            }

            try
            {
                lock (_lockObj)
                {

                    //试管在振荡
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                    {
                        if (sample.TubeStatus == 0 && cts?.IsCancellationRequested != true)
                        {
                          
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1 && cts?.IsCancellationRequested != true)
                        {
                          
                            sample.TubeStatus = 0;
                        }

                        _coldDic.Add(sample, posNum);

                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInShelf);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCold);
                    }


                    //试管在冰浴
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCold))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{ sample.Id}样品到冰浴失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                throw ex;
            }
        }



    }
}
