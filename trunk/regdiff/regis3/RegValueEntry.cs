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
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace com.tikumo.regis3
{
    /// <summary>
    /// This class represents a registry value in a 
    /// </summary>
    public class RegValueEntry
    {
        /// <summary>
        /// Name of this value. Warning: for default values this is going to be null.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Value data
        /// </summary>
        public object Value  { get; private set; }

        /// <summary>
        /// Type of data encoded in this object
        /// </summary>
        public RegValueEntryKind Kind { get; private set; }

        /// <summary>
        /// If this flag is set, you really want to remove the registry value rather than add it
        /// </summary>
        public bool RemoveFlag;

        /// <summary>
        /// This constructor creates a named value with unknown content
        /// </summary>
        /// <param name="name">Name of the value</param>
        public RegValueEntry(string name)
        {
            Name = name;
            Kind = RegValueEntryKind.Unknown;
            RemoveFlag = false;
        }

        /// <summary>
        /// This constructor creates a named value from a Windows registry value
        /// </summary>
        /// <param name="key">Parent registry key</param>
        /// <param name="name">Name of the value</param>
        public RegValueEntry(RegistryKey key, string name)
        {
            Kind = MapNativeKindToRegis3Kind(key.GetValueKind(name));
            Value = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            Name = name;
            if (Value is string)
            {
                string temp = Value as string;
                if (temp.EndsWith("\0"))
                {
                    Value = temp.Substring(0, temp.Length - 1);
                }
            }
            RemoveFlag = false;
        }

        /// <summary>
        /// Write this value back to the Windows registry
        /// </summary>
        /// <param name="registryKey">Parent registry key. Must be open with write permissions.</param>
        /// <param name="env">Helper class that can map $$-escaped strings.</param>
        public void WriteToTheRegistry(RegistryKey registryKey, RegEnvReplace env)
        {
            string name = env.Map(Name);
            RegistryValueKind kind = MapRegis3KindToNativeKind(Kind);
            if (Value is string)
            {
                string stringValue = Value as string;
                if( (env != null) && stringValue.Contains("$$") )
                {
                    stringValue = env.Map(stringValue);
                    if (Kind == RegValueEntryKind.DWord)
                    {
                        stringValue = stringValue.Trim();
                        if (stringValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        {
                            registryKey.SetValue(name, int.Parse(stringValue.Substring(2), System.Globalization.NumberStyles.HexNumber), kind);
                        }
                        else
                        {
                            registryKey.SetValue(name, int.Parse(stringValue), kind);
                        }
                    }
                    else if (Kind == RegValueEntryKind.QWord)
                    {
                        registryKey.SetValue(name, long.Parse(stringValue), kind);
                    }
                    else
                    {
                        registryKey.SetValue(name, stringValue, kind);
                    }
                }
                else
                {
                    registryKey.SetValue(name, stringValue, kind);
                }
            }
            else
            {
                registryKey.SetValue(name, Value, kind);
            }
        }

        private static RegistryValueKind MapRegis3KindToNativeKind(RegValueEntryKind nativeKind)
        {
            switch (nativeKind)
            {
                case RegValueEntryKind.None:
                    return RegistryValueKind.None;

                case RegValueEntryKind.Unknown:
                    return RegistryValueKind.Unknown;

                default:
                    return (RegistryValueKind)nativeKind;
            }
        }

        private static RegValueEntryKind MapNativeKindToRegis3Kind(RegistryValueKind nativeKind)
        {
            switch (nativeKind)
            {
                case RegistryValueKind.None:
                    return RegValueEntryKind.None;

                case RegistryValueKind.Unknown:
                    return RegValueEntryKind.Unknown;

                default:
                    return (RegValueEntryKind) nativeKind;
            }
        }

        /// <summary>
        /// The default constructor creates an unnamed value without contenet.
        /// </summary>
        public RegValueEntry()
        {
            Name = null;
            Kind = RegValueEntryKind.Unknown;
        }

        /// <summary>
        /// Describe the content of this value as a byte array. This should be used only 
        /// </summary>
        /// <returns></returns>
        public byte[] AsByteArray()
        {
            if (Value == null)
                return null;

            if (Value is byte[])
                return (byte[])Value;

            if (Value is string)
            {
                if (Kind == RegValueEntryKind.MultiSZ)
                {
                    string temp = (string)Value;
                    temp += "\0";
                    return Encoding.Unicode.GetBytes(temp);
                }
                else
                {
                    return Encoding.Unicode.GetBytes((string)Value);
                }
            }
            if (Value is string[])
            {
                List<byte> bytes = new List<byte>();
                foreach (string line in (Value as string[]))
                {
                    foreach (byte b in Encoding.Unicode.GetBytes(line))
                    {
                        bytes.Add(b);
                    }
                    bytes.Add(0);
                    bytes.Add(0);
                }
                return bytes.ToArray();
            }
            if (Value is long)
            {
                return BitConverter.GetBytes((long) Value);
            }
            if (Value is int)
            {
                return BitConverter.GetBytes((int) Value);
            }
            return null;
        }

        /// <summary>
        /// Copy Constructor
        /// </summary>
        /// <param name="objectSrc"></param>
        public RegValueEntry(RegValueEntry objectSrc)
        {
            Name = objectSrc.Name;
            Value = objectSrc.Value;
            Kind = objectSrc.Kind;
        }

        /// <summary>
        /// Define a string value
        /// </summary>
        /// <param name="value">content</param>
        public void SetStringValue(string value)
        {
            Kind = RegValueEntryKind.SZ;
            Value = value;
        }

        /// <summary>
        /// Define a expanded string value
        /// </summary>
        /// <param name="value">content</param>
        public void SetExpandedStringValue(string value)
        {
            Kind = RegValueEntryKind.ExpandSZ;
            Value = value;
        }


        /// <summary>
        /// Define a multi-string value
        /// </summary>
        /// <param name="values">content</param>
        public void SetMultiStringValue(List<string> values)
        {
            Kind = RegValueEntryKind.MultiSZ;
            string[] stringArray = values.ToArray();
            Value = stringArray;
        }
        
        /// <summary>
        /// Define an escaped integer value. If you're reading .REG files and you use the RegEnvReplace class to replace 
        /// content with variables at runtime, you can specify something like this:
        /// 
        /// "SomeValue"=dword:$$VARIABLE$$
        /// 
        /// </summary>
        /// <param name="value">Name of the escaped int variable</param>
        public void SetEscapedIntValue(string value)
        {
            Kind = RegValueEntryKind.DWord;
            Value = value;
        }

        /// <summary>
        /// Define an escaped integer value. If you're reading .REG files and you use the RegEnvReplace class to replace 
        /// content with variables at runtime, you can specify something like this:
        /// 
        /// "SomeValue"=qword:$$VARIABLE$$
        /// 
        /// </summary>
        /// <param name="value">Name of the escaped int variable</param>
        public void SetEscapedLongValue(string value)
        {
            Kind = RegValueEntryKind.QWord;
            Value = value;
        }

        /// <summary>
        /// Define an integer value
        /// </summary>
        /// <param name="value">integer value</param>
        public void SetIntValue(int value)
        {
            Kind = RegValueEntryKind.DWord;
            Value = value;
        }

        /// <summary>
        /// Define a long value
        /// </summary>
        /// <param name="value">integer value</param>
        public void SetLongValue(long value)
        {
            Kind = RegValueEntryKind.QWord;
            Value = value;
        }

        /// <summary>
        /// Associate 'None'-type with empty value
        /// </summary>
        public void SetNoneValue()
        {
            Value = null;
            Kind = RegValueEntryKind.None;
        }

        /// <summary>
        /// Given hex-encoded binary data, set a blob type
        /// </summary>
        /// <param name="kind">Type of registry entry</param>
        /// <param name="bytes">Byte representation of the data</param>
        public void SetBinaryType(RegValueEntryKind kind, byte[] bytes)
        {
            Value = bytes;
            Kind = kind;

            if (Kind == RegValueEntryKind.ExpandSZ)
            {
                try
                {
                    string temp = Encoding.Unicode.GetString((byte[])Value);
                    while (temp.EndsWith("\0"))
                        temp = temp.Substring(0, temp.Length - 1);
                    Value = temp;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            else if (Kind == RegValueEntryKind.MultiSZ)
            {
                try
                {
                    string temp = Encoding.Unicode.GetString(bytes);
                    while (temp.EndsWith("\0"))
                        temp = temp.Substring(0, temp.Length - 1);
                    Value = temp.Split('\0');
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            else if (Kind == RegValueEntryKind.DWord)
            {
                try
                {
                    if (bytes.Length == 4)
                    {
                        Value = (int)BitConverter.ToInt32(bytes, 0);
                    }
                    else
                    {
                        Value = (int) BitConverter.ToInt64(bytes, 0);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            else if (Kind == RegValueEntryKind.QWord)
            {
                try
                {
                    Value = BitConverter.ToInt64(bytes, 0);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Returns true if this object represents the default ("null") value in a registry key
        /// </summary>
        public bool IsDefaultValue
        {
            get
            {
                return Name == null;
            }
        }

        private void WriteHexEncodedValue(TextWriter output, string name, IEnumerable<byte> bytes)
        {
            if (Kind == RegValueEntryKind.Binary)
            {
                output.Write("{0}=hex:", name);
            }
            else
            {
                int hexkind = (int)Kind;
                output.Write("{0}=hex({1}):", name, hexkind.ToString("x"));
            }
            int bytesWritten = 0;
            string separator = ",";
            int i = 0;
            foreach(byte b in bytes)
            {
                if (i > 0)
                {
                    output.Write(separator);
                    separator = ",";
                }
                output.Write(b.ToString("X2"));
                if (bytesWritten < 19)
                    ++bytesWritten;
                else
                {
                    bytesWritten = 0;
                    separator = ",\\\r\n  ";
                }
                ++i;
            }
            output.WriteLine();
        }

        /// <summary>
        /// Escape a string for .REG files (i.e. replace " and \ tokens)
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>Escaped string</returns>
        public static string EscapeString(string input)
        {
            StringBuilder output = new StringBuilder();
            foreach (char c in input)
            {
                if (c == '"')
                {
                    output.Append('\\');
                    output.Append('"');
                }
                else if (c == '\\')
                {
                    output.Append('\\');
                    output.Append('\\');
                }
                else
                {
                    output.Append(c);
                }
            }
            return output.ToString();
        }

        /// <summary>
        /// Helper function: Export this function in .REG file format to an output stream
        /// </summary>
        /// <param name="output">Output stream</param>
        public void WriteRegFileFormat(TextWriter output)
        {
            string name = IsDefaultValue ? "@" : string.Format("\"{0}\"", EscapeString(Name));

            if (RemoveFlag)
            {
                output.WriteLine("{0}=-", name);
            }
            else if (Kind == RegValueEntryKind.SZ)
            {
                string value = (string)Value;
                output.WriteLine("{0}=\"{1}\"", name, EscapeString(value));
            }
            else if (Kind == RegValueEntryKind.DWord)
            {
                if (Value is int)
                {
                    output.WriteLine("{0}=dword:{1}", name, ((int)Value).ToString("X8"));
                }
                else
                {
                    output.WriteLine("{0}=dword:{1}", name, ((long)Value).ToString("X8"));
                }
            }
            else if( (Value != null ) && (Value is byte[]) )
            {
                WriteHexEncodedValue(output, name, Value as byte[]);
            }
            else if ((Kind == RegValueEntryKind.ExpandSZ) && (Value is string) )
            {
                WriteHexEncodedValue(output, name, AsByteArray());
            }
            else if ((Kind == RegValueEntryKind.MultiSZ) && (Value is string[]))
            {
                WriteHexEncodedValue(output, name, AsByteArray());
            }
            else if ((Kind == RegValueEntryKind.QWord) && (Value is long))
            {
                long value = (long)Value;
                byte[] bytes = BitConverter.GetBytes(value);
                WriteHexEncodedValue(output, name, bytes);
            }
            else
            {
                throw new InvalidOperationException(string.Format("ERROR, RegValueEntry {0} has unsupported data type {1}", this, Kind));
            }
        }
    }
}
