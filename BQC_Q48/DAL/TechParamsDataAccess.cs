using BQJX.Common;
using BQJX.Core.Interface;
using BQJX.DAL.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Data;

namespace Q_Platform.DAL
{
    public class TechParamsDataAccess : ITechParamsDataAccess
    {

        #region Private Members

        private IDataAccessBase _dataAccess;
        private ILogger _logger;

        #endregion

        #region Construtors

        public TechParamsDataAccess(IDataAccessBase dataAccess, ILogger logger)
        {
            this._dataAccess = dataAccess;
            this._logger = logger;
        }

        #endregion



        public List<TechParamsInfo> FindTechParamsById(int id)
        {
            try
            {
                string sql = $"Select * from techparamsinfo where id = {id} ";
                DataTable dt = _dataAccess.Query(sql);
                return DataTableToTechParams(dt);
            }
            catch (Exception ex)
            {
                _logger?.Error($"FindTechParamsById err:{ex.Message}");
                return null;
            }

        }

        public List<TechParamsInfo> FindTechParamsByName(string techName)
        {
            try
            {
                string sql = $"Select * from techparamsinfo where name = {techName} ";
                DataTable dt = _dataAccess.Query(sql);
                return DataTableToTechParams(dt);
            }
            catch (Exception ex)
            {
                _logger?.Error($"FindTechParamsByName err:{ex.Message}");
                return null;
            }
        }

        public bool UpdateTechParams(TechParamsInfo tech, int id)
        {
            try
            {
                List<string> memberNameList = new List<string>();
                Type type = typeof(TechParamsInfo);
                PropertyInfo[] propInfos = type.GetProperties();
                foreach (var pi in propInfos)
                {
                    Type t = pi.PropertyType;
                    bool b = t.IsArray;
                    if (b)
                    {
                        var arr = (Array)pi.GetValue(tech);
                        int i = 1;
                        foreach (var item in arr)
                        {
                            memberNameList.Add($"{pi.Name}{i}= '{item}'");
                            i++;
                        }

                    }
                    else
                    {
                        memberNameList.Add($"{pi.Name} = '{pi.GetValue(tech).ToString()}'");
                    }
                }

                string header = "update techparamsinfo set ";
                string body = string.Join(",", memberNameList.Select(mem => mem));
                string sql = header + body + $" where id = {id};";

                return _dataAccess.ExecuteNonQuery(sql) == 1;
            }
            catch (Exception ex)
            {
                _logger?.Error($"UpdateTechParams err:{ex.Message}");
                return false;
            }

        }


       

        public List<TechParamsInfo> GetTechParamsInfoById(int start,int end)
        {
            try
            {
                string sql = $"Select * from techparamsinfo LIMIT {start},{end}";
                DataTable dt = _dataAccess.Query(sql);
    
                return DataTableToTechParams(dt);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                return null;
            }
        }

        public List<TechParamsInfo> GetTechParamsInfoByTechName(string name)
        {
            try
            {
                string sql = $"Select * from techparamsinfo where Name Like '{name}%'";
                DataTable dt = _dataAccess.Query(sql);

                return DataTableToTechParams(dt);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                return null;
            }
        }

        public List<TechParamsInfo> GetTechParamsInfoByTime(DateTime start, DateTime end)
        {
            try
            {
                string sql = $"Select * from techparamsinfo where CreateTime between '{start}' and '{end}'";
                DataTable dt = _dataAccess.Query(sql);
                return DataTableToTechParams(dt);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                return null;
            }
        }

        public bool InsertTechParamsInfo(TechParamsInfo tech)
        {
            try
            {
                List<string> memberNameList = new List<string>();
                List<string> valueList = new List<string>();
                Type type = typeof(TechParamsInfo);
                PropertyInfo[] propInfos = type.GetProperties();
                foreach (var pi in propInfos)
                {
                    Type t = pi.PropertyType;
                    bool b = t.IsArray;
                    if (b)
                    {
                        var arr = (Array)pi.GetValue(tech);
                        int i = 1;
                        foreach (var item in arr)
                        {
                            memberNameList.Add($"{pi.Name}{i}");
                            valueList.Add($"'{ item.ToString()}'");
                            i++;
                        }
                    }
                    else
                    {
                        memberNameList.Add($"{pi.Name}");
                        valueList.Add($"'{pi.GetValue(tech).ToString()}'");
                    }
                }

                string header = "insert into techparamsinfo ";
                string values = "(" + string.Join(",", valueList.Select(v => v)) + ")";
                string body = "(" + string.Join(",", memberNameList.Select(mem => mem)) + ")" + " Values " + values;

                string sql = header + body + $";";

                return _dataAccess.ExecuteNonQuery(sql) == 1;
            }
            catch (Exception ex)
            {
                _logger?.Error($"InsertTechParams err:{ex.Message}");
                return false;
            }
        }




        public bool DeleteTechParamsInfo(TechParamsInfo tech)
        {
            try
            {
                string sql = "DELETE FROM techparamsinfo" +
                    $" WHERE `Id` = {tech.Id}";

                return _dataAccess.ExecuteNonQuery(sql) == 1;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                return false;
            }
        }




        private List<TechParamsInfo> DataTableToTechParams(DataTable dt)
        {
            var list = new List<TechParamsInfo>();
            foreach (var item in dt.AsEnumerable())
            {
                var tech = new TechParamsInfo();
                tech.Id = item.Field<int>("Id");
                tech.Name = item.Field<string>("Name");
                tech.AddWater = item.Field<double>("AddWater");
                tech.ACE = item.Field<double>("ACE");
                tech.Acid = item.Field<double>("Acid");
                tech.Formic = item.Field<double>("Formic");
                tech.Homo = item.Field<double>("Homo");
                tech.MgSO4 = item.Field<double>("MgSO4");
                tech.NaCl = item.Field<double>("NaCl");
                tech.Trisodium = item.Field<double>("Trisodium");
                tech.Monosodium = item.Field<double>("Monosodium");
                tech.Sodium = item.Field<double>("Sodium");
                tech.VortexTime = item.Field<int>("VortexTime");
                tech.VortexVel = item.Field<int>("VortexVel");
                tech.VibrationTime = item.Field<int>("VibrationTime");
                tech.VibrationVel = item.Field<int>("VibrationVel");
                tech.CentrifugalTime = item.Field<int>("CentrifugalTime");
                tech.CentrifugalVel = item.Field<int>("CentrifugalVel");
                tech.ExtractVolume = item.Field<double>("ExtractVolume");
                tech.ConcentrationTime = item.Field<int>("ConcentrationTime");
                tech.ConcentrationVel = item.Field<int>("ConcentrationVel");
                tech.Tech = item.Field<int>("Tech");
                tech.Createtime = item.Field<DateTime>("Createtime");
                list.Add(tech);
            }
            return list;
        }







    }
}
