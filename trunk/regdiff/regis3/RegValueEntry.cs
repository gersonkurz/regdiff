using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace com.tikumo.regis3
{
    public class RegValueEntry
    {
        public readonly string Name;
        public object Value  { get; private set; }
        public RegValueEntryKind Kind { get; private set; }
        public bool RemoveFlag;

        public RegValueEntry(string name)
        {
            Name = name;
            Kind = RegValueEntryKind.Unknown;
            RemoveFlag = false;
        }

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

        public void WriteToTheRegistry(RegistryKey registryKey, RegEnvReplace env)
        {
            string name = env.Map(Name);
            RegistryValueKind kind = MapRegis3KindToNativeKind(Kind);
            if (Value is string)
            {
                string stringValue = Value as string;
                if( stringValue.Contains("$$") )
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

        public RegValueEntry()
        {
            Name = null;
            Kind = RegValueEntryKind.Unknown;
        }

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

        public void SetStringValue(string value)
        {
            Kind = RegValueEntryKind.SZ;
            Value = value;
        }
        
        public void SetEscapedIntValue(string value)
        {
            Kind = RegValueEntryKind.DWord;
            Value = value;
        }

        public void SetIntValue(int value)
        {
            Kind = RegValueEntryKind.DWord;
            Value = value;
        }

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
                output.Write("{0}=hex({1}):", name, (int)Kind);
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
                    separator = ",\\\r\n";
                }
                ++i;
            }
            output.WriteLine();
        }

        public void WriteRegFileFormat(TextWriter output)
        {
            string name = IsDefaultValue ? "@" : string.Format("\"{0}\"", Name.Replace("\\", "\\\\"));

            if (RemoveFlag)
            {
                output.WriteLine("{0}=-", name);
            }
            else if (Kind == RegValueEntryKind.SZ)
            {
                string value = (string)Value;
                output.WriteLine("{0}=\"{1}\"", name, value.Replace("\\", "\\\\"));
            }
            else if (Kind == RegValueEntryKind.DWord)
            {
                output.WriteLine("{0}=dword:{1}", name, ((int)Value).ToString("X8"));
            }
            else if( (Value != null ) && (Value is byte[]) )
            {
                WriteHexEncodedValue(output, name, Value as byte[]);
            }
            else if ((Kind == RegValueEntryKind.ExpandSZ) && (Value is string) )
            {
                output.WriteLine("{0}=\"{1}\"", name, Value);
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
