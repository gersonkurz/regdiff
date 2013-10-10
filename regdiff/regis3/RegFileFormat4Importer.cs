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
    public class RegFileFormat4Importer : RegFileImporter
    {
        public static string HEADER = "REGEDIT4";

        public RegFileFormat4Importer(string content, RegFileImportOptions options)
            : base(content, HEADER, options)
        {
        }
    }
}
