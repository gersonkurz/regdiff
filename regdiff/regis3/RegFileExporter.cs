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
    public abstract class RegFileExporter : IRegistryExporter
    {
        private readonly string Header;
        private readonly Encoding FileEncoding;

        protected RegFileExporter(string header, Encoding fileEncoding)
        {
            Header = header;
            FileEncoding = fileEncoding;
        }

        public void Export(RegKeyEntry key, string filename)
        {
            using (StreamWriter sw = new StreamWriter(File.Open(filename, FileMode.Create), FileEncoding))
            {
                try
                {
                    Export(key, sw);
                }
                finally
                {
                    sw.Close();
                }
            }
        }

        public void Export(RegKeyEntry key, TextWriter file)
        {
            file.WriteLine(Header);
            file.WriteLine();
            key.WriteRegFileFormat(file);
        }
    }
}
