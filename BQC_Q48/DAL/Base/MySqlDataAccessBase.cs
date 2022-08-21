using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Data;

namespace BQJX.DAL.Base
{
    public class MySqlDataAccessBase : IDataAccessBase
    {
        #region Private Members

        private readonly string _connStr;
        private MySqlConnection _sqlConnection;
        private MySqlDataAdapter _dataAdapter;
        private MySqlCommand _sqlCommand;

        #endregion
        public MySqlDataAccessBase(string connStr)
        {
            this._connStr = connStr;
        }

        /// <summary>
        /// 增删改
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string sql, Dictionary<string, object> param = null)
        {
            try
            {
                this.Open();
                _sqlCommand = new MySqlCommand(sql, _sqlConnection);
                if (param != null)
                {
                    foreach (var item in param)
                    {
                        _sqlCommand.Parameters.Add(new MySqlParameter(item.Key, DbType.String){Value = item.Value });
                    }
                }
                int ret = _sqlCommand.ExecuteNonQuery();
                return ret;  
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                this.Dispose();
            }
        }

        /// <summary>
        /// 查
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public DataTable Query(string sql, Dictionary<string, object> param = null)
        {
            try
            {
                this.Open();
                _dataAdapter = new MySqlDataAdapter(sql, _sqlConnection);
                if (param != null)
                {
                    List<MySqlParameter> parameters = new List<MySqlParameter>();
                    foreach (var item in param)
                    {
                        parameters.Add(new MySqlParameter(item.Key, DbType.String) { Value = item.Value });
                        //_dataAdapter.SelectCommand.Parameters.Add(new MySqlParameter(item.Key, DbType.String) { Value = item.Value });
                    }
                    _dataAdapter.SelectCommand.Parameters.AddRange(parameters.ToArray());
                }
                DataTable dt = new DataTable();
                int row =  _dataAdapter.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                this.Dispose();
            }
        }

        /// <summary>
        /// 打开数据连接
        /// </summary>
        private void Open()
        {
            if (_sqlConnection == null)
            {
                _sqlConnection = new MySqlConnection(_connStr);
            }
            if (_sqlConnection.State != System.Data.ConnectionState.Open)
            {
                _sqlConnection.Open();
            }
   
        }

        /// <summary>
        /// 释放数据库资源
        /// </summary>
        private void Dispose()
        {
            _dataAdapter?.Dispose();
            _dataAdapter = null;
            _sqlCommand?.Dispose();
            _sqlCommand = null;
            _sqlConnection?.Close();
            _sqlConnection?.Dispose();
            _sqlConnection = null;

        }

    }

}
