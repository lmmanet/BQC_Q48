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
    public class AddSolidPosDataAccess : IAddSolidPosDataAccess
    {
        #region Private Members

        private IDataAccessBase _dataAccess;
        private ILogger _logger;

        #endregion

        #region Construtors

        public AddSolidPosDataAccess(IDataAccessBase dataAccess, ILogger logger)
        {
            this._dataAccess = dataAccess;
            this._logger = logger;
        }

        #endregion

        #region Public Methods

        public AddSolidPosData GetPosData()
        {
            AddSolidPosData data = new AddSolidPosData();
            try
            {
                string sql = "Select * from AddSolidPosData order by id limit 0,2 ";
                DataTable dt = _dataAccess.Query(sql);
                int i = 0;
                foreach (var item in dt.AsEnumerable())
                {
                    data.Solid_A[i] = item.Field<double>("Solid_A");
                    data.Solid_B[i] = item.Field<double>("Solid_B");
                    data.Solid_C[i] = item.Field<double>("Solid_C");
                    data.Solid_D[i] = item.Field<double>("Solid_D");
                    data.Solid_E[i] = item.Field<double>("Solid_E");
                    data.Solid_F[i] = item.Field<double>("Solid_F");

                    i++;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"GetPosData err:{ex.Message}");
                return null;
            }
            return data;
        }

        public bool UpdatePosData(AddSolidPosData data)
        {

            try
            {
                string sql = "";
                for (int i = 0; i < 2; i++)
                {
                    string header = "update AddSolidPosData set ";
                    string param = $"Solid_A = '{data.Solid_A[i]}'," +
                   $"Solid_B = '{data.Solid_B[i]}'," +
                   $"Solid_C = '{data.Solid_C[i]}'," +
                   $"Solid_D = '{data.Solid_D[i]}'," +
                   $"Solid_E = '{data.Solid_E[i]}'," +
                   $"Solid_F='{data.Solid_F[i]}'";
                    sql += header + param + $" where id = {i};";
                }

                return _dataAccess.ExecuteNonQuery(sql) == 2;

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
                string sql = $"update AddSolidPosData set {posInfo.MemberName} = '{posInfo.PosData}' where id = {id};";

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
                string header = "update AddSolidPosData set ";
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
