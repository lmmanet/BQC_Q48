using BQJX.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Ioc;
using Q_Platform.Common;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class VibrationOne : VibrationBase, IVibrationOne
    {
        private static ILogger logger = new MyLogger(typeof(VibrationOne));

        private readonly static object _lockObj = new object();

        private readonly IVortex _vortex;

        Task _vibrationOnevortexTask; //振荡1 涡旋

        #region Private Members

        private readonly ICarrierOne _carrier;

        #endregion

        #region Construtors

        public VibrationOne(IEtherCATMotion motion, IIoDevice io, IGlobalStatus globalStauts, ICarrierOne carrier, IVortex vortex) : base(motion, io, globalStauts, logger)
        {
            _axisNo = 4;
            _holding = 14;
            _holdingOpenSensor = 16; //原位
            _holdingCloseSensor = 15; //到位

            this._carrier = carrier;
            this._vortex = vortex;
        }

        #endregion

    


        /// <summary>
        /// 振荡涡旋提取
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="actionCallBack">振荡涡旋执行完成后回调</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public Task StartVibrationAndVortex(Sample sample, string methodAction, CancellationTokenSource cts)
        {
            //判断去重
            GlobalCache.AddVibrationOneVortexKeyValue(sample, methodAction);

            if (_vibrationOnevortexTask != null)
            {
                if (!_vibrationOnevortexTask.IsCompleted)
                {
                    return _vibrationOnevortexTask;
                }
            }

            _vibrationOnevortexTask = Task.Run(() =>
            {
                while (!_globalStauts.IsStopped && GlobalCache.GetVibrationOneVortexKeyValueCount() > 0)
                {
                    KeyValuePair<Sample, string> item = GlobalCache.GetVibrationOneVortexKeyValues(0);
                    var itemSample = item.Key;

                    try
                    {
                        //是否振荡  跳过    加水振荡
                        if (itemSample.TechParams.TechStep == 2)
                        {
                            if (TechStatusHelper.BitIsOn(itemSample.TechParams,TechStatus.Vibration))
                            {
                                if (!_globalStauts.IsStopped)
                                {
                                    var result = StartVibration(itemSample,0, cts);
                                    if (!result)
                                    {
                                        throw new Exception("StartVibration err");
                                    }

                                    TechStatusHelper.ResetBit(itemSample.TechParams, TechStatus.Vibration);
                                }
                                else
                                {
                                    throw new TaskCanceledException("程序停止");
                                }

                            }

                            //判断是否涡旋
                            if (TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.Vortex))
                            {
                                if (!_globalStauts.IsStopped)
                                {
                                    var result = _vortex.StartVortex(itemSample,0, cts);
                                    if (!result)
                                    {
                                        throw new Exception("StartVortex err");
                                    }
                                    TechStatusHelper.ResetBit(itemSample.TechParams, TechStatus.Vortex);
                                }
                                else
                                {
                                    throw new TaskCanceledException("程序停止");
                                }

                            }
                           
                        }
                        //是否振荡  跳过
                        if (itemSample.TechParams.TechStep == 4)
                        {
                            if (TechStatusHelper.BitIsOn(itemSample.TechParams,TechStatus.ExtractVibration1))
                            {
                                if (!_globalStauts.IsStopped)
                                {
                                    var result = StartVibration(itemSample,1, cts);
                                    if (!result)
                                    {
                                        throw new Exception("StartVibration err");
                                    }

                                    TechStatusHelper.ResetBit(itemSample.TechParams, TechStatus.ExtractVibration1);
                                }
                                else
                                {
                                    throw new TaskCanceledException("程序停止");
                                }

                            }

                            //判断是否涡旋
                            if (TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVortex1))
                            {
                                if (!_globalStauts.IsStopped)
                                {
                                    var result = _vortex.StartVortex(itemSample,1, cts);
                                    if (!result)
                                    {
                                        throw new Exception("StartVortex err");
                                    }
                                    TechStatusHelper.ResetBit(itemSample.TechParams, TechStatus.ExtractVortex1);
                                }
                                else
                                {
                                    throw new TaskCanceledException("程序停止");
                                }

                            }

                            sample.TechParams.TechStep = 5;
                        }
                        //是否振荡  跳过
                        if (itemSample.TechParams.TechStep == 6)
                        {
                            if (TechStatusHelper.BitIsOn(itemSample.TechParams,TechStatus.ExtractVibration2))
                            {
                                if (!_globalStauts.IsStopped)
                                {
                                    var result = StartVibration(itemSample,2, cts);
                                    if (!result)
                                    {
                                        throw new Exception("StartVibration err");
                                    }

                                    TechStatusHelper.ResetBit(itemSample.TechParams, TechStatus.ExtractVibration2);
                                }
                                else
                                {
                                    throw new TaskCanceledException("程序停止");
                                }

                            }

                            //判断是否涡旋
                            if (TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVortex2))
                            {
                                if (!_globalStauts.IsStopped)
                                {
                                    var result = _vortex.StartVortex(itemSample,2, cts);
                                    if (!result)
                                    {
                                        throw new Exception("StartVortex err");
                                    }
                                    TechStatusHelper.ResetBit(itemSample.TechParams, TechStatus.ExtractVortex2);
                                }
                                else
                                {
                                    throw new TaskCanceledException("程序停止");
                                }

                            }

                            sample.TechParams.TechStep = 10;
                        }
                        //是否振荡  跳过
                        if (itemSample.TechParams.TechStep == 22)
                        {
                            if (TechStatusHelper.BitIsOn(itemSample.TechParams,TechStatus.ExtractVibration3))
                            {
                                if (!_globalStauts.IsStopped)
                                {
                                    var result = StartVibration(itemSample,3, cts);
                                    if (!result)
                                    {
                                        throw new Exception("StartVibration err");
                                    }

                                    TechStatusHelper.ResetBit(itemSample.TechParams, TechStatus.ExtractVibration3);
                                }
                                else
                                {
                                    throw new TaskCanceledException("程序停止");
                                }

                            }

                            //判断是否涡旋
                            if (TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVortex3))
                            {
                                if (!_globalStauts.IsStopped)
                                {
                                    var result = _vortex.StartVortex(itemSample,3, cts);
                                    if (!result)
                                    {
                                        throw new Exception("StartVortex err");
                                    }
                                    TechStatusHelper.ResetBit(itemSample.TechParams, TechStatus.ExtractVortex3);
                                }
                                else
                                {
                                    throw new TaskCanceledException("程序停止");
                                }

                            }

                            sample.TechParams.TechStep = 30;
                        }




                    }
                    catch (Exception ex)
                    {
                        _globalStauts.PauseProgram();

                        _logger?.Error(ex.Message);
                        return;
                    }

                    //成功执行完成  == 》 调用下一步流程  加入上一步工作列表或者加入下一步列表
                    MethodHelper.ExcuteMethod(item.Value, itemSample, cts);

                    GlobalCache.RemoveVibrationOneVortexKeyValue(itemSample,item.Value);

                }
            });

            return _vibrationOnevortexTask;

        }














        #region Private Methods

        private bool StartVibration(Sample sample, int step, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;

            double vel = sample.TechParams.VibrationOneVel[step] / 60;
            int time = sample.TechParams.VibrationOneTime[step];

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
                    if (step == 2 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex2))
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



        #endregion


    }
}
