using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Xml;
using System.Globalization;

namespace com.tikumo.regis3
{
    /// <summary>
    /// This is the import interface supported by all regis3 importer classes: get data from somewhere
    /// </summary>
    public interface IRegistryImporter
    {
        /// <summary>
        /// Import the registry data from some source (be it the live registry, a different machine, a registry file, an xml file and so on).
        /// </summary>
        /// <returns>a newly created RegKeyEntry object</returns>
        RegKeyEntry Import();
    }
}
