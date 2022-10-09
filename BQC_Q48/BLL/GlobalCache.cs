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
        public List<Sample> VibrationOneDic = new List<Sample>();
        public List<Sample> VibrationOneDicPolish = new List<Sample>();
        //离心机部分
        public List<Sample> CentrifugalBig = new List<Sample>();
        public List<Sample> CentrifugalSmall = new List<Sample>();
        public List<Sample> CentrifugalPolish = new List<Sample>();
        //移液部分
        public List<Sample> PipettorDic = new List<Sample>();
        //冰浴字典
        //public List<Sample> ColdDic = new DictionaryEx<Sample, ushort>();
        public List<Sample> ColdDic = new List<Sample>();

        //回湿列表
        public List<Sample> WetBackSampleList = new List<Sample>();
        //浓缩列表
        public List<Sample> ConcentrationList = new List<Sample>();
        //提取列表
        public List<Sample> ExtractList = new List<Sample>();

        //任务列表
        public List<Sample> WorkList = new List<Sample>();

        /// <summary>
        /// 拧盖3占用中 （移液）
        /// </summary>
        public bool IsCapperThreeOccupy { get; set; }


        public bool[] IshaveCapper { get; set; } = new bool[5];


        /// <summary>
        /// 离心机当前执行样品
        /// </summary>
        public Sample CenRunningSample { get; set; }


    }








}
