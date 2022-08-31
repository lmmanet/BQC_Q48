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
    public class ConcentrationPosDataAccess : IConcentrationPosDataAccess
    {
        #region Private Members

        private IDataAccessBase _dataAccess;
        private ILogger _logger;

        #endregion

        #region Construtors

        public ConcentrationPosDataAccess(IDataAccessBase dataAccess, ILogger logger)
        {
            this._dataAccess = dataAccess;
            this._logger = logger;
        }

        #endregion

        #region Public Methods

        public ConcentrationPosData GetPosData()
        {
            ConcentrationPosData data = new ConcentrationPosData();
            try
            {
                string sql = "Select * from ConcentrationPosData where id = 0 ";
                DataTable dt = _dataAccess.Query(sql);

                foreach (var item in dt.AsEnumerable())
                {
                    data.PutGetPos = item.Field<double>("PutGetPos");
                    data.ConcPos = item.Field<double>("ConcPos");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"GetPosData err:{ex.Message}");
                throw ex;
            }
            return data;
        }

        public bool UpdatePosData(ConcentrationPosData data)
        {

            try
            {
                string sql = "";

                string header = "update ConcentrationPosData set ";
                string param = $"PutGetPos = '{data.PutGetPos}'," +
                $"ConcPos='{data.ConcPos}'";
                sql += header + param + " where id = 0;";

                return _dataAccess.ExecuteNonQuery(sql) == 1;

            }
            catch (Exception ex)
            {
                _logger?.Error($"UpdatePosData err:{ex.Message}");
                throw ex;
            }
        }


        public bool UpdatePosDataByAxisPosInfo(ushort id, AxisPosInfo posInfo)
        {
            try
            {
                string sql = $"update ConcentrationPosData set {posInfo.MemberName} = '{posInfo.PosData}' where id = {id};";

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
                string header = "update ConcentrationPosData set ";
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
