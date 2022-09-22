using BQJX.Common;
using BQJX.Common.Common;
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
        //任务线

        Task _vibrationTwoTask;       //提取净化液 + 振荡2
   
        Task _extSample;              //提取样品液
        //任务列表
      
     
        List<Sample> _vibrationTwoList = new List<Sample>();
        List<Sample> _extSampleList = new List<Sample>();

        List<Sample> _workList;


        private string _sampleFile = Environment.CurrentDirectory + "\\Sample.xml";

        #region Private Members

        private ICapperOne _capperOne;
        private IVortex _vortex;
        private ICapperTwo _capperTwo;
        private IVibrationOne _vibrationOne;
        private IAddSolid _addSolid;    
        private ICarrierOne _carrierOne;
        private ICentrifugal _centrifugal;

        #endregion


        #region Construtor

        public MainPro(ICapperOne capperOne, IVortex vortex, ICapperTwo capperTwo, IVibrationOne vibrationOne, IAddSolid addSolid, ICarrierOne carrierOne,ICentrifugal centrifugal)
        {
            this._capperOne = capperOne;
            this._vortex = vortex;
            this._capperTwo = capperTwo;
            this._vibrationOne = vibrationOne;
            this._addSolid = addSolid;
            this._carrierOne = carrierOne;
            this._centrifugal = centrifugal;

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
                if (!result6.GetAwaiter().GetResult())
                {
                    return;
                }

                var result1 = _capperOne.GoHome(cts);  //拧盖1回零
                var result2 = _vortex.GoHome(cts);  //涡旋回零
                var result3 = _capperTwo.GoHome(cts);  //拧盖2回零
                var result4 = _vibrationOne.GoHome(cts);  //振荡回零
                var result5 = _addSolid.GoHome(cts);  //加固回零  无需单独回零

                var result7 = _centrifugal.GoHome(cts); // 离心机回零
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
                Console.WriteLine("HomeDone");
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
                for (int i = 1; i < 24;)
                {
                    Sample sample = new Sample() { Id = (ushort)i++, Status = 1, TechParams = new TechParams() 
                    { 
                        AddWater = 8,
                        Solvent_A =3,
                        Solvent_B =7,
                        Solvent_C =6,
                        AddHomo = new double[3] { 0,0,1},
                        Solid_B = new double[3] { 1,1,1},
                        Solid_C = new double[3] { 1,1,0},
                        Solid_D = new double[3] { 1,1,0},
                        Solid_E = new double[3] { 1,1,0},
                        Solid_F = new double[3] { 1,1,0},
                        VibrationOneTime =new int[] {30,30,30 },
                        VibrationOneVel =new int[] { 400,400,400},
                        VortexTime = new int[] { 40,40, 40 },
                        VortexVel = new int[] { 1000, 1000, 1000 },
                        Tech = 0xF43EE00,
                        TechStep = 3
                    } };
                    _workList.Add(sample);

                    Sample sample1 = new Sample()
                    {
                        Id = (ushort)i++,
                        Status = 1,
                        TechParams = new TechParams()
                        {
                            AddWater = 8,
                            Solvent_A = 3,
                            Solvent_B = 7,
                            Solvent_C = 6,
                            AddHomo = new double[3] { 0, 1, 0 },
                            Solid_B = new double[3] { 1, 0, 0 },
                            Solid_C = new double[3] { 0, 1, 0 },
                            Solid_D = new double[3] { 0, 0, 1 },
                            Solid_E = new double[3] { 1, 0, 0 },
                            Solid_F = new double[3] { 0, 1, 0 },
                            VibrationOneTime = new int[] { 30, 30, 30 },
                            VibrationOneVel = new int[] { 400, 400, 400 },
                            VortexTime = new int[] { 40, 40, 40 },
                            VortexVel = new int[] { 1000, 1000, 1000 },
                            Tech = 0xF43EE19,
                            TechStep =1
                        }
                    };
                    _workList.Add(sample1);
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
                while (_workList.Count > 0 && cts?.IsCancellationRequested != true)
                {
                    var result = Ext(_workList[0]).GetAwaiter().GetResult();
                    if (!result)
                    {
                        return;
                    }

                    //var ret = _capperOne.Extract(_workList[0], ExtractCallBack, _workList, cts);
                    //if (!ret)
                    //{
                    //    return;
                    //}

                }
            
                
               
            });
        }

        public void StopPro()
        {
            MySerialization.SerializeToXml<List<Sample>>(_sampleFile, _workList);
            cts?.Cancel();
            Task.Run(() =>
            {

            }).ConfigureAwait(false);
        }

        public void ContinuePro()
        {
            if (_workList == null)
            {
                _workList = MySerialization.DeserializeFromXml<List<Sample>>(_sampleFile);
            }
               

            cts = new CancellationTokenSource();
            Task.Run(() =>
            {
            }).ConfigureAwait(false);
        }




        #endregion





        #region Protected Methods

        protected async Task<bool> Ext(Sample sample)
        {

            //加水提取
            if (sample.TechParams.TechStep ==1)
            {
                var ret = await _capperOne.AddWaterExtract(sample, cts).ConfigureAwait(false);
                if (!ret)
                {
                    //停止所有任务 
                    cts.Cancel();
                    return false;
                }
                //把样品加入到振荡涡旋列表  并启动程序
                sample.VibrationAndVortexStep = 1;
                _vibrationOne.StartVibrationAndVortex(sample, AddSolve,cts);
                //Thread.Sleep(500);
            }

            if (sample.TechParams.TechStep == 2)
            {
                var result = await _capperOne.AddSolveExtract(sample, cts).ConfigureAwait(false);
                if (!result)
                {
                    cts.Cancel();
                    Console.WriteLine("加液提取出错");
                    return false;
                }
                TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddSolve1);

                //把样品加入到振荡涡旋列表  并启动程序
                sample.VibrationAndVortexStep = 2;
                _vibrationOne.StartVibrationAndVortex(sample, AddSalt,cts);
                //Thread.Sleep(500);
            }

            if (sample.TechParams.TechStep == 3)
            {
                var result = await _capperOne.AddSaltExtract(sample, cts).ConfigureAwait(false);
                if (!result)
                {
                    cts.Cancel();
                    Console.WriteLine("加盐提取出错");
                    return false;
                }
                TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddSolve1);

                //把样品加入到振荡涡旋列表  并启动程序
                sample.VibrationAndVortexStep = 3;
                _vibrationOne.StartVibrationAndVortex(sample, Centrifugal,cts);
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
        protected void AddSolve(Sample sample, CancellationTokenSource cts)
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
                sample.TechParams.TechStep = 2;
            });
          
        }

        /// <summary>
        /// 加盐提取
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="workList">任务列表</param>
        protected void AddSalt(Sample sample, CancellationTokenSource cts)
        {
            _workList.Insert(1, sample);
            sample.TechParams.TechStep = 3;
        }


        //==============================================================================================================//




        //离心 
        public void Centrifugal(Sample sample, CancellationTokenSource cts)
        {
            _centrifugal.StartCentrifugal(sample, CentrifugalCallBack, cts);

        }


        //取上清液+ 振荡2  +  加入离心列表   离心回调
        public void CentrifugalCallBack(Sample sample, CancellationTokenSource cts)
        {
            if (sample.TechParams.TechStep == 5)
            {
                //取上清液

            }
            else if (sample.TechParams.TechStep == 8)
            {
                //提取净化液

            }
            else if (sample.TechParams.TechStep == 11)
            {
                //提取浓缩液

            }
        }


        //提取净化液
        public void Extract(Sample sample)
        {

        }




        #endregion







    }
}
