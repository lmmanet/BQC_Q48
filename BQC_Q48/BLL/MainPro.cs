using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Messaging;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Q_Platform.BLL
{
    public class MainPro : IMainPro
    {

        #region Private Variants

        /// <summary>
        /// 回零
        /// </summary>
        private Task<bool> _taskHome;

        /// <summary>
        /// 提取
        /// </summary>
        private Task _extractTask;

        /// <summary>
        /// 回湿
        /// </summary>
        private Task _wetBackTask;

        /// <summary>
        /// 任务列表
        /// </summary>
        private List<Sample> _workList = new List<Sample>();


        #endregion

        #region Private Members

        private ICapperOne _capperOne;
        private IVortex _vortex;
        private ICapperTwo _capperTwo;
        private ICapperThree _capperThree;
        private ICapperFour _capperFour;
        private ICapperFive _capperFive;
        private IConcentration _concentration;
        private IVibrationOne _vibrationOne;
        private IVibrationTwo _vibrationTwo;
        private IAddSolid _addSolid;

        private ICarrierOne _carrierOne;
        private ICarrierTwo _carrierTwo;

        private ICentrifugal _centrifugal;


        private ICentrifugalCarrier _centrifugalCarrier;


        private IGlobalStatus _globalStatus;

        private ILogger _logger;
        private IIoDevice _io;

        #endregion

        #region Construtor

        public MainPro(ICapperOne capperOne, IVortex vortex, ICapperTwo capperTwo, IVibrationOne vibrationOne, IVibrationTwo vibrationTwo, IAddSolid addSolid, ICarrierOne carrierOne, ICarrierTwo carrierTwo, ICentrifugal centrifugal, ICentrifugalCarrier centrifugalCarrier
            , ICapperThree capperThree, ICapperFour capperFour, ICapperFive capperFive, IConcentration concentration, IGlobalStatus globalStatus, IIoDevice io)
        {
            this._capperOne = capperOne;
            this._vortex = vortex;
            this._capperTwo = capperTwo;
            this._vibrationOne = vibrationOne;
            this._vibrationTwo = vibrationTwo;
            this._addSolid = addSolid;
            this._carrierOne = carrierOne;
            this._carrierTwo = carrierTwo;
            this._centrifugal = centrifugal;


            this._capperThree = capperThree;
            this._capperFour = capperFour;
            this._capperFive = capperFive;
            this._concentration = concentration;

            this._centrifugalCarrier = centrifugalCarrier;
            this._globalStatus = globalStatus;

            this._io = io;
            this._logger = new MyLogger(typeof(MainPro));
        }
        #endregion

        #region Public Methods

        public void GoHome(Expression<Func<bool>> homeDoneFlag)
        {
            //回零完成标志
            homeDoneFlag.SetPropertyValue(false);
            Task.Run(() =>
            {
                bool result = _globalStatus.InitStatus(StopDone, InitSys);
                if (result)
                {
                    homeDoneFlag.SetPropertyValue(true);
                }
            });
        }

        /// <summary>
        /// 初始化回零
        /// </summary>
        /// <param name="homeDoneFlag"></param>
        /// <returns></returns>
        private async Task<bool> InitSys()
        {
            if (_taskHome != null)
            {
                if (_taskHome.IsCompleted == false) //是否完成
                {
                    return false;
                }

                if (_taskHome.IsCanceled == true) //被取消而执行完成
                {
                    return false;
                }

                if (_taskHome.IsFaulted == true) //未经处理异常而停止
                {
                    return false;
                }
            }

            _taskHome = Task<bool>.Run(async() =>
            {
                var result6 = _carrierOne.GoHome(_globalStatus).ConfigureAwait(false);  //搬运1回零

                var result20 = _carrierTwo.GoHome(_globalStatus).ConfigureAwait(false); //搬运2回零

                if (!await result6)
                {
                    _logger?.Warn("搬运1回零失败!");
                    return false;
                }
                var result1 = _capperOne.GoHome(_globalStatus).ConfigureAwait(false);  //拧盖1回零
                var result2 = _vortex.GoHome(_globalStatus).ConfigureAwait(false);  //涡旋回零
                var result3 = _capperTwo.GoHome(_globalStatus).ConfigureAwait(false);  //拧盖2回零
                var result4 = _vibrationOne.GoHome(_globalStatus).ConfigureAwait(false);  //振荡回零
                var result12 = _vibrationTwo.GoHome(_globalStatus).ConfigureAwait(false);// 振荡2回零
                var result5 = _addSolid.GoHome(_globalStatus).ConfigureAwait(false);  //加固回零  无需单独回零

                var result7 = _centrifugal.GoHome(_globalStatus); // 离心机回零

                if (!await result20)
                {
                    _logger?.Warn("搬运2回零失败!");
                    return false;
                }

                var result8 = _capperThree.GoHome(_globalStatus); // 拧盖3回零
                var result9 = _capperFour.GoHome(_globalStatus); // 拧盖4回零
                var result10 = _capperFive.GoHome(_globalStatus); // 拧盖5回零
                var result11 = _concentration.GoHome(_globalStatus); // 浓缩回零

                if (!await result1)
                {
                    _logger?.Warn("拧盖1回零失败!");
                    return false;
                }
                if (!await result2)
                {
                    _logger?.Warn("涡旋回零失败!");
                    return false;
                }
                if (!await result3)
                {
                    _logger?.Warn("拧盖2回零失败!");
                    return false;
                }
                if (!await result4)
                {
                    _logger?.Warn("振荡1回零失败!");
                    return false;
                }
                if (!await result5)
                {
                    _logger?.Warn("加固回零失败!");
                    return false;
                }
                if (!await result7)
                {
                    _logger?.Warn("离心机回零失败!");
                    return false;
                }
                if (!await result12)
                {
                    _logger?.Warn("振荡2回零失败!");
                    return false;
                }
                if (!await result8)
                {
                    _logger?.Warn("拧盖3回零失败!");
                    return false;
                }
                if (!await result9)
                {
                    _logger?.Warn("拧盖4回零失败!");
                    return false;
                }
                if (!await result10)
                {
                    _logger?.Warn("拧盖5回零失败!");
                    return false;
                }
                if (!await result11)
                {
                    _logger?.Warn("浓缩回零失败!");
                    return false;
                }
                _logger.Info("回零初始化完成");

                return true;


            });

            return await _taskHome.ConfigureAwait(false);
        }

        public void ClearWorkList()
        {
            _workList = new List<Sample>();
        }

        public void StartPro()
        {
            if (_globalStatus.StartProgram())
            {
                if (_workList.Count == 0)
                {
                    _workList = GlobalCache.Instance.WorkList;
                    var exlist = GlobalCache.Instance.ExtractList;
                    foreach (var item in _workList)
                    {
                        exlist.Add(item);
                        Messenger.Default.Send<Sample>(item, "Add");
                    }
                }
      
                _centrifugalCarrier.StartConcentration(_globalStatus);
                _centrifugalCarrier.StartPipetting(_globalStatus);
                _centrifugal.StartCentrifugal(_globalStatus);
                _vibrationOne.StartVibration(_globalStatus);
                _vortex.StartVortex(_globalStatus);
                StartExtract();
            }
        }

        public void StopPro()
        {
            //保存状态数据
            GlobalCache.Save();
            _globalStatus.StopProgram(StopDone);
        }

        public void PausePro(Expression<Func<bool>> pauseFlag)
        {
            //保存状态数据
            GlobalCache.Save();
            var result = _globalStatus.PauseProgram();
            pauseFlag.SetPropertyValue(result);
        }

        public void ContinuePro()
        {
            if (_globalStatus.ContinueProgram())
            {

                _centrifugalCarrier.StartConcentration(_globalStatus);
                _centrifugalCarrier.StartPipetting(_globalStatus);
                _centrifugal.StartCentrifugal(_globalStatus);
                _vibrationOne.StartVibration(_globalStatus);
                _vortex.StartVortex(_globalStatus);


                StartExtract();//启动提取

                foreach (var item in GlobalCache.Instance.WorkList)
                {
                    Messenger.Default.Send<Sample>(item, "Add");
                }
            }
        }

        public void SwitchLight()
        {
            if (!_io.ReadBit_DO(3))
            {
                _io.WriteBit_DO(3, true);
                _io.WriteBit_DO(34, true);
                _io.WriteBit_DO(68, true);
            }
            else
            {
                _io.WriteBit_DO(3, false);
                _io.WriteBit_DO(34, false);
                _io.WriteBit_DO(68, false);
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// 需要一直存在
        /// </summary>
        /// <returns></returns>
        private Task StartExtract()
        {
            if (_extractTask != null)
            {
                if (!_extractTask.IsCompleted)
                {
                    return _extractTask;
                }
            }
            _extractTask = Task.Run(() =>
            {
                var extractList = GlobalCache.Instance.ExtractList;
                //正式程序
                while (!_globalStatus.IsStopped)
                {
                    if (extractList.Count == 0)
                    {
                        break;
                    }
                    if (extractList != null && extractList.Count > 0)
                    {
                        var samplevar = extractList[0];
                        if (samplevar != null)
                        {
                            var result = Ext(samplevar);
                            if (result)
                            {
                                extractList.Remove(samplevar);
                            }
                            if (!result)
                            {
                                while (_globalStatus.IsPause && !_globalStatus.IsStopped && !_globalStatus.IsEmgStop)
                                {
                                    Thread.Sleep(1000);
                                }
                            }
                        }
                    }
                    Thread.Sleep(1000);
                }
            });
            return _extractTask;
        }

        /// <summary>
        /// 溶液及盐包提取
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        protected bool Ext(Sample sample)
        {
            //加水提取
            if (sample.MainStep == 1 && !_globalStatus.IsStopped)
            {
                var ret = _capperOne.AddWaterExtract(sample, _globalStatus);
                if (!ret)
                {
                    //暂停所有任务 
                    _globalStatus.PauseProgram();
                    return false;
                }
                //把样品加入到振荡涡旋列表  并启动程序
                _vibrationOne.AddSampleToVibrationList(sample, _globalStatus);
                _vibrationOne.StartVibration(_globalStatus);
            }

            if (sample.MainStep == 2 && !_globalStatus.IsStopped)
            {
                var result = _capperOne.AddSolveExtract(sample, _globalStatus);
                if (!result)
                {
                    _globalStatus.PauseProgram();
                    return false;
                }

                //把样品加入到振荡涡旋列表  并启动程序
                _vibrationOne.AddSampleToVibrationList(sample, _globalStatus);
                _vibrationOne.StartVibration(_globalStatus);
            }

            if (sample.MainStep == 3 && !_globalStatus.IsStopped)
            {
                var result = _capperOne.AddSaltExtract(sample, _globalStatus);
                if (!result)
                {
                    _globalStatus.PauseProgram();
                    return false;
                }

                //把样品加入到振荡涡旋列表  并启动程序
                _vibrationOne.AddSampleToVibrationList(sample, _globalStatus);
                _vibrationOne.StartVibration(_globalStatus);
            }

            return true;
        }

        /// <summary>
        /// 加溶剂提取   回湿
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="workList">任务列表</param>
        public void WetBack(Sample sample, IGlobalStatus gs)
        {
            int minutes = sample.TechParams.WetTime;
            DateTime end = DateTime.Now + TimeSpan.FromMinutes(minutes);
            sample.WetBackEndTime = end;

            var list1 = GlobalCache.Instance.WetBackSampleList;
            if (!list1.Contains(sample))
            {
                list1.Add(sample);
            }

            if (_wetBackTask != null)
            {
                if (!_wetBackTask.IsCompleted)
                {
                    return;
                }
            }

            _wetBackTask = Task.Run(() =>
            {
                while (!_globalStatus.IsStopped)
                {
                    var list = GlobalCache.Instance.WetBackSampleList;
                    if (list.Count <= 0)
                    {
                        break;
                    }

                    var itemSample = list[0];
                    if (DateTime.Now > itemSample.WetBackEndTime)
                    {
                        itemSample.SubStep = 0;
                        itemSample.MainStep = 2;
                        //插入列表首位
                        if (GlobalCache.Instance.ExtractList.Count > 1)
                        {
                            var wList = GlobalCache.Instance.ExtractList;
                            var s = wList.LastOrDefault(sam => sam.MainStep == 2);//最后一个 进行第二步加溶剂
                            if (s == null)
                            {
                                wList.Insert(1, itemSample);
                            }
                            else
                            {
                                if (wList.IndexOf(s) == wList.Count-1)//为最后一个
                                {
                                    wList.Add(itemSample);
                                }
                                else
                                {
                                    wList.Insert(wList.IndexOf(s) + 1, itemSample);
                                }
                            }
                           
                        }
                        else
                        {
                            GlobalCache.Instance.ExtractList.Add(itemSample);
                        }

                        list.Remove(itemSample);
                        StartExtract();
                    }
                    Thread.Sleep(500);
                }

            });

        }

        /// <summary>
        /// 加盐提取
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="workList">任务列表</param>
        public void AddSalt(Sample sample, IGlobalStatus gs)
        {
            var list = GlobalCache.Instance.ExtractList;
            if (list.Count > 1)
            {
                var wList = GlobalCache.Instance.ExtractList;
                var s = wList.LastOrDefault(sam => sam.MainStep == 3);//最后一个 进行第三步加盐
                if (s == null)
                {
                    wList.Insert(1, sample);
                }
                else
                {
                    if (wList.IndexOf(s) == wList.Count - 1)//为最后一个
                    {
                        wList.Add(sample);
                    }
                    else
                    {
                        wList.Insert(wList.IndexOf(s) + 1, sample);
                    }
                }
                //list.Insert(1, sample);  bug 20221020
            }
            else
            {
                list.Add(sample);
            }

            StartExtract();

        }


        //==============================================================================================================//

        /// <summary>
        /// 离心
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        public void Centrifugal(Sample sample, IGlobalStatus gs)
        {
            if (sample.MainStep == 4)
            {
                _logger.Info("振荡完成 下一步一次离心!");
                _centrifugal.AddSampleToCentrifugalList(sample, "Q_Platform.BLL.IMainPro@CentrifugalCallBack");
                _centrifugal.StartCentrifugal(gs);
            }
            else if (sample.MainStep == 7)
            {
                _logger.Info("净化振荡完成 下一步二次离心!");
                _centrifugal.AddSampleToCentrifugalList(sample, "Q_Platform.BLL.IMainPro@CentrifugalCallBack", 1);
                _centrifugal.StartCentrifugal(gs);
            }

            else if (sample.MainStep == 10)
            {
                _logger.Info("萃取振荡完成 下一步三次离心!");
                _centrifugal.AddSampleToCentrifugalList(sample, "Q_Platform.BLL.IMainPro@CentrifugalCallBack", 2);
                _centrifugal.StartCentrifugal(gs);
            }

        }

        /// <summary>
        /// 离心回调
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        public void CentrifugalCallBack(Sample sample,IGlobalStatus gs)
        {
            if (sample.MainStep == 8 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractSupernate2))
            {
                _centrifugalCarrier.AddSampleToConcentrationList(sample);
                _centrifugalCarrier.StartConcentration(gs);
            }

            else if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractSupernate3) && sample.MainStep == 11)
            {
                _centrifugalCarrier.AddSampleToConcentrationList(sample);
                _centrifugalCarrier.StartConcentration(gs);
            }
            else
            {
                _centrifugalCarrier.AddSampleToPipettingList(sample, "Q_Platform.BLL.IMainPro@PipettingCallBack");
                _centrifugalCarrier.StartPipetting(gs);

            }
        }

        /// <summary>
        /// 移液回调
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="gs"></param>
        public void PipettingCallBack(Sample sample, IGlobalStatus gs)
        {
            //上一步  移液 净化振荡
            if (sample.MainStep == 7)
            {
                Centrifugal(sample, gs);
            }
            //上一步提取净化液
            if (sample.MainStep == 9 && TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractSupernate2))//振荡涡旋
            {
                //振荡涡旋
                //_vibrationOne.AddSampleToVibrationList(sample, "Q_Platform.BLL.IMainPro@Centrifugal", cts);
                //_vibrationOne.StartVibrationAndVortex(cts);
                _vibrationOne.AddSampleToVibrationList(sample, gs);
                _vibrationOne.StartVibration(gs);
            }


        }


        #endregion

        /// <summary>
        /// 判断程序是否停止
        /// </summary>
        /// <returns></returns>
        private bool StopDone()
        {
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(5);
            while (DateTime.Now < end)
            {
                if (_vortex.IsVortexTaskDone                          // 涡旋任务结束
                    && _centrifugalCarrier.IsConcentrationTaskDone    // 浓缩任务结束
                    && _centrifugalCarrier.IsPipttorTaskDone          // 移液任务结束
                    && _centrifugal.IsCentrifugalTaskDone             // 离心任务结束
                    && _vibrationOne.IsVibrationTaskDone              // 振荡任务结束
                    && _extractTask?.IsCompleted != false                     // 提取任务结束
                    && _wetBackTask?.IsCompleted != false)             // 回湿任务结束
                {
                    return true;
                }
                
            }
            return false;
        }



        private void TechParamDemo()
        {
            //113-2018 果蔬   2021020测试OK
            Sample sample1 = new Sample()     //113-2018 果蔬   2021020测试OK
            {
                Id = 1,
                Status = 0x11100101001,
                MainStep = 3,
                TechParams = new TechParams()  //121-2021 果蔬
                {
                    AddWater = 0,
                    Solvent_A = 10,    //ACE
                    Solvent_B = 0,
                    Solvent_C = 0,
                    WetTime = 0,
                    AddHomo = new double[3] { 0, 0, 1 },    //均质子
                    Solid_B = new double[3] { 0, 0, 4 },    //硫酸镁
                    Solid_C = new double[3] { 0, 0, 1 },    //氯化钠
                    Solid_D = new double[3] { 0, 0, 1 },    //柠檬酸钠
                    Solid_E = new double[3] { 0, 0, 0.5 },  //氢二钠
                    Solid_F = new double[3] { 0, 0, 0 },    //
                    VibrationOneTime = new int[] { 0, 0, 60, 0 },
                    VibrationOneVel = new int[] { 0, 0, 420, 0 },
                    VibrationTwoTime = new int[] { 60, 0 },
                    VibrationTwoVel = new int[] { 420, 0 },
                    VortexTime = new int[] { 60, 60, 60 },
                    VortexVel = new int[] { 2000, 2000, 2000 },
                    CentrifugalOneTime = new int[] { 5, 5, 0 },
                    CentrifugalOneVelocity = new int[] { 4200, 4200, 0 },
                    ExtractVolume = 9,//6ml
                    ConcentrationVolume = 2,
                    ConcentrationTime = 5,
                    ConcentrationVel = 10000,
                    Redissolve = 2,                             //乙酸乙酯
                    Add_Mark_B = 20,                            //加标20uL
                    ExtractSampleVolume = 1,                     //最终样品1ml
                    //移液高度 取液大管148
                    //吐液小管     71.92
                    //取液小管   44.5（用不上）
                    //净化管取液 167
                    //西林瓶取液  145.5
                    //移栽大管取液 73.371
                    //样品小瓶吐液 120
                    //西林瓶吐液  105.085
                    Tech = 0xF03FE00,                         //工艺
                }
            };

            //113-2018 坚果   20221022测试OK
            Sample sample2 = new Sample()    
            {
                Id = 1,
                Status = 0x11100101001,
                MainStep = 1,
                TechParams = new TechParams()  //113-2018 坚果
                {
                    AddWater = 10,
                    Solvent_A = 0,    //ACE  
                    Solvent_B = 15,   //乙腈醋酸
                    Solvent_C = 0,
                    WetTime = 30,
                    AddHomo = new double[3] { 0, 0, 1 },    //均质子
                    Solid_B = new double[3] { 0, 0, 0 },    //硫酸镁
                    Solid_C = new double[3] { 0, 0, 0 },    //氯化钠
                    Solid_D = new double[3] { 0, 0, 0 },    //柠檬酸钠
                    Solid_E = new double[3] { 0, 0, 6 },    //无水硫酸钠
                    Solid_F = new double[3] { 0, 0, 1.5 },    // 醋酸钠
                    VibrationOneTime = new int[] { 60, 60, 60, 60 },
                    VibrationOneVel = new int[] { 420, 420, 420, 420 },
                    VibrationTwoTime = new int[] { 60, 0 },
                    VibrationTwoVel = new int[] { 420, 0 },
                    VortexTime = new int[] { 60, 60, 60 },
                    VortexVel = new int[] { 2000, 2000, 2000 },
                    CentrifugalOneTime = new int[] { 5, 5, 0 },
                    CentrifugalOneVelocity = new int[] { 4200, 4200, 0 },
                    ExtractVolume = 8,
                    ConcentrationVolume = 2,
                    ConcentrationTime = 5,
                    ConcentrationVel = 10000,
                    Redissolve = 1,                             //乙酸乙酯
                    Add_Mark_B = 20,                            //加标20uL
                    ExtractSampleVolume = 1,                     //最终样品1ml
                    //移液高度 取液大管135
                    //吐液小管     71.92
                    //取液小管   44.5（用不上）
                    //净化管取液 167
                    //西林瓶取液  145.5
                    //移栽大管取液 73.371
                    //样品小瓶吐液 120
                    //西林瓶吐液  105.085
                    Tech = 0xF03EE19,                         //工艺
                }
            };

            //121-2021 果蔬  测试OK 20221020
            Sample sample3 = new Sample()     //121-2021 果蔬
            {
                Id = 1,
                Status = 0x11100101001,
                MainStep = 2,
                TechParams = new TechParams()  //121-2021 果蔬
                {
                    AddWater = 9,
                    Solvent_A = 10,    //ACE
                    Solvent_B = 0,    //乙腈醋酸
                    Solvent_C = 0,
                    AddHomo = new double[3] { 0, 1, 0 },    //均质子
                    Solid_B = new double[3] { 0, 0, 4 },    //硫酸镁
                    Solid_C = new double[3] { 0, 0, 1 },    //氯化钠
                    Solid_D = new double[3] { 0, 0, 1 },    //柠檬酸钠
                    Solid_E = new double[3] { 0, 0, 0.5 },  //氢二钠
                    Solid_F = new double[3] { 0, 0, 0 },    //
                    VibrationOneTime = new int[] { 60, 60, 60, 60 },
                    VibrationOneVel = new int[] { 420, 420, 420, 420 },
                    VibrationTwoTime = new int[] { 60, 60 },
                    VibrationTwoVel = new int[] { 420, 420 },
                    VortexTime = new int[] { 60, 60, 60 },
                    VortexVel = new int[] { 2000, 2000, 2000 },
                    CentrifugalOneTime = new int[] { 5, 5, 5 },
                    CentrifugalOneVelocity = new int[] { 4200, 4200, 4200 },
                    ExtractVolume = 6,//6ml
                    ConcentrationVolume = 0,
                    ConcentrationTime = 0,
                    ConcentrationVel = 0,
                    Redissolve = 0,                             //乙酸乙酯
                    Add_Mark_B = 0,                            //加标20uL
                    ExtractSampleVolume = 1,                     //最终样品1ml

                    Tech = 0x801F4E0,                         //工艺
                }
            };

            Sample sample4 = new Sample()     //121-2021 坚果
            {
                Id = 1,
                Status = 0x11100101001,
                MainStep = 1,
                TechParams = new TechParams()  //121-2021 坚果
                {
                    AddWater = 10,
                    Solvent_A = 15,    //ACE
                    Solvent_B = 0,    //乙腈醋酸
                    Solvent_C = 0,
                    WetTime = 30,
                    AddHomo = new double[3] { 0, 1, 0 },    //均质子
                    Solid_B = new double[3] { 0, 0, 0 },    //硫酸镁
                    Solid_C = new double[3] { 0, 0, 0 },    //氯化钠
                    Solid_D = new double[3] { 0, 0, 0 },    //柠檬酸钠
                    Solid_E = new double[3] { 0, 0, 6 },  //
                    Solid_F = new double[3] { 0, 0, 1.5 },    //
                    VibrationOneTime = new int[] { 0, 60, 60, 0 },
                    VibrationOneVel = new int[] { 0, 400, 400, 0 },
                    VibrationTwoTime = new int[] { 60, 0 },
                    VibrationTwoVel = new int[] { 400, 0 },
                    VortexTime = new int[] { 60, 0, 0 },
                    VortexVel = new int[] { 2000, 0, 0 },
                    CentrifugalOneTime = new int[] { 5, 5, 0 },
                    CentrifugalOneVelocity = new int[] { 4200, 4200, 0 },
                    ExtractVolume = 6,
                    ConcentrationVolume = 0,
                    ConcentrationTime = 0,
                    ConcentrationVel = 0,
                    Redissolve = 0,                             //乙酸乙酯
                    Add_Mark_B = 0,                            //加标20uL
                    ExtractSampleVolume = 1,                     //最终样品1ml

                    Tech = 0x801ECF9,                         //工艺

                }
            };

            Sample sample5 = new Sample()     //兽药
            {
                Id = 1,
                Status = 0x11100101001,
                MainStep = 1,
                TechParams = new TechParams()  //兽药
                {
                    AddWater = 1,
                    Solvent_A = 0,    //ACE
                    Solvent_B = 0,    //乙腈醋酸
                    Solvent_C = 10,    //甲酸乙腈溶液
                    WetTime = 0,
                    AddHomo = new double[3] { 2, 0, 0 },    //均质子
                    Solid_B = new double[3] { 0, 0, 0 },    //硫酸镁
                    Solid_C = new double[3] { 0, 0, 0 },    //氯化钠
                    Solid_D = new double[3] { 0, 0, 0 },    //柠檬酸钠
                    Solid_E = new double[3] { 0, 0, 0 },  //氢二钠
                    Solid_F = new double[3] { 0, 0, 0 },    //
                    VibrationOneTime = new int[] { 0, 60, 0, 120 },
                    VibrationOneVel = new int[] { 0, 400, 0, 400 },
                    VibrationTwoTime = new int[] { 120, 120 },
                    VibrationTwoVel = new int[] { 400, 400 },
                    VortexTime = new int[] { 60, 300, 0 },
                    VortexVel = new int[] { 2000, 2000, 0 },
                    CentrifugalOneTime = new int[] { 5, 5, 5 },
                    CentrifugalOneVelocity = new int[] { 4000, 4000, 4000 },
                    ExtractVolume = 5,
                    cusuanan = 5,                           //醋酸铵
                    Extract = 10,
                    ConcentrationVolume = 2,
                    ConcentrationTime = 5,
                    ConcentrationVel = 10000,
                    Redissolve = 0.95,                             //乙腈水溶液
                    Add_Mark_A = 50,                            //加标50uL                           
                    ExtractSampleVolume = 1,                     //最终样品1ml

                    Tech = 0x3EFDE1AB,                         //工艺
                }
            };

        }




        /// <summary>
        /// 兽药工艺  测试OK
        /// </summary>

        //Sample sample = new Sample()
        //{
        //    Id = (ushort)i,
        //    Status = 1172527124481
        //           //,MainStep =3,
        //           ,
        //    MainStep = 1,//兽药
        //    TechParams = new TechParams()  //兽药
        //    {
        //        AddWater = 1,
        //        Solvent_C = 10,    //甲酸乙腈溶液
        //        WetTime = 0,
        //        AddHomo = new double[3] { 2, 0, 0 },     
        //        VibrationOneTime = new int[] { 0, 100, 0, 100 },
        //        VibrationOneVel = new int[] { 0, 300, 0, 300 },
        //        VortexTime = new int[] { 200, 200, 200, 200 },
        //        VortexVel = new int[] { 1000, 1000, 1000, 1000 },
        //        VibrationTwoTime = new int[] { 300, 300 },
        //        VibrationTwoVel = new int[] { 300, 300 },
        //        CentrifugalOneTime = new int[] { 5, 5, 5 },
        //        CentrifugalOneVelocity = new int[] { 4020, 4020, 4020 },
        //        ExtractVolume = 5,                                  //提取上清液量 一次离心
        //        cusuanan = 5,                                       //醋酸铵
        //        Extract = 10,                                       //完全倾倒   二次离心提取量
        //        ConcentrationVolume = 2,                            //浓缩提取量
        //        ConcentrationTime = 5,                              //浓缩时间
        //        ConcentrationVel = 10000,                           //浓缩速度
        //        Redissolve = 0.95,                                  //乙腈水溶液
        //        Add_Mark_A = 50,                                    //加标50uL                           
        //        ExtractSampleVolume = 1,                            //最终样品1ml
        //        ExtractDeepOffset = new double[] { 82, 55, 90, 44 },    //兽药移液高度  需要修改移液高度
        //        Tech = 0x3DFDE1AB,                                  //工艺  3DFDE1AB
        //    }

        //};

        //测试OK
        //Sample sample = new Sample()
        //{
        //    Id = (ushort)i,
        //    Status = 1172527124481
        //               ,
        //    MainStep = 3,//农残
        //    TechParams = new TechParams()  //兽药
        //    {
        //        AddWater = 0,
        //        Solvent_A = 10,    //ACE
        //        Solvent_B = 0,
        //        Solvent_C = 0,
        //        WetTime = 0,
        //        //AddHomo = new double[3] { 0, 0, 1 },    //均质子
        //        //Solid_B = new double[3] { 0, 0, 4 },    //硫酸镁
        //        //Solid_C = new double[3] { 0, 0, 1 },    //氯化钠
        //        //Solid_D = new double[3] { 0, 0, 1 },    //柠檬酸钠
        //        //Solid_E = new double[3] { 0, 0, 0.5 },  //氢二钠
        //        //Solid_F = new double[3] { 0, 0, 0 },    //
        //        VibrationOneTime = new int[] { 0, 0, 60, 0 },
        //        VibrationOneVel = new int[] { 0, 0, 400, 0 },
        //        VibrationTwoTime = new int[] { 60, 0 },
        //        VibrationTwoVel = new int[] { 400, 0 },
        //        VortexTime = new int[] { 0, 0, 0 },
        //        VortexVel = new int[] { 0, 0, 0 },
        //        CentrifugalOneTime = new int[] { 5, 5, 0 },
        //        CentrifugalOneVelocity = new int[] { 4200, 4200, 0 },
        //        ExtractVolume = 6,
        //        ConcentrationVolume = 2,
        //        ConcentrationTime = 5,
        //        ConcentrationVel = 10000,
        //        Redissolve = 1,                             //乙酸乙酯
        //        Add_Mark_B = 20,                            //加标20uL
        //        ExtractSampleVolume = 1,                     //最终样品1ml
        //        ExtractDeepOffset = new double[] { 82, 55, 90, 44 },    //移液高度
        //        Tech = 0xF03EE00,                         //工艺
        //    }
        //}





    }
}
