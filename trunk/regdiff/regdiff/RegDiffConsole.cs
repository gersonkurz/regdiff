using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.tikumo.regis3;
using Microsoft.Win32;
using System.IO;
using System.Security.Principal;
using System.Security.AccessControl;

namespace com.tikumo.regdiff
{
    class RegDiffConsole
    {
        private RegFileImportOptions Options;
        private InputArgs Args;
        private List<string> Filenames;
        private bool FileFormat4;
        private bool FileFormatXML;
        private bool Quiet;
        private bool CompareAgainstRegistry;
        private string DiffFile;
        private bool WriteToTheRegistry;
        private bool AllAccess;
        private string MergeFile;
        private string ParamsFilename;
        private readonly List<RegKeyEntry> Files = new List<RegKeyEntry>();

        /// <summary>
        /// This program finds an executable on the PATH. It can also find other stuff on the path, but 
        /// mostly it finds the executable.s
        /// </summary>
        /// <param name="args"></param>
        private int Run(string[] args)
        {
            Options = RegFileImportOptions.IgnoreWhitespaces;
            Console.OutputEncoding = Encoding.GetEncoding(Encoding.Default.CodePage);
            Args = new InputArgs(
                "REGDIFF",
                string.Format("Version {0}\r\nFreeware written by Gerson Kurz (http://tikumo.com)",
                AppVersion.Get()));

            // TODO: support XML import 
            // TODO: support remove-keys/values when reading .REG File format
            // TODO: Export doesn't write blanks in front of hex dumps

            // LATER: hex dump format to mimic regedit format
            // LATER: support multiple locale strings
            
            Args.Add(InputArgType.RemainingParameters, "FILE {FILE}", null, Presence.Required, "one or more .REG files");
            Args.Add(InputArgType.Parameter, "merge", null, Presence.Optional, "create merged output file");
            Args.Add(InputArgType.Parameter, "diff", null, Presence.Optional, "create diff output file");
            Args.Add(InputArgType.Flag, "registry", false, Presence.Optional, "compare with the current registry value on your local machine");
            Args.Add(InputArgType.Flag, "4", false, Presence.Optional, "use .REG format 4 (non-unicode)");
            Args.Add(InputArgType.Flag, "quiet", false, Presence.Optional, "don't show diff on console");
            Args.Add(InputArgType.Flag, "xml", false, Presence.Optional, "use .XML format");
            Args.Add(InputArgType.Flag, "nocase", false, Presence.Optional, "ignore case (default: case-sensitive)");
            Args.Add(InputArgType.Flag, "write", false, Presence.Optional, "write keys/values to registry");
            Args.Add(InputArgType.Flag, "allaccess", false, Presence.Optional, "grant all access to everyone (when using the /write option)");
            Args.Add(InputArgType.Parameter, "params", null, Presence.Optional, "read value params from file (when using the /write option)");

            if (!Args.Process(args))
                return 10;

            Filenames = Args.GetStringList("FILE {FILE}");
            FileFormat4 = Args.GetFlag("4");
            Quiet = Args.GetFlag("quiet");
            FileFormatXML = Args.GetFlag("xml");
            DiffFile = Args.GetString("diff");
            MergeFile = Args.GetString("merge");
            CompareAgainstRegistry = Args.GetFlag("registry");
            WriteToTheRegistry = Args.GetFlag("write");
            AllAccess = Args.GetFlag("allaccess");
            ParamsFilename = Args.GetString("params");
            if (WriteToTheRegistry)
            {
                Options = RegFileImportOptions.AllowSemicolonComments | RegFileImportOptions.AllowHashtagComments | RegFileImportOptions.AllowVariableNamesForNonStringVariables | RegFileImportOptions.IgnoreWhitespaces;
            }

            if (CompareAgainstRegistry && (Filenames.Count > 1))
            {
                Console.WriteLine("ERROR, the /registry option supports only a single file");
                return 10;
            }
            if (!ReadInputFiles())
                return 10;

            if (Files.Count == 0)
            {
                Console.WriteLine("You must specify at least one file...");
                return 10;
            }

            if (Files.Count == 1)
            {
                return HandleSingleFile();
            }
            else
            {
                return HandleMultipleFiles();
            }
        }

