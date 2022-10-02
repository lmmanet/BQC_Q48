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
        public static GlobalCache Instance { private set; get; }

        static GlobalCache()
        {
            Instance = new GlobalCache();
        }

        private string _vibrationOnevortexDic = Environment.CurrentDirectory + "\\VibrationOnevortexDic.xml";
        private string _centrifugalDic = Environment.CurrentDirectory + "\\CentrifugalDic.xml";
        private string _coldDic = Environment.CurrentDirectory + "\\ColdDicDic.xml";

        //涡旋振荡部分
        private List<Sample> VibrationOnevortexSampleList = new List<Sample>();
        private List<string> VibrationOnevortexActionList = new List<string>();

        //回湿列表
        private List<Sample> WetBackSampleList = new List<Sample>();

        //离心机部分
        private List<Sample> CentrifugalSampleList = new List<Sample>();
        private List<string> CentrifugalActionList = new List<string>();

        //冰浴字典
        public Dictionary<Sample, ushort> ColdDic = new Dictionary<Sample, ushort>();

        //浓缩部分
        public List<Sample> ConcentrationList = new List<Sample>();   //浓缩列表


        public static void SaveStatus()
        {
            //MySerialization.SerializeToXml<Dictionary<Sample, string>>(_vibrationOnevortexDic, VibrationOnevortexDic);
            //MySerialization.SerializeToXml<Dictionary<Sample, string>>(_centrifugalDic, CentrifugalDic);
            //MySerialization.SerializeToXml<Dictionary<Sample, ushort>>(_coldDic, ColdDic);
        }

        public static void GetStatusFromFile()
        {
            //VibrationOnevortexDic = MySerialization.DeserializeFromXml<Dictionary<Sample, string>>(_vibrationOnevortexDic);
            //CentrifugalDic = MySerialization.DeserializeFromXml<Dictionary<Sample, string>>(_centrifugalDic);
            //ColdDic = MySerialization.DeserializeFromXml<Dictionary<Sample, ushort>>(_coldDic);
        }




        public static KeyValuePair<Sample, string> GetVibrationOneVortexKeyValues(int index)
        {
            var sample = GlobalCache.Instance.VibrationOnevortexSampleList[index];
            var action = GlobalCache.Instance.VibrationOnevortexActionList[index];

            return new KeyValuePair<Sample, string>(sample,action);
        }

        public static void RemoveVibrationOneVortexKeyValue(Sample sample,string str)
        {
            GlobalCache.Instance.VibrationOnevortexSampleList.Remove(sample);
            GlobalCache.Instance.VibrationOnevortexActionList.Remove(str);
        }

        public static void AddVibrationOneVortexKeyValue(Sample sample ,string str)
        {
            if (GlobalCache.Instance.VibrationOnevortexSampleList.Contains(sample) &&
                GlobalCache.Instance.VibrationOnevortexActionList.Contains(str))
            {
                return;
            }
            GlobalCache.Instance.VibrationOnevortexSampleList.Add(sample);
            GlobalCache.Instance.VibrationOnevortexActionList.Add(str);
        }
        public static int GetVibrationOneVortexKeyValueCount()
        {
            return GlobalCache.Instance.VibrationOnevortexSampleList.Count;
        }


        //=========================================================================================//

        public static void AddWetBack(Sample sample)
        {
            if (GlobalCache.Instance.WetBackSampleList.Contains(sample))
            {
                return;
            }
            GlobalCache.Instance.WetBackSampleList.Add(sample);
        }

        public static List<Sample>GetWetBackList()
        {
           return GlobalCache.Instance.WetBackSampleList;
        }









        public static KeyValuePair<Sample, string> GetCentrifugalKeyValues(int index)
        {
            var sample = GlobalCache.Instance.CentrifugalSampleList[index];
            var action = GlobalCache.Instance.CentrifugalActionList[index];

            return new KeyValuePair<Sample, string>(sample, action);
        }

        public static void RemoveCentrifugalKeyValue(Sample sample, string str)
        {
            GlobalCache.Instance.CentrifugalSampleList.Remove(sample);
            GlobalCache.Instance.CentrifugalActionList.Remove(str);
        }

        public static void AddCentrifugalKeyValue(Sample sample, string str)
        {
            if (GlobalCache.Instance.CentrifugalSampleList.Contains(sample) &&
                 GlobalCache.Instance.CentrifugalActionList.Contains(str))
            {
                return;
            }
            GlobalCache.Instance.CentrifugalSampleList.Add(sample);
            GlobalCache.Instance.CentrifugalActionList.Add(str);
        }

        public static void InsertCentrifugalKeyValue(Sample sample, string str)
        {
            if (GlobalCache.Instance.CentrifugalSampleList.Contains(sample) &&
                 GlobalCache.Instance.CentrifugalActionList.Contains(str))
            {
                return;
            }
            GlobalCache.Instance.CentrifugalSampleList.Insert(1,sample);
            GlobalCache.Instance.CentrifugalActionList.Insert(1,str);
        }

        public static int GetCentrifugalKeyValueCount()
        {
            return GlobalCache.Instance.CentrifugalSampleList.Count;
        }









    }
}
