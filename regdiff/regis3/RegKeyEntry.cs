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

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Security.Principal;

namespace com.tikumo.regis3
{
    /// <summary>
    /// This class represents a registry key in memory
    /// </summary>
    public class RegKeyEntry
    {
        /// <summary>
        /// Name of the key (not the complete path name: use the Path member for that)
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Parent key or null if this is a root key
        /// </summary>
        public RegKeyEntry Parent { get; protected set; }

        /// <summary>
        /// Subkeys relative to this key
        /// </summary>
        public readonly Dictionary<string, RegKeyEntry> Keys = new Dictionary<string, RegKeyEntry>();

        /// <summary>
        /// Values in this key
        /// </summary>
        public readonly Dictionary<string, RegValueEntry> Values = new Dictionary<string, RegValueEntry>();

        /// <summary>
        /// Default value or null if undefined
        /// </summary>
        public RegValueEntry DefaultValue;

        /// <summary>
        /// Flag indicating wether the .REG file actually asks to REMOVE this value, rather than add it.
        /// </summary>
        public bool RemoveFlag;

        /// <summary>
        /// The default constructor creates an empty - unnamed - registry key.
        /// </summary>
        protected RegKeyEntry()
            :   this(null, null)
        {
        }

        /// <summary>
        /// This constructor creates a named registry key, relative to an existing parent
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        public RegKeyEntry(RegKeyEntry parent, string name)
        {
            Parent = parent;
            Name = name;
            DefaultValue = null;
            RemoveFlag = false;
        }

        /// <summary>
        /// Copy constructor: takes an existing RegKeyEntry and creates a full clone of it
        /// </summary>
        /// <param name="objectSrc"></param>
        public RegKeyEntry(RegKeyEntry objectSrc)
        {
            Name = objectSrc.Name;
            Parent = objectSrc.Parent;
            foreach (string subkeyName in objectSrc.Keys.Keys)
            {
                Keys[subkeyName] = new RegKeyEntry(objectSrc.Keys[subkeyName]);
                Keys[subkeyName].Parent = this;
            }
            foreach (string valueName in objectSrc.Values.Keys)
            {
                Values[valueName] = new RegValueEntry(objectSrc.Values[valueName]);
            }
            if (objectSrc.DefaultValue == null)
            {
                DefaultValue = null;
            }
            else
            {
                DefaultValue = new RegValueEntry(objectSrc.DefaultValue);
            }
        }

        /// <summary>
        /// When creating a diff/merge file, asks the key to remove a subkey based on an existing key
        /// </summary>
        /// <param name="removeThis">Existing key</param>
        /// <returns>Key in the diff/merge file with the RemoveFlag set</returns>
        public RegKeyEntry AskToRemoveKey(RegKeyEntry removeThis)
        {
            RegKeyEntry key = AskToAddKey(removeThis);
            key.RemoveFlag = true;
            return key;
        }

        /// <summary>
        /// When creating a diff/merge file, asks the key to create a subkey based on an existing key
        /// </summary>
        /// <param name="addThis">Existing key</param>
        /// <returns>Key in the diff/merge file</returns>
        public RegKeyEntry AskToAddKey(RegKeyEntry addThis)
        {
            RegKeyEntry key = FindOrCreateKey(addThis.Path);
            foreach (string subkeyName in addThis.Keys.Keys)
            {
                key.Keys[subkeyName] = new RegKeyEntry(addThis.Keys[subkeyName]);
                key.Keys[subkeyName].Parent = key;
            }
            foreach (string valueName in addThis.Values.Keys)
            {
                key.Values[valueName] = new RegValueEntry(addThis.Values[valueName]);
            }
            if (addThis.DefaultValue == null)
            {
                key.DefaultValue = null;
            }
            else
            {
                key.DefaultValue = new RegValueEntry(addThis.DefaultValue);
            }
            return key;
        }

        /// <summary>
        /// When creating a diff/merge file, asks the key to remove a value based on an existing value
        /// </summary>
        /// <param name="key">existing key</param>
        /// <param name="value">existing value</param>
        public void AskToRemoveValue(RegKeyEntry key, RegValueEntry value)
        {
            key = AskToAddKey(key);
            if (value.IsDefaultValue)
            {
                key.DefaultValue = new RegValueEntry(value);
                key.DefaultValue.RemoveFlag = true;
            }
            else
            {
                string valueName = value.Name.ToLower();
                key.Values[valueName] = new RegValueEntry(value);
                key.Values[valueName].RemoveFlag = true;
            }
        }

        /// <summary>
        /// When creating a diff/merge file, asks the key to add a value based on an existing value
        /// </summary>
        /// <param name="key">existing key</param>
        /// <param name="value">existing value</param>
        public void AskToAddValue(RegKeyEntry key, RegValueEntry value)
        {
            key = AskToAddKey(key);
            if (value.IsDefaultValue)
            {
                key.DefaultValue = new RegValueEntry(value);
            }
            else
            {
                string valueName = value.Name.ToLower();
                key.Values[valueName] = new RegValueEntry(value);
            }
        }

        /// <summary>
        /// Return the complete (recursive) path of this key
        /// </summary>
        public string Path
        {
            get
            {
                List<string> path = new List<string>();

                RegKeyEntry k = this;
                while (k != null)
                {
                    if (k.Parent == null)
                    {
                        if (!string.IsNullOrEmpty(k.Name))
                        {
                            path.Add(k.Name);
                        }
                        break;
                    }
                    path.Add(k.Name);
                    k = k.Parent;
                }
                path.Reverse();

                StringBuilder temp = new StringBuilder();
                bool first = true;
                foreach (string key in path)
                {
                    if( first )
                        first = false;
                    else
                        temp.Append("\\");
                   temp.Append(key);
                }
                return temp.ToString();
            }
        }

