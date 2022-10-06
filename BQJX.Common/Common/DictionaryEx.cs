using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace BQJX.Common.Common
{
    [Serializable]
    public class DictionaryEx<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                return;
            }
            reader.Read();
            XmlSerializer KeySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer ValueSerializer = new XmlSerializer(typeof(TValue));

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (!reader.IsEmptyElement)
                {
                    reader.ReadStartElement("Item");
                    reader.ReadStartElement("Key");
                    TKey tk = (TKey)KeySerializer.Deserialize(reader);
                    reader.ReadEndElement();
                    reader.ReadStartElement("Value");
                    TValue vl = (TValue)ValueSerializer.Deserialize(reader);
                    reader.ReadEndElement();
                    reader.ReadEndElement();
                    this.Add(tk, vl);
                    reader.MoveToContent();
                }
                else
                {
                    return;
                }
                
            }
            reader.ReadEndElement();

        }

        public void WriteXml(XmlWriter writer)
        {
            XmlSerializer KeySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer ValueSerializer = new XmlSerializer(typeof(TValue));

            foreach (KeyValuePair<TKey, TValue> kv in this)
            {
                writer.WriteStartElement("Item");
                writer.WriteStartElement("Key");
                KeySerializer.Serialize(writer, kv.Key);
                writer.WriteEndElement();
                writer.WriteStartElement("Value");
                ValueSerializer.Serialize(writer, kv.Value);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
    }






}
