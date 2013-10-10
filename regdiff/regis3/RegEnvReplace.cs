// Copyright (c) 2013, Gerson Kurz
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// Redistributions of source code must retain the above copyright notice, this list
// of conditions and the following disclaimer. Redistributions in binary form must
// reproduce the above copyright notice, this list of conditions and the following
// disclaimer in the documentation and/or other materials provided with the distribution.
// 
// Neither the name regdiff nor the names of its contributors may be used to endorse
// or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
// IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
// NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using com.tikumo.regis3.ini;

namespace com.tikumo.regis3
{
    /// <summary>
    /// This class supports replacing $$ variables in .REG files by dynamic content.
    /// 
    /// When reading .REG files, you can use the syntax $$NAME$$ to defer resolution of names / content to runtime.
    /// At runtime, variables are declared either as environment variables or by reading them from .XML files / .INI files
    /// and replaced back in the registry.
    /// </summary>
    public class RegEnvReplace
    {
        /// <summary>
        /// Dictionary of key/value pairs for replacement
        /// </summary>
        public readonly Dictionary<string, string> Variables = new Dictionary<string, string>();

        private readonly Dictionary<string, string> Cache = new Dictionary<string, string>();
        private bool IsCacheValid;

        /// <summary>
        /// The default constructor creates an empty list of variables
        /// </summary>
        public RegEnvReplace()
        {
            IsCacheValid = false;
        }

        /// <summary>
        /// Read variables from an .XML file. It must contain entries of the form
        /// 
        /// &lt;value name="NAME"&gt;CONTENT&lt;/value&gt;
        /// </summary>
        /// <param name="filename">Filename to read variables from</param>
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

        /// <summary>
        /// Read variables from an .INI file. It must contain entries of the form
        /// 
        /// NAME=VALUE
        /// </summary>
        /// <param name="filename">Filename to read variables from</param>
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

        /// <summary>
        /// Read content of the Environment variables and merge them with this set of variables
        /// </summary>
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

        /// <summary>
        /// Given an input string, try to map it. 
        /// </summary>
        /// <param name="input">Input string with $$ variables</param>
        /// <returns>Mapped result string</returns>
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
