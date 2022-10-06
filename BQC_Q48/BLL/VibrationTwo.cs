using BQJX.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class VibrationTwo : VibrationBase ,IVibrationTwo
    {
        private static ILogger logger = new MyLogger(typeof(VibrationTwo));

        private readonly static object _lockObj = new object();

        private ICarrierTwo _carrier;

        #region Construtors


        public VibrationTwo(IEtherCATMotion motion, IIoDevice io, IGlobalStatus globalStauts, ICarrierTwo carrier) : base(motion, io, globalStauts, logger)
        {
            this._carrier = carrier;
            _axisNo = 13;
            _holding = 40;
            _holdingOpenSensor = 41; //原位
            _holdingCloseSensor = 40; //到位
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// 提取完上清液振荡
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool StartVibrationOne(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;

            double vel = sample.TechParams.VibrationTwoVel[0] / 60;
            int time = sample.TechParams.VibrationTwoTime[0];

            if (!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.PurifyVibration) && SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
            {
                return true;
            }

            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"样品{sampleId}净化管开始振荡-{time}s-{vel}rpm");
                    bool result;
                    //振荡回零
                    if (sample.SubStep == 18 && !_globalStatus.IsStopped)
                    {
                        result = GoHome(cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("振荡回零失败!");
                        }
                        sample.SubStep++;
                    }

                    //搬运  从拧盖3搬运净化管到振荡
                    if (sample.SubStep == 19 && !_globalStatus.IsStopped)
                    {
                        if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInVibration))
                        {
                            result = _carrier.GetSampleFromCapperThreeToVibration(sample, cts);
                            if (!result)
                            {
                                throw new Exception("搬运样品到振荡失败!");
                            }
                        }
                        sample.SubStep++;
                    }

                    //开始振荡
                    if (sample.SubStep == 20 && !_globalStatus.IsStopped)
                    {
                        if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.PurifyVibration))
                        {
                            result = base.StartVibration(time, vel, cts).GetAwaiter().GetResult();
                            if (!result)
                            {
                                throw new Exception("样品振荡失败!");
                            }
                        }
                        sample.SubStep++;
                    }

                    //搬运净化管到试管架
                    if (sample.SubStep == 21 && !_globalStatus.IsStopped)
                    {
                        if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                        {
                            result = _carrier.GetSampleFromVibrationToMaterial(sample, cts);
                            if (!result)
                            {
                                throw new Exception("搬运样品到试管架失败!");
                            }
                        }
                        sample.SubStep++;
                    }


                    //完成
                    if (sample.SubStep == 22 && !_globalStatus.IsStopped)
                    {
                        if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
                        {
                            return true;
                        }
                    }
                    throw new Exception("从拧盖3搬运净化管到振荡 失败!");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"样品{sampleId}净化管开始振荡-{time}s-{vel}rpm 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 提取上清液前振荡  兽药 加入醋酸铵水溶液后
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public bool StartVibrationTwo(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;

            double vel = sample.TechParams.VibrationTwoVel[1] / 60;
            int time = sample.TechParams.VibrationTwoTime[1];

            if (!TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.VibrationBeforePurify) && !SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInShelf))
            {
                return true;
            }

            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"样品{sampleId}油脂管开始振荡-{time}s-{vel}rpm");

                    //振荡回零
                    var result = GoHome(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("振荡回零失败!");
                    }

                    //搬运  从拧盖3搬运净化管到振荡
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInVibration))
                    {
                        result = _carrier.GetSampleFromCapperThreeToVibration(sample, cts);
                        if (!result)
                        {
                            throw new Exception("搬运样品到振荡失败!");
                        }
                    }

                    //开始振荡
                    if (TechStatusHelper.BitIsOn(sample.TechParams, TechStatus.VibrationBeforePurify))
                    {
                        result = base.StartVibration(time, vel, cts).GetAwaiter().GetResult();
                        if (!result)
                        {
                            throw new Exception("样品振荡失败!");
                        }
                    }


                    //搬运净化管到拧盖3
                    if (!SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        result = _carrier.GetSampleFromVibrationToCapperThree(sample, cts);
                        if (!result)
                        {
                            throw new Exception("搬运样品到拧盖3失败!");
                        }
                    }


                    //完成
                    if (SampleStatusHelper.BitIsOn(sample, SampleStatus.IsPurfyInCapper))
                    {
                        return true;
                    }
                    throw new Exception("从拧盖3搬运油脂管到振荡 失败!");
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"样品{sampleId}油脂开始振荡-{time}s-{vel}rpm 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        } 



        #endregion

    }
}
