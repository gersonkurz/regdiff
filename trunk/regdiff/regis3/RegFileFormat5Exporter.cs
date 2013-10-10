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
    /// Subclass of RegFileExporter for Version 5 files (=Unicode)
    /// </summary>
    public class RegFileFormat5Exporter : RegFileExporter
    {
        public RegFileFormat5Exporter()
            : base(RegFileFormat5Importer.HEADER, Encoding.Unicode)
        {
        }
    }
}
