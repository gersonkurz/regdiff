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
    public abstract class RegFileImporter : IRegistryImporter
    {
        private RegKeyEntry Result;
        private readonly RegFileParser Parser;
        private readonly string Content;

        public RegFileImporter(string content, string expectedHeader, RegFileImportOptions options)
        {
            Parser = new RegFileParser(expectedHeader, options);
            Content = content;
            Result = null;
        }

        public RegKeyEntry Import()
        {
            if (Result == null)
            {
                Result = Parser.Parse(Content);
            }
            return Result;
        }

    }
}
