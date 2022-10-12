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
    public class Vortex : IVortex
    {
        Task _vortexTask; //涡旋

        #region Private Members

        private readonly IIoDevice _io;

        private readonly ILogger _logger;

        private readonly ILS_Motion _stepMotion;

        private readonly ICarrierOne _carrier;

        private readonly IGlobalStatus _globalStatus;

        private readonly IVortexPosDataAccess _dataAccess;

        private readonly static object _lockObj = new object();

        #endregion

        #region Variable

        protected double _stepMoveVel = 500;

        protected ushort _axisY = 8;  //Y轴
        protected ushort _vortexMotion1 =32;
        protected ushort _vortexMotion2 =33;
        protected ushort _vortexMotionVel1 = 0;
        protected ushort _vortexMotionVel2 = 1;

        protected ushort _press = 15;  //下压气缸
        protected ushort _pressUpSensor = 17;    //下压气缸上感应
        protected ushort _pressDownSensor =18;  //下压气缸下感应

        protected double _xOffset = 50;    //涡旋X偏移量

        protected VortexPosData _posData;


        #endregion

        #region Constructors

        public Vortex(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, IVortexPosDataAccess dataAccess,ICarrierOne carrier)
        {
            this._io = io;
            this._stepMotion = motion;
            this._logger = new MyLogger(typeof(Vortex));
            this._globalStatus = globalStatus;
            this._dataAccess = dataAccess;

            this._carrier = carrier;

            _posData = dataAccess.GetPosData();

            _globalStatus.StopProgramEventArgs += StopMove;
        }

        public void UpdatePosData()
        {
            _posData = _dataAccess.GetPosData();
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// 回零
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> GoHome(CancellationTokenSource cts)
        {
            _logger.Info("涡旋回零");
            try
            {
                //下压气缸上升
                PressUp();

                //使能Y轴
                await _stepMotion.ServoOn(_axisY).ConfigureAwait(false);
                //Y轴回零
                var result = await _stepMotion.GoHomeWithCheckDone(_axisY, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Y轴回零失败");
                }

                //Y轴移动到上下料位
                result = await _stepMotion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _stepMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("Y轴到上下料位失败");
                }
                return true;
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested == true)
                {
                    return false;
                }
                _logger?.Warn($"{ex.Message}");
                return false;
            }
        }

        public bool StopMove()
        {
            //停止涡旋电机
            _io.WriteBit_DO(_vortexMotion1, false);
            _io.WriteBit_DO(_vortexMotion2, false);
            _stepMotion.StopMove(_axisY);
            return true;
        }

        public bool StartVortex(Sample sample, int step, CancellationTokenSource cts)
        {
            int vel = sample.TechParams.VortexVel[step];
            int time = sample.TechParams.VortexTime[step];
            try
            {

                lock (_lockObj)
                {
                    _logger?.Info($"{sample.Id}样品涡旋-{time}s-{vel}rpm");

                    //到上下料位
                    var result = MovePutGetPos(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("涡旋到上下料位失败");
                    }

                    //搬运
                    if (step == 3)   //萃取管涡旋
                    {
                        //上一步是振荡的情况
                        if (!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex3) && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration3))
                        {
                            //从振荡搬运萃取管到试管架  并返回完成
                            result = _carrier.GetPolishFromVibrationToMaterial(sample, cts);
                            if (!result)
                            {
                                throw new Exception("从振荡搬运萃取管到试管架失败!");
                            }
                           //返回无下一步
                            return true;
                        }
                        //上一步没有振荡的情况
                        else if(!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration3) && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex3))
                        {
                            //从试管架搬运试管到涡旋


                            //继续下一步
                        }
                        else
                        {
                            result = _carrier.GetPolishFromVibrationToVortex(sample, null, null, cts);
                            if (!result)
                            {
                                throw new Exception("从振荡搬运萃取管到涡旋失败!");
                            }
                        }
                    }
                    else
                    {
                        result = _carrier.GetSampleToVortex(sample, cts);
                        if (!result)
                        {
                            throw new Exception($"搬运{sample.Id}样品到涡旋失败!");
                        }
                    }

                    //开始涡旋
                    result = StartVortexAsync(time, vel, cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("涡旋失败");
                    }

                    //搬运到试管架
                    if (step == 3)
                    {
                        //从涡旋搬运萃取管到试管架
                        result = _carrier.GetPolishFromVortexToMaterial(sample, cts);
                        if (!result)
                        {
                            throw new Exception("从涡旋搬运萃取管到试管架失败!");
                        }
                        return true;
                    }
                    else
                    {
                        result = _carrier.GetSampleFromVortexToMaterial(sample, cts);
                        if (!result)
                        {
                            throw new Exception("搬运样品到试管架失败");
                        }

                        return true;
                    }

                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"样品{sample.Id}振荡-{time}s-{vel}rpm 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        #endregion


        #region Protected Methods

        /// <summary>
        /// 移动到上下料位
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> MovePutGetPos(CancellationTokenSource cts)
        {
            try
            {
                PressUp();

                //步进Y轴移动到原点位置
               s1: var result = await _stepMotion.P2pMoveWithCheckDone(_axisY, _posData.PutGetPos, _stepMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    if (_globalStatus.IsPause)
                    {
                        while (_globalStatus.IsPause)
                        {
                            Thread.Sleep(1000);
                        }

                        if (!_globalStatus.IsStopped)
                        {
                            goto s1;
                        }
                    }
                    throw new Exception("Y轴移动到指定位置出错!");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message);
                return false;
            }
           

        }

        /// <summary>
        /// 开始涡旋
        /// </summary>
        /// <param name="time"></param>
        /// <param name="vel">30~3000</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        protected async Task<bool> StartVortexAsync(int time,int vel,CancellationTokenSource cts)
        {
            try
            {
                PressUp();

                //Y轴移动到涡旋位置
                var result = await _stepMotion.P2pMoveWithCheckDone(_axisY, _posData.VortexPos, _stepMoveVel, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }
                //下压气缸动作
                PressDown();

                //备用
                if (GlobalCache.Instance.VortexStep == 0 && !_globalStatus.IsStopped)
                {
                    GlobalCache.Instance.VortexStep++;
                }

                //备用
                if (GlobalCache.Instance.VortexStep == 1 && !_globalStatus.IsStopped)
                {
                    GlobalCache.Instance.VortexStep++;
                }

                //启动涡旋电机
                if (GlobalCache.Instance.VortexStep == 2 && !_globalStatus.IsStopped)
                {
                    _io.WriteByte_DA(_vortexMotionVel1, vel * 10);
                    _io.WriteByte_DA(_vortexMotionVel2, vel * 10);

                    _io.WriteBit_DO(_vortexMotion1, true);
                    _io.WriteBit_DO(_vortexMotion2, true);

                    DateTime end = DateTime.Now + TimeSpan.FromSeconds(time);
                    do
                    {
                        Thread.Sleep(1000);
                        if (DateTime.Now > end)
                        {
                            break;
                        }
                        if (_globalStatus.IsStopped)
                        {
                            _io.WriteBit_DO(_vortexMotion1, false);
                            _io.WriteBit_DO(_vortexMotion2, false);
                            await Task.Delay(500).ConfigureAwait(false);
                            //下压气缸上升
                            PressUp();
                            return false;
                        }
                    } while (true);

                    GlobalCache.Instance.VortexStep++;

                 
                }

                if (GlobalCache.Instance.VortexStep == 3 && !_globalStatus.IsStopped)
                {
                    GlobalCache.Instance.VortexStep++;
                }
               
                //停止涡旋电机
                _io.WriteBit_DO(_vortexMotion1, false);
                _io.WriteBit_DO(_vortexMotion2, false);
                //延时
                if (GlobalCache.Instance.VortexStep == 4 && !_globalStatus.IsStopped)
                {
                    await Task.Delay(1000).ConfigureAwait(false); 
                    GlobalCache.Instance.VortexStep++;
                }

                //Y轴移动到上下料位置
                if (GlobalCache.Instance.VortexStep == 5 && !_globalStatus.IsStopped)
                {
                    result = await MovePutGetPos(cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    GlobalCache.Instance.VortexStep = 0;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message);
                return false;
            }
          
        }

        /// <summary>
        /// 下压气缸动作
        /// </summary>
        protected virtual void PressDown(bool checkSensor = true)
        {
            //下压气缸向下
            var result = _io.WriteBit_DO(_press, true);
            if (!result)
            {
                throw new Exception("PressDown Err!");
            }
            if (!checkSensor)
            {
                return;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_pressDownSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new Exception("PressDown 超时");
                }
            } while (!result);



        }

        /// <summary>
        /// 下压气缸释放
        /// </summary>
        protected virtual void PressUp(bool checkSensor = true)
        {
            //下压气缸向上
            var result = _io.WriteBit_DO(_press, false);
            if (!result)
            {
                throw new Exception("PressUp Err!");
            }

            if (!checkSensor)
            {
                return;
            }

            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_pressUpSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new Exception("PressUp 超时");
                }
            } while (!result);

        }

        #endregion


        //==============================================涡旋单独用===================================================//
        /// <summary>
        /// 添加样品到涡旋列表
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        public void AddSampleToVortexList(Sample sample, CancellationTokenSource cts)
        {
            //判断去重
            if (sample != null)
            {
                //不存在加水涡旋工艺  直接跳过  无需进入列表  == >  进入回湿程序
                if (sample.MainStep == 1 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Vortex))
                {
                    sample.SubStep++;
                    sample.ActionCallBack = "Q_Platform.BLL.IMainPro@WetBack";
                    MethodHelper.ExcuteMethod(sample, cts);
                    return;
                }
                //不存在加溶剂涡旋工艺 直接跳过  无需进入列表
                if (sample.MainStep == 2 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex1))
                {
                    sample.SubStep = 0;
                    sample.MainStep++;
                    return;
                }
                //不存在加溶剂涡旋工艺 直接跳过  无需进入列表
                if (sample.MainStep == 3 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex2))
                {
                    sample.SubStep = 0;
                    sample.MainStep++;
                    return;
                }
                //不存在加溶剂涡旋工艺 直接跳过  无需进入列表
                if (sample.MainStep == 9 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex2))
                {
                    sample.SubStep = 0;
                    sample.MainStep++;
                    return;
                }

                //加入到列表
                var list = GlobalCache.Instance.VortexList;
                if (!list.Contains(sample))
                {
                    list.Add(sample);
                }
            }
        }

        /// <summary>
        /// 启动涡旋任务
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public Task StartVortex(CancellationTokenSource cts)
        {
            if (_vortexTask != null)
            {
                if (!_vortexTask.IsCompleted)
                {
                    return _vortexTask;
                }
            }

            _vortexTask = Task.Run(() =>
            {
                while (!_globalStatus.IsStopped)
                {
                    var list = GlobalCache.Instance.VortexList;
                    if (list == null || list.Count <=0)
                    {
                        break;
                    }

                    lock (_lockObj)
                    {  
                        //找出优先级高的样品
                        var workSample = FindHighPrioritySample(list);
            
                        try
                        {
                            //涡旋
                            if (workSample.SubStep == 5 && !_globalStatus.IsStopped)
                            {
                                var result = StartVortex(workSample, cts);
                                if (!result)
                                {
                                    throw new Exception("StartVortex err");
                                }
                                list.Remove(workSample);
                                GlobalCache.Instance.VortexCurrentSample = null;
                                workSample.SubStep++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _globalStatus.PauseProgram();
                            _logger.Warn(ex.Message);
                            return;
                        }
                    }
                    Thread.Sleep(1000);
                }
            });

            return _vortexTask;
        }


        /// <summary>
        /// 开始涡旋
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool StartVortex(Sample sample,CancellationTokenSource cts)
        {
            try
            {
                _logger?.Info($"{sample.Id}样品涡旋");

                //到上下料位
                var result = MovePutGetPos(cts).GetAwaiter().GetResult();
                if (!result)
                {
                    throw new Exception("涡旋到上下料位失败");
                }

                //搬运样品到涡旋
                result = GetSampleFromMaterialToVortex(sample, cts);
                if (!result)
                {
                    throw new Exception("搬运样品到涡旋失败!");
                }

                //开始涡旋
                if (sample.MainStep == 9)
                {
                    int vel = sample.TechParams.VortexVel[3];
                    int time = sample.TechParams.VortexTime[3];
                    result = StartVortexAsync(time, vel, cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("涡旋4失败");
                    }
                }
                else if (sample.MainStep == 1)
                {
                    int vel = sample.TechParams.VortexVel[0];
                    int time = sample.TechParams.VortexTime[0];
                    result = StartVortexAsync(time, vel, cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("涡旋1失败");
                    }
                }
                else if (sample.MainStep == 2)
                {
                    int vel = sample.TechParams.VortexVel[1];
                    int time = sample.TechParams.VortexTime[1];
                    result = StartVortexAsync(time, vel, cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("涡旋2失败");
                    }
                }
                else if (sample.MainStep == 3)
                {
                    int vel = sample.TechParams.VortexVel[2];
                    int time = sample.TechParams.VortexTime[2];
                    result = StartVortexAsync(time, vel, cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("涡旋3失败");
                    }
                }

                //从涡旋搬运到试管架
                if (sample.MainStep == 9)
                {
                    result = GetSampleFromVortexToMaterial(sample, 2, cts);
                    if (!result)
                    {
                        return false;
                    }
                    return true;
                }
                else
                {
                    result = GetSampleFromVortexToMaterial(sample, 1, cts);
                    if (!result)
                    {
                        return false;
                    }
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
            if (GlobalCache.Instance.VortexCurrentSample != null)
            {
                sample = GlobalCache.Instance.VortexCurrentSample;
            }
            //萃取管涡旋
            else if (list.Exists(s => s.MainStep == 9))
            {
                sample = list.Find(s => s.MainStep == 9);
            }
            //加盐涡旋
            else if (list.Exists(s => s.MainStep == 3))
            {
                sample = list.Find(s => s.MainStep == 3);
            }
            //加液涡旋
            else if (list.Exists(s => s.MainStep == 2))
            {
                sample = list.Find(s => s.MainStep == 2);
            }
            //加水涡旋
            else if (list.Exists(s => s.MainStep == 1))
            {
                sample = list.Find(s => s.MainStep == 1);
            }

            GlobalCache.Instance.VortexCurrentSample = sample;

            return sample;
        }


        /// <summary>
        /// 从涡旋搬运试管到试管架1 试管架2  冰浴
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="var">1;到试管架1 2:到试管架2 3:样品管到冰浴 4;萃取管到冰浴</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetSampleFromVortexToMaterial(Sample sample,int var,CancellationTokenSource cts)
        {
            if (var == 1)
            {
                //样品管从涡旋到试管架
                return _carrier.GetSampleFromVortexToMaterial(sample, cts);
            }
            else if (var == 2)
            {
                //从涡旋搬运萃取管到试管架
                return _carrier.GetPolishFromVortexToMaterial(sample, cts);
            }
            else if (var == 3)
            {
                //从涡旋搬运试管到冰浴
                return _carrier.GetSampleFromVortexToCold(sample, cts);
            }
            else if (var == 4)
            {
                //从涡旋搬运萃取管到冰浴
                return _carrier.GetPolishFromVortexToCold(sample, cts);
            }
            return false;
        }


        /// <summary>
        /// 从试管架搬运试管到涡旋
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool GetSampleFromMaterialToVortex(Sample sample,CancellationTokenSource cts)
        {
            //从试管架2搬运萃取管到涡旋
            if (sample.MainStep == 9)
            {
                return _carrier.GetPolishFromMaterialToVortex(sample, cts);
            }
            //从试管架1搬运试管到涡旋
            else
            {
                return _carrier.GetSampleToVortex(sample, cts);
            }
          

        }
        //=================================================================================================//


    }

}

