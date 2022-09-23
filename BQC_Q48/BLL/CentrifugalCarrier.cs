using BQJX.Common;
using BQJX.Common.Common;
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
    public class CentrifugalCarrier : ICentrifugalCarrier
    {
        //运行参数
        private Task _pipttorTask;            //移液提取 （包括离心搬运 拧盖2 拧盖3）
        private List<Sample> _pipttorList = new List<Sample>();
        private Dictionary<Sample, Action<Sample, CancellationTokenSource>> _sampleActionDic = new Dictionary<Sample, Action<Sample, CancellationTokenSource>>(); //回调列表


        #region Private Members
        private readonly static object _lockObj = new object();

        private readonly IEtherCATMotion _motion;
        private readonly IIoDevice _io;
        private readonly ILS_Motion _stepMotion;
        private readonly IEPG26 _claw;
        private readonly ILogger _logger;
        private readonly ICentrifugalCarrierPosDataAccess _dataAccess;
        private readonly ICapperTwo _capperTwo;
        private readonly ICapperThree _capperThree;

        private CentrifugalCarrierPosData _posData;

        #endregion

        #region Variants

        private ushort _axisZ = 7;               //搬运Z轴
        private ushort _axisX = 14;              //搬运X轴
        private ushort _axisC = 15;              //搬运旋转轴

        private ushort _y_Ctr = 2;               //Y气缸控制
        private ushort _y_HP = 3;                //Y气缸缩回感应
        private ushort _y_WP = 2;                //Y气缸伸出感应

        private ushort _clawId = 2;              //电爪485地址
        private double _stepMoveVel = 30;        //步进电机移动速度
        private double _sevorMoveVel = 50;       //伺服电机移动速度
        #endregion

        #region Construtors

        public CentrifugalCarrier(IEtherCATMotion motion, IIoDevice io, ILS_Motion stepMotion, IEPG26 claw, ICentrifugalCarrierPosDataAccess dataAccess, ICapperTwo capperTwo,ICapperThree capperThree)
        {
            this._motion = motion;
            this._io = io;
            this._stepMotion = stepMotion;
            this._dataAccess = dataAccess;
            this._claw = claw;

            this._capperTwo = capperTwo;
            this._capperThree = capperThree;

            this._logger = new MyLogger(typeof(CentrifugalCarrier));

            _posData = _dataAccess.GetPosData();
        }


        #endregion

        #region Public Methods

        /// <summary>
        /// 模块回零
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GoHome(CancellationTokenSource cts)
        {
            _logger?.Info("离心机搬运回零");
            try
            {
                //失能夹爪
                var result = await _claw.Disable(_clawId).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //使能夹爪
                result = await _claw.Enable(_clawId).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //Z轴回零
                result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, 10, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //X C轴回零
                await _stepMotion.ServoOn(_axisX).ConfigureAwait(false);
                await _stepMotion.ServoOn(_axisC).ConfigureAwait(false);
                var ret1 = _stepMotion.GoHomeWithCheckDone(_axisX, cts).ConfigureAwait(false);
                var ret2 = _stepMotion.GoHomeWithCheckDone(_axisC, cts).ConfigureAwait(false);
                if (!ret1.GetAwaiter().GetResult() || !ret2.GetAwaiter().GetResult())
                {
                    return false;
                }
                //气缸回零
                Y_Cylinder_Put();

                return true;
            }
            //catch (CommunicationException cmex)
            //{
            //    return false;
            //}
            //catch (EtherCATMotionException ecex)
            //{
            //    return false;
            //}
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

        }

        //================================================================离心机搬运部分=====================================================================//

        /// <summary>
        /// 搬运试管到离心机
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="isBig"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetTubeInCentrifugal(Sample sample, Func<ushort, Task<bool>> func, bool isBig, CancellationTokenSource cts)
        {
            byte clawOpen = 110;
            ushort sampleId = sample.Id;

            //判断大小管
            ushort pos1 = 2, pos2 = 4;
            if (isBig)
            {
                clawOpen = 0;
                pos1 = 1;
                pos2 = 3;
            }
          
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"搬运{sampleId}样品到离心机");
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                    {
                        if (sample.TubeStatus == 0)
                        {
                            //移栽上取料
                            var result = GetTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId - 1), isBig), clawOpen, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"从离心移栽取{sampleId}样品失败！ TubeStatus-{sample.TubeStatus}");
                            }

                            //离心机放料
                            result = PutTubeAtCentrifugal(pos1, func, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"放{sampleId}样品到离心机失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }

                        if (sample.TubeStatus == 1)
                        {
                            //移栽上取料
                            var result = GetTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId), isBig), clawOpen, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"从离心移栽取{sampleId}样品失败！ TubeStatus-{sample.TubeStatus}");
                            }

                            //离心机放料
                            result = PutTubeAtCentrifugal(pos2, func, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"放{sampleId}样品到离心机失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }

                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInTransfer);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInCentrifugal);
                    }

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCentrifugal))
                    {
                        return true;
                    }
                    throw new Exception($"搬运{sampleId}样品到离心机失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"搬运{sampleId}样品到离心机 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从离心机搬运试管出来
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="func"></param>
        /// <param name="isBig"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetTubeOutCentrifugal(Sample sample, Func<ushort, Task<bool>> func, bool isBig, CancellationTokenSource cts)
        {
            byte clawOpen = 110;
            ushort sampleId = sample.Id;

            //判断大小管
            ushort pos1 = 2, pos2 = 4;
            if (isBig)
            {
                clawOpen = 0;
                pos1 = 1;
                pos2 = 3;
            }
          
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从离心机搬运{sampleId}样品到移栽");
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCentrifugal))
                    {
                        if (sample.TubeStatus == 0)
                        {
                            //离心机取料
                            var result = GetTubeAtCentrifugal(pos1, func, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"从离心机取{sampleId}样品失败！ TubeStatus-{sample.TubeStatus}");
                            }

                            //移栽放料
                            result = PutTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId - 1), isBig), clawOpen, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"放{sampleId}样品到移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 1;
                        }

                        if (sample.TubeStatus == 1)
                        {
                            //离心机取料
                            var result = GetTubeAtCentrifugal(pos2, func, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"从离心机取{sampleId}样品失败！ TubeStatus-{sample.TubeStatus}");
                            }

                            //移栽放料
                            result = PutTubeAtTransfer(GetCenterCoordinate((ushort)(2 * sampleId), isBig), clawOpen, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception($"放{sampleId}样品到移栽失败！ TubeStatus-{sample.TubeStatus}");
                            }
                            sample.TubeStatus = 0;
                        }

                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInCentrifugal);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                    }

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                    {
                        return true;
                    }

                    throw new Exception($"从离心机搬运{sampleId}样品到移栽失败,SampleStatus-{sample.Status}");

                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从离心机搬运{sampleId}样品到移栽 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }





        }


        //================================================================搬运1 离心机调用部分=====================================================================//
       
        /// <summary>
        /// 从冰浴搬运试管到移栽
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromColdToTransfer(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"取{sample.Id}样品到移栽");

                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInTransfer))
                    {
                        return true;
                    }

                    var result = _capperTwo.GetSampleFromColdToTransfer(sample, TransferMoveLeftPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"取{ sample.Id }样品到移栽 失败");
                    }
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"取{sample.Id}样品到移栽 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

            
        }

        /// <summary>
        /// 从移栽取离心完成后的试管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="isBig"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromTransferToMarterial(Sample sample, bool isBig, CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    if (isBig)
                    {
                        _logger?.Info($"从移栽取出{sample.Id}样品管");

                        //取出大管
                        var result = _capperTwo.GetSampleFromTransferToMaterial(sample, TransferMoveLeftPutGetPos, cts);
                        if (!result)
                        {
                            throw new Exception($"从移栽取出{sample.Id}样品管 失败");
                        }

                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInTransfer);
                        return true;
                    }
                    else
                    {
                        _logger?.Info($"从移栽取出{sample.Id}样品净化管");

                        //取出小管
                        var result = _capperThree.GetSampleFromTransferToMarterial(sample, TransferMoveRightPutGetPos, cts);
                        if (!result)
                        {
                            throw new Exception($"从移栽取出{sample.Id}样品净化管 失败");
                        }

                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsInTransfer);
                        return true;
                    }


                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽取出{sample.Id}样品管 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        //================================================================拧盖2 移液调用部分=====================================================================//

        /// <summary>
        /// 从移栽搬运萃取管到试管架
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromTransferToMaterial(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从移栽取出{sample.Id}萃取空管");

                    //从移栽取无盖试管到拧盖2
                    var result = _capperTwo.GetPolishFromTransferToCapperTwo(sample, TransferMoveLeftPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"从移栽取出{sample.Id}样萃取空管 失败");
                    }
                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishInTransfer);
                }

                bool ret = _capperTwo.GetPolishFromCapperTwoToMaterial(sample, cts);
                if (!ret)
                {
                    throw new Exception($"从拧盖2取出{sample.Id}样萃取空管 失败");
                }
                SampleStatusHelper.ResetBit(sample, SampleStatus.IsPolishInShelf);

                return true;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽取出{sample.Id}萃取空管 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从萃取管架搬运萃取管到移栽  移液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromMaterialToTransfer(Sample sample,  CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)//
                {
                    _logger?.Info($"取{sample.Id}样品萃取管到移栽");

                    //从试管架取无盖试管到移栽
                    var result = _capperTwo.GetPolishFromMaterialToTransfer(sample, TransferMoveLeftPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"取{sample.Id}样品萃取管到移栽 失败");
                    }

                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPolishInTransfer);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"取{sample.Id}样品萃取管到移栽 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 搬运空萃取管到拧盖2 接受上清液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetPolishFromMaterialToCapperTwo(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)//
                {
                    _logger?.Info($"取{sample.Id}样品萃取管到拧盖2接上清液");

                    //从试管架取无盖试管到移栽
                    var result = _capperTwo.GetPolishFromMaterialToCapperTwo(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"取{sample.Id}样品萃取管到拧盖2 失败");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"取{sample.Id}样品萃取管到拧盖2接上清液 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从拧盖2取萃取管到冰浴冷藏
        /// </summary>
        /// <returns></returns>
        public bool GetPolishFromCapperTwoToCold(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)//
                {
                    _logger?.Info($"从拧盖2取{sample.Id}样品萃取管到冰浴");

                    //从试管架取无盖试管到移栽
                    var result = _capperTwo.GetPolishFromMaterialToCapperTwo(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"取{sample.Id}样品萃取管到冰浴 失败");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从拧盖2取{sample.Id}样品萃取管到冰浴 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从样品架取样品管到拧盖2  移去上清液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromMaterialToCapperTwo(Sample sample ,CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)//
                {
                    _logger?.Info($"从试管架1取{sample.Id}样品管到拧盖2");

                    //从试管架取无盖试管到移栽
                    var result = _capperTwo.GetSampleFromMaterialToCapperTwo(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"取{sample.Id}样品管到拧盖2 失败");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从试管架1取{sample.Id}样品管到拧盖2 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 从拧盖2取回样品试管
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool GetSampleFromCapperTwoToMaterial(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)//
                {
                    _logger?.Info($"从拧盖2取{sample.Id}样品管到试管架1");

                    //从试管架取无盖试管到移栽
                    var result = _capperTwo.GetSampleFromCapperTwoToMaterial(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"取{sample.Id}样品管到试管架1 失败");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从拧盖2取{sample.Id}样品管到试管架1 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        //================================================================拧盖3 移栽配合部分=====================================================================//

        public bool GetSampleFromMarterialToTransfer(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"取{sample.Id}样品净化管到移栽");

                    var result = _capperThree.GetSampleFromMarterialToTransfer(sample, TransferMoveRightPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"取{ sample.Id }样品净化管到移栽 失败");
                    }
                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"取{sample.Id}样品净化管到移栽 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        public bool GetSampleFromCapperThreeToTransfer(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"取{sample.Id}样品净化管到移栽");

                    //取净化管到移栽
                    var result = _capperThree.GetSampleFromCapperThreeToTransfer(sample, TransferMoveRightPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"取{sample.Id}样品净化管到移栽 失败");
                    }

                    SampleStatusHelper.SetBitOn(sample, SampleStatus.IsInTransfer);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"取{sample.Id}样品净化管到移栽 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        public bool GetSampleFromTransferToMarterialPiperttor(Sample sample, CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"从移栽取出{sample.Id}样品净化管");

                    //取出净化管
                    var result = _capperThree.GetSampleFromTransferToCapperThree(sample, TransferMoveRightPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"从移栽取出{sample.Id}样品净化管 失败");
                    }

                    result = _capperThree.GetSampleFromCapperThreeToMaterial(sample, cts);

                    SampleStatusHelper.ResetBit(sample, SampleStatus.IsInTransfer);
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽取出{sample.Id}样品净化管 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }



        //================================================================移液部分=====================================================================//

        public Task StartPipetting(Sample sample, Action<Sample, CancellationTokenSource> actionCallBack, CancellationTokenSource cts)
        {
            //判断去重
            var ret = _pipttorList.Contains(sample);
            if (!ret)
            {
                _pipttorList.Add(sample);
                //保存回调列表
                _sampleActionDic.Add(sample, actionCallBack);
            }

            if (_pipttorTask != null)
            {
                if (!_pipttorTask.IsCompleted)
                {
                    return _pipttorTask;
                }
            }

            _pipttorTask = Task.Run(() =>
            {
                while (cts?.IsCancellationRequested != true && _pipttorList.Count > 0)
                {
                    var itemSample1 = _pipttorList[0];

                    try
                    {
                        //是否提取上清液
                        if (TechStatusHelper.BitIsOn(itemSample1.TechParams, TechStatus.ExtractSupernate) && sample.TechParams.TechStep == 5)
                        {
                            if (cts.IsCancellationRequested != true)
                            {
                                var result = DoPipettingOne(itemSample1, cts);
                                if (!result)
                                {
                                    throw new Exception("DoCentrifugal1 err");
                                }
                                TechStatusHelper.ResetBit(itemSample1.TechParams, TechStatus.ExtractSupernate);
                                sample.TechParams.TechStep = 6;
                            }
                            else
                            {
                                throw new TaskCanceledException("程序停止");
                            }

                        }

                        //提取净化管 净化液
                        if (TechStatusHelper.BitIsOn(itemSample1.TechParams, TechStatus.ExtractPurify) && sample.TechParams.TechStep == 8)
                        {
                            if (cts.IsCancellationRequested != true)
                            {
                                var result = DoPipettingTwo(itemSample1, cts);
                                if (!result)
                                {
                                    throw new Exception("DoCentrifugal2 err");
                                }
                                TechStatusHelper.ResetBit(itemSample1.TechParams, TechStatus.ExtractPurify);
                                sample.TechParams.TechStep = 9;
                            }
                            else
                            {
                                throw new TaskCanceledException("程序停止");
                            }

                        }

               
                    }
                    catch (Exception ex)
                    {
                        cts.Cancel();
                        return;
                    }
                    //触发后续动作   振荡净化 提取样品液  振荡涡旋
                    _sampleActionDic[itemSample1]?.Invoke(itemSample1, cts);
                    //样品和任务从列表移除
                    _pipttorList.Remove(itemSample1);
                    _sampleActionDic.Remove(itemSample1);

                }
            });

            return _pipttorTask;

 
        }





        /// <summary>
        /// 移液大管到净化管
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool DoPipettingOne(Sample sample,CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"{sample.Id}样品开始移液-Volume-{sample.TechParams.ExtractVolume}ml");

                    //搬运净化管到拧盖3 ==> 拆盖 ==>搬运无盖试管到移栽
                    var result = _capperThree.GetSampleFromCapperThreeToTransfer(sample, TransferMoveRightPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"{sample.Id}样品取净化管移液 失败");
                    }

                    //移栽移动到移液位
                    result = TransferMoveLeftPipettorPos(cts);
                    if (!result)
                    {
                        throw new Exception($"移栽移动到左侧 移液 失败");
                    }

                    //搬运样品管到拧盖2 ==> 拆盖 
                    result = _capperTwo.GetSampleFromMaterialToCapperTwo(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"搬运{sample.Id}样品离心管移液 失败");
                    }

                    //拧盖2 ==》搬运1开始移液
                    result = _capperTwo.DoPipettingOne(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"{sample.Id}样品管取上清液 失败");
                    }

                    result = _capperThree.GetSampleFromTransferToCapperThree(sample, TransferMoveRightPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"搬运{sample.Id}净化管到拧盖3 失败");
                    }

                }
                //拧盖2 回收试管
                var result1 = _capperTwo.GetSampleFromCapperTwoToMaterial(sample, cts); //包括搬运到试管架
                if (!result1)
                {
                    throw new Exception($"从拧盖2取{sample.Id}样品管到试管架1 失败");
                }

                //搬运空试管到拧盖3
                result1 = _capperThree.GetSampleFromCapperThreeToMaterial(sample, cts);
                if (!result1)
                {
                    throw new Exception($"搬运{sample.Id}净化管到试管架失败");
                }

                return true;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"{sample.Id}样品开始移液-Volume-{sample.TechParams.ExtractVolume}ml 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

        }

        /// <summary>
        /// 净化管移液到大管  或者 净化管移液到样品小瓶  或者 净化移液到西林瓶
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool DoPipettingTwo(Sample sample,CancellationTokenSource cts)
        {
            //净化管到大管
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"{sample.Id}样品开始移液-Volume-{sample.TechParams.ExtractVolume}ml");

                    //搬运净化管到拧盖3 ==> 拆盖 ==>搬运无盖试管到移栽
                    var result = _capperThree.GetSampleFromCapperThreeToTransfer(sample, TransferMoveRightPutGetPos, cts);
                    if (!result)
                    {
                        throw new Exception($"{sample.Id}样品取净化管移液 失败");
                    }

                    //搬运离心管到拧盖2 ==> 拆盖 
                    result = _capperTwo.GetPolishFromMaterialToCapperTwo(sample, cts);
                    if (!result)
                    {
                        throw new Exception($"搬运{sample.Id}样品离心管移液 失败");
                    }

                    //==》搬运1开始移液
                    result = _capperTwo.DoPipettingTwo(sample, cts);
                    if (!result)
                    {
                        throw new Exception("提取净化液失败!");
                    }


                    //下料

                    //搬运空试管到拧盖3
                    var result1 = _capperThree.GetSampleFromTransferToCapperThree(sample, TransferMoveRightPutGetPos, cts);
                    if (!result1)
                    {
                        throw new Exception($"搬运{sample.Id}净化管到拧盖3 失败");
                    }


                }

                var result3 = _capperThree.GetSampleFromCapperThreeToMaterial(sample, cts);
                if (!result3)
                {
                    throw new Exception($"搬运{sample.Id}净化管到试管架 失败");
                }


                //放冰浴
                var  result2 = _capperTwo.GetPolishFromCapperTwoToCold(sample, cts);
                if (!result2)
                {
                    throw new Exception("搬运到冰浴失败!");
                }

                return true;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"{sample.Id}样品开始移液-Volume-{sample.TechParams.ExtractVolume}ml 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

                
     






        //旋转到指定位置取料  与搬运配合
        public bool GetSampleOut(Sample sample, bool isBig, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            _logger?.Info($"从移栽移动{sample.Id }样品到拧盖2位");

            try
            {
                lock (_lockObj)
                {
                    if (sample.TubeStatus == 0)
                    {
                        double[] poss1 = GetLeftCoordinate(2 * sample.Id - 1, isBig);
                        //X C轴回零
                        var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, poss1[0], _stepMoveVel, cts).GetAwaiter().GetResult();
                        if (!result1)
                        {
                            throw new Exception("移栽X轴移动到目标位失败!");
                        }

                        var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, poss1[1], _stepMoveVel, cts).GetAwaiter().GetResult();
                        if (!result2)
                        {
                            throw new Exception("移栽旋转轴移动到目标位失败!");
                        }

                        var result3 = func.Invoke((ushort)(2 * sample.Id - 1), cts);
                        if (!result3)
                        {
                            throw new Exception("机械手取料失败!");
                        }
                        sample.TubeStatus = 1;
                    }

                    if (sample.TubeStatus == 1)
                    {
                        double[] poss2 = GetLeftCoordinate(2 * sample.Id, isBig);
                        //X C轴回零
                        var result4 = _stepMotion.P2pMoveWithCheckDone(_axisX, poss2[0], _stepMoveVel, cts).GetAwaiter().GetResult();
                        if (!result4)
                        {
                            throw new Exception("移栽X轴移动到目标位失败!");
                        }

                        var result5 = _stepMotion.P2pMoveWithCheckDone(_axisC, poss2[1], _stepMoveVel, cts).GetAwaiter().GetResult();
                        if (!result5)
                        {
                            throw new Exception("移栽旋转轴移动到目标位失败!");
                        }

                        var result6 = func.Invoke((ushort)(2 * sample.Id), cts);
                        if (!result6)
                        {
                            throw new Exception("机械手取料失败!");
                        }

                        sample.TubeStatus = 0;

                        return true;
                    }

                    throw new Exception($"从移栽移动{sample.Id }样品到拧盖2位失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽移动{sample.Id }样品到拧盖2位 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        public bool GetSampleToCapperThree(Sample sample, bool isBig, Func<ushort, CancellationTokenSource, bool> func, CancellationTokenSource cts)
        {
            _logger?.Info($"从移栽移动{sample.Id }样品到拧盖3位");

            try
            {
                lock (_lockObj)
                {
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInTransfer))
                    {
                        if (sample.TubeStatus == 0)
                        {
                            double[] poss1 = GetRightCoordinate(2 * sample.Id - 1, isBig);
                            //X C轴回零
                            var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, poss1[0], _stepMoveVel, cts).GetAwaiter().GetResult();
                            if (!result1)
                            {
                                throw new Exception("移栽X轴移动到目标位失败!");
                            }

                            var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, poss1[1], _stepMoveVel, cts).GetAwaiter().GetResult();
                            if (!result2)
                            {
                                throw new Exception("移栽旋转轴移动到目标位失败!");
                            }

                            var result3 = func.Invoke((ushort)(2 * sample.Id - 1), cts);
                            if (!result3)
                            {
                                throw new Exception("机械手取料失败!");
                            }
                            sample.TubeStatus = 1;
                        }

                        if (sample.TubeStatus == 1)
                        {
                            double[] poss2 = GetRightCoordinate(2 * sample.Id, isBig);
                            //X C轴回零
                            var result4 = _stepMotion.P2pMoveWithCheckDone(_axisX, poss2[0], _stepMoveVel, cts).GetAwaiter().GetResult();
                            if (!result4)
                            {
                                throw new Exception("移栽X轴移动到目标位失败!");
                            }

                            var result5 = _stepMotion.P2pMoveWithCheckDone(_axisC, poss2[1], _stepMoveVel, cts).GetAwaiter().GetResult();
                            if (!result5)
                            {
                                throw new Exception("移栽旋转轴移动到目标位失败!");
                            }

                            var result6 = func.Invoke((ushort)(2 * sample.Id), cts);
                            if (!result6)
                            {
                                throw new Exception("机械手取料失败!");
                            }

                            sample.TubeStatus = 0;

                            return true;
                        }

                        SampleStatusHelper.ResetBit(sample, SampleStatus.IsPurfyInTransfer);
                        SampleStatusHelper.SetBitOn(sample, SampleStatus.IsPurfyInCapper);
                    }


                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        return true;
                    }

                    throw new Exception($"从移栽移动{sample.Id }样品到拧盖3位失败,SampleStatus-{sample.Status}");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"从移栽移动{sample.Id }样品到拧盖3位 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }

        }






        #endregion

        #region Protected Methods



        /// <summary>
        /// 离心移栽移动到左侧上下料位(传入到搬运1)
        /// </summary>
        /// <param name="num"></param>
        /// <param name="isBig"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool TransferMoveLeftPutGetPos(ushort num, CancellationTokenSource cts)
        {
            double[] poss1 = GetLeftCoordinate(num, true);
            //X C轴回零
            var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, poss1[0], _stepMoveVel, cts).GetAwaiter().GetResult();
            if (!result1)
            {
                throw new Exception("移栽X轴移动到目标位失败!");
            }

            var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, poss1[1], _stepMoveVel, cts).GetAwaiter().GetResult();
            if (!result2)
            {
                throw new Exception("移栽旋转轴移动到目标位失败!");
            }
            return true;
        }

        /// <summary>
        /// 离心移栽移动左侧移液位，旋转角度固定
        /// </summary>
        /// <returns></returns>
        protected bool TransferMoveLeftPipettorPos(CancellationTokenSource cts)
        {
            double[] poss1 = GetLeftCoordinate(1, true);
            //X C轴回零
            var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, poss1[0], _stepMoveVel, cts).GetAwaiter().GetResult();
            if (!result1)
            {
                throw new Exception("移栽X轴移动到目标位失败!");
            }

            var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, poss1[1], _stepMoveVel, cts).GetAwaiter().GetResult();
            if (!result2)
            {
                throw new Exception("移栽旋转轴移动到目标位失败!");
            }
            return true;
        }


        /// <summary>
        /// 离心移栽移动到右侧上下料位（传入到搬运2）
        /// </summary>
        /// <param name="num"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected bool TransferMoveRightPutGetPos(ushort num, CancellationTokenSource cts)
        {
            double[] poss1 = GetRightCoordinate(num, false);
            //X C轴回零
            var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, poss1[0], _stepMoveVel, cts).GetAwaiter().GetResult();
            if (!result1)
            {
                throw new Exception("移栽X轴移动到目标位失败!");
            }

            var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, poss1[1], _stepMoveVel, cts).GetAwaiter().GetResult();
            if (!result2)
            {
                throw new Exception("移栽旋转轴移动到目标位失败!");
            }
            return true;
        }

        /// <summary>
        /// 离心移栽移动右侧侧移液位，旋转角度固定
        /// </summary>
        /// <returns></returns>
        protected bool TransferMoveRightPipettorPos(CancellationTokenSource cts)
        {
            double[] poss1 = GetRightCoordinate(1, false);
            //X C轴回零
            var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, poss1[0], _stepMoveVel, cts).GetAwaiter().GetResult();
            if (!result1)
            {
                throw new Exception("移栽X轴移动到目标位失败!");
            }

            var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, poss1[1], _stepMoveVel, cts).GetAwaiter().GetResult();
            if (!result2)
            {
                throw new Exception("移栽旋转轴移动到目标位失败!");
            }
            return true;
        }




        /// <summary>
        /// 离心机取料
        /// </summary>
        /// <param name="func">离心机旋转</param>
        /// <returns></returns>
        protected async Task<bool> GetTubeAtCentrifugal(ushort pos, Func<ushort, Task<bool>> func, CancellationTokenSource cts, byte clawCloseByte = 255, byte clawOpenByte = 0)
        {
            _logger.Debug($"GetTubeAtCentrifugal-{pos},clawOpenByte-{clawOpenByte}");
            try
            {
                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece())
                {
                    //打开手爪到指定位置
                    var ret = await OpenClaw(clawOpenByte).ConfigureAwait(false);
                    if (!ret)
                    {
                        throw new Exception("手爪打开出错");
                    }
                }
                else //手爪有抓取物件
                {
                    return true;
                }

                //离心机转到指定位置
                var result3 = func(pos).ConfigureAwait(false);

                //Y轴移动到离心机位
                Y_Cylinder_Put();

                //判断离心机旋转到指定位1 //打开离心机门
                if (!await result3)
                {
                    throw new Exception("离心机转到指定位出错");
                }

                //Z轴下降到取料位
                var result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZCentrifugalCoordinate(true), _sevorMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Z轴到取料位出错！");
                }

                //手爪夹紧
                result = await CloseClaw(clawCloseByte).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪夹紧出错");
                }

                return true;
            }
            catch (Exception ex)
            {
                await OpenClaw(clawOpenByte).ConfigureAwait(false);
                await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }
        }

        /// <summary>
        /// 离心机放料
        /// </summary>
        /// <param name="clawOpenByte">手爪打开位置</param>
        /// <param name="pos">离心机旋转位</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> PutTubeAtCentrifugal(ushort pos, Func<ushort, Task<bool>> func, CancellationTokenSource cts, byte clawOpenByte = 0)
        {
            _logger.Debug($"PutTubeAtCentrifugal-{pos},clawOpenByte-{clawOpenByte}");
            try
            {
                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);


                //如果手爪打开 判定放料完成
                if (await _claw.ClawGetchStatus(_clawId) == 1)
                {
                    return true;

                }

                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece())
                {
                    throw new Exception("手爪上无试管");
                }

                //离心机转到指定位置
                var result3 = func(pos).ConfigureAwait(false);

                //Y轴移动到离心机位
                Y_Cylinder_Put();

                //判断离心机旋转到指定位1 //打开离心机门
                if (!await result3)
                {
                    throw new Exception("离心机转到指定位出错");
                }

                //Z轴下降到放料位
                var result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZCentrifugalCoordinate(false), _sevorMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Z轴到放料位出错！");
                }

                //手爪打开
                result = await OpenClaw(clawOpenByte).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪打开出错！");
                }

                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                await OpenClaw(clawOpenByte).ConfigureAwait(false);
                await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }
        }

        /// <summary>
        /// 在移栽上取料
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="clawOpenByte"></param>
        /// <param name="cts"></param>
        /// <param name="clawCloseByte"></param>
        /// <returns></returns>
        protected async Task<bool> GetTubeAtTransfer(double[] pos, byte clawOpenByte, CancellationTokenSource cts, byte clawCloseByte = 255)
        {
            _logger.Debug($"GetTubeAtTransfer-{pos},clawOpenByte-{clawOpenByte}");
            try
            {
                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece())
                {
                    //打开手爪到指定位置
                    var ret = await OpenClaw(clawOpenByte).ConfigureAwait(false);
                    if (!ret)
                    {
                        throw new Exception("手爪打开出错");
                    }
                }
                else //手爪有抓取物件
                {
                    return true;
                }

                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);

                //X轴移动到位置1  //C轴旋转到大小试管位   //离心机旋转到指定位1 
                var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, pos[0], _stepMoveVel, cts).ConfigureAwait(false);
                var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, pos[1], _stepMoveVel, cts).ConfigureAwait(false);
                //Y气缸移动到位
                Y_Cylinder_Get();

                //Z轴下降抓取第一支试管
                if (!await result1 || !await result2)
                {
                    throw new Exception("离心移栽运动出错");
                }
                var result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZTransferCoordinate(true), _sevorMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Z轴运动到取料位出错");
                }

                //手爪夹紧
                result = await CloseClaw(clawCloseByte).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪夹紧出错");
                }

                return true;

            }
            catch (Exception ex)
            {
                await OpenClaw(clawOpenByte).ConfigureAwait(false);
                await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }

        }

        /// <summary>
        /// 在移栽上放料放料
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="clawOpenByte"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> PutTubeAtTransfer(double[] pos, byte clawOpenByte, CancellationTokenSource cts)
        {
            _logger.Debug($"PutTubeAtTransfer-{pos},clawOpenByte-{clawOpenByte}");
            try
            {
                //如果手爪打开 判定放料完成
                if (await _claw.ClawGetchStatus(_clawId) == 1)
                {
                    return true;

                }

                //判断手爪是否抓取物件 在指定打开位置
                if (!await ClawIsGetchPiece())
                {
                    throw new Exception("手爪上无试管");
                }

                //判断Z轴是否在原点
                await CheckAxisZInSafePos(cts).ConfigureAwait(false);


                //X轴移动到位置1  //C轴旋转到大小试管位   //离心机旋转到指定位1 
                var result1 = _stepMotion.P2pMoveWithCheckDone(_axisX, pos[0], _stepMoveVel, cts).ConfigureAwait(false);
                var result2 = _stepMotion.P2pMoveWithCheckDone(_axisC, pos[1], _stepMoveVel, cts).ConfigureAwait(false);
                //Y气缸移动到位
                Y_Cylinder_Get();

                //Z轴下降抓取第一支试管
                if (!await result1 || !await result2)
                {
                    throw new Exception("离心移栽运动出错");
                }
                var result = await _motion.P2pMoveWithCheckDone(_axisZ, GetZTransferCoordinate(false), _sevorMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Z轴运动到放料位出错");
                }

                //手爪松开
                result = await OpenClaw(clawOpenByte).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("手爪打开出错");
                }

                //判断Z轴是否在原点
                if (!await CheckAxisZInSafePos(cts))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                await OpenClaw(clawOpenByte).ConfigureAwait(false);
                await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, null).ConfigureAwait(false);
                if (cts?.IsCancellationRequested != false)
                {
                    return false;
                }
                throw ex;
            }
        }

        /// <summary>
        /// Y气缸取放料位
        /// </summary>
        protected void Y_Cylinder_Get(bool checkSensor = true)
        {
            _logger?.Debug($"Y_Cylinder_Get-{checkSensor}");
            var result = _io.WriteBit_DO(_y_Ctr, true);
            if (!result)
            {
                throw new Exception("Y_Cylinder_Get Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_y_WP);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new TimeoutException("Y气缸到取放料位超时");
                }
            } while (!result);
        }

        /// <summary>
        /// Y气缸离心机位
        /// </summary>
        protected void Y_Cylinder_Put(bool checkSensor = true)
        {
            _logger?.Debug($"Y_Cylinder_Put-{checkSensor}");
            var result = _io.WriteBit_DO(_y_Ctr, false);
            if (!result)
            {
                throw new Exception("Y_Cylinder_Put Err!");
            }

            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_y_HP);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new TimeoutException("Y气缸到离心机位超时");
                }
            } while (!result);
        }

        /// <summary>
        /// 检查Z轴是否在安全位置
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> CheckAxisZInSafePos(CancellationTokenSource cts)
        {
            if (!AxisIsInSafePos(_axisZ))
            {
                var result = await _motion.P2pMoveWithCheckDone(_axisZ, 0, _sevorMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception($"Z轴移动到安全位置失败!");
                }
            }
            return true;
        }

        /// <summary>
        /// 手爪是否抓取到物件
        /// </summary>
        /// <returns>true:手爪上有物件 false:手爪上无物件</returns>
        protected async Task<bool> ClawIsGetchPiece()
        {
            var status = await _claw.ClawGetchStatus(_clawId).ConfigureAwait(false);
            return status == 2;
        }

        /// <summary>
        /// 打开手爪到指定位置
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> OpenClaw(byte openPos)
        {
            _logger?.Debug($"OpenClaw-{openPos}");
            var result = await _claw.SendCommand(_clawId, openPos, 255, 255).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception("手爪打开失败！");
            }
            int status = 0;
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(10);
            do
            {
                status = _claw.ClawGetchStatus(_clawId).GetAwaiter().GetResult();
                if (status == 3)
                {
                    return true;
                }
                if (status == 1)
                {
                    throw new Exception("手爪打开受阻！");
                }
                Thread.Sleep(300);
                if (DateTime.Now > end)
                {
                    throw new TimeoutException("离心机手爪动作超时！");
                }

            } while (status == 0);

            throw new Exception($"手爪状态错误 err{status}");
        }

        /// <summary>
        /// 关闭手爪
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> CloseClaw(byte closePos)
        {
            _logger?.Debug($"CloseClaw-{closePos}");
            var result = await _claw.SendCommand(_clawId, closePos, 255, 255).ConfigureAwait(false);
            if (!result)
            {
                throw new Exception("手爪抓取物件失败！");
            }
            int status = 0;
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(10);
            do
            {
                status = _claw.ClawGetchStatus(_clawId).GetAwaiter().GetResult();
                if (status == 2)
                {
                    return true;
                }
                if (status == 3)
                {
                    throw new Exception("手爪未抓取到物料！");
                }
                Thread.Sleep(300);
                if (DateTime.Now > end)
                {
                    throw new TimeoutException("离心机手爪动作超时！");
                }

            } while (status == 0);
            throw new Exception($"手爪状态错误 err{status}");
        } 

        #endregion


        #region Private Methods


        /// <summary>
        /// 判断轴是否在安全位置
        /// </summary>
        /// <param name="axisNo"></param>
        /// <returns></returns>
        private bool AxisIsInSafePos(ushort axisNo)
        {
            var currentPos = _motion.GetCurrentPos(axisNo);
            return Math.Round(currentPos, 1) == 0;
        }

        /// <summary>
        /// 获取右侧坐标
        /// </summary>
        /// <param name="num">试管编号</param>
        /// <param name="isBig">是否是大管</param>
        /// <returns></returns>
        private double[] GetRightCoordinate(int num, bool isBig)
        {
            bool b = num % 2 == 0;
            if (isBig)
            {
                if (b)
                {
                    return new double[] { _posData.RightPos, _posData.CRightPos1 };
                }
                return new double[] { _posData.RightPos, _posData.CRightPos2 };
            }
            else
            {
                if (b)
                {
                    return new double[] { _posData.RightPos, _posData.CRightPos3 };
                }
                return new double[] { _posData.RightPos, _posData.CRightPos4 };
            }
        }

        /// <summary>
        /// 获取左侧坐标
        /// </summary>
        /// <param name="num">试管编号</param>
        /// <param name="isBig">是否是大管</param>
        /// <returns></returns>
        private double[] GetLeftCoordinate(int num, bool isBig)
        {
            bool b = num % 2 == 0;
            if (isBig)
            {
                if (!b)
                {
                    return new double[] { _posData.LeftPos, _posData.CLeftPutPos1 };
                }
                return new double[] { _posData.LeftPos, _posData.CLeftPutPos2 };
            }
            else
            {
                if (!b)
                {
                    return new double[] { _posData.LeftPos, _posData.CLeftPutPos3 };
                }
                return new double[] { _posData.LeftPos, _posData.CLeftPutPos4 };
            }
        }

        /// <summary>
        /// 获取中间取料点位
        /// </summary>
        /// <param name="num">试管编号</param>
        /// <param name="isBig">是否是大管</param>
        /// <returns></returns>
        private double[] GetCenterCoordinate(ushort num, bool isBig)
        {
            bool b = num % 2 == 0;
            if (isBig)
            {
                if (!b)
                {
                    return new double[] { _posData.XCentPos1, _posData.CCentPos1 };
                }
                return new double[] { _posData.XCentPos2, _posData.CCentPos1 };
            }
            else
            {
                if (!b)
                {
                    return new double[] { _posData.XCentPos1, _posData.CCentPos2 };
                }
                return new double[] { _posData.XCentPos2, _posData.CCentPos2 };
            }

        }

        /// <summary>
        /// 获取Z取试管坐标
        /// </summary>
        /// <param name="isGet">是否获取取料位/放料位</param>
        /// <returns></returns>
        private double GetZTransferCoordinate(bool isGet = true)
        {
            if (!isGet)
            {
                return _posData.ZGetPos - 3;
            }
            return _posData.ZGetPos;
        }

        /// <summary>
        /// 获取Z离心机放料坐标
        /// </summary>
        /// <param name="isGet">是否获取取料位/放料位</param>
        /// <returns></returns>
        private double GetZCentrifugalCoordinate(bool isGet = true)
        {
            if (!isGet)
            {
                return _posData.ZPutPos - 3;
            }
            return _posData.ZPutPos;
        }

        #endregion




    }
}
