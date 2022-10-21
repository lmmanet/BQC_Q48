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
    public class VortexPosDataAccess : IVortexPosDataAccess
    {
        #region Private Members

        private IDataAccessBase _dataAccess;
        private ILogger _logger;

        #endregion

        #region Construtors

        public VortexPosDataAccess(IDataAccessBase dataAccess, ILogger logger)
        {
            this._dataAccess = dataAccess;
            this._logger = logger;
        }

        #endregion

        #region Public Methods

        public VortexPosData GetPosData()
        {
            VortexPosData data = new VortexPosData();
            try
            {
                string sql = "Select * from VortexPosData where id = 1 ";
                DataTable dt = _dataAccess.Query(sql);
                foreach (var item in dt.AsEnumerable())
                {
                    data.PutGetPos = item.Field<double>("PutGetPos");
                    data.VortexPos = item.Field<double>("VortexPos");

                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"GetPosData err:{ex.Message}");
                return null;
            }
            return data;
        }

        public bool UpdatePosData(VortexPosData data)
        {

            try
            {
                string sql = "";
                for (int i = 0; i < 2; i++)
                {
                    string header = "update VortexPosData set ";
                    string param = $"PutGetPos = '{data.PutGetPos}'," +
                   $"VortexPos='{data.VortexPos}'";
                    sql += header + param + " where id = 1;";
                }

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
                string sql = $"update VortexPosData set {posInfo.MemberName} = '{posInfo.PosData}' where id = {id};";

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
                string header = "update VortexPosData set ";
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
