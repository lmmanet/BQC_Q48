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
    public class VibrationOne : VibrationBase , IVibrationOne
    {
        private static ILogger logger = new MyLogger(typeof(VibrationOne));

        private readonly static object _lockObj = new object();

        #region Private Members

        private readonly ICarrierOne _carrier;

        #endregion

        #region Construtors

        public VibrationOne(IEtherCATMotion motion, IIoDevice io, IGlobalStatus globalStauts,ICarrierOne carrier) : base(motion, io, globalStauts, logger)
        {
            _axisNo = 4;
            _holding = 14;
            _holdingOpenSensor = 16; //原位
            _holdingCloseSensor = 15; //到位

            this._carrier = carrier;
        }

        #endregion


       
        public bool StartVibration(Sample sample, bool carryToCold , CancellationTokenSource cts)
        {
            ushort sampleId = sample.Id;

            double vel = 400 / 60;
            int time = 30;
          
            try
            {
                lock (_lockObj)
                {
                    _logger?.Info($"样品{sampleId}开始振荡-{time}s-{vel}rpm");
                    //振荡回零
                    var result = GoHome(cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("振荡回零失败!");
                    }

                    //搬运
                    result = _carrier.GetSampleToVibration(sample, cts);
                    if (!result)
                    {
                        throw new Exception("搬运样品到振荡失败!");
                    }

                    //开始振荡
                    result = base.StartVibration(time, vel, cts).GetAwaiter().GetResult();
                    if (!result)
                    {
                        throw new Exception("样品振荡失败!");
                    }

                    //搬运下料     //判断是哪次振荡   下料到试管架或者冰浴
                    if (carryToCold)
                    {
                        //result = _carrier.GetSampleToCold(sample,1, cts);
                        //if (!result)
                        //{
                        //    throw new Exception("搬运样品到冰浴失败!");
                        //}
                    }
                    else
                    {
                        result = _carrier.GetSampleToMaterial(sample, cts);
                        if (!result)
                        {
                            throw new Exception("搬运样品到试管架失败!");
                        }
                    }
                   
                    //完成

                    return true;
                }
            }
            catch (Exception ex)
            {
                if (cts?.IsCancellationRequested != false)
                {
                    _logger?.Info($"样品{sampleId}开始振荡-{time}s-{vel}rpm 停止");
                    return false;
                }
                _logger?.Warn(ex.Message);
                return false;
            }
        }



    }
}
