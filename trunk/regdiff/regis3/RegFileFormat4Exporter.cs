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
    /// Subclass of RegFileExporter for Version 4 files (=ANSI Encoding, Windows NT 4.0)
    /// </summary>
    public class RegFileFormat4Exporter : RegFileExporter
    {
        public RegFileFormat4Exporter()
            : base(RegFileFormat4Importer.HEADER, Encoding.Default)
        {
        }
    }
}
