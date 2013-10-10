using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace com.tikumo.regdiff
{
    public static class AppVersion
    {
        public static string Get()
        {
            string appName = Assembly.GetEntryAssembly().Location;
            AssemblyName assemblyName = AssemblyName.GetAssemblyName(appName);

            string[] tokens = assemblyName.Version.ToString().Split('.');
            return string.Format("{0}.{1}", tokens[0], tokens[1]);
        }
    }
}
