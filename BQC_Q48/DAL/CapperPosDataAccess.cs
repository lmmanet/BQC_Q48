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
    public class CapperPosDataAccess : ICapperPosDataAccess
    {
        #region Private Members

        private IDataAccessBase _dataAccess;
        private ILogger _logger;

        #endregion

        #region Construtors

        public CapperPosDataAccess(IDataAccessBase dataAccess, ILogger logger)
        {
            this._dataAccess = dataAccess;
            this._logger = logger;
        }

        #endregion

        #region Public Methods

        public CapperPosData GetCapperPosData(int id)
        {
            CapperPosData data = new CapperPosData();

            try
            {
                string sql = $"Select * from capperposdata where id = {id} ";
                DataTable dt = _dataAccess.Query(sql);

                foreach (var item in dt.AsEnumerable())
                {
                    data.PutGetPos = item.Field<double>("PutGetPos");
                    data.AddLiquidPos = item.Field<double>("AddLiquidPos");
                    data.CapperPos = item.Field<double>("CapperPos");
                    data.CapperPos_Z = item.Field<double>("CapperPos_Z");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"GetCapperPosData err:{ex.Message}");
                throw ex;
            }
            return data;



        }

        public bool UpdateCapperPosData(CapperPosData data, int id)
        {
            try
            {
                string sql = "";

                string header = "update capperposdata set ";
                string param = $"PutGetPos = '{data.PutGetPos}'," +
                $"AddLiquidPos = '{data.AddLiquidPos}'," +
                $"CapperPos='{data.CapperPos}'";
                sql += header + param + $" where id = {id};";


                return _dataAccess.ExecuteNonQuery(sql) == 1;

            }
            catch (Exception ex)
            {
                _logger?.Error($"UpdateCapperPosData err:{ex.Message}");
                throw ex;
            }
        }



        public bool UpdatePosDataByAxisPosInfo(ushort id, AxisPosInfo posInfo)
        {
            try
            {
                string sql = $"update capperposdata set {posInfo.MemberName} = '{posInfo.PosData}' where id = {id};";

                return _dataAccess.ExecuteNonQuery(sql) == 1;
            }
            catch (Exception ex)
            {
                _logger?.Error($"UpdatePosDataByAxisPosInfo err:{ex.Message}");
                throw ex;
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
                string header = "update capperposdata set ";
                string body = string.Join(",", list.Select(info => $"{info.MemberName} = '{info.PosData}'"));
                string sql = header + body + $" where id = {id};";

                return _dataAccess.ExecuteNonQuery(sql) == 1;
            }
            catch (Exception ex)
            {
                _logger?.Error($"UpdatePosDataByAxisPosInfo err:{ex.Message}");
                throw ex;
            }
        }


        #endregion








    }
}
