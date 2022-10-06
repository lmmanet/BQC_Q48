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
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class Centrifugal : ICentrifugal
    {
        //运行参数
        private Task _centrifugalTask;        //离心   （离心搬运分离出来）

        #region Private Members

        private readonly IEtherCATMotion _motion;
        private readonly IIoDevice _io;
        private readonly ILogger _logger;
        private readonly ICentrifugalCarrier _carrier;
        private readonly IGlobalStatus _globalStatus;
        private readonly static object _lockObj = new object();

        #endregion

        #region Variants

        private ushort _axisCentrigugal = 8; //离心机轴
        private ushort _homeMode = 33; //离心机回零模式

        private ushort _shadowOpen = 0; //离心机门打开控制
        private ushort _shadowClose = 1; //离心机门关闭控制
        private ushort _shadowOpenSensor = 0; //离心机门打开感应
        private ushort _shadowCloseSensor = 1; //离心机门关闭感应

        #endregion

        #region Construtors

        public Centrifugal(IEtherCATMotion motion, IIoDevice io, ICentrifugalCarrier carrier,IGlobalStatus globalStatus)
        {
            this._motion = motion;
            this._io = io;
            this._carrier = carrier; 
            this._logger = new MyLogger(typeof(Centrifugal));
            this._globalStatus = globalStatus;
            _globalStatus.StopProgramEventArgs += StopMove;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 模块回零
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GoHome(CancellationTokenSource cts)
        {
            _logger?.Info("离心机回零");
            try
            {
                var result =await _carrier.GoHome(cts).ConfigureAwait(false);
                if (!result)
                {
                    throw new Exception("离心移栽回零失败!");
                }
                OpenShadow();

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

        public bool StopMove()
        {
            _motion.StopMove(_axisCentrigugal);
            return true;
        }

        public void AddSampleToCentrifugalList(Sample sample, string actionCallBack, int var = 0)
        {
            if (sample != null)
            {
                var dicBig = GlobalCache.Instance.CentrifugalBig;   //大管
                var dicSmall = GlobalCache.Instance.CentrifugalSmall;//小管
                var dicPolish = GlobalCache.Instance.CentrifugalPolish;//萃取大管

                if (var == 1)
                {
                    if (!dicSmall.ContainsKey(sample))
                    {
                        dicSmall.Add(sample, actionCallBack);
                    }
                }
                else if (var == 2)
                {
                    if (!dicPolish.ContainsKey(sample))
                    {
                        dicPolish.Add(sample, actionCallBack);
                    }
                }
                else
                {
                    if (!dicBig.ContainsKey(sample))
                    {
                        dicBig.Add(sample, actionCallBack);
                    }
                }
            }

        }

        /// <summary>
        /// 启动离心机任务
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="actionCallBack"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public Task StartCentrifugal(CancellationTokenSource cts)
        {
            if (_centrifugalTask != null)
            {
                if (!_centrifugalTask.IsCompleted)
                {
                    return _centrifugalTask;
                }
            }

            _centrifugalTask = Task.Run(() =>
            {
                while (cts?.IsCancellationRequested != true)
                {
                    var dicBig1 = GlobalCache.Instance.CentrifugalBig;   //大管
                    var dicSmall1 = GlobalCache.Instance.CentrifugalSmall;//小管
                    var dicPolish1 = GlobalCache.Instance.CentrifugalPolish;//萃取大管


                    if (dicBig1.Count <= 0 && dicSmall1.Count <= 0 && dicPolish1.Count <= 0)
                    {
                        break;
                    }

                    KeyValuePair<Sample, string> item = dicPolish1.FirstOrDefault();

                    if (item.Key ==null) //萃取列表无数据
                    {
                        item = dicSmall1.FirstOrDefault();
                    }

                    if (item.Key == null) //净化管列表无数据
                    {
                        item = dicBig1.FirstOrDefault();
                    }

                    if (item.Key == null) //样品大管无数据
                    {
                        break;
                    }
                   
                    var itemSample1 = item.Key;

                    try
                    {
                        lock (_lockObj)
                        {  
                            //是否一次离心
                            if (itemSample1.MainStep == 4 && !_globalStatus.IsStopped)
                            {
                                if (!_globalStatus.IsStopped && TechStatusHelper.BitIsOn(itemSample1.TechParams, TechStatus.Centrifugal1))
                                {
                                    var result = DoCentrifugal(itemSample1, cts);
                                    if (!result)
                                    {
                                        throw new Exception("DoCentrifugal1 err");
                                    }
                               
                                    itemSample1.MainStep++;

                                    //触发后续动作   取上清液  加入移液列表
                                    MethodHelper.ExcuteMethod(item.Value, itemSample1, cts);

                                    //样品和任务从列表移除
                                    dicBig1.Remove(itemSample1);
                                }
                            }

                            //是否二次离心   净化管离心
                            else if (itemSample1.MainStep == 7 && !_globalStatus.IsStopped)
                            {
                                //二次离心
                                if (!_globalStatus.IsStopped && TechStatusHelper.BitIsOn(itemSample1.TechParams, TechStatus.Centrifugal2))
                                {
                                    var item2 = dicBig1.FirstOrDefault();
                                    var itemSample2 = item2.Key;
                                    if (itemSample2 != null)
                                    {
                                        //大小管一起离心
                                        if (itemSample2.MainStep == 4 && TechStatusHelper.BitIsOn(itemSample2.TechParams, TechStatus.Centrifugal1))//一次离心
                                        {
                                            var ret = DoCentrifugalBigAndSmall(itemSample2, itemSample1, 1, cts);
                                            if (!ret)
                                            {
                                                throw new Exception("DoCentrifugal3 err");
                                            }

                                            itemSample1.MainStep++;
                                            itemSample2.MainStep++;

                                            //触发后续动作   取净化液  加入到移液列表
                                            MethodHelper.ExcuteMethod(item.Value, itemSample1, cts);
                                            MethodHelper.ExcuteMethod(item2.Value, itemSample2, cts);

                                            //样品和任务从列表移除
                                            dicSmall1.Remove(itemSample1);
                                            dicBig1.Remove(itemSample2);
                                            continue;
                                        }
                                    }
                                    //单独小管离心
                                    var result = DoCentrifugalSmall(itemSample1, cts);
                                    if (!result)
                                    {
                                        throw new Exception("DoCentrifugal2 err");
                                    }

                                    itemSample1.MainStep++;

                                    //触发后续动作   取净化液  加入到移液列表
                                    MethodHelper.ExcuteMethod(item.Value, itemSample1, cts);

                                    //样品和任务从列表移除

                                    dicSmall1.Remove(itemSample1);
                                }

                            }

                            //是否三次离心
                            else if (itemSample1.MainStep == 10 && !_globalStatus.IsStopped)
                            {
                                if (!_globalStatus.IsStopped && TechStatusHelper.BitIsOn(itemSample1.TechParams, TechStatus.Centrifugal3))
                                {
                                    var item2 = dicSmall1.FirstOrDefault();
                                    var itemSample2 = item2.Key;
                                    if (itemSample2 != null)
                                    {
                                        //大小管一起离心
                                        if (itemSample2.MainStep == 7 && TechStatusHelper.BitIsOn(itemSample2.TechParams, TechStatus.Centrifugal2))
                                        {
                                            var ret = DoCentrifugalBigAndSmall(itemSample1, itemSample2, 2, cts);
                                            if (!ret)
                                            {
                                                throw new Exception("DoCentrifugal4 err");
                                            }

                                            itemSample1.MainStep++;
                                            itemSample2.MainStep++;

                                            //触发后续动作   取净化液  加入到移液列表
                                            MethodHelper.ExcuteMethod(item.Value, itemSample1, cts);
                                            MethodHelper.ExcuteMethod(item2.Value, itemSample2, cts);

                                            //样品和任务从列表移除
                                            dicPolish1.Remove(itemSample1);
                                            dicSmall1.Remove(itemSample2);
                                            continue;
                                        }
                                    }
                                    var result = DoPolishCentrifugal(itemSample1, cts);
                                    if (!result)
                                    {
                                        throw new Exception("DoCentrifugal3 err");
                                    }

                                    itemSample1.MainStep++;

                                    //触发后续动作   取萃取液  加入移液列表
                                    MethodHelper.ExcuteMethod(item.Value, itemSample1, cts);

                                    //样品和任务从列表移除

                                    dicPolish1.Remove(itemSample1);

                                }

                            }

                            else
                            {
                                _logger?.Warn($"当前步骤不存在{itemSample1.MainStep}不等4 、7 、10");
                                _globalStatus.PauseProgram();
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _globalStatus.PauseProgram();
                        _logger?.Warn(ex.Message);
                        while (_globalStatus.IsPause)
                        {
                            Thread.Sleep(1000);
                            if (_globalStatus.IsStopped)
                            {
                                return;
                            }
                        }
                        return;
                    }
                }
            });

            return _centrifugalTask;
        }  
        

        /// <summary>
        /// 大管离心
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool DoCentrifugal(Sample sample, CancellationTokenSource cts)
        {
            lock (_lockObj)
            {
                bool result;
                //从冰浴搬运样品到离心机
                if ( sample.SubStep == 0 && !_globalStatus.IsStopped)
                {
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCentrifugal))
                    {
                        result = _carrier.GetSampleFromColdToCentrifugal(sample, GoStation, cts);
                        if (!result)
                        {
                            return false;
                        }
                    }
                   
                    sample.SubStep++;
                }
             
                ///离心
                if (sample.SubStep == 1 && !_globalStatus.IsStopped)
                {
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCentrifugal))
                    {
                        int time = sample.TechParams.CentrifugalOneTime[0];
                        int vel = sample.TechParams.CentrifugalOneVelocity[0];
                        result = DoCentrifugal(time, vel, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("大管离心出错!");
                        }
                    }
                    sample.SubStep++;
                }

                //从离心机搬运离心完后的样品到试管架
                if (sample.SubStep == 2 && !_globalStatus.IsStopped)
                {
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                    {
                        result = _carrier.GetSampleFromCentrifugalToMaterial(sample, GoStation, cts);
                        if (!result)
                        {
                            return false;
                        }
                    }
                    sample.SubStep++;

                }
               

                if (sample.SubStep == 3)
                {
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf))
                    {
                        sample.SubStep = 0;
                        return true;
                    }
                }

                return false;
            }

        }
        
        /// <summary>
        /// 萃取大管离心
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool DoPolishCentrifugal(Sample sample, CancellationTokenSource cts)
        {
            lock (_lockObj)
            {
                bool result;
                //上料
                if (sample.SubStep == 0 && !_globalStatus.IsStopped)
                {
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCentrifugal))
                    {
                        result = _carrier.GetPolishFromColdToCentrifugal(sample, GoStation, cts);
                        if (!result)
                        {
                            return false;
                        }
                    }
                    sample.SubStep++;
                }
           
                //离心
                if (sample.SubStep == 1 && !_globalStatus.IsStopped)
                {
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCentrifugal))
                    {
                        int time = sample.TechParams.CentrifugalOneTime[2];
                        int vel = sample.TechParams.CentrifugalOneVelocity[2];
                        result = DoCentrifugal(time, vel, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("萃取管离心出错!");
                        }
                    }
                    sample.SubStep++;
                }

                //取出试管
                if (sample.SubStep == 2 && !_globalStatus.IsStopped)
                {
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInShelf))
                    {
                        result = _carrier.GetPolishFroCentrifugaToShelf(sample, GoStation, cts);
                        if (!result)
                        {
                            return false;
                        }
                    }
                    sample.SubStep++;
                }
              
                if (sample.SubStep == 3)
                {
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInShelf))
                    {
                        sample.SubStep = 0;
                        return true;
                    }
                }

                return false;
            }

        }

        /// <summary>
        /// 小管离心
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool DoCentrifugalSmall(Sample sample, CancellationTokenSource cts)
        {
            lock (_lockObj)
            {
                bool result;
                if ( sample.SubStep ==0 && !_globalStatus.IsStopped)
                {
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCentrifugal))
                    {
                        result = _carrier.GetPurifyFromMaterialToCentrifugal(sample, GoStation, cts);
                        if (!result)
                        {
                            return false;
                        }
                    }
                    sample.SubStep++;
                }
                   
                //离心
                if (sample.SubStep==1 && !_globalStatus.IsStopped)
                {
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCentrifugal))
                    {
                        int time = sample.TechParams.CentrifugalOneTime[1];
                        int vel = sample.TechParams.CentrifugalOneVelocity[1];
                        result = DoCentrifugal(time, vel, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("小管离心出错!");
                        }
                    }
                    sample.SubStep++;
                }

                if (sample.SubStep == 2 && !_globalStatus.IsStopped)
                {
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        result = _carrier.GetPurifyFromCentrifugalToMaterial(sample, GoStation, cts);
                        if (!result)
                        {
                            return false;
                        }
                    }
                    sample.SubStep++;
                }
                    

                if (sample.SubStep == 3)
                {
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                    {
                        sample.SubStep = 0;
                        return true;
                    }
                   
                }

                return false;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sample1">大管</param>
        /// <param name="sample2">小管</param>
        /// <param name="var">1:样品管 2:萃取管</param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private bool DoCentrifugalBigAndSmall(Sample sample1,Sample sample2,int var,CancellationTokenSource cts)
        {
            lock (_lockObj)
            {
                bool result;

                //搬运大小管到离心机
                if ((sample1.SubStep < 2 && !_globalStatus.IsStopped) || (sample2.SubStep < 2 && !_globalStatus.IsStopped))
                {
                    result = _carrier.GetBigAndSmallToCentrifugal(sample1, sample2, var, GoStation, cts);
                    if (!result)
                    {
                        return false;
                    }
                }
              
                ///离心
                if (sample1.SubStep == 2 && sample2.SubStep == 2 && !_globalStatus.IsStopped)
                {
                    int time = sample2.TechParams.CentrifugalOneTime[1];
                    int vel = sample2.TechParams.CentrifugalOneVelocity[1];
                    result = DoCentrifugal(time, vel, cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("大小管离心出错!");
                    }

                    sample1.SubStep++;
                    sample2.SubStep++;
                }

                //搬运大小管下料
                if (sample1.SubStep >= 3 && sample2.SubStep >= 3 && !_globalStatus.IsStopped)
                {
                    result = _carrier.GetBigAndSmallToToMarterial(sample1,sample2,var, GoStation, cts);
                    if (!result)
                    {
                        return false;
                    }
                }


                if (sample1.SubStep == 5 && sample2.SubStep == 5)
                {
                    sample1.SubStep = 0;
                    sample2.SubStep = 0;
                    return true;
                }

                return false;
            }

        }


        #endregion

        #region Protected Methods

        /// <summary>
        /// 离心机开始离心
        /// </summary>
        /// <param name="time"></param>
        /// <param name="vel"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        private async Task<bool> DoCentrifugal(int time,int vel, CancellationTokenSource cts)
        {
            //关闭门
            CloseShadow();

            //开始离心
            double velocity = vel / 60;
            var result = _motion.VelocityMove(_axisCentrigugal, velocity, 1);
            if (!result)
            {
                return false;
            }
            //延时
            DateTime end = DateTime.Now + TimeSpan.FromMinutes(time);
            bool isDone = false;
            await Task.Delay(1000).ConfigureAwait(false);
            while (true)
            {
                Thread.Sleep(1000);
                if (DateTime.Now >= end)
                {
                    isDone = true;
                    break;
                }
                if (_globalStatus.IsStopped)
                {
                    isDone = false;
                    break;
                }
            }
            //停止电机
            _motion.StopMove(_axisCentrigugal);

            //等待停止  
            DateTime timeout = DateTime.Now + TimeSpan.FromSeconds(30);
            while (true)
            {
                var cv = Math.Abs(Math.Round(_motion.GetCurrentVel(_axisCentrigugal), 1));
                if (cv < 0.2)
                {
                    break;
                }
                if (DateTime.Now > timeout)
                {
                    throw new TimeoutException("检测离心机停止超时");
                }
                if (cts?.IsCancellationRequested == true)
                {
                    throw new TaskCanceledException();
                }
            }
          
            //打开门
            OpenShadow();

            return isDone;
        }

        #endregion


        #region 底层方法

        /// <summary>
        /// 打开离心机护罩
        /// </summary>
        protected void OpenShadow(bool checkSensor = true)
        {
            var result = _io.WriteBit_DO(_shadowClose, false);
            if (!result)
            {
                throw new Exception("OpenShadow Err!");
            }
            result = _io.WriteBit_DO(_shadowOpen, true);
            if (!result)
            {
                throw new Exception("OpenShadow Err!");
            }
            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_shadowOpenSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new TimeoutException("离心机护罩打开超时");
                }
            } while (!result);
        }

        /// <summary>
        /// 关闭离心机护罩
        /// </summary>
        protected void CloseShadow(bool checkSensor = true)
        {
            var result = _io.WriteBit_DO(_shadowOpen, false);
            if (!result)
            {
                throw new Exception("CloseShadow Err!");
            }
            result = _io.WriteBit_DO(_shadowClose, true);
            if (!result)
            {
                throw new Exception("CloseShadow Err!");
            }
            if (!checkSensor)
            {
                Thread.Sleep(500);
                return;
            }
            int temp = 0;
            do
            {
                result = _io.ReadBit_DI(_shadowCloseSensor);
                Thread.Sleep(500);
                temp++;
                if (temp > 6)
                {
                    throw new TimeoutException("离心机护罩关闭超时");
                }
            } while (!result);
        }
           
        /// <summary>
        /// 旋转到指定工位
        /// </summary>
        /// <param name="num">1:初始位 2：90°位 3：180°位 4：270°位</param>
        protected async Task<bool> GoStation(ushort num)
        {
            //判断离心机是否停止
            var cv = Math.Abs(Math.Round(_motion.GetCurrentVel(_axisCentrigugal), 1));
            if (cv > 0.2)
            {
                _logger?.Error($"离心机未停止 速度：{cv}");
                throw new Exception("离心机未停止");
            }

            //打开护罩
            OpenShadow();

            //离心机使能
            var status = _motion.GetMotionStatus(_axisCentrigugal);
            if ((status & 4) != 4)
            {
                _motion.ServoOn(_axisCentrigugal);
            }

            double offset = 0;

            //离心机移动到指定位置
            if (num == 1)
            {
                offset = 0.05;
            }
            else if(num == 2)
            {
                offset = 0.3;
            }
            else if(num == 3)
            {
                offset = 0.55;
            }
            else if (num == 4)
            {
                offset = 0.8;
            }
            else
            {
                throw new Exception($"指定位置编号不存在 num：{num}");
            }

            //离心机回零
            var result = await _motion.GohomeWithCheckDone(_axisCentrigugal, _homeMode,offset, null).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Error($"离心机移动到指定位出错");
                throw new Exception("离心机移动到指定位置出错");
            }

            return true;
         
        }

        #endregion

    }
}
