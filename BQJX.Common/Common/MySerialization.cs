using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace BQJX.Common.Common
{
    public static class MySerialization
    {

        public static string Serialize<T>(T obj)
        {
            using (StringWriter sw = new StringWriter())
            {
                XmlSerializer xz = new XmlSerializer(typeof(T));
                xz.Serialize(sw, obj);
                return sw.ToString();
            }
        }  
        
        public static string SerializeToString<T>(T obj)
        {
            Type type = typeof(T);

            using (MemoryStream ms = new MemoryStream())
            {
                XmlSerializer xmlSerializer = new XmlSerializer(type);
                xmlSerializer.Serialize(ms, obj);
                ms.Position = 0;
                StreamReader streamReader = new StreamReader(ms);
                string result = streamReader.ReadToEnd();
                streamReader.Dispose();
                return result;
            }
        }

        public static T Deserialize<T>(string s)
        {
            using (StreamReader sr = new StreamReader(s))
            {
                Type type = typeof(T);
                XmlSerializer xz = new XmlSerializer(type);
                var result = xz.Deserialize(sr);
                return (T)result;
            }
        }
        
        public static T Deserialize<T>(Stream stream)
        {
            Type type = typeof(T);
            XmlSerializer xz = new XmlSerializer(type);
            var result = xz.Deserialize(stream);
            return (T)result;

        }





        public static string SerializeToXml<T>(T myObj)
        {
            if (myObj == null)
                return string.Empty;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

            using (MemoryStream ms = new MemoryStream())
            {
                XmlTextWriter xmlTextWriter = new XmlTextWriter(ms, Encoding.UTF8);
                xmlTextWriter.Formatting = Formatting.None;
                xmlSerializer.Serialize(xmlTextWriter, myObj);
                ms.Position = 0;
                StringBuilder stringBuilder = new StringBuilder();
                using (StreamReader streamReader = new StreamReader(ms, Encoding.UTF8))
                {
                    string value;
                    while ((value = streamReader.ReadLine()) != null)
                    {
                        stringBuilder.Append(value);
                    }
                }
                xmlTextWriter.Close();
                return stringBuilder.ToString();
            }

        }


        public static T DeserializeToObject<T>(string xml)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringReader stringReader = new StringReader(xml))
            {
                return (T)xmlSerializer.Deserialize(stringReader);
            }
        }


        public static void SerializeToXml<T>(string filePath, T obj)
        {
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                xmlSerializer.Serialize(sw, obj);
            }
        }



        public static T DeserializeFromXml<T>(string filePath)
        {
            if (!File.Exists(filePath))
                throw new ArgumentNullException(filePath + " not Exists");
            using (StreamReader sr = new StreamReader(filePath))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                return (T)xmlSerializer.Deserialize(sr);
            }
        }












    }
}
