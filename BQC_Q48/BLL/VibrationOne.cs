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
        #region Private Members

        private static ILogger logger = new MyLogger(typeof(VibrationOne));

        private readonly static object _lockObj = new object();

        private readonly IVortex _vortex;

        Task _vibrationTask; //振荡1  

        #endregion

        #region Private Members

        private readonly ICarrierOne _carrier;

        #endregion

        #region Properties

        public bool IsVibrationTaskDone
        {
            get
            {
                if (_vibrationTask != null)
                {
                    return _vibrationTask.IsCompleted;
                }
                else
                {
                    return true;
                }
            }
        }


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

        #region Public Methods

        public void AddSampleToVibrationList(Sample sample, IGlobalStatus gs)
        {
            //判断去重
            if (sample != null)
            {
                //不存在加水振荡工艺  直接跳过  无需进入列表  == >  进入回湿程序
                if (sample.MainStep == 1 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Vibration))
                {
                    sample.SubStep = 9;
                    _vortex.AddSampleToVortexList(sample, gs);
                    _vortex.StartVortex(gs);
                    return;
                }
                //不存在加溶剂振荡工艺 直接跳过  无需进入列表
                if (sample.MainStep == 2 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration1))
                {
                    sample.SubStep = 9;
                    _vortex.AddSampleToVortexList(sample, gs);
                    _vortex.StartVortex(gs);
                    return;
                }
                //不存在加溶剂振荡工艺 直接跳过  无需进入列表
                if (sample.MainStep == 3 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration2))
                {
                    sample.SubStep = 9;
                    _vortex.AddSampleToVortexList(sample, gs);
                    _vortex.StartVortex(gs);
                    return;
                }
                //不存在振荡工艺 直接跳过  无需进入列表
                if (sample.MainStep == 9 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration3))
                {
                    sample.SubStep = 9;
                    _vortex.AddSampleToVortexList(sample, gs);
                    _vortex.StartVortex(gs);
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

        public Task StartVibration(IGlobalStatus gs)
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
                            if (workSample.SubStep >= 4 && workSample.SubStep < 8 &&!_globalStatus.IsStopped)
                            {
                                var result = StartVibration(workSample, gs);
                                if (!result)
                                {
                                    throw new Exception("StartVibration err");
                                }
                                workSample.SubStep++; //第9步

                                _vortex.AddSampleToVortexList(workSample, gs);
                                _vortex.StartVortex(gs);

                                list.Remove(workSample);
                                GlobalCache.Instance.VibrationCurrentSample = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (_globalStatus.IsStopped || _globalStatus.IsPause)
                            {
                              _logger?.Warn("程序暂停") ;
                            }
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


        #endregion

        #region Private Methods

        private bool StartVibration(Sample sample, IGlobalStatus gs)
        {
            ushort sampleId = sample.Id;
            int step = 0;
            if (sample.MainStep == 1)
            {
                step = 0;
            }
            else if (sample.MainStep == 2)
            {
                step = 1;
            }
            else if (sample.MainStep == 3)
            {
                step = 2;
            }
            else if (sample.MainStep == 9)
            {
                step = 3;
            }

            double vel = sample.TechParams.VibrationOneVel[step] / 60;
            int time = sample.TechParams.VibrationOneTime[step];
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"样品{sampleId}开始振荡-{time}s-{vel*60}rpm");
                    bool result = false; ;
                    //振荡回零
                    if (sample.SubStep == 4 && !_globalStatus.IsStopped)
                    {
                        result = GoHome(gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("振荡回零失败!");
                        }
                        sample.SubStep++;
                    }


                    //搬运
                    if (sample.SubStep == 5 && !_globalStatus.IsStopped)
                    {
                        result = GetSampleFromMaterialToVibration(sample, gs);
                        if (!result)
                        {
                            throw new Exception("搬运样品到振荡失败!");
                        }
                        sample.SubStep++;
                    }


                    //开始振荡
                    if (sample.SubStep == 6 && !_globalStatus.IsStopped)
                    {
                        result = base.StartVibration(time, vel, gs).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("样品振荡失败!");
                        }
                        sample.SubStep++;
                    }


                    //搬运下料
                    if (sample.SubStep == 7 && !_globalStatus.IsStopped)
                    {
                        if (sample.MainStep == 9)
                        {
                            //从振荡搬运萃取管到试管架
                            result = GetSampleFromVibrationToMaterial(sample, 2, gs);
                            if (!result)
                            {
                                throw new Exception("搬运萃取管到试管架2失败!");
                            }
                        }
                        //农残 第三步到冰浴
                        else if (sample.MainStep == 3 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex2))//  //ExtractSupernate2
                        {
                            //从振荡搬运试管到冰浴
                            result = GetSampleFromVibrationToMaterial(sample, 3, gs);
                            if (!result)
                            {
                                throw new Exception("搬运样品到冰浴失败!");
                            }
                        }
                        else   //if(sample.MainStep == 1 || sample.MainStep == 2 || sample.MainStep == 3 )
                        {
                            //样品管从振荡到试管架
                            result = GetSampleFromVibrationToMaterial(sample, 1, gs);
                            if (!result)
                            {
                                throw new Exception("搬运样品到试管架1失败!");
                            }
                        }
                        sample.SubStep++;
                        //完成
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
        /// <param name="gs"></param>
        /// <returns></returns>
        private bool GetSampleFromVibrationToMaterial(Sample sample, int var, IGlobalStatus gs)
        {
            if (var == 1)
            {
                //样品管从振荡到试管架
                return _carrier.GetSampleFromVibrationToMaterial(sample, gs);
            }
            else if (var == 2)
            {
                //从振荡搬运萃取管到试管架
                return _carrier.GetPolishFromVibrationToMaterial(sample, gs);
            }
            else if (var == 3)
            {
                //从振荡搬运试管到冰浴
                return _carrier.GetSampleFromVibrationToCold(sample, gs);
            }
            else if (var == 4)
            {
                //从振荡搬运萃取管到冰浴
                return _carrier.GetPolishFromVibrationToCold(sample, gs);
            }
            return false;
        }

        /// <summary>
        /// 从试管架搬运试管到振荡
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        private bool GetSampleFromMaterialToVibration(Sample sample, IGlobalStatus gs)
        {
            //从试管架2搬运萃取管到振荡
            if (sample.MainStep == 9)
            {
                return _carrier.GetPolishFromMaterialToVibration(sample, gs);
            }
            //从试管架1搬运试管到振荡
            else
            {
                return _carrier.GetSampleToVibration(sample, gs);
            }


        }

        #endregion



    }
}
