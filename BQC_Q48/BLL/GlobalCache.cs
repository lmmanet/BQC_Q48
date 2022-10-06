using BQJX.Common;
using BQJX.Common.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public class GlobalCache
    {
        private static Cache _instance;
        public static Cache Instance
        {
            get
            {
                if (_instance==null)
                {
                    _instance = new Cache();
                }
                return _instance;
            }
        }

        private static string filePath = Environment.CurrentDirectory + "\\workList.xml";


        public static void Save()
        {
           MySerialization.SerializeToXml<Cache>(filePath, GlobalCache.Instance);
        }

        public static void Load()
        {
            _instance =  MySerialization.DeserializeFromXml<Cache>(filePath);
        }


    }


    public class Cache
    {

        //涡旋振荡部分
        public DictionaryEx<Sample, string> VibrationOneDic = new DictionaryEx<Sample, string>();
        //离心机部分
        public DictionaryEx<Sample, string> CentrifugalBig = new DictionaryEx<Sample, string>();
        public DictionaryEx<Sample, string> CentrifugalSmall = new DictionaryEx<Sample, string>();
        public DictionaryEx<Sample, string> CentrifugalPolish = new DictionaryEx<Sample, string>();
        //移液部分
        public DictionaryEx<Sample, string> PipettorDic = new DictionaryEx<Sample, string>();
        //冰浴字典
        public DictionaryEx<Sample, ushort> ColdDic = new DictionaryEx<Sample, ushort>();

        //回湿列表
        public List<Sample> WetBackSampleList = new List<Sample>();
        //浓缩列表
        public List<Sample> ConcentrationList = new List<Sample>();
        //提取列表
        public List<Sample> ExtractList = new List<Sample>();


        /// <summary>
        /// 拧盖3占用中 （移液）
        /// </summary>
        public bool IsCapperThreeOccupy { get; set; }


    }








}
