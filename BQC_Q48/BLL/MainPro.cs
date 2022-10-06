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
        CancellationTokenSource cts = new CancellationTokenSource();
        Task _taskHome;

        Task _main;

        Task _wetBackTask;
        Task _vibrationTask;
        Task _centrifugalTask;
        Task _pipettingTask;
        Task _concentrationTask;
        //任务列表

        List<Sample> _workList;


        private string _sampleFile = Environment.CurrentDirectory + "\\Sample.xml";

        #region Private Members

        private ICapperOne _capperOne;
        private IVortex _vortex;
        private ICapperTwo _capperTwo;
        private ICapperThree _capperThree;
        private ICapperFour _capperFour;
        private ICapperFive _capperFive;
        private IConcentration _concentration;
        private IVibrationOne _vibrationOne;
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

        public MainPro(ICapperOne capperOne, IVortex vortex, ICapperTwo capperTwo, IVibrationOne vibrationOne, IAddSolid addSolid, ICarrierOne carrierOne, ICarrierTwo carrierTwo,ICentrifugal centrifugal, ICentrifugalCarrier centrifugalCarrier
            , ICapperThree capperThree, ICapperFour capperFour, ICapperFive capperFive, IConcentration concentration, IGlobalStatus globalStatus,IIoDevice io)
        {
            this._capperOne = capperOne;
            this._vortex = vortex;
            this._capperTwo = capperTwo;
            this._vibrationOne = vibrationOne;
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

        public Task GoHome(Expression<Func<bool>> homeDoneFlag)
        {
            //回零完成标志
            homeDoneFlag.SetPropertyValue(false);

            if (_taskHome != null)
            {
                if (_taskHome.IsCompleted == false) //是否完成
                {
                    return _taskHome;
                }

                if (_taskHome.IsCanceled == true) //被取消而执行完成
                {
                    return _taskHome;
                }

                if (_taskHome.IsFaulted == true) //未经处理异常而停止
                {
                    return _taskHome;
                }
            }
           
            _taskHome = Task.Run(async() =>
            {
                var result6 = _carrierOne.GoHome(cts).ConfigureAwait(false);  //搬运1回零
               
                var result20 = _carrierTwo.GoHome(cts).ConfigureAwait(false); //搬运2回零

                if (!await result6)
                {
                    Console.WriteLine("carrieroneHomeErr");
                    return;
                }
                var result1 = _capperOne.GoHome(cts).ConfigureAwait(false);  //拧盖1回零
                var result2 = _vortex.GoHome(cts).ConfigureAwait(false);  //涡旋回零
                var result3 = _capperTwo.GoHome(cts).ConfigureAwait(false);  //拧盖2回零
                var result4 = _vibrationOne.GoHome(cts).ConfigureAwait(false);  //振荡回零
                var result5 = _addSolid.GoHome(cts).ConfigureAwait(false);  //加固回零  无需单独回零

                var result7 = _centrifugal.GoHome(cts); // 离心机回零

                if (!await result20)
                {
                    Console.WriteLine("carriertwoHomeErr");
                    return ;
                }

                var result8 = _capperThree.GoHome(cts); // 拧盖3回零
                var result9 = _capperFour.GoHome(cts); // 拧盖4回零
                var result10 = _capperFive.GoHome(cts); // 拧盖5回零
                var result11= _concentration.GoHome(cts); // 浓缩回零

                if (!await result1)
                {
                    return;
                }
                if (!await result2)
                {
                    return;
                }
                if (!await result3)
                {
                    return;
                }
                if (!await result4)
                {
                    return;
                }
                if (!await result5)
                {
                    return;
                }
                if (!await result7)
                {
                    return;
                }     
                if (!await result8)
                {
                    Console.WriteLine("回零失败 result8！");
                    return;
                }    
                if (!await result9)
                {
                    Console.WriteLine("回零失败 result9！");
                    return;
                }   
                if (!await result10)
                {
                    Console.WriteLine("回零失败 result10！");
                    return;
                }      
                if (!await result11)
                {
                    Console.WriteLine("回零失败 result11！");
                    return;
                }
               _logger.Info("HomeDone");

                //回零完成
                homeDoneFlag.SetPropertyValue(true);
          
                
            });

            return _taskHome;

        }

        public void StartPro()
        {
            if (_workList == null)
            {
                /// GB23200.113-2018 果蔬    0xF43EE00
                /// GB23200.113-2018 坚果    0xF43EE19
                /// GB23200.121-2021 果蔬    0x803EEF9
                /// GB23200.121-2021 坚果    0x803EEF9
                /// 兽药                     0xFFDE1EB
                _workList = new List<Sample>();
                for (int i = 1; i < 11;)
                {
                    Sample sample = new Sample() { Id = (ushort)i, Status = 1172527124481,MainStep =3, TechParams = new TechParams()  //113-2018 果蔬
                    {
                        //AddWater = 0,
                        //Solvent_A = 10,    //ACE
                        //Solvent_B = 0,
                        //Solvent_C = 0,
                        WetTime = 0,
                        //AddHomo = new double[3] { 0, 0, 1 },    //均质子
                        //Solid_B = new double[3] { 0, 0, 4 },    //硫酸镁
                        //Solid_C = new double[3] { 0, 0, 1 },    //氯化钠
                        //Solid_D = new double[3] { 0, 0, 1 },    //柠檬酸钠
                        //Solid_E = new double[3] { 0, 0, 0.5 },  //氢二钠
                        //Solid_F = new double[3] { 0, 0, 0 },    //
                        VibrationOneTime = new int[] { 0, 0, 60, 0 },
                        VibrationOneVel = new int[] { 0, 0, 400, 0 },
                        VibrationTwoTime = new int[] { 60, 0 },
                        VibrationTwoVel = new int[] { 400, 0 },
                        VortexTime = new int[] { 0, 0, 0 },
                        VortexVel = new int[] { 0, 0, 0 },
                        CentrifugalOneTime = new int[] { 5, 5, 0 },
                        CentrifugalOneVelocity = new int[] { 4200, 4200, 0 },
                        ExtractVolume = 6,
                        ConcentrationVolume = 2,
                        ConcentrationTime = 5,
                        ConcentrationVel = 10000,
                        Redissolve = 1,                             //乙酸乙酯
                        Add_Mark_B = 20,                            //加标20uL
                        ExtractSampleVolume = 1,                     //最终样品1ml

                        Tech = 0xF03EE00,                         //工艺
                       
                    }
                    };
                    _workList.Add(sample);
                    i++;

                    //Sample sample1 = new Sample()
                    //{
                    //    Id = (ushort)i,
                    //    Status = 1172527124481, MainStep =1
                    //    TechParams = new TechParams()
                    //    {
                    //        AddWater = 8,
                    //        Solvent_A = 3,
                    //        Solvent_B = 7,
                    //        Solvent_C = 6,
                    //        ExtractVolume = 8,
                    //        AddHomo = new double[3] { 0, 1, 0 },
                    //        Solid_B = new double[3] { 1, 0, 0 },
                    //        Solid_C = new double[3] { 0, 1, 0 },
                    //        Solid_D = new double[3] { 0, 0, 1 },
                    //        Solid_E = new double[3] { 1, 0, 0 },
                    //        Solid_F = new double[3] { 0, 1, 0 },
                    //        VibrationOneTime = new int[] { 30, 30, 30 },
                    //        VibrationOneVel = new int[] { 400, 400, 400 },
                    //        VortexTime = new int[] { 40, 40, 40 },
                    //        VortexVel = new int[] { 1000, 1000, 1000 },
                    //        Tech = 0x1F43EE19,
                    //       
                    //    }
                    //};
                    //_workList.Add(sample1);
                    //i++;
                }

                //调试
                //_workList[0].Status = 1172527144961;
                //_workList[0].TechParams.Tech = 0xD03EEF9;//D03EEF9


                foreach (var item in _workList)
                {
                    Messenger.Default.Send<Sample>(item,"Add");
                }




            }




            if (_main != null)
            {
                if (!_main.IsCompleted)
                {
                    return;
                }
            }
            _main = Task.Run(() =>
            {

                //离心
                for (int i = 0; i < 8; i++)
                {
                    var sample = _workList[i];
                    sample.Status = 0x11100101080;
                    sample.TechParams.Tech = 0x3EFDE1AB;   //兽药
                    sample.TechParams.cusuanan = 5;
                    sample.TechParams.CentrifugalOneVelocity = new int[] { 1000,1000,1000};
                    sample.TechParams.CentrifugalOneTime = new int[] {1,1,1 };
                    GlobalCache.Instance.ColdDic.Add(sample, (ushort)(i + 1));
                    sample.MainStep = 4;
                    Centrifugal(sample, cts);
                }


                //浓缩测试  及  浓缩移液
                //for (int i = 0; i < 8; i++)
                //{
                //    var sample = _workList[i];
                //    sample.Status = 0x11100101080;
                //    sample.TechParams.Tech = 0xF43EE00;   //E03EE00:农残取液   F03EE00：农残浓缩
                //    sample.TechParams.ConcentrationTime = 1;
                //    GlobalCache.Instance.ConcentrationList.Add(sample);
                //    //sample.MainStep = 8;  浓缩/提取样品
                //    sample.MainStep = 11;  //兽药浓缩
                //    _centrifugalCarrier.StartConcentration(sample, cts);
                //}


                //左侧移液
                //for (int i = 0; i < 8; i++)
                //{
                //    var sample = _workList[i];
                //    sample.Status = 0x11100101001;
                //    sample.TechParams.Tech = 0x3EFDE1AB;   //  //0x3EFDE1AB:兽药移液1   0xF45EE00:兽药移液2
                //    sample.TechParams.cusuanan = 5;
                //    sample.TechParams.ConcentrationTime = 1;
                //    sample.TechParams.VibrationTwoTime = new int[] {30,30 };
                //    sample.TechParams.VibrationTwoVel = new int[] { 300,300};


                //    //GlobalCache.Instance.ConcentrationList.Add(sample);
                //    //sample.MainStep = 8;  浓缩/提取样品
                //    sample.MainStep = 8;  //兽药浓缩
                //    //sample.MainStep = 5;  //兽药移液1
                //    _pipettingTask = _centrifugalCarrier.StartPipetting(sample, "Q_Platform.BLL.IMainPro@test", cts);
                //}



                // 正式程序
                //while (_workList.Count > 0 && !_globalStatus.IsStopped)
                //{
                //    var result = Ext(_workList[0]);
                //    if (!result)
                //    {
                //        while (_globalStatus.IsPause)
                //        {
                //            Thread.Sleep(2000);
                //            if (!_globalStatus.IsPause)
                //                continue;
                //        }
                //        return;
                //    }

                //}
            });
        }

        public void StopPro()
        {
            GlobalCache.Save();
            //MySerialization.SerializeToXml<List<Sample>>(_sampleFile, _workList);
            _globalStatus.StopProgram();
            Task.Run(() =>
            {

            }).ConfigureAwait(false);
        }

        public void PausePro(Expression<Func<bool>> pauseFlag)
        {
          
            pauseFlag.SetPropertyValue(true);
            _globalStatus.PauseProgram();
        }

        public void ContinuePro()
        {
            _globalStatus.ContinueProgram();

            //启动任务  如果已经完成  重新实例再启动？   
            // _wetBackTask?.Start();
            // _vibrationTask?.Start();
            //_centrifugalTask?.Start();
            //_pipettingTask?.Start();

            if (_pipettingTask!=null)
            {
                var b = _pipettingTask.IsCompleted;
                if (b)
                {
                    _pipettingTask = _centrifugalCarrier.StartPipetting(null, "Q_Platform.BLL.IMainPro@test", cts);
                }
            }
            _pipettingTask = _centrifugalCarrier.StartPipetting(null, "Q_Platform.BLL.IMainPro@test", cts);
        }

        public void SwitchLight()
        {
            GlobalCache.Load();
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

        protected bool Ext(Sample sample)
        {
            //加水提取
            if (sample.MainStep ==1 && !_globalStatus.IsStopped)
            {
                var ret = _capperOne.AddWaterExtract(sample, cts);
                if (!ret)
                {
                    //暂停所有任务 
                    _globalStatus.PauseProgram();
                    return false;
                }
                //把样品加入到振荡涡旋列表  并启动程序
                _vibrationTask = _vibrationOne.StartVibrationAndVortex(sample, "Q_Platform.BLL.IMainPro@WetBack", cts);
            }

            if (sample.MainStep == 2 && !_globalStatus.IsStopped)
            {
                var result = _capperOne.AddSolveExtract(sample, cts);
                if (!result)
                {
                    _globalStatus.PauseProgram();
                    return false;
                }

                //把样品加入到振荡涡旋列表  并启动程序
                //_vibrationOne.StartVibrationAndVortex(sample, AddSalt,cts);
                _vibrationTask = _vibrationOne.StartVibrationAndVortex(sample, "Q_Platform.BLL.IMainPro@AddSalt", cts);
                //Thread.Sleep(500);
            }

            if (sample.MainStep == 3 && !_globalStatus.IsStopped)
            {
                var result = _capperOne.AddSaltExtract(sample, cts);
                if (!result)
                {
                    _globalStatus.PauseProgram();
                    return false;
                }

                //把样品加入到振荡涡旋列表  并启动程序
                //_vibrationOne.StartVibrationAndVortex(sample, Centrifugal,cts);

                _vibrationTask = _vibrationOne.StartVibrationAndVortex(sample, "Q_Platform.BLL.IMainPro@Centrifugal", cts);

                //Thread.Sleep(500);
            }

            _workList.Remove(sample);
            return true;
        }

        /// <summary>
        /// 加溶剂提取   回湿
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="workList">任务列表</param>
        public void WetBack(Sample sample, CancellationTokenSource cts)
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
                int count = 10;
                while (count > 0 && !_globalStatus.IsStopped)
                {
                    var list = GlobalCache.Instance.WetBackSampleList;

                    for (int i = 0; i < list.Count; i++)
                    {
                        var itemSample = list[i];
                        if (DateTime.Now > itemSample.WetBackEndTime)
                        {
                            itemSample.SubStep = 0;
                            itemSample.MainStep = 2;
                            //插入列表首位
                            _workList.Insert(1, itemSample);
                            list.Remove(itemSample);
                        }
                    }
                    count = list.Count;
                    Thread.Sleep(5000);
                }
               
            });
         
        }

        /// <summary>
        /// 加盐提取
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="workList">任务列表</param>
        public void AddSalt(Sample sample, CancellationTokenSource cts)
        {
            _workList.Insert(1, sample);
        }


        //==============================================================================================================//

        //一次离心 
        public void Centrifugal(Sample sample, CancellationTokenSource cts)
        {
            if (sample.MainStep ==4)
            {
                _logger.Info("振荡完成 下一步一次离心!");
                _centrifugalTask = _centrifugal.StartCentrifugal(sample, "Q_Platform.BLL.IMainPro@CentrifugalCallBack", cts);
            }
            else if (sample.MainStep == 7)
            {
                _logger.Info("净化振荡完成 下一步二次离心!");
                _centrifugalTask = _centrifugal.StartCentrifugal(sample, "Q_Platform.BLL.IMainPro@CentrifugalCallBack", cts,1);
            }
            
            else if (sample.MainStep == 10)
            {
                _logger.Info("萃取振荡完成 下一步三次离心!");
                _centrifugalTask = _centrifugal.StartCentrifugal(sample, "Q_Platform.BLL.IMainPro@CentrifugalCallBack", cts,2);
            }
            
        }


        //一次移液  二次移液
        public void CentrifugalCallBack(Sample sample, CancellationTokenSource cts)
        {
            if (sample.MainStep == 8 && !TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractSupernate2))
            {
                _concentrationTask = _centrifugalCarrier.StartConcentration(sample, cts);
            }

            else if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractSupernate3) && sample.MainStep == 11)
            {
                _concentrationTask = _centrifugalCarrier.StartConcentration(sample, cts);
            }
            else
            {
                _pipettingTask = _centrifugalCarrier.StartPipetting(sample, "Q_Platform.BLL.IMainPro@Centrifugal", cts);

            }
        }






        #endregion


        private void TechParamDemo()
        {
            Sample sample1 = new Sample()     //113-2018 果蔬
            {
                Id = 1,
                Status = 0x11100101001,
                MainStep = 3,
                TechParams = new TechParams()  //113-2018 果蔬
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
                    VibrationOneVel = new int[] { 0, 0, 400, 0 },
                    VibrationTwoTime = new int[] { 60, 0 },
                    VibrationTwoVel = new int[] { 400, 0 },
                    VortexTime = new int[] { 0, 0, 0 },
                    VortexVel = new int[] { 0, 0, 0 },
                    CentrifugalOneTime = new int[] { 5, 5, 0 },
                    CentrifugalOneVelocity = new int[] { 4200, 4200, 0 },
                    ExtractVolume = 6,
                    ConcentrationVolume = 2,
                    ConcentrationTime = 5,
                    ConcentrationVel = 10000,
                    Redissolve = 1,                             //乙酸乙酯
                    Add_Mark_B = 20,                            //加标20uL
                    ExtractSampleVolume = 1,                     //最终样品1ml

                    Tech = 0xF03EE00,                         //工艺
                  
                }
            };

            Sample sample2 = new Sample()     //113-2018 果蔬
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
                    Solid_E = new double[3] { 0, 0, 0 },  //氢二钠
                    Solid_F = new double[3] { 0, 0, 7.5 },    // 无水硫酸钠+醋酸钠
                    VibrationOneTime = new int[] { 0, 0, 60, 0 },
                    VibrationOneVel = new int[] { 0, 0, 400, 0 },      
                    VibrationTwoTime = new int[] { 60, 0 },
                    VibrationTwoVel = new int[] { 400, 0 },
                    VortexTime = new int[] { 60, 0, 0 },
                    VortexVel = new int[] { 2000, 0, 0 },
                    CentrifugalOneTime = new int[] { 5, 5, 0 },
                    CentrifugalOneVelocity = new int[] { 4200, 4200, 0 },
                    ExtractVolume = 8,
                    ConcentrationVolume = 2,
                    ConcentrationTime = 5,
                    ConcentrationVel = 10000,
                    Redissolve = 1,                             //乙酸乙酯
                    Add_Mark_B = 20,                            //加标20uL
                    ExtractSampleVolume = 1,                     //最终样品1ml

                    Tech = 0xF03EE19,                         //工艺
                }
            };

            Sample sample3 = new Sample()     //121-2021 果蔬
            {
                Id = 1,
                Status = 0x11100101001,
                MainStep = 1,
                TechParams = new TechParams()  //121-2021 果蔬
                {
                    AddWater = 9,
                    Solvent_A = 10,    //ACE
                    Solvent_B = 0,    //乙腈醋酸
                    Solvent_C = 0,
                    WetTime = 30,
                    AddHomo = new double[3] { 0, 1, 0 },    //均质子
                    Solid_B = new double[3] { 0, 0, 4 },    //硫酸镁
                    Solid_C = new double[3] { 0, 0, 1 },    //氯化钠
                    Solid_D = new double[3] { 0, 0, 1 },    //柠檬酸钠
                    Solid_E = new double[3] { 0, 0, 0.5 },  //氢二钠
                    Solid_F = new double[3] { 0, 0, 0 },    //
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

            Sample sample4 = new Sample()     //121-2021 坚果
            {
                Id = 1,
                Status = 0x11100101001,MainStep =1,
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
                    Solid_E = new double[3] { 0, 0, 0 },  //氢二钠
                    Solid_F = new double[3] { 0, 0, 7.5 },    //
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
                    cusuanan  = 5,                           //醋酸铵
                    Extract=10,
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


   


    }
}
