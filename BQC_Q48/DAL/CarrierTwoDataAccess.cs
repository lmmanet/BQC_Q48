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
    public class CarrierTwoDataAccess : ICarrierTwoDataAccess
    {

        #region Private Members

        private IDataAccessBase _dataAccess;
        private ILogger _logger;

        #endregion

        #region Construtors

        public CarrierTwoDataAccess(IDataAccessBase dataAccess, ILogger logger)
        {
            this._dataAccess = dataAccess;
            this._logger = logger;
        }

        #endregion

        public CarrierTwoPosData GetPosData()
        {
            CarrierTwoPosData data = new CarrierTwoPosData();
            try
            {
                string sql = "Select * from carriertwoposdata order by id limit 0,3 ";
                DataTable dt = _dataAccess.Query(sql);
                int i = 0;
                foreach (var item in dt.AsEnumerable())
                {
                    data.PurifyTubePos1[i] = item.Field<double>("PurifyTubePos1");
                    data.PurifyTubePos2[i] = item.Field<double>("PurifyTubePos2");
                    data.SeilingPos1[i] = item.Field<double>("SeilingPos1");
                    data.SeilingPos2[i] = item.Field<double>("SeilingPos2");
                    data.BottlePos1[i] = item.Field<double>("BottlePos1");
                    data.BottlePos2[i] = item.Field<double>("BottlePos2");
                    data.BottlePos3[i] = item.Field<double>("BottlePos3");
                    data.BottlePos4[i] = item.Field<double>("BottlePos4");
                    data.CapperThreePos[i] = item.Field<double>("CapperThreePos");
                    data.CapperFourPos[i] = item.Field<double>("CapperFourPos");
                    data.CapperFivePos[i] = item.Field<double>("CapperFivePos");
                    data.VibrationTwoPos[i] = item.Field<double>("VibrationTwoPos");
                    data.ConcentrationPos[i] = item.Field<double>("ConcentrationPos");
                    data.WeightPos[i] = item.Field<double>("WeightPos");
                    data.TransferRightPos[i] = item.Field<double>("TransferRightPos");
                    data.NeedlePos1[i] = item.Field<double>("NeedlePos1");
                    data.NeedlePos2[i] = item.Field<double>("NeedlePos2");
                    data.PipettingSourcePos[i] = item.Field<double>("PipettingSourcePos");
                    data.PipettingSourcePos1[i] = item.Field<double>("PipettingSourcePos1");
                    data.PipettingSourcePos2[i] = item.Field<double>("PipettingSourcePos2");
                    data.PipettingTargetPos1[i] = item.Field<double>("PipettingTargetPos1");
                    data.PipettingTargetPos2[i] = item.Field<double>("PipettingTargetPos2");
                    //data.PipettingTargetPos3[i] = item.Field<double>("PipettingTargetPos3");
                    data.SyringSourcePos[i] = item.Field<double>("SyringSourcePos");
                    data.SyringTargePos[i] = item.Field<double>("SyringTargePos");
                    data.SyringWashPos[i] = item.Field<double>("SyringWashPos");
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

        public bool UpdatePosData(CarrierTwoPosData data)
        {

            try
            {
                string sql = "";
                for (int i = 0; i < 3; i++)
                {
                    string header = "update carriertwoposdata set ";
                    string param = $"PurifyTubePos1 = '{data.PurifyTubePos1[i]}'," +
                   $"PurifyTubePos2 = '{data.PurifyTubePos2[i]}'," +
                   $"SeilingPos1 = '{data.SeilingPos1[i]}'," +
                   $"SeilingPos2 = '{data.SeilingPos2[i]}'," +
                   $"BottlePos1 = '{data.BottlePos1[i]}'," +
                   $"BottlePos2 = '{data.BottlePos2[i]}'," +
                   $"BottlePos3 = '{data.BottlePos3[i]}'," +
                   $"BottlePos4 = '{data.BottlePos4[i]}'," +
                   $"CapperThreePos = '{data.CapperThreePos[i]}'," +
                   $"CapperFourPos ='{data.CapperFourPos[i]}'," +
                   $"CapperFivePos ='{data.CapperFivePos[i]}'," +
                   $"VibrationTwoPos ='{data.VibrationTwoPos[i]}'," +
                   $"ConcentrationPos ='{data.ConcentrationPos[i]}'," +
                   $"WeightPos ='{data.WeightPos[i]}'," +
                   $"TransferRightPos ='{data.TransferRightPos[i]}'," +
                   $"NeedlePos1 ='{data.NeedlePos1[i]}'," +
                   $"NeedlePos2 ='{data.NeedlePos2[i]}'," +
                   $"PipettingSourcePos ='{data.PipettingSourcePos[i]}'," +
                   $"PipettingSourcePos1 ='{data.PipettingSourcePos1[i]}'," +
                   $"PipettingSourcePos2 ='{data.PipettingSourcePos2[i]}'," +
                   $"PipettingTargetPos1='{data.PipettingTargetPos1[i]}'," +
                   $"PipettingTargetPos2='{data.PipettingTargetPos2[i]}'," +
                   //$"PipettingTargetPos3='{data.PipettingTargetPos3[i]}'," +
                   $"SyringSourcePos='{data.SyringSourcePos[i]}'," +
                   $"SyringTargePos='{data.SyringTargePos[i]}'," +
                   $"SyringWashPos='{data.SyringWashPos[i]}'";
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


        public bool UpdatePosDataByAxisPosInfo(ushort id, AxisPosInfo posInfo)
        {
            try
            {
                string sql = $"update carriertwoposdata set {posInfo.MemberName} = '{posInfo.PosData}' where id = {id};";

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
                string header = "update carriertwoposdata set ";
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



    }
}
