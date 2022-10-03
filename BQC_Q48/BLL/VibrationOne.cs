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
                    lock (_lockObj)
                    {
                        KeyValuePair<Sample, string> item = GlobalCache.GetVibrationOneVortexKeyValues(0);
                        var itemSample = item.Key;

                        try
                        {
                            //加水振荡 涡旋
                            if (itemSample.MainStep == 1 && !_globalStauts.IsStopped)
                            {
                                //振荡 
                                if (itemSample.SubStep == 4)
                                {
                                    // 跳过
                                    if (!TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.Vibration))
                                    {
                                        itemSample.SubStep++;
                                    }

                                    if (TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.Vibration) && !_globalStauts.IsStopped)
                                    {
                                        var result = StartVibration(itemSample, 0, cts);
                                        if (!result)
                                        {
                                            throw new Exception("StartVibration err");
                                        }

                                        itemSample.SubStep++;
                                    }

                                }

                                //涡旋
                                if (itemSample.SubStep == 5)
                                {
                                    // 跳过
                                    if (!TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.Vortex))
                                    {
                                        itemSample.SubStep++;
                                    }
                                    //是否涡旋
                                    if (!_globalStauts.IsStopped && TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.Vortex))
                                    {
                                        var result = _vortex.StartVortex(itemSample, 0, cts);
                                        if (!result)
                                        {
                                            throw new Exception("StartVortex err");
                                        }
                                        itemSample.SubStep++;
                                    }

                                }

                            }

                            //加溶剂振荡 涡旋
                            if (itemSample.MainStep == 2 && !_globalStauts.IsStopped)
                            {
                                if (itemSample.SubStep == 4)
                                {
                                    if (!TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVibration1))
                                    {
                                        itemSample.SubStep++;
                                    }

                                    if (!_globalStauts.IsStopped && TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVibration1))
                                    {
                                        var result = StartVibration(itemSample, 1, cts);
                                        if (!result)
                                        {
                                            throw new Exception("StartVibration err");
                                        }
                                        itemSample.SubStep++;
                                    }
                                }

                                //判断是否涡旋
                                if (itemSample.SubStep == 5)
                                {
                                    if (!TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVortex1))
                                    {
                                        itemSample.SubStep++;
                                    }

                                    if (!_globalStauts.IsStopped && TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVortex1))
                                    {
                                        var result = _vortex.StartVortex(itemSample, 1, cts);
                                        if (!result)
                                        {
                                            throw new Exception("StartVortex err");
                                        }
                                        itemSample.SubStep++;
                                    }

                                }

                            }

                            //加盐振荡 涡旋
                            if (itemSample.MainStep == 3 && !_globalStauts.IsStopped)
                            {
                                if (itemSample.SubStep == 4)
                                {
                                    if (!TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVibration2))
                                    {
                                        itemSample.SubStep++;
                                    }

                                    if (!_globalStauts.IsStopped && TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVibration2))
                                    {
                                        var result = StartVibration(itemSample, 2, cts);
                                        if (!result)
                                        {
                                            throw new Exception("StartVibration err");
                                        }

                                        itemSample.SubStep++;
                                    }

                                }

                                //判断是否涡旋
                                if (itemSample.SubStep == 5)
                                {
                                    if (!TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVortex2))
                                    {
                                        itemSample.SubStep++;
                                    }

                                    if (!_globalStauts.IsStopped && TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVortex2))
                                    {
                                        var result = _vortex.StartVortex(itemSample, 2, cts);
                                        if (!result)
                                        {
                                            throw new Exception("StartVortex err");
                                        }
                                        itemSample.SubStep++;
                                    }
                                }
                            }

                            //程序完成 
                            if (itemSample.MainStep != 1 && itemSample.SubStep == 6)
                            {
                                itemSample.MainStep++;
                                itemSample.SubStep = 0;
                            }


                            //成功执行完成  == 》 调用下一步流程  加入上一步工作列表或者加入下一步列表
                            MethodHelper.ExcuteMethod(item.Value, itemSample, cts);

                            //移除当前项
                            GlobalCache.RemoveVibrationOneVortexKeyValue(itemSample, item.Value);

                        }
                        catch (Exception ex)
                        {
                            _globalStauts.PauseProgram();

                            _logger?.Warn(ex.Message);
                            return;
                        }
                    }
                    Thread.Sleep(1000);
                }
            });

            return _vibrationOnevortexTask;

        }


        /// <summary>
        /// 萃取管振荡涡旋
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool StartPolishVibrationAndVortex(Sample sample,CancellationTokenSource cts)
        {
            try
            {
                lock (_lockObj)
                {
                    //萃取振荡 涡旋
                    if (sample.MainStep == 9 && !_globalStauts.IsStopped)
                    {
                        if (sample.SubStep == 0 && !_globalStauts.IsStopped)
                        {
                            //从拧盖2搬运萃取管到振荡
                            if (!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration3))
                            {
                                sample.SubStep++;
                            }

                            if (!_globalStauts.IsStopped && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration3))
                            {
                                var result = StartVibration(sample, 3, cts);
                                if (!result)
                                {
                                    throw new Exception("StartVibration err");
                                }

                                sample.SubStep++;
                            }

                        }

                        //判断是否涡旋
                        if (sample.SubStep == 1 && !_globalStauts.IsStopped)
                        {
                            if (!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex3))
                            {
                                sample.SubStep++;
                            }

                            if (!_globalStauts.IsStopped && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex3))
                            {
                                var result = _vortex.StartVortex(sample, 3, cts);
                                if (!result)
                                {
                                    throw new Exception("StartVortex err");
                                }
                                sample.SubStep++;
                            }
                        }

                        if (sample.SubStep == 2)
                        {
                            sample.SubStep = 0;
                            return true;
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
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