        private bool ReadInputFiles()
        {
            foreach (string filename in Filenames)
            {
                Console.WriteLine("Reading {0}...", filename);

                string rootPathWithoutHive;
                RegistryKey key = Regis3.OpenRegistryHive(filename, out rootPathWithoutHive);
                IRegistryImporter importer;
                try
                {
                    if (key != null)
                    {
                        importer = new RegistryImporter(key, rootPathWithoutHive);
                    }
                    else
                    {
                        importer = RegFile.CreateImporterFromFile(filename, Options);
                    }
                    Files.Add(importer.Import());
                }
                catch (SyntaxErrorException e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }
            }
            if (CompareAgainstRegistry)
            {
                Files.Add(new RegistryImportRelativeToExistingRegKeyEntry(Files[0]).Import());
                Filenames.Add("REGISTRY");
            }
            Console.WriteLine();
            return true;
        }
        private int HandleMultipleFiles()
        {
            if (WriteToTheRegistry)
            {
                Console.WriteLine("ERROR, /write is only allowed with single files");
                return 10;
            }
            if (AllAccess)
            {
                Console.WriteLine("ERROR, /allaccess is only allowed in combination with /write");
                return 10;
            }
            if (!string.IsNullOrEmpty(ParamsFilename))
            {
                Console.WriteLine("ERROR, /params is only allowed in combination with /write");
                return 10;
            }
            for (int i = 0; i < Files.Count; ++i)
            {
                for (int j = i + 1; j < Files.Count; ++j)
                {
                    RegKeyEntry file1 = Files[i];
                    RegKeyEntry file2 = Files[j];
                    RegDiff rc = new RegDiff(file1, Filenames[i], file2, Filenames[j]);
                    if (!Quiet)
                    {
                        Console.WriteLine(rc.ToString());
                    }
                    if (!string.IsNullOrEmpty(DiffFile))
                    {
                        CreateRegFileExporter().Export(rc.CreateDiffKeyEntry(), DiffFile);
                    }
                    if (!string.IsNullOrEmpty(MergeFile))
                    {
                        CreateRegFileExporter().Export(rc.CreateMergeKeyEntry(), MergeFile);
                    }
                }
            }
            return 0;
        }

        private int HandleSingleFile()
        {
            RegKeyEntry regKeyEntry = Files[0];
            if (!string.IsNullOrEmpty(MergeFile))
            {
                CreateRegFileExporter().Export(regKeyEntry, MergeFile);
            }
            if (WriteToTheRegistry)
            {
                RegEnvReplace env = new RegEnvReplace();
                if (!string.IsNullOrEmpty(ParamsFilename))
                {
                    if (!File.Exists(ParamsFilename))
                    {
                        Console.WriteLine("ERROR, /params file '{0}' not found", ParamsFilename);
                        return 10;
                    }
                    try
                    {
                        if (ParamsFilename.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                        {
                            env.ReadIniFile(ParamsFilename);
                        }
                        else if (ParamsFilename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        {
                            env.ReadXmlFile(ParamsFilename);
                        }
                        else
                        {
                            Console.WriteLine("ERROR, /params file '{0}' has an unsupported extension (only '.xml' and '.ini' are allowed)", ParamsFilename);
                            return 10;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("ERROR, unable to read /params file '{0}'", ParamsFilename);
                        Console.WriteLine(e.Message);
                        return 10;
                    }
                }
                env.MergeEnvironmentVariables();
                RegistryWriteOptions registryWriteOptions = RegistryWriteOptions.Recursive;
                if (AllAccess)
                    registryWriteOptions |= RegistryWriteOptions.AllAccessForEveryone;
                regKeyEntry.WriteToTheRegistry(registryWriteOptions, env);
            }
            else if (AllAccess)
            {
                Console.WriteLine("ERROR, /allaccess is only allowed in combination with /write");
                return 10;
            }
            else if( !string.IsNullOrEmpty(ParamsFilename) )
            {
                Console.WriteLine("ERROR, /params is only allowed in combination with /write");
                return 10;
            }

            return 0;
        }

        private IRegistryExporter CreateRegFileExporter()
        {
            if (FileFormatXML)
                return new XmlRegFileExporter();

            if (FileFormat4)
                return new RegFileFormat4Exporter();

            return new RegFileFormat5Exporter();
        }

        public static int Main(string[] args)
        {
            string[] test = new string[] { "F:\\test.reg", "/params", "vars.ini", "/write", "/allaccess"};
            return new RegDiffConsole().Run(test);
        }
    }
}
