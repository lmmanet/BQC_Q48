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



        public TechParams FindTechParamsById(int id)
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

        public TechParams FindTechParamsByName(string techName)
        {
            try
            {
                string sql = $"Select * from techparamsinfo where techname = {techName} ";
                DataTable dt = _dataAccess.Query(sql);
                return DataTableToTechParams(dt);
            }
            catch (Exception ex)
            {
                _logger?.Error($"FindTechParamsByName err:{ex.Message}");
                return null;
            }
        }

        public bool UpdateTechParams(TechParams tech, int id)
        {
            try
            {
                List<string> memberNameList = new List<string>();
                Type type = typeof(TechParams);
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


        public bool InsertTechParams(TechParams tech,string techName)
        {
            try
            {
                List<string> memberNameList = new List<string>();
                List<string> valueList = new List<string>();
                Type type = typeof(TechParams);
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

                valueList.Add($"'{techName}'");
                memberNameList.Add($"techname = '{techName}'");

                string header = "insert into techparamsinfo ";
                string values = "(" + string.Join(",", valueList.Select(v => v)) +")";
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


        public List<TechParams> GetTechParamsInfoById(int start,int end)
        {
            return null;
        }

        public List<TechParams> GetTechParamsInfoByTechName(string name)
        {
            return null;
        }

        public List<TechParams> GetTechParamsInfoByTime(DateTime start, DateTime end)
        {
            return null;
        }

        public bool InsertTechParamsInfo(TechParams tech)
        {
            return false;
        }




        public bool DeleteTechParamsInfo(TechParams tech)
        {
            return false;
        }




        private TechParams DataTableToTechParams(DataTable dt)
        {
            var tech = new TechParams();
            foreach (var item in dt.AsEnumerable())
            {
                tech.AddWater = item.Field<double>($"AddWater");
                tech.Solvent_A = item.Field<double>($"Solvent_A");
                tech.Solvent_B = item.Field<double>($"Solvent_B");
                tech.Solvent_C = item.Field<double>($"Solvent_C");
                tech.AddHomo = new double[] { item.Field<double>($"AddHomo1"), item.Field<double>($"AddHomo2"), item.Field<double>($"AddHomo3") };
                tech.Solid_B = new double[] { item.Field<double>($"Solid_B1"), item.Field<double>($"Solid_B2"), item.Field<double>($"Solid_B3") };
                tech.Solid_C = new double[] { item.Field<double>($"Solid_C1"), item.Field<double>($"Solid_C2"), item.Field<double>($"Solid_C3")};
                tech.Solid_D = new double[] { item.Field<double>($"Solid_D1"), item.Field<double>($"Solid_D2"), item.Field<double>($"Solid_D3")};
                tech.Solid_E = new double[] { item.Field<double>($"Solid_E1"), item.Field<double>($"Solid_E2"), item.Field<double>($"Solid_E3")};
                tech.Solid_F = new double[] { item.Field<double>($"Solid_F1"), item.Field<double>($"Solid_F2"), item.Field<double>($"Solid_F3") };
                tech.WetTime = item.Field<int>($"WetTime");
                tech.VortexTime = new int[] { item.Field<int>($"VortexTime1"), item.Field<int>($"VortexTime2"), item.Field<int>($"VortexTime3"), item.Field<int>($"VortexTime4")};
                tech.VortexVel = new int[] { item.Field<int>($"VortexVel1"), item.Field<int>($"VortexVel2"), item.Field<int>($"VortexVel3"), item.Field<int>($"VortexVel4") };
                tech.VibrationOneTime = new int[] { item.Field<int>($"VibrationOneTime1"), item.Field<int>($"VibrationOneTime2") , item.Field<int>($"VibrationOneTime3") , item.Field<int>($"VibrationOneTime4") };
                tech.VibrationOneVel = new int[] { item.Field<int>($"VibrationOneVel1"), item.Field<int>($"VibrationOneVel2"), item.Field<int>($"VibrationOneVel3"), item.Field<int>($"VibrationOneVel4") };
                tech.VibrationTwoTime = new int[] { item.Field<int>($"VibrationTwoTime1"), item.Field<int>($"VibrationTwoTime2") };
                tech.VibrationTwoVel = new int[] { item.Field<int>($"VibrationTwoVel1") , item.Field<int>($"VibrationTwoVel2") };
                tech.CentrifugalOneVelocity = new int[] { item.Field<int>($"CentrifugalOneVelocity1"), item.Field<int>($"CentrifugalOneVelocity2"), item.Field<int>($"CentrifugalOneVelocity3") };
                tech.CentrifugalOneTime = new int[] { item.Field<int>($"CentrifugalOneTime1"), item.Field<int>($"CentrifugalOneTime2"), item.Field<int>($"CentrifugalOneTime3") };
                tech.ExtractDeepOffset = new double[] { item.Field<double>($"ExtractDeepOffset1") , item.Field<double>($"ExtractDeepOffset2") , item.Field<double>($"ExtractDeepOffset3") , item.Field<double>($"ExtractDeepOffset4") };
                tech.ExtractVolume = item.Field<double>($"ExtractVolume");
                tech.cusuanan = item.Field<double>($"cusuanan");
                tech.Extract = item.Field<double>($"Extract");
                tech.ConcentrationVolume = item.Field<double>($"ConcentrationVolume");
                tech.ConcentrationVel = item.Field<int>($"ConcentrationVel");
                tech.ConcentrationTime = item.Field<int>($"ConcentrationTime");
                tech.Redissolve = item.Field<double>($"Redissolve");
                tech.Add_Mark_A = item.Field<double>($"Add_Mark_A");
                tech.Add_Mark_B = item.Field<double>($"Add_Mark_B");
                tech.ExtractSampleVolume = item.Field<double>($"ExtractSampleVolume");
                tech.Tech = item.Field<int>($"Tech");
                tech.Createtime = item.Field<DateTime>($"Createtime");
            }
            return tech;
        }







    }
}
