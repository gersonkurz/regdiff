using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Security;

namespace com.tikumo.regis3
{
    public class RegKeyEntry
    {
        public readonly string Name;
        public RegKeyEntry Parent { get; protected set; }
        public readonly Dictionary<string, RegKeyEntry> Keys = new Dictionary<string, RegKeyEntry>();
        public readonly Dictionary<string, RegValueEntry> Values = new Dictionary<string, RegValueEntry>();
        public RegValueEntry DefaultValue;
        public bool RemoveFlag;

        protected RegKeyEntry()
            :   this(null, null)
        {
        }

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

        public RegKeyEntry AskToRemoveKey(RegKeyEntry removeThis)
        {
            RegKeyEntry key = AskToAddKey(removeThis);
            key.RemoveFlag = true;
            return key;
        }

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

        public override string ToString()
        {
            return Name;
        }

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


        public void WriteToTheRegistry(RegistryWriteOptions registryWriteOptions, RegEnvReplace env)
        {
            if ((registryWriteOptions & RegistryWriteOptions.Recursive) != 0)
            {
                foreach (RegKeyEntry subkey in Keys.Values)
                {
                    subkey.WriteToTheRegistry(registryWriteOptions, env);
                }
            }

            string rootPath = env.Map(Path);
            string rootPathWithoutHive;
            using (RegistryKey registryKey = Regis3.OpenRegistryHive(rootPath, out rootPathWithoutHive))
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
