using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml;
using System.IO;
using com.tikumo.regis3.ini;

namespace com.tikumo.regis3
{
    public class RegEnvReplace
    {
        public readonly Dictionary<string, string> Variables = new Dictionary<string, string>();
        private readonly Dictionary<string, string> Cache = new Dictionary<string, string>();
        private bool IsCacheValid;

        public RegEnvReplace()
        {
            IsCacheValid = false;
        }

        public void ReadXmlFile(string filename)
        {
            StringBuilder textContent = null;
            string currentValueName = null;
            using (XmlReader reader = XmlReader.Create(new StreamReader(filename)))
            {
                while (reader.Read())
                {
                    switch(reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name.Equals("value"))
                            {
                                currentValueName = reader.GetAttribute("name");
                                textContent = new StringBuilder();
                            }
                            break;
                        case XmlNodeType.Text:
                            if (textContent != null)
                            {
                                textContent.Append(reader.Value);
                            }
                            break;
                        case XmlNodeType.EndElement:
                            if (textContent != null)
                            {
                                Variables[currentValueName] = textContent.ToString();
                                textContent = null;
                            }
                            break;
                    }
                }
            }
        }

        public void ReadIniFile(string filename)
        {
            IniFile file = new IniFile();
            file.Read(filename);

            AddVarsRecursive(file);
        }

        private void AddVarsRecursive(IniFileSection section)
        {
            foreach (IniFileSection subsection in section.Sections)
            {
                AddVarsRecursive(subsection);
            }
            foreach (IniFileEntry entry in section.Entries)
            {
                Variables[entry.Name] = entry.Data;
            }
        }

        private void EnsureCacheIsValid()
        {
            if (!IsCacheValid)
            {
                Cache.Clear();
                foreach (string key in Variables.Keys)
                {
                    Cache[string.Format("$${0}$$", key.ToUpper())] = Variables[key];
                }
                IsCacheValid = true;
            }
        }

        public void MergeEnvironmentVariables()
        {
            // also merge all environment variables 
            System.Collections.IDictionary env = Environment.GetEnvironmentVariables();
            foreach (string key in env.Keys)
            {
                Variables[key] = (string) env[key];
            }
            IsCacheValid = false;
        }

        public string Map(string input)
        {
            if (input.Contains("$$"))
            {
                EnsureCacheIsValid();
                foreach (string key in Cache.Keys)
                {
                    input = input.Replace(key, Cache[key]);
                }
            }
            return input;
        }
    }
}
