using BQJX.Common.Common;
using BQJX.Core.Interface;
using BQJX.DAL.Base;
using Q_Platform.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.DAL
{
    public class CentrifugalCarrierPosDataAccess : ICentrifugalCarrierPosDataAccess
    {
        #region Private Members

        private IDataAccessBase _dataAccess;
        private ILogger _logger;

        #endregion

        #region Construtors

        public CentrifugalCarrierPosDataAccess(IDataAccessBase dataAccess, ILogger logger)
        {
            this._dataAccess = dataAccess;
            this._logger = logger;
        }

        #endregion

        #region Public Methods

        public CentrifugalCarrierPosData GetPosData()
        {
            CentrifugalCarrierPosData data = new CentrifugalCarrierPosData();
            try
            {
                string sql = "Select * from CentrifugalCarrierPosData where id = 1 ";
                DataTable dt = _dataAccess.Query(sql);

                foreach (var item in dt.AsEnumerable())
                {
                    data.LeftPos = item.Field<double>("LeftPos");
                    data.RightPos = item.Field<double>("RightPos");
                    data.ZGetPos = item.Field<double>("ZGetPos");
                    data.ZPutPos = item.Field<double>("ZPutPos");
                    data.CLeftPutPos1 = item.Field<double>("CLeftPutPos1");
                    data.CLeftPutPos2 = item.Field<double>("CLeftPutPos2");
                    data.CLeftPutPos3 = item.Field<double>("CLeftPutPos3");
                    data.CLeftPutPos4 = item.Field<double>("CLeftPutPos4");
                    data.CCentPos1 = item.Field<double>("CCentPos1");
                    data.CCentPos2 = item.Field<double>("CCentPos2");
                    data.XCentPos1 = item.Field<double>("XCentPos1");
                    data.XCentPos2 = item.Field<double>("XCentPos2");
                    data.CRightPos1 = item.Field<double>("CRightPos1");
                    data.CRightPos2 = item.Field<double>("CRightPos2");
                    data.CRightPos3 = item.Field<double>("CRightPos3");
                    data.CRightPos4 = item.Field<double>("CRightPos4");

                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"GetPosData err:{ex.Message}");
                return null;
            }
            return data;
        }

        public bool UpdatePosData(CentrifugalCarrierPosData data)
        {

            try
            {
                string sql = "";

                string header = "update CentrifugalCarrierPosData set ";
                string param = $"LeftPos = '{data.LeftPos}'," +
               $"RightPos = '{data.RightPos}'," +
               $"ZGetPos = '{data.ZGetPos}'," +
               $"ZPutPos = '{data.ZPutPos}'," +
               $"CLeftPutPos1 = '{data.CLeftPutPos1}'," +
               $"CLeftPutPos2 = '{data.CLeftPutPos2}'," +
               $"CLeftPutPos3 = '{data.CLeftPutPos3}'," +
               $"CLeftPutPos4 = '{data.CLeftPutPos4}'," +
               $"CCentPos1 = '{data.CCentPos1}'," +
               $"CCentPos2 = '{data.CCentPos2}'," +
               $"XCentPos1 = '{data.XCentPos1}'," +
               $"XCentPos2 = '{data.XCentPos2}'," +
               $"CRightPos1 = '{data.CRightPos1}'," +
               $"CRightPos2 = '{data.CRightPos2}'," +
               $"CRightPos3 = '{data.CRightPos3}'," +
               $"CRightPos4='{data.CRightPos4}'";
                sql += header + param + " where id = 1;";


                return _dataAccess.ExecuteNonQuery(sql) == 1;

            }
            catch (Exception ex)
            {
                _logger?.Error($"UpdatePosData err:{ex.Message}");
                return false;
            }
        }


        public bool UpdatePosDataByAxisPosInfo(ushort id, AxisPosInfo posInfo)
        {
            try
            {
                string sql = $"update CentrifugalCarrierPosData set {posInfo.MemberName} = '{posInfo.PosData}' where id = {id};";

                return _dataAccess.ExecuteNonQuery(sql) == 1;
            }
            catch (Exception ex)
            {
                _logger?.Error($"UpdatePosDataByAxisPosInfo err:{ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// 更新一行数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public bool UpdatePosDataByAxisPosInfo(ushort id, List<AxisPosInfo> list)
        {
            try
            {
                string header = "update CentrifugalCarrierPosData set ";
                string body = string.Join(",", list.Select(info => $"{info.MemberName} = '{info.PosData}'"));
                string sql = header + body + $" where id = {id};";

                return _dataAccess.ExecuteNonQuery(sql) == 1;
            }
            catch (Exception ex)
            {
                _logger?.Error($"UpdatePosDataByAxisPosInfo err:{ex.Message}");
                return false;
            }
        }



        #endregion



    }
}
