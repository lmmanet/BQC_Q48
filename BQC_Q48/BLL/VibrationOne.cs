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

        Task _vibrationTask; //振荡1 

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


        public void AddSampleToVibrationList(Sample sample, string methodAction, CancellationTokenSource cts)
        {
            //判断去重
            if (sample != null)
            {
                sample.ActionCallBack = methodAction;
                //不存在振荡涡旋工艺  直接进行下一步
                if (!TechStatusHelper.BitIsOn(sample.TechParams,TechStatus.ExtractVibration2)
                    && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex2)
                    && sample.MainStep == 3)
                {
                    sample.MainStep++;
                    MethodHelper.ExcuteMethod(sample, cts);
                    return;
                }

                if (sample.MainStep == 9 && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractSupernate2))
                {
                    var dic = GlobalCache.Instance.VibrationOneDicPolish;
                    if (!dic.Contains(sample))
                    {
                        dic.Add(sample);
                    }
                }
                else
                {
                    var dic = GlobalCache.Instance.VibrationOneDic;
                    if (!dic.Contains(sample))
                    {
                        dic.Add(sample);
                    }
                }
            }
        }

        /// <summary>
        /// 振荡涡旋提取
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public Task StartVibrationAndVortex(CancellationTokenSource cts)
        {
            if (_vibrationOnevortexTask != null)
            {
                if (!_vibrationOnevortexTask.IsCompleted)
                {
                    return _vibrationOnevortexTask;
                }
            }

            _vibrationOnevortexTask = Task.Run(() =>
            {
                while (!_globalStatus.IsStopped)
                {
                    var dic1 = GlobalCache.Instance.VibrationOneDic;
                    var dic2 = GlobalCache.Instance.VibrationOneDicPolish;

                    if (dic1.Count <=0 && dic2.Count<=0)
                    {
                        break;
                    }

                    lock (_lockObj)
                    {
                        Sample itemSample = null;
                        if (dic2.Count > 0)
                        {
                            itemSample = dic2[0];
                        }
                        else if(dic1.Count > 0)
                        {
                            itemSample = dic1[0];
                        }

                        var runSample = GlobalCache.Instance.VibRunningSample;
                        if (runSample != null)
                        {
                            itemSample = runSample;
                        }
                        else
                        {
                            runSample = itemSample;
                        }

                        try
                        {
                            //加水振荡 涡旋
                            if (itemSample.MainStep == 1 && !_globalStatus.IsStopped)
                            {
                                //振荡 
                                if (itemSample.SubStep == 4)
                                {
                                    // 跳过
                                    if (!TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.Vibration))
                                    {
                                        itemSample.SubStep++;
                                    }

                                    if (TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.Vibration) && !_globalStatus.IsStopped)
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
                                    if (!_globalStatus.IsStopped && TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.Vortex))
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
                            if (itemSample.MainStep == 2 && !_globalStatus.IsStopped)
                            {
                                if (itemSample.SubStep == 4)
                                {
                                    if (!TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVibration1))
                                    {
                                        itemSample.SubStep++;
                                    }

                                    if (!_globalStatus.IsStopped && TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVibration1))
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

                                    if (!_globalStatus.IsStopped && TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVortex1))
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
                            if (itemSample.MainStep == 3 && !_globalStatus.IsStopped)
                            {
                                if (itemSample.SubStep == 4)
                                {
                                    if (!TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVibration2))
                                    {
                                        itemSample.SubStep++;
                                    }

                                    if (!_globalStatus.IsStopped && TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVibration2))
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

                                    if (!_globalStatus.IsStopped && TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVortex2))
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

                            //萃取振荡 涡旋
                            if (itemSample.MainStep == 9 && !_globalStatus.IsStopped)
                            {
                                if (itemSample.SubStep == 0 && !_globalStatus.IsStopped)
                                {
                                    //从拧盖2搬运萃取管到振荡
                                    if (!TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVibration3))
                                    {
                                        itemSample.SubStep++;
                                    }

                                    if (!_globalStatus.IsStopped && TechStatusHelper.BitIsOn(itemSample.TechParams, TechStatus.ExtractVibration3))
                                    {
                                        var result = StartVibration(itemSample, 3, cts);
                                        if (!result)
                                        {
                                            throw new Exception("StartVibration err");
                                        }

                                        itemSample.SubStep++;
                                    }

                                }

                                //判断是否涡旋  在涡旋内部判断
                                if (itemSample.SubStep == 1 && !_globalStatus.IsStopped)
                                {
                                    if (!_globalStatus.IsStopped)
                                    {
                                        var result = _vortex.StartVortex(itemSample, 3, cts);
                                        if (!result)
                                        {
                                            throw new Exception("StartVortex err");
                                        }
                                        itemSample.SubStep++;
                                    }
                                }

                                if (itemSample.SubStep == 2)
                                {
                                    itemSample.SubStep = 6;
                                }
                            }

                            //程序完成 
                            if (itemSample.SubStep == 6)
                            {
                                //回湿执行完成信号
                                if (itemSample.MainStep != 1)
                                {
                                    itemSample.MainStep++;
                                    itemSample.SubStep = 0;
                                }
                              
                                //成功执行完成  == 》 调用下一步流程  加入上一步工作列表或者加入下一步列表
                                MethodHelper.ExcuteMethod(itemSample, cts);

                                //移除当前项
                                if (itemSample.MainStep == 10)
                                {
                                    dic2.Remove(itemSample);
                                }
                                else
                                {
                                    dic1.Remove(itemSample);
                                }

                                runSample = null;
                            }

                        }
                        catch (Exception ex)
                        {
                            _globalStatus.PauseProgram();
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
                    if (sample.MainStep == 9 && !_globalStatus.IsStopped)
                    {
                        if (sample.SubStep == 0 && !_globalStatus.IsStopped)
                        {
                            //从拧盖2搬运萃取管到振荡
                            if (!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration3))
                            {
                                sample.SubStep++;
                            }

                            if (!_globalStatus.IsStopped && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration3))
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
                        if (sample.SubStep == 1 && !_globalStatus.IsStopped)
                        {
                            if (!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex3))
                            {
                                sample.SubStep++;
                            }

                            if (!_globalStatus.IsStopped && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex3))
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


        //============================================================振荡分离出来单独用=========================================================================//
        public void AddSampleToVibrationList(Sample sample,CancellationTokenSource cts)
        {
            //判断去重
            if (sample != null)
            {
                //不存在加水振荡工艺  直接跳过  无需进入列表  == >  进入回湿程序
                if (sample.MainStep == 1 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Vibration))
                {
                    sample.SubStep++;
                    _vortex.AddSampleToVortexList(sample, cts);
                    return;
                }
                //不存在加溶剂振荡工艺 直接跳过  无需进入列表
                if (sample.MainStep == 2 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration1))
                {
                    sample.SubStep++;
                    _vortex.AddSampleToVortexList(sample, cts);
                    return;
                }
                //不存在加溶剂振荡工艺 直接跳过  无需进入列表
                if (sample.MainStep == 3 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration2))
                {
                    sample.SubStep++;
                    _vortex.AddSampleToVortexList(sample, cts);
                    return;
                }
                //不存在振荡工艺 直接跳过  无需进入列表
                if (sample.MainStep == 9 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration3))
                {
                    sample.SubStep++; 
                    _vortex.AddSampleToVortexList(sample, cts);
                    return;
                }

                //加入到列表
                var list = GlobalCache.Instance.VibrationList;
                if (!list.Contains(sample))
                {
                    list.Add(sample);
                }
            }
        }

        public Task StartVibration(CancellationTokenSource cts)
        {
            if (_vibrationTask != null)
            {
                if (!_vibrationTask.IsCompleted)
                {
                    return _vibrationTask;
                }
            }

            _vibrationTask = Task.Run(() =>
            {
                while (!_globalStatus.IsStopped)
                {
                    var list = GlobalCache.Instance.VibrationList;

                    if (list == null || list.Count <= 0)
                    {
                        break;
                    }

                    lock (_lockObj)
                    { 
                        //找出优先级高的样品
                        var workSample = FindHighPrioritySample(list);

                        try
                        {
                            //振荡 
                            if (workSample.MainStep == 1 && !_globalStatus.IsStopped)
                            {
                                //振荡 
                                if (workSample.SubStep == 4 && !_globalStatus.IsStopped)
                                {
                                    var result = StartVibration(workSample, cts);
                                    if (!result)
                                    {
                                        throw new Exception("StartVibration err");
                                    }
                                    list.Remove(workSample);
                                    GlobalCache.Instance.VortexCurrentSample = null;
                                    workSample.SubStep++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _globalStatus.PauseProgram();
                            _logger?.Warn(ex.Message);
                            return;
                        }
                    }
                    Thread.Sleep(1000);
                }
            });

            return _vibrationTask;
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
                    if (step == 3)  //萃取管振荡
                    {
                        result = _carrier.GetPolishFromMaterialToVibration(sample, cts);
                        if (!result)
                        {
                            throw new Exception("搬运样品到振荡失败!");
                        }
                    }
                    else
                    {
                        result = _carrier.GetSampleToVibration(sample, cts);
                        if (!result)
                        {
                            throw new Exception("搬运样品到振荡失败!");
                        }
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
                    else if (step == 3)
                    {
                        return true;
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
                _logger?.Warn(ex.Message);
                return false;
            }
        }




        #endregion

        //未完成
        private bool StartVibration(Sample sample,CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;
            int step = 0;
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
                    result = GetSampleFromMaterialToVibration(sample, cts);
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

                    //搬运下料
                    if (sample.MainStep == 9)
                    {
                        result = GetSampleFromVibrationToMaterial(sample, 3, cts);
                        if (!result)
                        {
                            throw new Exception("搬运样品到冰浴失败!");
                        }
                    }
                    else
                    {
                        result = GetSampleFromVibrationToMaterial(sample, 2, cts);
                        if (!result)
                        {
                            throw new Exception("搬运样品到冰浴失败!");
                        }
                    }
                   

                    //完成

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger?.Warn(ex.Message);
                return false;
            }
        }


        /// <summary>
        /// 找出列表优先级高的样品
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private Sample FindHighPrioritySample(List<Sample> list)
        {
            var sample = list[0];

            //判断当前样品是否完成
            if (GlobalCache.Instance.VibrationCurrentSample != null)
            {
                sample = GlobalCache.Instance.VibrationCurrentSample;
            }
            //萃取管振荡
            else if (list.Exists(s => s.MainStep == 9))
            {
                sample = list.Find(s => s.MainStep == 9);
            }
            //加盐振荡
            else if (list.Exists(s => s.MainStep == 3))
            {
                sample = list.Find(s => s.MainStep == 3);
            }
            //加液振荡
            else if (list.Exists(s => s.MainStep == 2))
            {
                sample = list.Find(s => s.MainStep == 2);
            }
            //加水振荡
            else if (list.Exists(s => s.MainStep == 1))
            {
                sample = list.Find(s => s.MainStep == 1);
            }

            GlobalCache.Instance.VibrationCurrentSample = sample;

            return sample;
        }


        /// <summary>
        /// 从振荡搬运试管到试管架1 试管架2  冰浴
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">1;到试管架1 2:到试管架2 3:样品管到冰浴 4;萃取管到冰浴</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetSampleFromVibrationToMaterial(Sample sample, int var, CancellationTokenSource cts)
        {
            if (var == 1)
            {
                //样品管从振荡到试管架
                return _carrier.GetSampleFromVibrationToMaterial(sample, cts);
            }
            else if (var == 2)
            {
                //从振荡搬运萃取管到试管架
                return _carrier.GetPolishFromVibrationToMaterial(sample, cts);
            }
            else if (var == 3)
            {
                //从振荡搬运试管到冰浴
                return _carrier.GetSampleFromVibrationToCold(sample, cts);
            }
            else if (var == 4)
            {
                //从振荡搬运萃取管到冰浴
                return _carrier.GetPolishFromVibrationToCold(sample, cts);
            }
            return false;
        }

        /// <summary>
        /// 从试管架搬运试管到振荡
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetSampleFromMaterialToVibration(Sample sample, CancellationTokenSource cts)
        {
            //从试管架2搬运萃取管到振荡
            if (sample.MainStep == 9)
            {
                return _carrier.GetPolishFromMaterialToVibration(sample, cts);
            }
            //从试管架1搬运试管到振荡
            else
            {
                return _carrier.GetSampleToVibration(sample, cts);
            }


        }
    }
}
