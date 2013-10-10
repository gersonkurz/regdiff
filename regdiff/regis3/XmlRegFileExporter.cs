using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Xml;

namespace com.tikumo.regis3
{
    public class XmlRegFileExporter : IRegistryExporter
    {
        public void Export(RegKeyEntry key, string filename)
        {
            using (StreamWriter sw = new StreamWriter(File.Open(filename, FileMode.Create), Encoding.UTF8))
            {
                try
                {
                    Export(key, sw);
                }
                finally
                {
                    sw.Close();
                }
            }
        }

        public void Export(RegKeyEntry key, TextWriter file)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            XmlWriter Writer = XmlWriter.Create(file, settings);
            Writer.WriteStartElement("registry");
           
            WriteXmlFileFormat(Writer, key);
            
            Writer.WriteEndElement();
            Writer.Close();
        }

        public static bool IsValidXmlString(string s)
        {
            foreach (char current in s)
            {
                if ((current == 0x9) ||
                    (current == 0xA) ||
                    (current == 0xD) ||
                    ((current >= 0x20) && (current <= 0xD7FF)) ||
                    ((current >= 0xE000) && (current <= 0xFFFD)))
                {
                    // is valid
                }
                else
                {
                    return false;
                }
                    
            }
            return true;
        }

        private static string EncodeBase64(string str)
        {
            return Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(str));
        }

        private static void EncodeStringValue(XmlWriter Writer, RegValueEntry value, string xmlName)
        {
            if (value.Value is string)
            {
                string rep = value.Value.ToString();
                if (IsValidXmlString(rep))
                {
                    Writer.WriteElementString(xmlName, rep);
                }
                else
                {
                    Writer.WriteElementString("base64-" + xmlName, EncodeBase64(rep));
                }
            }
            else if (value.Value is string[])
            {
                foreach(string rep in (string[])value.Value)
                {
                    if (IsValidXmlString(rep))
                    {
                        Writer.WriteElementString(xmlName, rep);
                    }
                    else
                    {
                        Writer.WriteElementString("base64-" + xmlName, EncodeBase64(rep));
                    }
                }
            }
        }

        private static void WriteXmlFileFormat(XmlWriter Writer, RegKeyEntry key)
        {
            List<string> names;

            if (key.RemoveFlag)
            {
                if (key.Name != null)
                {
                    Writer.WriteStartElement("remove-key");
                    Writer.WriteAttributeString("name", key.Name);
                    Writer.WriteEndElement();
                }
            }
            else
            {
                if (key.Name != null)
                {
                    Writer.WriteStartElement("key");
                    Writer.WriteAttributeString("name", key.Name);
                }

                names = key.Keys.Keys.ToList<string>();
                names.Sort();
                foreach (string name in names)
                {
                    WriteXmlFileFormat(Writer, key.Keys[name]);
                }

                if (key.DefaultValue != null)
                {
                    WriteXmlFileFormat(Writer, key.DefaultValue);
                }

                names = key.Values.Keys.ToList<string>();
                names.Sort();
                foreach (string name in names)
                {
                    WriteXmlFileFormat(Writer, key.Values[name]);
                }
                if (key.Name != null)
                {
                    Writer.WriteEndElement();
                }
            }
        }

        private static void WriteHexEncodedValue(XmlWriter writer, RegValueEntryKind kind, IEnumerable<byte> bytes)
        {
            writer.WriteStartElement("hex");
            writer.WriteAttributeString("kind", kind.ToString());
            
            StringBuilder buffer = new StringBuilder();
            foreach (byte b in bytes)
            {
                buffer.Append(b.ToString("X2"));
            }
            writer.WriteElementString("data", buffer.ToString());
            writer.WriteEndElement();
            
        }

        private static void WriteXmlFileFormat(XmlWriter writer, RegValueEntry value)
        {
            if (value.RemoveFlag)
            {
                if (value.IsDefaultValue)
                {
                    writer.WriteStartElement("remove-default-value");
                }
                else
                {
                    writer.WriteStartElement("remove-value");
                    writer.WriteAttributeString("name", value.Name);
                }
            }
            else
            {
                if (value.IsDefaultValue)
                {
                    writer.WriteStartElement("default-value");
                }
                else
                {
                    writer.WriteStartElement("value");
                    writer.WriteAttributeString("name", value.Name);
                }

                switch (value.Kind)
                {
                    case RegValueEntryKind.DWord:
                        writer.WriteElementString("dword", value.Value.ToString());
                        break;
                    case RegValueEntryKind.SZ:
                        EncodeStringValue(writer, value, "string");
                        break;
                    case RegValueEntryKind.ExpandSZ:
                        EncodeStringValue(writer, value, "expand-string");
                        break;
                    case RegValueEntryKind.MultiSZ:
                        EncodeStringValue(writer, value, "multi-string");
                        break;
                    case RegValueEntryKind.QWord:
                        writer.WriteElementString("qword", value.Value.ToString());
                        break;

                    default:
                        if ((value.Value != null) && (value.Value is byte[]))
                        {
                            WriteHexEncodedValue(writer, value.Kind, value.Value as byte[]);
                        }
                        else
                        {
                            throw new Exception(string.Format("ERROR, XmlRegFileExporter() isn't prepared to handle data of type {0}", value.Kind));
                        }
                        break;
                }
            }
            writer.WriteEndElement();
        }
    }
}
