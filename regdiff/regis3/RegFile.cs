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
    public static class RegFile
    {
        public static IRegistryImporter CreateImporterFromFile(string filename, RegFileImportOptions options)
        {
            return CreateImporterFromString(File.ReadAllText(filename), options);
        }

        public static IRegistryImporter CreateImporterFromString(string content, RegFileImportOptions options)
        {
            if (content.StartsWith(RegFileFormat5Importer.HEADER))
            {
                return new RegFileFormat5Importer(content, options);
            }
            else if (content.StartsWith(RegFileFormat4Importer.HEADER))
            {
                return new RegFileFormat4Importer(content, options);
            }
            throw new BadImageFormatException("Unsupported .REG file format detected");
        }
    }
}
