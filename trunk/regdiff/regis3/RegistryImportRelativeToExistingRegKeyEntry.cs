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
    public class RegistryImportRelativeToExistingRegKeyEntry : IRegistryImporter
    {
        private readonly RegKeyEntry Result;

        public RegKeyEntry Import()
        {
            return Result;
        }

        public RegistryImportRelativeToExistingRegKeyEntry(RegKeyEntry existingRegistry)
        {
            Result = new RegKeyEntry(null, existingRegistry.Path);

            string rootPath = existingRegistry.Path;
            string rootPathWithoutHive;
            RegistryKey rootKey = Regis3.OpenRegistryHive(rootPath, out rootPathWithoutHive);
            using (RegistryKey key = rootKey.OpenSubKey(rootPathWithoutHive))
            {
                ImportRecursive(Result, key, existingRegistry);
            }
        }

        private void ImportRecursive(RegKeyEntry parent, RegistryKey key, RegKeyEntry relativeKey)
        {
            foreach (var name in key.GetSubKeyNames())
            {
                string keyName = name.ToLower();
                if (relativeKey.Keys.ContainsKey(keyName))
                {
                    RegKeyEntry entry = new RegKeyEntry(parent, name);
                    parent.Keys[name.ToLower()] = entry;
                    try
                    {
                        using (RegistryKey subkey = key.OpenSubKey(name))
                        {
                            ImportRecursive(entry, subkey, relativeKey.Keys[keyName]);
                        }
                    }
                    catch (System.Security.SecurityException)
                    {
                        // ignore
                    }
                }
            }

            foreach (var name in key.GetValueNames())
            {
                parent.Values[name.ToLower()] = new RegValueEntry(key, name);
            }
        }
    }
}
