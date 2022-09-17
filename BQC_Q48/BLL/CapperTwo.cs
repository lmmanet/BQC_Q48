using BQJX.Common;
using BQJX.Common.Common;
using BQJX.Common.Interface;
using BQJX.Core.Interface;
using Q_Platform.DAL;
using Q_Platform.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class CapperTwo : CapperBase, ICapperTwo
    {

        private static ILogger logger = new MyLogger(typeof(CapperTwo));

        #region Construtors

        public CapperTwo(IIoDevice io, ILS_Motion motion, IGlobalStatus globalStatus, ICapperPosDataAccess dataAccess, ICarrierOne carrier) : base(io, motion, globalStatus, dataAccess, logger)
        {
            _axisY = 9;
            _axisC1 = 10;
            _axisC2 = 11;
            _axisZ = 13;
            _holding = 19;
            _claw = 20;

            //this._carrier = carrier;

            _xOffset = 60;    //拧盖X偏移量
            _posData = _dataAccess.GetCapperPosData(2);
        }

        #endregion



        public async Task<bool> DoPipetting(Sample sample, CancellationTokenSource cts)
        {

            ushort sampleId = 1;
            try
            {
                //移动到位
                var result = await Y_MoveToPutGet(cts).ConfigureAwait(false);
                if (!result)
                {
                    return false;
                }

                //搬运上料


                //开始拆盖


                //==============================///
                //拧盖3开始拆盖   准备移液

                //




                return true;
            }
            catch (Exception ex)
            {

                throw;
            }




        }






































    }
}
