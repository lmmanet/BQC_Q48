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



        private Sample sample = new Sample() { Id = 1,Status=256, TechParams = new TechParams() { Solvent_A = 10, Solid_B = 10, Solvent_C = 10 } };  //测试离心搬运的
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

               // var result7 = _centrifugal.GoHome(cts); // 离心机回零
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

        List<Sample> list;

        public void StartPro()
        {
            if (list == null)
            {
                list = new List<Sample>();
                for (int i = 1; i < 49; i++)
                {
                    Sample sample = new Sample() { Id = (ushort)i, Status = 1, TechParams = new TechParams() { Solvent_A = 10, Solid_B = 10, Solvent_C = 10, Tech = 0x01e0 } };
                    list.Add(sample);
                }
            }
      

            Task.Run(async () =>
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var result = await AddSolveExtract(list[i],cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return;
                    }
                }

            }).ConfigureAwait(false);
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



        /// <summary>
        /// 加水提取
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> AddWaterExtract(Sample sample, CancellationTokenSource cts)
        {
            //判断是否有加水工艺
            if ((sample.TechParams.Tech & 0x04) == 0)
            {
                return true;
            }

            try
            {

                bool result;

                //加水  //加溶剂A  
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddWater))
                {
                    result = await _capperOne.AddSolve(sample, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddWater);
                }

                //加固
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddHomo))
                {
                    result = await _capperOne.MovePutGetPos(cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    result = await _addSolid.AddSolidAsync(sample,new int[] {1 },new double[] { 1 } , cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddHomo);
                }

                //装盖 内部判断在拧盖1就执行
                result = await _capperOne.MoveOut(sample, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //是否振荡  跳过

                //判断是否涡旋
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Vortex))
                {
                    result = await _vortex.StartVortexAsync(sample, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.Vortex);
                }

                //判断试管是否下料到试管架


                //下料到试管架
                result = _carrierOne.GetSampleToMaterial(sample, cts);
                if (!result)
                {
                    return false;
                }

                //完成
                return true;

            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    Console.WriteLine("触发停止");
                    return false;
                }
                Console.WriteLine(ex.Message);
                throw;
            }







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
        /// 加溶剂提取
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> AddSolveExtract(Sample sample, CancellationTokenSource cts)
        {
            //判断是否有加溶剂工艺
            if ((sample.TechParams.Tech & 0x01e0) == 0)
            {
                return true;
            }

            try
            {

                bool result;

                //加溶剂 
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSolve1))
                {
                    result = await _capperOne.AddSolve(sample, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddSolve1);
                }

                //加盐
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSalt1))
                {
                    result = await _capperOne.MovePutGetPos(cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }

                    result = await _addSolid.AddSolidAsync(sample, new int[] { 1 }, new double[] { 1 }, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddSalt1);
                }

                //装盖 内部判断在拧盖1就执行
                result = await _capperOne.MoveOut(sample, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //是否振荡  跳过
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration1))
                {
                    result = await _vibrationOne.StartVibrationAsync(sample, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.ExtractVibration1);
                }

                //判断是否涡旋  +   下料到试管架
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex1))
                {
                    result = await _vortex.StartVortexAsync(sample, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.ExtractVortex1);
                }

             

                //完成
                return true;

            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    Console.WriteLine("触发停止");
                    return false;
                }
                Console.WriteLine(ex.Message);
                throw;
            }


        }

        /// <summary>
        /// 加盐提取
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> AddSaltExtract(Sample sample, CancellationTokenSource cts)
        {
            //判断是否有加盐工艺
            if ((sample.TechParams.Tech & 0x1e00) == 0)
            {
                return true;
            }

            try
            {

                bool result;

                //加溶剂 
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSolve2))
                {
                    result = await _capperOne.AddSolve(sample, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddSolve2);
                }

                //加盐
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSalt2))
                {
                    result = await _addSolid.AddSolidAsync(sample, new int[] { 1 }, new double[] { 1 }, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddSalt2);
                }

                //装盖 内部判断在拧盖1就执行
                result = await _capperOne.MoveOut(sample, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //是否振荡  跳过
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration2))
                {
                    result = await _vibrationOne.StartVibrationAsync(sample, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.ExtractVibration2);
                }

                //判断是否涡旋
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex2))
                {
                    result = await _vortex.StartVortexAsync(sample, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.ExtractVortex2);
                }

                //判断试管是否下料到试管架


                //下料到试管架
                result = _carrierOne.GetSampleToMaterial(sample, cts);
                if (!result)
                {
                    return false;
                }

                //完成
                return true;

            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    Console.WriteLine("触发停止");
                    return false;
                }
                Console.WriteLine(ex.Message);
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
            try
            {
                bool result;
                //离心
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.Centrifugal1))
                {
                    result = await _capperOne.AddSolve(sample, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.Centrifugal1);
                }

                //加盐
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.AddSalt2))
                {
                    result = await _addSolid.AddSolidAsync(sample, new int[] { 1 }, new double[] { 1 }, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.AddSalt2);
                }

                //装盖 内部判断在拧盖1就执行
                result = await _capperOne.MoveOut(sample, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //是否振荡  跳过
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVibration2))
                {
                    result = await _vibrationOne.StartVibrationAsync(sample, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.ExtractVibration2);
                }

                //判断是否涡旋
                if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.ExtractVortex2))
                {
                    result = await _vortex.StartVortexAsync(sample, cts).ConfigureAwait(false);
                    if (!result)
                    {
                        return false;
                    }
                    TechStatusHelper.ResetBit(sample.TechParams, TechStatus.ExtractVortex2);
                }

                //判断试管是否下料到试管架


                //下料到试管架
                result = _carrierOne.GetSampleToMaterial(sample, cts);
                if (!result)
                {
                    return false;
                }

                //完成
                return true;

            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    Console.WriteLine("触发停止");
                    return false;
                }
                Console.WriteLine(ex.Message);
                throw;
            }
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



    }
}
