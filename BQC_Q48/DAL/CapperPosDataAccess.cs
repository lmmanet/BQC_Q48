using BQJX.Common.Common;
using BQJX.Core.Interface;
using BQJX.DAL.Base;
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
                string sql = $"Select * from CapperPosData where id = {id} ";
                DataTable dt = _dataAccess.Query(sql);

                foreach (var item in dt.AsEnumerable())
                {
                    data.PutGetPos = item.Field<double>("PutGetPos");
                    data.AddLiquidPos = item.Field<double>("AddLiquidPos");
                    data.CapperPos = item.Field<double>("CapperPos");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"GetCapperPosData err:{ex.Message}");
                return null;
            }
            return data;



        }

        public bool UpdateCapperPosData(CapperPosData data, int id)
        {
            try
            {
                string sql = "";

                string header = "update AddSolidPosData set ";
                string param = $"PutGetPos = '{data.PutGetPos}'," +
                $"AddLiquidPos = '{data.AddLiquidPos}'," +
                $"CapperPos='{data.CapperPos}'";
                sql += header + param + $" where id = {id};";


                return _dataAccess.ExecuteNonQuery(sql) == 1;

            }
            catch (Exception ex)
            {
                _logger?.Error($"UpdateCapperPosData err:{ex.Message}");
                return false;
            }
        }

        #endregion








    }
}
