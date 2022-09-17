using BQJX.Common.Common;
using BQJX.Core.Interface;
using BQJX.DAL.Base;
using Q_Platform.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
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
                    data.VibrationOnePos[i] = item.Field<double>("VibrationOnePos");
                    data.TransferLeftPos[i] = item.Field<double>("TransferLeftPos");
                    data.NeedlePos[i] = item.Field<double>("NeedlePos");
                    data.PipettingSourcePos[i] = item.Field<double>("PipettingSourcePos");
                    data.PipettingTargetPos[i] = item.Field<double>("PipettingTargetPos");     
                    data.PipettingSourcePos2[i] = item.Field<double>("PipettingSourcePos2");
                    i++;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"GetPosData err:{ex.Message}");
                throw ex;
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
                   $"VibrationOnePos ='{data.VibrationOnePos[i]}'," +
                   $"TransferLeftPos ='{data.TransferLeftPos[i]}'," +
                   $"NeedlePos ='{data.NeedlePos[i]}'," +
                   $"PipettingSourcePos='{data.PipettingSourcePos[i]}'," + 
                   $"PipettingTargetPos='{data.PipettingTargetPos[i]}'," +
                   $"PipettingSourcePos2='{data.PipettingSourcePos2[i]}'";
                    sql += header + param + $" where id = {i};";
                }

                return _dataAccess.ExecuteNonQuery(sql) == 3;
                
            }
            catch (Exception ex)
            {
                _logger?.Error($"UpdatePosData err:{ex.Message}");
                throw ex;
            }
        }


        public List<AxisPosInfo> GetAxisPosInfo(ushort id)
        {
            var result = new List<AxisPosInfo>();
            Type type = typeof(CarrierOnePosData);
            foreach (var item in type.GetProperties())
            {
                string name = item.Name;
                if (item.IsDefined(typeof(PosNameAttribute)))
                {
                    var posNameAtt = item.GetCustomAttribute(typeof(PosNameAttribute)) as PosNameAttribute;
                    name = posNameAtt.PosName;
                }

                result.Add(new AxisPosInfo()
                {
                    MemberName = item.Name,
                    PosName = name,
                    AxisNo = 0,
                    AxisName = ""

                }) ;
            }

            try
            {
                string sql = $"Select * from carrieroneposdata where id = {id} ";
                DataTable dt = _dataAccess.Query(sql);
                foreach (var item in dt.AsEnumerable())
                {
                    foreach (var axisInfo in result)
                    {
                        axisInfo.PosData = item.Field<double>($"{axisInfo.MemberName}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"GetAxisPosInfo err:{ex.Message}");
                throw ex;
            }

            return result;

        }

        /// <summary>
        /// 更新单个数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="posInfo"></param>
        /// <returns></returns>
        public bool UpdatePosDataByAxisPosInfo(ushort id, AxisPosInfo posInfo)
        {
            try
            {
                string sql = $"update carrieroneposdata set {posInfo.MemberName} = '{posInfo.PosData}' where id = {id};";
                
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
                string header = "update carrieroneposdata set ";
                string body = string.Join(",",list.Select(info => $"{info.MemberName} = '{info.PosData}'"));
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
