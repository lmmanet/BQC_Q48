using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using GalaSoft.MvvmLight.Messaging;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public void GoHome()
        {
            if (_taskHome != null)
            {
                if (_taskHome.IsCompleted == false) //是否完成
                {
                    return;
                }

                if (_taskHome.IsCanceled == true) //被取消而执行完成
                {
                    return;
                }

                if (_taskHome.IsFaulted == true) //未经处理异常而停止
                {
                    return;
                }
            }
           
            _taskHome = Task.Run(() =>
            {
                var result6 = _carrierOne.GoHome(cts);  //搬运1回零
               
                var result20 = _carrierTwo.GoHome(cts); //2

                if (!result6.GetAwaiter().GetResult())
                {
                    Console.WriteLine("carrieroneHomeErr");
                    return;
                }
                var result1 = _capperOne.GoHome(cts);  //拧盖1回零
                var result2 = _vortex.GoHome(cts);  //涡旋回零
                var result3 = _capperTwo.GoHome(cts);  //拧盖2回零
                var result4 = _vibrationOne.GoHome(cts);  //振荡回零
                var result5 = _addSolid.GoHome(cts);  //加固回零  无需单独回零

                var result7 = _centrifugal.GoHome(cts); // 离心机回零

                if (!result20.GetAwaiter().GetResult())
                {
                    Console.WriteLine("carriertwoHomeErr");
                    return;
                }

                var result8 = _capperThree.GoHome(cts); // 拧盖3回零
                var result9 = _capperFour.GoHome(cts); // 拧盖4回零
                var result10 = _capperFive.GoHome(cts); // 拧盖5回零
                var result11= _concentration.GoHome(cts); // 浓缩回零

                if (!result1.GetAwaiter().GetResult())
                {
                    return;
                }
                if (!result2.GetAwaiter().GetResult())
                {
                    return;
                }
                if (!result3.GetAwaiter().GetResult())
                {
                    return;
                }
                if (!result4.GetAwaiter().GetResult())
                {
                    return;
                }
                if (!result5.GetAwaiter().GetResult())
                {
                    return;
                }
                if (!result7.GetAwaiter().GetResult())
                {
                    return;
                }     
                if (!result8.GetAwaiter().GetResult())
                {
                    Console.WriteLine("回零失败 result8！");
                    return;
                }    
                if (!result9.GetAwaiter().GetResult())
                {
                    Console.WriteLine("回零失败 result9！");
                    return;
                }   
                if (!result10.GetAwaiter().GetResult())
                {
                    Console.WriteLine("回零失败 result10！");
                    return;
                }      
                if (!result11.GetAwaiter().GetResult())
                {
                    Console.WriteLine("回零失败 result11！");
                    return;
                }
               _logger.Info("HomeDone");
                
            });
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
                    Sample sample = new Sample() { Id = (ushort)i, Status = 1172527124481, TechParams = new TechParams()  //113-2018 果蔬
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
                        TechStep = 5
                    }
                    };
                    _workList.Add(sample);
                    i++;

                    //Sample sample1 = new Sample()
                    //{
                    //    Id = (ushort)i,
                    //    Status = 1172527124481,
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
                    //        TechStep =1
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

                //测试离心部分
                //for (int i = 0; i < 8; i++)
                //{
                //    (_carrierOne as CarrierOne).Test(_workList[i], cts);
                //}
                //while (_workList.Count > 0 && !_globalStauts.IsStopped)
                //{

                //    Centrifugal(_workList[0], cts);

                //    Thread.Sleep(5000);
                //    _workList.Remove(_workList[0]);
                //}

                //for (int i = 0; i < 10; i++)
                //{
                //    var result = _capperFour.DoConcentrationOne(_workList[i], cts);
                //    if (!result)
                //    {
                //        return;
                //    }
                //}


                //正式程序
                while (_workList.Count > 0 && !_globalStatus.IsStopped)
                {
                    var result = Ext(_workList[0]);
                    if (!result)
                    {
                        while (_globalStatus.IsPause)
                        {
                            Thread.Sleep(2000);
                            if (!_globalStatus.IsPause)
                                continue;
                        }
                        return;
                    }
                   
                }
            });
        }

        public void StopPro()
        {
            MySerialization.SerializeToXml<List<Sample>>(_sampleFile, _workList);
            GlobalCache.SaveStatus();
            cts?.Cancel();
            _globalStatus.StopProgram();
            Task.Run(() =>
            {

            }).ConfigureAwait(false);
        }

        public void PausePro()
        {
            _globalStatus.PauseProgram();
        }

        public void ContinuePro()
        {
            _globalStatus.ContinueProgram();
            //if (_workList == null)
            //{
            //    _workList = MySerialization.DeserializeFromXml<List<Sample>>(_sampleFile);
            //}
            //GlobalCache.GetStatusFromFile();

            //cts = new CancellationTokenSource();
            //Task.Run(() =>
            //{
            //}).ConfigureAwait(false);
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

        protected bool Ext(Sample sample)
        {

            //加水提取
            if (sample.TechParams.TechStep ==1 && !_globalStatus.IsStopped)
            {
                var ret = _capperOne.AddWaterExtract(sample, cts);
                if (!ret)
                {
                    //暂停所有任务 
                    _globalStatus.PauseProgram();
                   
                    return false;
                }
                //把样品加入到振荡涡旋列表  并启动程序
                sample.TechParams.TechStep = 2;
                _vibrationOne.StartVibrationAndVortex(sample, "Q_Platform.BLL.IMainPro@AddSolve", cts);
            }

            if (sample.TechParams.TechStep == 3 && !_globalStatus.IsStopped)
            {
                var result = _capperOne.AddSolveExtract(sample, cts);
                if (!result)
                {
                    _globalStatus.PauseProgram();
                    Console.WriteLine("加液提取出错");
                    return false;
                }
                TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddSolve1);

                //把样品加入到振荡涡旋列表  并启动程序
                sample.TechParams.TechStep = 4;
                //_vibrationOne.StartVibrationAndVortex(sample, AddSalt,cts);
                _vibrationOne.StartVibrationAndVortex(sample, "Q_Platform.BLL.IMainPro@AddSalt", cts);
                //Thread.Sleep(500);
            }

            if (sample.TechParams.TechStep == 5 && !_globalStatus.IsStopped)
            {
                var result = _capperOne.AddSaltExtract(sample, cts);
                if (!result)
                {
                    _globalStatus.PauseProgram();
                    Console.WriteLine("加盐提取出错");
                    return false;
                }
                TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddSolve1);

                //把样品加入到振荡涡旋列表  并启动程序
                sample.TechParams.TechStep = 6;
                //_vibrationOne.StartVibrationAndVortex(sample, Centrifugal,cts);
                _vibrationOne.StartVibrationAndVortex(sample, "Q_Platform.BLL.IMainPro@Centrifugal", cts);

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
        public void AddSolve(Sample sample, CancellationTokenSource cts)
        {
            Task.Run(() =>
            {
                int minutes = sample.TechParams.WetTime;
                DateTime end = DateTime.Now + TimeSpan.FromMinutes(minutes);
                while (minutes >0)
                {
                    if (DateTime.Now >= end)
                    {
                        break;
                    }
                    Thread.Sleep(10000);
                }
                _workList.Insert(1, sample);
                sample.TechParams.TechStep = 3;
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
            if (sample.TechParams.TechStep ==10)
            {
                _logger.Info("振荡完成 下一步一次离心!");
                _centrifugal.StartCentrifugal(sample, "Q_Platform.BLL.IMainPro@CentrifugalCallBack", cts);
            }
            else if (sample.TechParams.TechStep == 20)
            {
                _logger.Info("净化振荡完成 下一步二次离心!");
                _centrifugal.StartCentrifugal(sample, "Q_Platform.BLL.IMainPro@CentrifugalCallBack", cts,true);
            }
            
            else if (sample.TechParams.TechStep == 30)
            {
                _logger.Info("萃取振荡完成 下一步三次离心!");
                _centrifugal.StartCentrifugal(sample, "Q_Platform.BLL.IMainPro@CentrifugalCallBack", cts,true);
            }
            
        }


        //一次移液  二次移液
        public void CentrifugalCallBack(Sample sample, CancellationTokenSource cts)
        {
            _centrifugalCarrier.StartPipetting(sample, Centrifugal, cts);
        }






        #endregion


        private void TechParamDemo()
        {
            Sample sample1 = new Sample()     //113-2018 果蔬
            {
                Id = 1,
                Status = 0x11100101001,
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
                    TechStep = 3
                }
            };

            Sample sample2 = new Sample()     //113-2018 果蔬
            {
                Id = 1,
                Status = 0x11100101001,
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
                    TechStep = 1
                }
            };

            Sample sample3 = new Sample()     //121-2021 果蔬
            {
                Id = 1,
                Status = 0x11100101001,
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
                    TechStep = 1
                }
            };

            Sample sample4 = new Sample()     //121-2021 坚果
            {
                Id = 1,
                Status = 0x11100101001,
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
                    TechStep = 1
                }
            };

            Sample sample5 = new Sample()     //兽药
            {
                Id = 1,
                Status = 0x11100101001,
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
                    TechStep = 1
                }
            };

        }



    }
}
