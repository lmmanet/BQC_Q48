using BQJX.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class VibrationOne : VibrationBase , IVibrationOne
    {
        private static ILogger logger = new MyLogger(typeof(VibrationOne));

        private readonly static object _lockObj = new object();

        private readonly IVortex _vortex;

        //程序运行参数
        List<Sample> _vibrationOnevortexList = new List<Sample>();  //振荡涡旋列表
        Dictionary<Sample, Action<Sample, CancellationTokenSource>> _sampleActionDic = new Dictionary<Sample, Action<Sample, CancellationTokenSource>>(); //回调列表
        Task _vibrationOnevortexTask; //振荡1 涡旋




        #region Private Members

        private readonly ICarrierOne _carrier;

        #endregion

        #region Construtors

        public VibrationOne(IEtherCATMotion motion, IIoDevice io, IGlobalStatus globalStauts,ICarrierOne carrier, IVortex vortex) : base(motion, io, globalStauts, logger)
        {
            _axisNo = 4;
            _holding = 14;
            _holdingOpenSensor = 16; //原位
            _holdingCloseSensor = 15; //到位

            this._carrier = carrier;
            this._vortex = vortex;
        }

        #endregion

        public bool StartVibration(Sample sample, CancellationTokenSource cts)
        {
            int step = sample.VibrationAndVortexStep - 1;
            ushort sampleId = sample.Id;

            double vel = sample.TechParams.VibrationOneVel[step] / 60;
            int time = sample.TechParams.VibrationOneTime[step];

            int bitIn = 0;
            switch (sample.VibrationAndVortexStep)
            {
                case 1:
                    bitIn = 2;
                    break;
                case 2:
                    bitIn = 7;
                    break;
                case 3:
                    bitIn = 11;
                    break;
                case 4:
                    bitIn = 19;
                    break;
                default:
                    break;
            }

            if (!TechStatusHelper.BitIsOn(sample.TechParams, bitIn))
            {
                return true;
            }


            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"样品{sampleId}开始振荡-{time}s-{vel}rpm");

                    //振荡回零
                    var result = GoHome(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("振荡回零失败!");
                    }

                    //搬运
                    result = _carrier.GetSampleToVibration(sample, cts);
                    if (!result)
                    {
                        throw new Exception("搬运样品到振荡失败!");
                    }

                    //开始振荡
                    result = base.StartVibration(time, vel, cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("样品振荡失败!");
                    }

                    //搬运下料     //判断是哪次振荡   下料到试管架或者冰浴
                    if (step == 2 && !TechStatusHelper.BitIsOn(sample.TechParams,TechStatus.ExtractVortex2))
                    {
                        TechStatusHelper.ResetBit(sample.TechParams, TechStatus.ExtractVibration2);
                        result = _carrier.GetSampleFromVibrationToCold(sample, cts);
                        if (!result)
                        {
                            throw new Exception("搬运样品到冰浴失败!");
                        }
                    }
                    else
                    {
                        result = _carrier.GetSampleFromVibrationToMaterial(sample, cts);
                        if (!result)
                        {
                            throw new Exception("搬运样品到试管架失败!");
                        }
                    }
                   
                    //完成

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"样品{sampleId}开始振荡-{time}s-{vel}rpm 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }


        /// <summary>
        /// 振荡涡旋提取
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="actionCallBack">振荡涡旋执行完成后回调</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public Task StartVibrationAndVortex(Sample sample, Action<Sample, CancellationTokenSource> actionCallBack, CancellationTokenSource cts)
        {
            //判断去重
            var ret = _vibrationOnevortexList.Contains(sample);
            if (!ret)
            {
                _vibrationOnevortexList.Add(sample);
                _vibrationOnevortexList.Distinct();
                //保存回调列表
                _sampleActionDic.Add(sample, actionCallBack);
                _sampleActionDic.Distinct();
            }


            if (_vibrationOnevortexTask != null)
            {
                if (!_vibrationOnevortexTask.IsCompleted)
                {
                    return _vibrationOnevortexTask;
                }
            }

            _vibrationOnevortexTask = Task.Run(() =>
            {
                while (cts?.IsCancellationRequested != true && _vibrationOnevortexList.Count >0)
                {
                    var itemSample = _vibrationOnevortexList[0];
                    int techStatus = 0;
                    switch (itemSample.VibrationAndVortexStep)
                    {
                        case 1:
                            techStatus = 2;
                            break;
                        case 2:
                            techStatus = 7;
                            break;
                        case 3:
                            techStatus = 11;
                            break;
                        case 4:
                            techStatus = 19;
                            break;
                        default:
                            throw new Exception("振荡步骤错误!");
                    }
                    try
                    {
                        //是否振荡  跳过
                        if (TechStatusHelper.BitIsOn(itemSample.TechParams, (TechStatus)techStatus))
                        {
                            if (cts.IsCancellationRequested != true)
                            {
                                var result = StartVibration(itemSample, cts);
                                if (!result)
                                {
                                    throw new Exception("StartVibration err");
                                }
                                TechStatusHelper.ResetBit(itemSample.TechParams, (TechStatus)techStatus);
                            }
                            else
                            {
                                throw new TaskCanceledException("程序停止");
                            }

                        }

                        //判断是否涡旋
                        if (TechStatusHelper.BitIsOn(itemSample.TechParams, (TechStatus)(techStatus + 1)))
                        {
                            if (cts.IsCancellationRequested != true)
                            {
                                var result = _vortex.StartVortex(itemSample, cts);
                                if (!result)
                                {
                                    throw new Exception("StartVortex err");
                                }
                                TechStatusHelper.ResetBit(itemSample.TechParams, (TechStatus)(techStatus + 1));
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

                    _sampleActionDic[itemSample]?.Invoke(itemSample,cts);


                    _vibrationOnevortexList.Remove(itemSample);
                    _sampleActionDic.Remove(itemSample);

                    if (_vibrationOnevortexList.Count == 0)
                    {
                        return;
                    }
                }
            });

            return _vibrationOnevortexTask;

        }


    }
}
