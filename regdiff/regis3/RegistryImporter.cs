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
    /// <summary>
    /// This is the standard importer to read data from an existing registry (on the local machine, or on a remote machine).
    /// </summary>
    public class RegistryImporter : IRegistryImporter
    {
        private readonly RegKeyEntry Result;

        /// <summary>
        /// The import function returns the RegKeyEntry imported from the registry
        /// </summary>
        /// <returns></returns>
        public RegKeyEntry Import()
        {
            return Result;
        }

        public RegistryImporter(string rootPath)
        {
            Result = new RegKeyEntry(null, rootPath);

            string rootPathWithoutHive;
            RegistryKey rootKey = Regis3.OpenRegistryHive(rootPath, out rootPathWithoutHive);
            using (RegistryKey key = rootKey.OpenSubKey(rootPathWithoutHive))
            {
                ImportRecursive(Result, key);
            }
        }

        public RegistryImporter(RegistryKey rootKey, string rootPath)
        {
            Result = new RegKeyEntry(null, rootKey.Name);

            RegKeyEntry parent = Result;
            foreach (string token in rootPath.Split('\\'))
            {
                RegKeyEntry next = new RegKeyEntry(parent, token);
                parent.Keys[token.ToLower()] = next;
                parent = next;
            }

            using (RegistryKey key = rootKey.OpenSubKey(rootPath))
            {
                ImportRecursive(parent, key);
            }
        }

        private void ImportRecursive(RegKeyEntry parent, RegistryKey key)
        {
            foreach (var name in key.GetSubKeyNames())
            {
                RegKeyEntry entry = new RegKeyEntry(parent, name);
                parent.Keys[name.ToLower()] = entry;
                try
                {
                    using (RegistryKey subkey = key.OpenSubKey(name))
                    {
                        ImportRecursive(entry, subkey);
                    }
                }
                catch (System.Security.SecurityException)
                {
                    // ignore
                }
            }

            foreach (var name in key.GetValueNames())
            {
                parent.Values[name.ToLower()] = new RegValueEntry(key, name);
            }
        }
    }
}
