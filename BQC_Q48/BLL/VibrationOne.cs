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



        public override async Task<bool> StartVibrationAsync(Sample sample, CancellationTokenSource cts)
        {
            ushort sampleId = 1;

            double vel = 500 / 60;
            int time = 300;
            try
            {
                //振荡回零
                var result = await GoHome(cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //搬运
                result = _carrier.GetSampleToVibration(sample, cts);
                if (!result)
                {
                    return false;
                }

                //开始振荡
                result = await StartVibration(time, vel, cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //搬运下料
                //result = _carrier.GetSampleToMaterial(sample, cts);
                //if (!result)
                //{
                //    return false;
                //}
                //result = _carrier.GetSampleFromVibrationToMaterial((ushort)(2 * sampleId ), CloseHold, OpenHold, cts);
                //if (!result)
                //{
                //    return false;
                //}

                //完成

                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }









    }
}
