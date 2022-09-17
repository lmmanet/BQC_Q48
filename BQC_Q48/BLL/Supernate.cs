using BQJX.Common.Interface;
using BQJX.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using BQJX.Common;

namespace Q_Platform.BLL
{
    public class Supernate
    {

        #region Private Members

        private readonly ICarrierOne _carrierOne;

        private readonly ICapperTwo _capperTwo;

        private readonly ICarrierTwo _carrierTwo;

        private readonly ICapperThree _capperThree;

        private readonly ICentrifugal _centrifugal;

        private readonly ILogger _logger;

        private readonly IGlobalStatus _globalStauts;

        #endregion

        #region Construtors

        public Supernate(ICarrierOne carrierOne, ICapperTwo capperTwo, ICarrierTwo carrierTwo, ICapperThree capperThree, ICentrifugal centrifugal, ILogger logger, IGlobalStatus globalStatus)
        {
            this._carrierOne = carrierOne;
            this._capperTwo = capperTwo;
            this._carrierTwo = carrierTwo;
            this._capperThree = capperThree;
            this._centrifugal = centrifugal;
            this._logger = logger;
            this._globalStauts = globalStatus;
        }

        #endregion


        /// <summary>
        /// 左侧提取上清液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> LeftExtractSupernateFromBigToSmall(Sample sample,CancellationTokenSource cts)
        {
            //拧盖2取样拆盖

            //拧盖3取样拆盖


            //搬运2（拧盖3）取试管到离心中转


            //离心中转移动到移液位

            //搬运1 开始移液
            
          






            throw new Exception();
        }


        /// <summary>
        /// 左侧提取上清液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> LeftExtractSupernateFromSmallToBig(Sample sample, CancellationTokenSource cts)
        {
            //拧盖2取样拆盖

            //拧盖3取样拆盖


            //搬运2（拧盖3）取试管到离心中转


            //离心中转移动到移液位

            //搬运1 开始移液

            //






            throw new Exception();
        }











        /// <summary>
        /// 右侧提取上清液
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> RightExtractSupernate(Sample sample, CancellationTokenSource cts)
        {
            //拧盖2取样拆盖

            //拧盖3取样拆盖


            //搬运2（拧盖3）取试管到离心中转


            //离心中转移动到移液位

            //搬运1 开始移液

            //






            throw new Exception();
        }



















    }
}