        /// <summary>
        /// Create a string description of this instance
        /// </summary>
        /// <returns>String description of this instance</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Find or create a named registry value
        /// </summary>
        /// <param name="name">Name of the registry value</param>
        /// <returns>Newly created registry value or null if it cannot be created</returns>
        public RegValueEntry FindOrCreateValue(string name)
        {
            string key = null;
            if (name == null)
            {
                if (DefaultValue == null)
                {
                    DefaultValue = new RegValueEntry();
                }
                return DefaultValue;
            }
            else
            {
                key = name.ToLower();
            }
            if (Values.ContainsKey(key))
            {
                return Values[key];
            }
            RegValueEntry result = new RegValueEntry(name);
            Values[key] = result;
            return result;
        }

        /// <summary>
        /// Find or create a subkey relative to this one
        /// </summary>
        /// <param name="path">Subkey path relative to this one</param>
        /// <returns>Newly created subkey</returns>
        public RegKeyEntry FindOrCreateKey(string path)
        {
            Trace.Assert(!string.IsNullOrEmpty(path));

            RegKeyEntry result = this;
            foreach (string token in path.Split('\\'))
            {
                string key = token.ToLower();

                if (result.Keys.ContainsKey(key))
                {
                    RegKeyEntry subkey = result.Keys[key];
                    Trace.Assert(subkey.Parent == result);
                    result = subkey;
                }
                else
                {
                    RegKeyEntry subkey = new RegKeyEntry(result, token);
                    result.Keys[key] = subkey;
                    Trace.Assert(subkey.Parent == result);
                    result = subkey;
                }
            }
            return result;
        }

        /// <summary>
        /// Write the content of this key to an output stream in .REG file format
        /// </summary>
        /// <param name="output">Output stream</param>
        public void WriteRegFileFormat(TextWriter output)
        {
            List<string> names;

            if (!string.IsNullOrEmpty(Name))
            {
                if (RemoveFlag)
                {
                    output.WriteLine("[-{0}]", Path);
                }
                else
                {
                    output.WriteLine("[{0}]", Path);
                    if (DefaultValue != null)
                    {
                        DefaultValue.WriteRegFileFormat(output);
                    }

                    names = Values.Keys.ToList<string>();
                    names.Sort();
                    foreach (string name in names)
                    {
                        Values[name].WriteRegFileFormat(output);
                    }
                }
                output.WriteLine();
            }

            names = Keys.Keys.ToList<string>();
            names.Sort();
            foreach (string name in names)
            {
                Keys[name].WriteRegFileFormat(output);
            }
        }

        private static RegistrySecurity AllAccessForEveryoneCache = null;

        private static IdentityReference GetEveryOneGroupSID()
        {
            WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
            foreach (IdentityReference refID in currentUser.Groups)
            {
                if (refID.Value == "S-1-1-0") // "S-1-1-0" is the Well-Known-SID for the "Everyone" Group
                {
                    return refID;
                }
            }
            return null;
        }

        /// <summary>
        /// Helper function: RegistrySecurity object representing "Full control for the Everyone group" 
        /// </summary>
        public static RegistrySecurity AllAccessForEveryone
        {
            get
            {
                if( AllAccessForEveryoneCache == null )
                {
                    // Create a security object that grants no access.
                    AllAccessForEveryoneCache = new RegistrySecurity();

                    // Add a rule that grants the current user ReadKey
                    // rights. ReadKey is a combination of four other 
                    // rights. The rule is inherited by all 
                    // contained subkeys.
                    RegistryAccessRule rule = new RegistryAccessRule(GetEveryOneGroupSID(),
                        RegistryRights.FullControl,
                        InheritanceFlags.ContainerInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow);
                    AllAccessForEveryoneCache.AddAccessRule(rule);
                }
                return AllAccessForEveryoneCache;
            }
        }

        /// <summary>
        /// Write the contents of this object back to the registry (possibly recursively)
        /// </summary>
        /// <param name="registryWriteOptions">Options for writing to the registry</param>
        /// <param name="env">Optional handler for environment variable replacement</param>
        /// <param name="registryView">Type of registry you want to see (32-bit, 64-bit, default).</param>
        public void WriteToTheRegistry(RegistryWriteOptions registryWriteOptions, RegEnvReplace env, RegistryView registryView)
        {
            if ((registryWriteOptions & RegistryWriteOptions.Recursive) != 0)
            {
                foreach (RegKeyEntry subkey in Keys.Values)
                {
                    subkey.WriteToTheRegistry(registryWriteOptions, env, registryView);
                }
            }

            string rootPath = env.Map(Path);
            string rootPathWithoutHive;
            using (RegistryKey registryKey = Regis3.OpenRegistryHive(rootPath, out rootPathWithoutHive, registryView))
            {
                if ((registryWriteOptions & RegistryWriteOptions.AllAccessForEveryone) != 0)
                {
                    using (RegistryKey subkey = registryKey.CreateSubKey(rootPathWithoutHive,
                        RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None, AllAccessForEveryone))
                    {
                        foreach (RegValueEntry regValueEntry in Values.Values)
                        {
                            regValueEntry.WriteToTheRegistry(subkey, env);
                        }
                        subkey.Close();
                    }
                }
                else
                {
                    using (RegistryKey subkey = registryKey.CreateSubKey(rootPathWithoutHive))
                    {
                        foreach (RegValueEntry regValueEntry in Values.Values)
                        {
                            regValueEntry.WriteToTheRegistry(subkey, env);
                        }
                        subkey.Close();
                    }
                }
            }
        }


    }
}
