using BQJX.Common;
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
    public class SampleDataAccess : ISampleDataAccess
    {
        #region Private Members

        private IDataAccessBase _dataAccess;
        private ILogger _logger;

        #endregion

        #region Construtors

        public SampleDataAccess(IDataAccessBase dataAccess, ILogger logger)
        {
            this._dataAccess = dataAccess;
            this._logger = logger;
        }

        #endregion


        /// <summary>
        /// 通过创建时间查找样品信息
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<SampleInfo> GetSampleInfoByTime(DateTime start, DateTime end)
        {
            try
            {
                string sql = $"Select * from SampleInfo where CreateTime between '{start}' and '{end}'";
                DataTable dt = _dataAccess.Query(sql);
                List<SampleInfo> list = new List<SampleInfo>();
                foreach (var item in dt.AsEnumerable())
                {
                    SampleInfo data = new SampleInfo();
                    data.Id = item.Field<int>("Id");
                    data.SnNum = item.Field<string>("SnNum");
                    data.Name = item.Field<string>("Name");
                    data.TechName = item.Field<string>("TechName");
                    data.Status = byte.Parse(item.Field<object>("Status").ToString());
                    data.CreateTime = item.Field<DateTime>("CreateTime");
                    list.Add(data);
                }
                return list;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 通过样品ID查询样品信息
        /// </summary>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<SampleInfo> GetSampleInfoById(int start, int count)
        {
            try
            {
                string sql = $"Select * from SampleInfo LIMIT {start},{count}";
                DataTable dt = _dataAccess.Query(sql);
                List<SampleInfo> list = new List<SampleInfo>();
                foreach (var item in dt.AsEnumerable())
                {
                    SampleInfo data = new SampleInfo();
                    data.Id = item.Field<int>("Id");
                    data.SnNum = item.Field<string>("SnNum");
                    data.Name = item.Field<string>("Name");
                    data.TechName = item.Field<string>("TechName");
                    data.Status = byte.Parse(item.Field<object>("Status").ToString());
                    data.CreateTime = item.Field<DateTime>("CreateTime");
                    list.Add(data);
                }
                return list;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 通过样品名称查询样品信息
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<SampleInfo> GetSampleInfoByName(string name)
        {
            try
            {
                string sql = $"Select * from SampleInfo where Name Like '{name}%'";
                DataTable dt = _dataAccess.Query(sql);
                List<SampleInfo> list = new List<SampleInfo>();
                foreach (var item in dt.AsEnumerable())
                {
                    SampleInfo data = new SampleInfo();
                    data.Id = item.Field<int>("Id");
                    data.SnNum = item.Field<string>("SnNum");
                    data.Name = item.Field<string>("Name");
                    data.TechName = item.Field<string>("TechName");
                    data.Status = byte.Parse(item.Field<object>("Status").ToString());
                    data.CreateTime = item.Field<DateTime>("CreateTime");
                    list.Add(data);
                }
                return list;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 通过编号查询样品信息
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<SampleInfo> GetSampleInfoBySnNum(string SnNum)
        {
            try
            {
                string sql = $"Select * from SampleInfo where SnNum Like '{SnNum}%'";
                DataTable dt = _dataAccess.Query(sql);
                List<SampleInfo> list = new List<SampleInfo>();
                foreach (var item in dt.AsEnumerable())
                {
                    SampleInfo data = new SampleInfo();
                    data.Id = item.Field<int>("Id");
                    data.SnNum = item.Field<string>("SnNum");
                    data.Name = item.Field<string>("Name");
                    data.TechName = item.Field<string>("TechName");
                    data.Status = byte.Parse(item.Field<object>("Status").ToString());
                    data.CreateTime = item.Field<DateTime>("CreateTime");
                    list.Add(data);
                }
                return list;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 新增样品信息
        /// </summary>
        /// <param name="sampleInfo"></param>
        /// <returns></returns>
        public bool InsertSampleInfo(SampleInfo sampleInfo)
        {
            try
            {
                string sql = "INSERT INTO SampleInfo (`SnNum`, `Name`, `TechName` , `Status` ,`CreateTime`) VALUES" +
                    $"('{sampleInfo.SnNum}', '{sampleInfo.Name}', '{sampleInfo.TechName}', '{sampleInfo.Status}','{sampleInfo.CreateTime}')";

                return _dataAccess.ExecuteNonQuery(sql) == 1;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 删除样品信息
        /// </summary>
        /// <param name="sampleInfo"></param>
        /// <returns></returns>
        public bool DeleteSampleInfo(SampleInfo sampleInfo)
        {
            //DELETE FROM `tcm`.`sampleinfo` WHERE `Id` = 2
            try
            {
                string sql = "DELETE FROM SampleInfo" +
                    $" WHERE `Id` = {sampleInfo.Id}";

                return _dataAccess.ExecuteNonQuery(sql) == 1;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex.Message);
                return false;
            }
        }








    }
}
