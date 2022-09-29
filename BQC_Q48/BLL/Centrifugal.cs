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

                var ret3 = Centri_GoHome().ConfigureAwait(false);
                if (!await ret3)
                {
                    return false;
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

        /// <summary>
        /// 启动离心机任务
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="actionCallBack"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public Task StartCentrifugal(Sample sample, string actionCallBack, CancellationTokenSource cts, bool isInsert = false)
        {
            if (isInsert)
            {
                GlobalCache.InsertCentrifugalKeyValue(sample, actionCallBack);
            }
            else
            {
                GlobalCache.AddCentrifugalKeyValue(sample, actionCallBack);
            }
         
   
            if (_centrifugalTask != null)
            {
                if (!_centrifugalTask.IsCompleted)
                {
                    return _centrifugalTask;
                }
            }

            _centrifugalTask = Task.Run(() =>
            {
                while (cts?.IsCancellationRequested != true && GlobalCache.GetCentrifugalKeyValueCount() > 0)
                {
                    KeyValuePair<Sample, string> item = GlobalCache.GetCentrifugalKeyValues(0);
                    var itemSample1 = item.Key;

                    try
                    {
                        lock (_lockObj)
                        {  
                            //是否一次离心
                            if (TechStatusHelper.BitIsOn(itemSample1.TechParams, TechStatus.Centrifugal1) && itemSample1.TechParams.TechStep == 10)
                            {
                                if (!_globalStatus.IsStopped)
                                {
                                    var result = DoCentrifugal(itemSample1, cts);
                                    if (!result)
                                    {
                                        throw new Exception("DoCentrifugal1 err");
                                    }
                                    TechStatusHelper.ResetBit(itemSample1.TechParams, TechStatus.Centrifugal1);
                                    itemSample1.TechParams.TechStep = 11;

                                    //触发后续动作
                                    MethodHelper.ExcuteMethod(item.Value, itemSample1, cts);

                                    //样品和任务从列表移除

                                    GlobalCache.RemoveCentrifugalKeyValue(itemSample1, item.Value);
                                }
                                else
                                {
                                    throw new TaskCanceledException("程序停止");
                                }

                            }

                            //是否二次离心   净化管离心
                            else if (TechStatusHelper.BitIsOn(itemSample1.TechParams, TechStatus.Centrifugal2) && itemSample1.TechParams.TechStep == 20)
                            {
                                if (!_globalStatus.IsStopped)
                                {
                                    var result = DoCentrifugalSmall(itemSample1, cts);
                                    if (!result)
                                    {
                                        throw new Exception("DoCentrifugal2 err");
                                    }
                                    TechStatusHelper.ResetBit(itemSample1.TechParams, TechStatus.Centrifugal2);
                                    itemSample1.TechParams.TechStep = 21;

                                    //触发后续动作
                                    MethodHelper.ExcuteMethod(item.Value, itemSample1, cts);

                                    //样品和任务从列表移除

                                    GlobalCache.RemoveCentrifugalKeyValue(itemSample1, item.Value);
                                }
                                else
                                {
                                    throw new TaskCanceledException("程序停止");
                                }

                            }

                            //是否三次离心
                            else if (TechStatusHelper.BitIsOn(itemSample1.TechParams, TechStatus.Centrifugal3) && itemSample1.TechParams.TechStep == 30)
                            {
                                if (!_globalStatus.IsStopped)
                                {
                                    var result = DoPolishCentrifugal(itemSample1, cts);
                                    if (!result)
                                    {
                                        throw new Exception("DoCentrifugal3 err");
                                    }
                                    TechStatusHelper.ResetBit(itemSample1.TechParams, TechStatus.Centrifugal3);
                                    itemSample1.TechParams.TechStep = 31;

                                    //触发后续动作
                                    MethodHelper.ExcuteMethod(item.Value, itemSample1, cts);

                                    //样品和任务从列表移除

                                    GlobalCache.RemoveCentrifugalKeyValue(itemSample1, item.Value);
                                }
                                else
                                {
                                    throw new TaskCanceledException("程序停止");
                                }

                            }

                            else
                            {
                                return;
                            }
                        }
                     

                    }
                    catch (Exception ex)
                    {
                        _logger?.Error(ex.Message);
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
                if (!SampleStatusHelper.BitIsOn(sample,SampleStatus.IsInCentrifugal) && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Centrifugal1))
                {
                    result = _carrier.GetSampleFromColdToCentrifugal(sample, GoStation, cts);
                    if (!result)
                    {
                        return false;
                    }
                }
             
                ///离心
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInCentrifugal) && TechStatusHelper.BitIsOn(sample.TechParams,TechStatus.Centrifugal1)
                    && !_globalStatus.IsStopped)
                {


                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.Centrifugal1);
                }

                //从离心机搬运离心完后的样品到试管架
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf) )
                {
                    result = _carrier.GetSampleFromCentrifugalToMaterial(sample, GoStation, cts);
                    if (!result)
                    {
                        return false;
                    }
                }
               
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsInShelf) && !_globalStatus.IsStopped)
                {
                    return true;
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
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCentrifugal) && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Centrifugal3))
                {
                    result = _carrier.GetPolishFromColdToCentrifugal(sample, GoStation, cts);
                    if (!result)
                    {
                        return false;
                    }
                }
           

                ///离心
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInCentrifugal) && TechStatusHelper.BitIsOn(sample.TechParams,TechStatus.Centrifugal3)
                    && !_globalStatus.IsStopped)
                {


                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.Centrifugal3);
                }


                //取出试管
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInShelf))
                {
                    result = _carrier.GetPolishFroCentrifugaToShelf(sample, GoStation, cts);
                    if (!result)
                    {
                        return false;
                    }
                }
              
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPolishInShelf) && !_globalStatus.IsStopped)
                {
                    return true;
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
                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCentrifugal) && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Centrifugal2))
                {
                    result = _carrier.GetPurifyFromMaterialToCentrifugal(sample, GoStation, cts);
                    if (!result)
                    {
                        return false;
                    }
                }
                   

                ///离心
                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCentrifugal) && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Centrifugal2)
                    && !_globalStatus.IsStopped)
                {


                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.Centrifugal2);
                }

                if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                {
                    result = _carrier.GetPurifyFromCentrifugalToMaterial(sample, GoStation, cts);
                    if (!result)
                    {
                        return false;
                    }
                }
                    

                if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf) && !_globalStatus.IsStopped)
                {
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
        public async Task<bool> DoCentrifugal(int time,int vel, CancellationTokenSource cts)
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
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(time);
            bool isDone = false;
            while (true)
            {
                Thread.Sleep(1000);
                if (DateTime.Now >= end)
                {
                    isDone = true;
                    break;
                }
                if (cts?.IsCancellationRequested == true)
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
          

            //离心机回零
            result = await Centri_GoHome().ConfigureAwait(false);
            if (!result)
            {
                return false;
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


            //离心机回零

            var status = _motion.GetMotionStatus(_axisCentrigugal);
            if ((status & 4) != 4)
            {
                _motion.ServoOn(_axisCentrigugal);
            }

            var result = await _motion.GohomeWithCheckDone(_axisCentrigugal, _homeMode, null).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Error($"离心机回零出错");
                throw new Exception("离心机回零出错");
            }


            //离心机移动到指定位置
            if (num != 1)
            {
                double offset = 0;
                if (num == 2)
                {
                    offset = 0.25;
                }
                else if (num == 3)
                {
                    offset = 0.5;
                }
                else if (num == 4)
                {
                    offset = 0.75;
                }
                else
                {
                    throw new Exception($"指定位置编号不存在 num：{num}");
                }

                result = await _motion.P2pMoveWithCheckDone(_axisCentrigugal, offset, 1, null).ConfigureAwait(false);
                if (!result)
                {
                    _logger?.Error($"离心机转到指定位置出错");
                    throw new Exception("离心机转到指定位置出错");
                }

                return true;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 离心机回零
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> Centri_GoHome()
        {
            //检查是否使能
            if (!_motion.IsServeOn(_axisCentrigugal))
            {
                _motion.ServoOn(_axisCentrigugal);
            }

            var result = await _motion.GohomeWithCheckDone(_axisCentrigugal, _homeMode, null).ConfigureAwait(false);
            if (!result)
            {
                _logger?.Error($"离心机回零出错");
                throw new Exception("离心机回零出错");
            }
            return true;
        }


        #endregion

    }
}
