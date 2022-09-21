using BQJX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class MainPro : IMainPro
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        Task _taskHome;

        Task _main;
        //任务线程
        Task _addWaterExtTask;        //加水提取
        Task _vibrationOnevortexTask; //振荡1 涡旋
        Task _wetBackTask;            //回湿
        Task _centrifugalTask;        //离心   （离心搬运分离出来）
        Task _pipttorTask;            //移液提取 （包括离心搬运 拧盖2 拧盖3）
        Task _vibrationTwoTask;       //提取净化液 + 振荡2
   
        Task _extSample;              //提取样品液
        //任务列表
        List<Sample> _extList = new List<Sample>();
        List<Sample> _vibrationOnevortexList = new List<Sample>();  //振荡涡旋列表
        Dictionary<Sample, Action<Sample>> _sampleActionDic = new Dictionary<Sample, Action<Sample>>();
        List<Sample> _wetBackList = new List<Sample>();
        List<Sample> _addSolveExtList = new List<Sample>();
        List<Sample> _addSolidExtList = new List<Sample>();
        List<Sample> _centrifugalList = new List<Sample>();
        List<Sample> _pipttorList = new List<Sample>();
        List<Sample> _vibrationTwoList = new List<Sample>();
        List<Sample> _extSampleList = new List<Sample>();

        List<Sample> _workList;


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



        private Sample sample1 = new Sample() { Id = 1,Status=256, TechParams = new TechParams() { Solvent_A = 10, Solid_B = 10, Solvent_C = 10 } };  //测试离心搬运的
        private Sample sample2 = new Sample() { Id = 1,Status=256, TechParams = new TechParams() { Solvent_A = 10, Solid_B = 10, Solvent_C = 10 } };


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

                //var result7 = _centrifugal.GoHome(cts); // 离心机回零
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
                //if (!result7.GetAwaiter().GetResult())
                //{
                //    return;
                //}
                Console.WriteLine("HomeDone");
            });
        }

       

        public void StartPro()
        {
            if (_workList == null)
            {
                _workList = new List<Sample>();
                for (int i = 1; i < 49; i++)
                {
                    Sample sample = new Sample() { Id = (ushort)i, Status = 1, TechParams = new TechParams() { Solvent_A = 5, Tech = 0xFFFFFFF } };
                    _workList.Add(sample);
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
                for (int i = 0; i < _workList.Count; i++)
                {
                    if (cts.IsCancellationRequested == true)
                    {
                        return;
                    }
                    var result = Ext(_workList[i]).GetAwaiter().GetResult();
                    if (!result)
                    {
                        return;
                    }


                }
            });
        }

        public void StopPro()
        {
            cts?.Cancel();
            Task.Run(() =>
            {

            }).ConfigureAwait(false);
        }

        public void ContinuePro()
        {
            cts = new CancellationTokenSource();
            Task.Run(() =>
            {
            }).ConfigureAwait(false);
        }




        #endregion





        #region Protected Methods

        public async Task<bool> Ext(Sample sample)
        {
            while ( _addSolidExtList.Count > 0 && cts?.IsCancellationRequested != true) //加固提取
            {
                var itemSample = _addSolidExtList[0];

                var result = await _capperOne.AddSaltExtract(itemSample, cts).ConfigureAwait(false);
                if (!result)
                {
                    cts.Cancel();
                    Console.WriteLine("加盐提取出错");
                    break;
                }
                TechStatusHelper.ResetBit(itemSample.TechParams, TechStatus.AddSolve1);

                //把样品加入到振荡涡旋列表  并启动程序

                VibrationAndVortex(itemSample,3, Centrifugal, true);

                _addSolidExtList.Remove(itemSample);
            }

            while (_addSolveExtList.Count > 0 && cts?.IsCancellationRequested != true)//加液提取
            {
                var itemSample = _addSolveExtList[0];

                var result = await _capperOne.AddSolveExtract(itemSample, cts).ConfigureAwait(false);
                if (!result)
                {
                    cts.Cancel();
                    Console.WriteLine("加液提取出错");
                    break;
                }
                TechStatusHelper.ResetBit(itemSample.TechParams, TechStatus.AddSolve1);

                //把样品加入到振荡涡旋列表  并启动程序

                VibrationAndVortex(itemSample,2, AddSalt, false);

                _addSolveExtList.Remove(itemSample);
            }

            //加水提取
            var ret = await _capperOne.AddWaterExtract(sample, cts).ConfigureAwait(false);
            if (!ret)
            {
                //停止所有任务 
                cts.Cancel();
                return false;
            }
            //把样品加入到振荡涡旋列表  并启动程序

            VibrationAndVortex(sample, 1,AddSolve, false);

            return true;
        }



       
        /// <summary>
        /// 振荡涡旋提取
        /// </summary>
        /// <param name="sample"></param>
        public void VibrationAndVortex(Sample sample,int step,Action<Sample> action,bool carryToCold)
        {
            var ret =_vibrationOnevortexList.Contains(sample);
            if (!ret)
            {
                _vibrationOnevortexList.Add(sample);
                _vibrationOnevortexList.Distinct();

                _sampleActionDic.Add(sample, action);
                _sampleActionDic.Distinct();
            }
          

            if (_vibrationOnevortexTask != null)
            {
                if (!_vibrationOnevortexTask.IsCompleted )
                {
                    return;
                }
            }

            _vibrationOnevortexTask = Task.Run(() =>
            {
                while (cts?.IsCancellationRequested != true)
                {
                    var itemSample = _vibrationOnevortexList[0];
                    int techStatus = 0;
                    switch (step)
                    { 
                        case 1:
                            techStatus = 2;
                            break;
                        case 2:
                            techStatus = 7;
                            break;
                        case 3:
                            techStatus = 11;
                            break;
                        case 4:
                            techStatus = 19;
                            break;
                        default:
                            throw new Exception("振荡步骤错误!");
                    }
                    try
                    {
                        //是否振荡  跳过
                        if (TechStatusHelper.BitIsOn(itemSample.TechParams, (TechStatus)techStatus))
                        {
                            if (cts.IsCancellationRequested != true)
                            {
                                var result = _vibrationOne.StartVibration(itemSample, carryToCold, cts);
                                if (!result)
                                {
                                    throw new Exception("StartVibration err");
                                }
                                TechStatusHelper.ResetBit(itemSample.TechParams, (TechStatus)techStatus);
                            }
                            else
                            {
                                throw new TaskCanceledException("程序停止");
                            }
                        
                        }

                        //判断是否涡旋
                        if (TechStatusHelper.BitIsOn(itemSample.TechParams, (TechStatus)(techStatus+1)))
                        {
                            if (cts.IsCancellationRequested != true)
                            {
                                var result = _vortex.StartVortex(itemSample, cts);
                                if (!result)
                                {
                                    throw new Exception("StartVortex err");
                                }
                                TechStatusHelper.ResetBit(itemSample.TechParams, (TechStatus)(techStatus + 1));
                            }
                            else
                            {
                                throw new TaskCanceledException("程序停止");
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        cts.Cancel();
                        return;
                    }

                    _sampleActionDic[itemSample]?.Invoke(itemSample);
                    

                    _vibrationOnevortexList.Remove(itemSample);
                    _sampleActionDic.Remove(itemSample);

                    if (_vibrationOnevortexList.Count == 0)
                    {
                        return;
                    }
                }
            });

        }


        //==============================================================================================================//

  
        /// <summary>
        /// 加溶剂提取
        /// </summary>
        /// <param name="sample"></param>
        public void AddSolve(Sample sample)
        {
            _addSolveExtList.Add(sample);
            //去重复
            _addSolveExtList.Distinct();

        }

        /// <summary>
        /// 加盐提取
        /// </summary>
        /// <param name="sample"></param>
        public void AddSalt(Sample sample)
        {
            _addSolidExtList.Add(sample);
            _addSolidExtList.Distinct();

        }

        //离心 提取上清液或提取净化液
        public void Centrifugal(Sample sample)
        {
            Console.WriteLine($"离心{sample.Id}样品");
        }


        //取上清液  +  加入离心列表
        public void Pipettor(Sample sample)
        {

        }


        //提取净化液
        public void Extract(Sample sample)
        {

        }



        /// <summary>
        /// 回湿
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> WetBack(Sample sample,CancellationTokenSource cts)
        {
            try
            {
                bool result;

                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.WetBack))
                {
                    await Task.Delay(100000).ConfigureAwait(false);
                    //result = 
                    //if (!result)
                    //{
                    //    return false;
                    //}
                    //时间到把样品加入到下一工序


                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.WetBack);
                }
                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }
            


        /// <summary>
        /// 一次离心
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> CentrifugalOne(Sample sample,CancellationTokenSource cts)
        {
            throw new NotImplementedException();
           
        }




        #endregion


        ///////离心机取料程序
        //while (true)
        //{
        //    var result = await (_centrifugal as Centrifugal).GetTubeIn(sample, false, cts);
        //    if (!result)
        //    {
        //        return;
        //    }

        //    result = await (_centrifugal as Centrifugal).GetTubeIn(sample2, true, cts);
        //    if (!result)
        //    {
        //        return;
        //    }

        //    result = await (_centrifugal as Centrifugal).GetTubeOut(sample, false, cts);
        //    if (!result)
        //    {
        //        return;
        //    }


        //    result = await (_centrifugal as Centrifugal).GetTubeOut(sample2, true, cts);
        //    if (!result)
        //    {
        //        return;
        //    }
        //}

        //==================================多线程测试===============================================================//




        ////加液完成后  进入循环
        //while (true)
        //{
        //    //加液提取
        //    if (_addSolveExtList.Count > 0)
        //    {
        //        var itemSample = _addSolveExtList[0];

        //        var result = await AddSolveExtract(itemSample, cts).ConfigureAwait(false);
        //        if (!result)
        //        {
        //            throw new Exception();
        //        }
        //        TechStatusHelper.ResetBit(itemSample.TechParams, TechStatus.AddSolve1);

        //        //把样品加入到振荡涡旋列表  并启动程序

        //        VibrationAndVortex(itemSample, AddSalt, false);

        //        _addSolveExtList.Remove(itemSample);
        //        if (_addSolveExtList.Count == 0)
        //        {
        //            break;
        //        }
        //    }

        //    //加固提取
        //    if (_addSolidExtList.Count > 0)
        //    {
        //        var itemSample = _addSolidExtList[0];

        //        var result = await AddSaltExtract(itemSample, cts).ConfigureAwait(false);
        //        if (!result)
        //        {
        //            throw new Exception();
        //        }
        //        TechStatusHelper.ResetBit(itemSample.TechParams, TechStatus.AddSolve1);

        //        //把样品加入到振荡涡旋列表  并启动程序

        //        VibrationAndVortex(itemSample, Centrifugal, true);

        //        _addSolidExtList.Remove(itemSample);
        //        if (_addSolidExtList.Count == 0)
        //        {
        //            break;
        //        }
        //    }

        //    //列表不存在任务
        //    if (_addSolveExtList.Count<1 && _addSolidExtList.Count <1)
        //    {
        //        break;
        //    }
        //}









    }
}
