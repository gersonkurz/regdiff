using System;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Diagnostics;

namespace com.tikumo.regis3
{
    /// <summary>
    /// Static helper functions of general use when dealing with registry objects
    /// </summary>
    public static class Regis3
    {
        /// <summary>
        /// Given a registry path, locate the correct root key and return the relative path. For example, when the user gives the absolute registry path
        /// 
        /// HKEY_LOCAL_MACHINE\Software\Microsoft
        /// 
        /// you really have two parts: HKEY_LOCAL_MACHINE is a "registry hive" root, and "Software\Microsoft" the relative path. 
        /// </summary>
        /// <param name="rootPath">absolute registry path</param>
        /// <param name="rootPathWithoutHive">Returns the relative path</param>
        /// <param name="use32BitRegistry">True if you want to access the 32-bit regisrty, or false if you want to access the 64-bit registry.</param>
        /// <param name="remoteMachineName">Name of a remote machine. User must ensure that caller has sufficient privileges to access the key</param>
        /// <returns>"registry hive" root</returns>
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

        /// <summary>
        /// Given an (absolute) registry key, delete everything under it including subkeys.
        /// </summary>
        /// <param name="sourcePath">Absolute registry path (i.e. one starting with HKEY_LOCAL_MACHINE or something similar)</param>
        /// <param name="use32BitRegistry">True if you want to access the 32-bit regisrty, or false if you want to access the 64-bit registry.</param>
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
