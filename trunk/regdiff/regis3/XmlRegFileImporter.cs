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
using System.Xml;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace com.tikumo.regis3
{
    /// <summary>
    /// This is the importer for .XML files.
    /// </summary>
    public class XmlRegFileImporter : IRegistryImporter
    {
        private RegKeyEntry Result;       
        private readonly string Content;

        /// <summary>
        /// The constructor takes the content of a .REG file, the expected header (=first line of the .REG file) and import options (if any)
        /// </summary>
        /// <param name="content">Content of the .REG file</param>
        public XmlRegFileImporter(string content)
        {
            Content = content;            
            Result = null;
        }

        /// <summary>
        /// Import the content of the .XML file and return the RegKeyEntry created from it
        /// </summary>
        /// <returns></returns>
        public RegKeyEntry Import()
        {
            if (Result == null)
            {
                ParseXmlContent(Content);
            }
            return Result;
        }

        private byte[] DecodeHexByteArray(string content)
        {
            Trace.Assert(content.Length % 2 == 0);

            byte[] result = new byte[content.Length / 2];
            int writepos = 0;
            for (int readpos = 0; readpos < content.Length; ++readpos)
            {
                char c = content[readpos];
                byte nibble = (byte)"0123456789ABCDEF".IndexOf(c);
                if ((readpos % 2) == 0)
                {
                    result[writepos] = (byte)(nibble << 4);
                }
                else
                {
                    result[writepos] |= nibble;
                    ++writepos;
                }
            }
            return result;
        }


        private void ParseXmlContent(string content)
        {
            RegValueEntry CurrentValue = null;
            RegKeyEntry CurrentKey = null;
            StringBuilder CurrentContent = null;
            RegValueEntryKind CurrentKind = RegValueEntryKind.Unknown;
            bool isBase64Encoding = false;
            List<string> currentStringList = new List<string>();

            using (XmlReader reader = XmlReader.Create(new StringReader(content)))
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name.Equals("registry"))
                            {
                                string version = reader.GetAttribute("version");
                                if (version == "2")
                                {
                                    // ok, this version is supported
                                }
                                else
                                {
                                    throw new System.Data.SyntaxErrorException("Unexpected XML format: must be using registry version 2.0 or higher");
                                }
                            }
                            else if (reader.Name.Equals("key"))
                            {
                                string name = reader.GetAttribute("name");
                                if (CurrentKey == null)
                                {
                                    Trace.Assert(Result == null);
                                    Result = new RegKeyEntry(null, name);
                                    CurrentKey = Result;
                                }
                                else
                                {
                                    RegKeyEntry newKey = new RegKeyEntry(CurrentKey, name);
                                    CurrentKey.Keys[newKey.Name.ToLower()] = newKey;
                                    if (!reader.IsEmptyElement)
                                    {
                                        CurrentKey = newKey;
                                    }
                                }
                            }
                            else if ((CurrentKind == RegValueEntryKind.MultiSZ) && reader.Name.Equals("line"))
                            {
                                if (reader.IsEmptyElement)
                                {
                                    currentStringList.Add("");
                                }
                                else
                                {
                                    CurrentContent = new StringBuilder();

                                    string encoding = reader.GetAttribute("encoding");
                                    isBase64Encoding = (encoding != null) && encoding.Equals("base-64");
                                }
                            }
                            else 
                            {
                                try
                                {
                                    CurrentKind = (RegValueEntryKind)Enum.Parse(typeof(RegValueEntryKind), reader.Name);
                                }
                                catch (ArgumentException)
                                {
                                    throw new System.Data.SyntaxErrorException(
                                        string.Format("ERROR, {0} is not a valid entry in a registry .XML file", reader.Name));
                                }
                                string name = reader.GetAttribute("name");
                                CurrentValue = new RegValueEntry(name);
                                if (name == null)
                                {
                                    CurrentKey.DefaultValue = CurrentValue;
                                }
                                else
                                {
                                    CurrentKey.Values[name.ToLower()] = CurrentValue;
                                }
                                if (reader.IsEmptyElement)
                                {
                                    if (RegValueEntryKind.SZ == CurrentKind)
                                    {
                                        CurrentValue.SetStringValue("");
                                    }
                                    else if (RegValueEntryKind.ExpandSZ == CurrentKind)
                                    {
                                        CurrentValue.SetExpandedStringValue("");
                                    }
                                    else if (RegValueEntryKind.DWord == CurrentKind)
                                    {
                                        CurrentValue.SetIntValue(0);
                                    }
                                    else if (RegValueEntryKind.QWord == CurrentKind)
                                    {
                                        CurrentValue.SetLongValue(0);
                                    }
                                    else if (RegValueEntryKind.MultiSZ == CurrentKind)
                                    {
                                        CurrentValue.SetMultiStringValue(new List<string>());
                                    }
                                    else
                                    {
                                        CurrentValue.SetBinaryType(CurrentKind, new byte[] { });
                                    }
                                    CurrentValue = null;
                                }
                                else
                                {
                                    CurrentContent = new StringBuilder();
                                    string encoding = reader.GetAttribute("encoding");
                                    isBase64Encoding = (encoding != null) && encoding.Equals("base-64");

                                    if (CurrentKind == RegValueEntryKind.MultiSZ)
                                    {
                                        currentStringList.Clear();
                                    }
                                    else
                                    {
                                        CurrentContent = new StringBuilder();
                                    }

                                }
                            }
                            break;
                        case XmlNodeType.Text:
                            if (CurrentContent != null)
                            {
                                CurrentContent.Append(reader.Value);
                            }
                            break;
                        case XmlNodeType.EndElement:
                            if (reader.Name.Equals("key"))
                            {
                                Trace.Assert(CurrentKey != null);
                                CurrentKey = CurrentKey.Parent;
                            }
                            else if ((CurrentKind == RegValueEntryKind.MultiSZ) && reader.Name.Equals("line"))
                            {
                                if (isBase64Encoding)
                                {
                                    byte[] bytes = Convert.FromBase64String(CurrentContent.ToString());
                                    currentStringList.Add(System.Text.Encoding.Unicode.GetString(bytes));
                                }
                                else
                                {
                                    currentStringList.Add(CurrentContent.ToString());
                                }
                            }
                            else if (reader.Name.Equals("registry"))
                            {
                            }
                            else if (reader.Name.Equals(CurrentKind.ToString()))
                            {
                                if (RegValueEntryKind.SZ == CurrentKind)
                                {
                                    if (isBase64Encoding)
                                    {
                                        byte[] bytes = Convert.FromBase64String(CurrentContent.ToString());
                                        CurrentValue.SetStringValue(System.Text.Encoding.Unicode.GetString(bytes));
                                    }
                                    else
                                    {
                                        CurrentValue.SetStringValue(CurrentContent.ToString());
                                    }
                                }
                                else if (RegValueEntryKind.ExpandSZ == CurrentKind)
                                {
                                    if (isBase64Encoding)
                                    {
                                        byte[] bytes = Convert.FromBase64String(CurrentContent.ToString());
                                        CurrentValue.SetExpandedStringValue(System.Text.Encoding.Unicode.GetString(bytes));
                                    }
                                    else
                                    {
                                        CurrentValue.SetExpandedStringValue(CurrentContent.ToString());
                                    }
                                }
                                else if (RegValueEntryKind.DWord == CurrentKind)
                                {
                                    string temp = CurrentContent.ToString();
                                    if (temp.Contains("$$"))
                                    {
                                        CurrentValue.SetEscapedIntValue(temp);
                                    }
                                    else
                                    {
                                        CurrentValue.SetIntValue(int.Parse(temp));
                                    }
                                }
                                else if (RegValueEntryKind.QWord == CurrentKind)
                                {
                                    string temp = CurrentContent.ToString();
                                    if (temp.Contains("$$"))
                                    {
                                        CurrentValue.SetEscapedLongValue(temp);
                                    }
                                    else
                                    {
                                        CurrentValue.SetLongValue(long.Parse(temp));
                                    }
                                }
                                else if (RegValueEntryKind.MultiSZ == CurrentKind)
                                {
                                    CurrentValue.SetMultiStringValue(currentStringList);
                                    currentStringList.Clear();
                                }
                                else
                                {
                                    CurrentValue.SetBinaryType(CurrentKind, DecodeHexByteArray(CurrentContent.ToString()));
                                }
                                CurrentValue = null;
                            }
                            break;
                    }
                }
            }
        }

    }
}
