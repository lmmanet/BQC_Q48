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
    public class CarrierTwo : CarrierBase , ICarrierTwo
    {
        private static ILogger logger = new MyLogger(typeof(CarrierTwo));

        private int _pipStep;    //移液步骤

        #region Private Members

        private readonly static object _lockObj = new object();

        private string _currentMethodName = string.Empty;

        private CarrierTwoPosData _posData;

        private ICarrierTwoDataAccess _dataAccess;

        #endregion

        #region Construtors
        public CarrierTwo(IEtherCATMotion motion, IEPG26 claw, IGlobalStatus globalStatus, ICarrierTwoDataAccess dataAccess) : base(motion, claw, globalStatus, logger)
        {
            _axisX = 9;
            _axisY = 10;
            _axisZ1 = 11;
            _axisZ2 = 12;
            _axisP = 14;
            _clawSlaveId = 3;
            _axisP = 15;
            _putOffNeedle = -0.5;
            this._dataAccess = dataAccess;

            _posData = _dataAccess.GetPosData();
        }

        #endregion

        #region Public Methods

        public override Task<bool> GoHome(CancellationTokenSource cts)
        {
            _pipStep = 0;
            return base.GoHome(cts);
        }

        public bool DoPipetting(ushort num, double[] src, double[] dst, double volume, CancellationTokenSource cts)
        {
            throw new NotImplementedException();
        }


        public bool DoPipetting(Sample sample, double[] src, double[] dst, CancellationTokenSource cts)
        {
            double volume = 2;
            lock (_lockObj)
            {
                //取枪头
                //&& cts?.IsCancellationRequested != true

                //移液  净化管（2ml）==》小瓶  浓缩西林瓶  ==》 小瓶  净化管（2ml） ==》西林瓶    大管==》西林瓶    



                //放枪头

            }
            throw new NotImplementedException();
        }









        /// <summary>
        /// 搬运试管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleToCapperThree(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
         
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"搬运{sampleId}净化管到拧盖3");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf2))
                    {
                        if (sample.TubeStatus == 0)
                        {
                            result = GetSampleFromMaterialToCapperThree((ushort)(2 * sampleId - 1), cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1)
                        {
                            result = GetSampleFromMaterialToCapperThree((ushort)(2 * sampleId), cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到拧盖3失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInShelf2);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCapperThree);
                    }

                    //试管在振荡
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInVibrationTwo))
                    {
                        if (sample.TubeStatus == 0)
                        {
                            result = GetSampleFromVibrationToCapperThree((ushort)(2 * sampleId -1), null, null,cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡2搬运{sampleId}净化管到拧盖3失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1)
                        {
                            result = GetSampleFromVibrationToCapperThree((ushort)(2 * sampleId), null, null, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡2" +
                                    $"搬运{sampleId}净化管到拧盖3失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInVibrationTwo);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCapperThree);
                    }

                    //试管在移栽
                  
                    //试管在拧盖3   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperThree))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}净化管到拧盖3失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"搬运{sampleId}净化管到拧盖3 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从移栽搬运试管到拧盖3
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromTransferToCapperThree(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
        
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从移栽搬运{sampleId}净化管到拧盖3");
                    //试管在移栽
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                    {
                        if (sample.TubeStatus == 0)
                        {
                            result = GetSampleFromTransferToCapperThree((ushort)(2 * sampleId), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从移栽搬运{sampleId}净化管到拧盖3失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1)
                        {
                            result = GetSampleFromTransferToCapperThree((ushort)(2 * sampleId -1), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从移栽搬运{sampleId}净化管到拧盖3失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInTransfer);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCapperThree);
                    }

                    //试管在拧盖3   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperThree))
                    {
                        return true;
                    }
                    throw new Exception($"从移栽搬运{sampleId}净化管到拧盖3失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽搬运{sampleId}净化管到拧盖3 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 搬运试管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleToTransfer(Sample sample, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            bool result;
           
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"搬运{sampleId}净化管到移栽");
                    //试管在净化试管架
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf2))
                    {
                        if (sample.TubeStatus == 0)
                        {
                            result = GetSampleFromMaterialToTransfer((ushort)(2 * sampleId - 1), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1)
                        {
                            result = GetSampleFromMaterialToTransfer((ushort)(2 * sampleId), func, cts);
                            if (!result)
                            {
                                throw new Exception($"从试管架搬运{sampleId}净化管到移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInShelf2);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                    }

                    //试管在振荡
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInVibrationTwo))
                    {
                        if (sample.TubeStatus == 0)
                        {
                            result = GetSampleFromVibrationToTransfer((ushort)(2 * sampleId - 1), null, null,func, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡2搬运{sampleId}净化管到移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1)
                        {
                            result = GetSampleFromVibrationToTransfer((ushort)(2 * sampleId), null, null,func, cts);
                            if (!result)
                            {
                                throw new Exception($"从振荡2搬运{sampleId}净化管到移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInVibrationTwo);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                    }

                    //试管在拧盖3   
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCapperThree))
                    {
                        if (sample.TubeStatus == 0)
                        {
                            result = GetSampleFromCapperThreeToTransfer((ushort)(2 * sampleId - 1), null, null, func, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖3搬运{sampleId}净化管到移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }
                        if (sample.TubeStatus == 1)
                        {
                            result = GetSampleFromCapperThreeToTransfer((ushort)(2 * sampleId), null, null, func, cts);
                            if (!result)
                            {
                                throw new Exception($"从拧盖3搬运{sampleId}净化管到移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }
                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCapperThree);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                    }

                    //试管在移栽
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}净化管到移栽失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"搬运{sampleId}净化管到移栽 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        #endregion

        #region Protected Methods  搬运部分

        /// <summary>
        /// 从试管架到拧盖3
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromMaterialToCapperThree(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

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
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

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
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //旋转到指定位置
            var result = func.Invoke(num, cts);
            if (!result)
            {
                throw new Exception("移栽移动到指定位失败!");
            }

            //放料
            base.PutTubeAsync(GetTransferCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从拧盖3到试管架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperThreeToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

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
        /// 从拧盖3到振荡
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperThreeToVibration(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //放料
            base.PutTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从拧盖3到移栽
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="func3">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromCapperThreeToTransfer(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //旋转到指定位置
            result = func.Invoke(num, cts);
            if (!result)
            {
                throw new Exception("移栽移动到指定位失败!");
            }

            //放料
            base.PutTubeAsync(GetTransferCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

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
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

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
        protected bool GetSampleFromVibrationToTransfer(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //旋转到指定位置
            result = func.Invoke(num, cts);
            if (!result)
            {
                throw new Exception("移栽移动到指定位失败!");
            }

            //放料
            base.PutTubeAsync(GetTransferCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从振荡到拧盖3
        /// </summary>
        /// <returns></returns>
        protected bool GetSampleFromVibrationToCapperThree(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料辅助动作
            var result = func1?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //取料
            base.GetTubeAsync(GetVibrationCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //取料完成辅助动作
            result = func2?.Invoke(num) != false;
            if (!result)
            {
                return false;
            }

            //放料
            base.PutTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从移栽到试管架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromTransferToMaterial(ushort num, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //旋转到指定位置
            var result = func.Invoke(num, cts);
            if (!result)
            {
                throw new Exception("移栽移动到指定位失败!");
            }

            //取料
            base.GetTubeAsync(GetTransferCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();


            //放料
            base.PutTubeAsync(GetSampleTubeCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从移栽到拆盖3
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func">移栽旋转指定角度</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSampleFromTransferToCapperThree(ushort num, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            byte clawOpenByte = 0;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //旋转到指定位置
            var result = func.Invoke(num,cts);
            if (!result)
            {
                throw new Exception("移栽移动到指定位失败!");
            }

            //取料
            base.GetTubeAsync(GetTransferCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();


            //放料
            base.PutTubeAsync(GetCapperThreeCoordinatte(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        //===============================================================================================================================================//

        /// <summary>
        /// 从西林瓶架到拧盖4
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromMaterialToCapperFour(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(GetSeilingCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从拧盖4到西林瓶架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromCapperFourToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetSeilingCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从拧盖4到浓缩
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromCapperFourToConcentration(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetConcentrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从拧盖4到称重
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromCapperFourToWeight(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetWeightCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从浓缩到拧盖4
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromConcentrationToCapperFour(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(GetConcentrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从浓缩到称重
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromConcentrationToWeight(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;


            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }
            //取料
            base.GetTubeAsync(GetConcentrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetWeightCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从称重到拧盖4
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromWeightToCapperFour(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(GetWeightCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetCapperFourCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从称重到浓缩
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool GetSeilingFromWeightToConcentration(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 10;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(GetWeightCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetConcentrationCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从气质小瓶架到拧盖5
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool Get_GC_BottleFromMaterialToCapperFive(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 180;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(Get_GC_BottleCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetCapperFiveCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从拧盖5到气质小瓶架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool Get_GC_BottleFromCapperFiveToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 180;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(GetCapperFiveCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(Get_GC_BottleCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从液质小瓶架到拧盖5
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool Get_LC_BottleFromMaterialToCapperFive(ushort num, CancellationTokenSource cts)
        {
            byte clawOpenByte = 180;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(Get_LC_BottleCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(GetCapperFiveCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// 从拧盖5到气质液质小瓶架
        /// </summary>
        /// <param name="num"></param>
        /// <param name="func1">取料前动作</param>
        /// <param name="func2">取料后动作</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool Get_LC_BottleFromCapperFiveToMaterial(ushort num, Func<ushort, bool> func1, Func<ushort, bool> func2, CancellationTokenSource cts)
        {
            byte clawOpenByte = 180;

            if (cts?.IsCancellationRequested == true)
            {
                throw new TaskCanceledException($"触发停止 cts:{cts.IsCancellationRequested}");
            }

            //取料
            base.GetTubeAsync(GetCapperFiveCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            //放料
            base.PutTubeAsync(Get_LC_BottleCoordinate(num), clawOpenByte, cts).GetAwaiter().GetResult();

            return true;
        }

        #endregion

        #region Protected Methods 移液部分

        protected override async Task<bool> DoPipettingAsync(double[] sourcePos, double[] targetPos, double volume, CancellationTokenSource cts)
        {
            //移液1ml
            if (volume <= 1 && cts?.IsCancellationRequested != true)
            {
                return await base.DoPipettingAsync(sourcePos, targetPos, volume, cts).ConfigureAwait(false);
            }

            //移液1~2ml
            if (volume > 1 && volume <= 2)
            {
                if (_pipStep == 0 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 1 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, volume - 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep = 0;
                    return true;
                }
            }

            //移液2~3ml
            if (volume > 2 && volume <= 3)
            {
                if (_pipStep == 0 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 1 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 2 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, volume - 2, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep = 0;
                    return true;
                }
            }

            //移液3~4ml
            if (volume > 3 && volume <= 4)
            {
                if (_pipStep == 0 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 1 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 2 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 3 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, volume - 3, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep = 0;
                    return true;
                }
            }

            //移液4~5ml
            if (volume > 4 && volume <= 5)
            {
                if (_pipStep == 0 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 1 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 2 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 3 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, 1, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep++;
                }
                if (_pipStep == 4 && cts?.IsCancellationRequested != true)
                {
                    var result = await base.DoPipettingAsync(sourcePos, targetPos, volume - 4, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        throw new Exception($"DoPipettingAsync err step:{_pipStep}");
                    }
                    _pipStep = 0;
                    return true;
                }
            }

            throw new InvalidOperationException($"移液参数超出范围{volume}");
        }


        #endregion

        #region Private Methods

        /// <summary>
        /// 获取15ml试管位置
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] GetSampleTubeCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.PurifyTubePos1;
            if (tubeId > 48)
            {
                xyz = _posData.PurifyTubePos2;
            }

            //计算偏移
            int id = (tubeId - 1) % 48;

            //计算结果

            return base.GetCoordinate(id + 1, 12, 4, -32, 32, xyz);

        }

        /// <summary>
        /// 获取拧盖3坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetCapperThreeCoordinatte(int tubeId)
        {
            double x = _posData.CapperThreePos[0];
            double y = _posData.CapperThreePos[1];
            double z = _posData.CapperThreePos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 60, y, z };
            }
            return new double[] { x, y, z };
        }    
      
        /// <summary>
        /// 获取振荡坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetVibrationCoordinatte(int tubeId)
        {
            double x = _posData.VibrationTwoPos[0];
            double y = _posData.VibrationTwoPos[1];
            double z = _posData.VibrationTwoPos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 60, y, z };
            }
            return new double[] { x, y, z };
        }

        /// <summary>
        /// 获取移栽坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetTransferCoordinatte(int tubeId)
        {
            double x = _posData.TransferRightPos[0];
            double y = _posData.TransferRightPos[1];
            double z = _posData.TransferRightPos[2];

            int i = (tubeId - 1) % 2;
            //if (i == 1)  //单数
            //{
            //    return new double[] { x, y + 50, z };
            //}
            return new double[] { x, y, z };
        }

        //=======================================================================//

        /// <summary>
        /// 获取拧盖4坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetCapperFourCoordinate(int tubeId)
        {
            double x = _posData.CapperFourPos[0];
            double y = _posData.CapperFourPos[1];
            double z = _posData.CapperFourPos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 60, y, z };
            }
            return new double[] { x, y, z };
        }

        /// <summary>
        /// 获取西林瓶坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] GetSeilingCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.SeilingPos1;
            if (tubeId > 48)
            {
                xyz = _posData.SeilingPos2;
            }

            //计算偏移
            int id = (tubeId - 1) % 48;

            //计算结果

            return base.GetCoordinate(id + 1, 12, 4, -32, 32, xyz);
        }

        /// <summary>
        /// 获取浓缩位置坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetConcentrationCoordinate(int tubeId)
        {
            double x = _posData.ConcentrationPos[0];
            double y = _posData.ConcentrationPos[1];
            double z = _posData.ConcentrationPos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 50, y, z };
            }
            return new double[] { x, y, z };
        }

        /// <summary>
        /// 获取称重位置坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetWeightCoordinate(int tubeId)
        {
            return _posData.WeightPos;
        }



        /// <summary>
        /// 获取气质小瓶坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] Get_GC_BottleCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.BottlePos1;
            if (tubeId > 48)
            {
                xyz = _posData.BottlePos2;
            }

            //计算偏移
            int id = (tubeId - 1) % 48;

            //计算结果

            return base.GetCoordinate(id + 1, 12, 4, -16, 15, xyz);
        }

        /// <summary>
        /// 获取液质小瓶坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] Get_LC_BottleCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.BottlePos3;
            if (tubeId > 48)
            {
                xyz = _posData.BottlePos4;
            }

            //计算偏移
            int id = (tubeId - 1) % 48;

            //计算结果

            return base.GetCoordinate(id + 1, 12, 4, -16, 15, xyz);
        }

        /// <summary>
        /// 获取拧盖5坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <returns></returns>
        private double[] GetCapperFiveCoordinate(int tubeId)
        {
            double x = _posData.CapperFivePos[0];
            double y = _posData.CapperFivePos[1];
            double z = _posData.CapperFivePos[2];

            int i = (tubeId - 1) % 2;
            if (i == 1)  //单数
            {
                return new double[] { x + 60, y, z };
            }
            return new double[] { x, y, z };
        }


        /// <summary>
        /// 获取Tip1头坐标
        /// </summary>
        /// <param name="tubeId">1-96</param>
        /// <returns></returns>
        private double[] GetTipCoordinate(int tubeId)
        {
            //获取参考点坐标
            double[] xyz = _posData.NeedlePos1;
            if (tubeId>48)
            {
                xyz = _posData.NeedlePos2;
            }
     
            return base.GetCoordinate(tubeId, 8, 12, -10, 20, xyz);
        }

        /// <summary>
        /// 获取移液取液坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <param name="tech_i"></param>
        /// <returns></returns>
        private double[] GetPipettorSourceCoordinate(int tubeId ,int tech_i)
        {
            //净化管（2ml）==》小瓶  浓缩西林瓶  ==》 小瓶  净化管（2ml） ==》西林瓶    大管==》西林瓶  
            switch (tech_i)
            {
                case 1:
                case 3:  //净化管（2ml）  拧盖3处
                    return _posData.PipettingSourcePos;
                case 2:  //浓缩西林瓶
                    return _posData.PipettingSourcePos1;
                case 4:  //移栽大管
                    return _posData.PipettingSourcePos2;
                default:
                    throw new InvalidOperationException($"移液工艺错误 err:{tech_i}");
            }


        }

        /// <summary>
        /// 获取移液吐液坐标
        /// </summary>
        /// <param name="tubeId"></param>
        /// <param name="tech_i"></param>
        /// <returns></returns>
        private double[] GetPipettorTargetCoordinate(int tubeId,int tech_i)
        {
            //净化管（2ml）==》小瓶  浓缩西林瓶  ==》 小瓶  净化管（2ml） ==》西林瓶    大管==》西林瓶  
            switch (tech_i)
            {
                case 1:
                case 2: //小瓶（2ml）  拧盖5处
                    return _posData.PipettingTargetPos1;
                case 3: //西林瓶 拧盖4处
                case 4: //西林瓶 拧盖4处
                    return _posData.PipettingTargetPos2;
                default:
                    throw new InvalidOperationException($"移液工艺错误 err:{tech_i}");
            }

        }


        #endregion


        /// <summary>
        /// 移液器规避位置
        /// </summary>
        /// <returns></returns>
        protected override double[] GetPipettingSafePos()
        {
            throw new NotImplementedException();
        }


    }
}
