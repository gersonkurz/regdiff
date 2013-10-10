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
    /// Static helper functions of general use when dealing with registry objects
    /// </summary>
    public static class Regis3
    {
        public static RegistryKey OpenRegistryHive(string rootPath, out string rootPathWithoutHive, bool use32BitRegistry = true, string remoteMachineName = null)
        {
            rootPathWithoutHive = "";
            bool found = false;
            RegistryView registryView = use32BitRegistry ? RegistryView.Registry32 : RegistryView.Registry64;
            foreach (string key in KnownHives.Keys)
            {
                if (rootPath.StartsWith(key+"\\", StringComparison.OrdinalIgnoreCase))
                {
                    rootPathWithoutHive = rootPath.Substring(key.Length+1);
                    found = true;
                }
                if (rootPath.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    rootPathWithoutHive = rootPath.Substring(key.Length);
                    found = true;
                }
                if (found)
                {
                    if (string.IsNullOrEmpty(remoteMachineName))
                    {
                        return RegistryKey.OpenBaseKey(KnownHives[key], registryView);
                    }
                    else
                    {
                        return RegistryKey.OpenRemoteBaseKey(KnownHives[key], remoteMachineName, registryView);
                    }
                }
            }
            Trace.TraceWarning("'{0}' is not a well-formed registry path", rootPath);
            return null;
        }

        public static void DeleteKeyRecursive(string sourcePath, bool use32BitRegistry = true)
        {
            string subKeyName;
            using(RegistryKey root = OpenRegistryHive(sourcePath, out subKeyName, use32BitRegistry))
            {
                try
                {
                    root.DeleteSubKeyTree(subKeyName);
                }
                catch (Exception e)
                {
                    Trace.TraceError("Exception {0} caught while deleting registry key", e);
                }
            }
        }
        
        #region Private Helpers

        /// <summary>
        /// This is a mapping of all registry hive names to their corresponding values
        /// </summary>
        private static Dictionary<string, RegistryHive> KnownHives = new Dictionary<string, RegistryHive>()
        {
            { "HKEY_CLASSES_ROOT", RegistryHive.ClassesRoot },
            { "HKEY_CURRENT_USER", RegistryHive.CurrentUser },
            { "HKEY_USERS", RegistryHive.Users },
            { "HKEY_LOCAL_MACHINE", RegistryHive.LocalMachine },
            { "HKEY_PERFORMANCE_DATA", RegistryHive.PerformanceData },
            { "HKEY_CURRENT_CONFIG", RegistryHive.CurrentConfig },
            { "HKEY_DYN_DATA", RegistryHive.DynData },
            { "HKCR", RegistryHive.ClassesRoot },
            { "HKCU", RegistryHive.CurrentUser },
            { "HKLM", RegistryHive.LocalMachine },
            { "HKU", RegistryHive.Users },
            { "HKPD", RegistryHive.PerformanceData },
            { "HKCC", RegistryHive.CurrentConfig },
            { "HKDD", RegistryHive.DynData },
        };
        #endregion

    }
}
