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
    public class CarrierOneDataAccess : ICarrierOneDataAccess
    {

        #region Private Members

        private IDataAccessBase _dataAccess;
        private ILogger _logger;

        #endregion

        #region Construtors

        public CarrierOneDataAccess(IDataAccessBase dataAccess, ILogger logger)
        {
            this._dataAccess = dataAccess;
            this._logger = logger;
        }

        #endregion


        #region Public Methods

        public CarrierOnePosData GetPosData()
        {
            CarrierOnePosData data = new CarrierOnePosData();
            try
            {
                string sql = "Select * from carrieroneposdata order by id limit 0,3 ";
                DataTable dt = _dataAccess.Query(sql);
                int i = 0;
                foreach (var item in dt.AsEnumerable())
                {
                    data.SamplePos1[i] = item.Field<double>("SamplePos1");
                    data.SamplePos2[i] = item.Field<double>("SamplePos2");
                    data.SamplePos3[i] = item.Field<double>("SamplePos3");
                    data.SamplePos4[i] = item.Field<double>("SamplePos4");
                    data.ColdPos[i] = item.Field<double>("ColdPos");
                    data.AddSolidPos[i] = item.Field<double>("AddSolidPos");
                    data.CapperOnePos[i] = item.Field<double>("CapperOnePos");
                    data.VortexPos[i] = item.Field<double>("VortexPos");
                    data.CapperTwoPos[i] = item.Field<double>("CapperTwoPos");
                    data.VibratioOnePos[i] = item.Field<double>("VibrationOnePos");
                    data.TransferLeftPos[i] = item.Field<double>("TransferLeftPos");
                    data.NeedlePos[i] = item.Field<double>("NeedlePos");
                    data.PipettingSourcePos[i] = item.Field<double>("PipettingSourcePos");
                    data.PipettingTargetPos[i] = item.Field<double>("PipettingTargetPos");
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

        public bool UpdatePosData(CarrierOnePosData data)
        {

            try
            {
                string sql = "";
                for (int i = 0; i < 3; i++)
                {
                    string header = "update carrieroneposdata set ";
                    string param = $"SamplePos1 = '{data.SamplePos1[i]}'," +
                   $"SamplePos2 = '{data.SamplePos2[i]}'," +
                   $"SamplePos3 = '{data.SamplePos3[i]}'," +
                   $"SamplePos4 = '{data.SamplePos4[i]}'," +
                   $"ColdPos = '{data.ColdPos[i]}'," +
                   $"AddSolidPos = '{data.AddSolidPos[i]}'," +
                   $"CapperOnePos = '{data.CapperOnePos[i]}'," +
                   $"VortexPos = '{data.VortexPos[i]}'," +
                   $"CapperTwoPos = '{data.CapperTwoPos[i]}'," +
                   $"VibrationOnePos ='{data.VibratioOnePos[i]}'," +
                   $"TransferLeftPos ='{data.TransferLeftPos[i]}'," +
                   $"NeedlePos ='{data.NeedlePos[i]}'," +
                   $"PipettingSourcePos='{data.PipettingSourcePos[i]}'," +
                   $"PipettingTargetPos='{data.PipettingTargetPos[i]}'";
                    sql += header + param + $" where id = {i};";
                }

                return _dataAccess.ExecuteNonQuery(sql) == 3;
                
            }
            catch (Exception ex)
            {
                _logger?.Error($"UpdatePosData err:{ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
