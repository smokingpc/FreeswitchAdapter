using System;
using System.IO;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;

namespace FSAdapter
{
    internal static class Extension
    {
        internal static Int64 ToLinuxEpoch64(this DateTime dt)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
            return Convert.ToInt64((dt.ToUniversalTime() - epoch).TotalMilliseconds);
        }

    }

    internal static class XMLUtils
    {
        internal static XmlDocument LoadXML(string filename)
        {
            XmlDocument doc = new XmlDocument();
            using (XmlReader reader = XmlReader.Create(filename))
            {
                doc.Load(reader);
            }
            return doc;
        }

        internal static void SaveXML(XmlDocument doc, string filename)
        {
            using (XmlTextWriter writer = new XmlTextWriter(filename, Encoding.ASCII))
            {
                writer.Formatting = Formatting.Indented;
                doc.Save(writer);
            }
        }

        internal static XmlNode CreateElementNode(XmlDocument doc, string name, Dictionary<string, string> attr_map)
        {
            XmlNode node = doc.CreateNode(XmlNodeType.Element, name, null);

            foreach (var item in attr_map)
            {
                var attr = doc.CreateAttribute(item.Key);
                attr.Value = item.Value;
                node.Attributes.Append(attr);
            }

            return node;
        }
    }

    internal static class Utils
    { 
        //目前只能吃microsecond與second兩種epoch
        internal static DateTime FromLinuxEpoch32(Int32 second)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return epoch.AddSeconds(second);
        }

        internal static DateTime FromLinuxEpoch64(Int64 microsecond)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
            return epoch.AddMilliseconds(microsecond / 1000).ToLocalTime();
        }

        //刪除指定目錄下所有檔案與sub-folders
        internal static void DeleteAll(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            foreach (FileInfo file in dir.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo item in dir.GetDirectories())
            {
                DeleteAll(item.FullName);
                Directory.Delete(item.FullName);
            }
        }
    }
}
