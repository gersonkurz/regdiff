// Copyright (c) 2013, Gerson Kurz
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// Redistributions of source code must retain the above copyright notice, this list
// of conditions and the following disclaimer. Redistributions in binary form must
// reproduce the above copyright notice, this list of conditions and the following
// disclaimer in the documentation and/or other materials provided with the distribution.
// 
// Neither the name regdiff nor the names of its contributors may be used to endorse
// or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
// IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
// NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

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
    /// <summary>
    /// This program is a small command line tool to compare two registry files, export the registry, merge .REG files and much more. 
    /// Refer to http://p-nand-q.com/download/regdiff.html for a detailed manual.
    /// Refer to http://code.google.com/p/regdiff/ for the complete sourcecode.
    /// Copyright (C) 2013 by Gerson Kurz
    /// BSD-Licensed
    /// </summary>
    /// <todo>support remove-keys/values when reading .REG File format</todo>
    /// <todo>throw syntax error if $$ option cannot be found</todo>
    /// <todo>support path aliasing</todo>
    /// <todo>LATER: hex dump format to mimic regedit format</todo>
    /// <todo>LATER: support multiple locale strings</todo>
    class regdiff
    {
        private RegFileImportOptions Options;
        private InputArgs Args;
        private List<string> Filenames;
        private bool FileFormat4;
        private bool FileFormatXML;
        private bool Quiet;
        private bool NoEmptyKeys;
        private bool CompareAgainstRegistry;
        private string DiffFile;
        private bool WriteToTheRegistry;
        private bool AllAccess;
        private string MergeFile;
        private string ParamsFilename;
        private readonly List<RegKeyEntry> Files = new List<RegKeyEntry>();
        private RegistryView CurrentRegistryView;
        private readonly Dictionary<string, string> Aliases = new Dictionary<string, string>();
        
        private int Run(string[] args)
        {
            Options = RegFileImportOptions.IgnoreWhitespaces;
            Console.OutputEncoding = Encoding.GetEncoding(Encoding.Default.CodePage);

            string processType;
            if (Wow.Is64BitProcess)
            {
                processType = "64-bit";
                CurrentRegistryView = RegistryView.Registry64;
            }
            else if (Wow.Is64BitOperatingSystem)
            {
                processType = "32-bit process on 64-bit OS";
                CurrentRegistryView = RegistryView.Registry32;
            }
            else
            {
                processType = "32-bit";
                CurrentRegistryView = RegistryView.Default;
            }


            Args = new InputArgs(
                "REGDIFF",
                string.Format("Version {0}\r\nFreeware written by Gerson Kurz (http://p-nand-q.com) [{1}]",
                AppVersion.Get(), processType));
            
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
            Args.Add(InputArgType.MultipleParameters, "alias", null, Presence.Optional, "alias FOO=BAR");
            Args.Add(InputArgType.Flag, "no-empty-keys", false, Presence.Optional, "don't create empty keys");

            if (Wow.Is64BitProcess)
            {
                Args.Add(InputArgType.Flag, "32", false, Presence.Optional, "use 32-bit registry (default for this process: 64-bit)");
            }
            else if (Wow.Is64BitOperatingSystem)
            {
                Args.Add(InputArgType.Flag, "64", false, Presence.Optional, "use 64-bit registry (default for this process: 32-bit)");
            }
            else
            {
                // There is only the 32-bit registry: no need to add this
            }

            if (!Args.Process(args))
                return 10;

            List<string> aliases = Args.GetStringList("alias");
            if (aliases != null)
            {
                foreach(string alias in aliases)
                {
                    string[] tokens = alias.Split('=');
                    if (tokens.Length == 2)
                    {
                        Aliases[tokens[0].ToLower()] = tokens[1];
                        Aliases[tokens[1].ToLower()] = tokens[0];
                    }
                    else
                    {
                        Console.WriteLine("ERROR, the /alias option must be of the form FOO=BAR");
                        return 10;
                    }
                }
            }

            Filenames = Args.GetStringList("FILE {FILE}");
            FileFormat4 = Args.GetFlag("4");
            Quiet = Args.GetFlag("quiet");
            NoEmptyKeys = Args.GetFlag("no-empty-keys");
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

            if (Wow.Is64BitProcess)
            {
                if (Args.GetFlag("32"))
                {
                    CurrentRegistryView = RegistryView.Registry32;
                }
            }
            else if (Wow.Is64BitOperatingSystem)
            {
                if (Args.GetFlag("64"))
                {
                    CurrentRegistryView = RegistryView.Registry64;
                }
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

                IRegistryImporter importer = null;
                try
                {
                    if (File.Exists(filename))
                    {
                        if (filename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        {
                            importer = new XmlRegFileImporter(File.ReadAllText(filename));
                        }
                        else if (filename.EndsWith(".reg", StringComparison.OrdinalIgnoreCase))
                        {
                            importer = RegFile.CreateImporterFromFile(filename, Options);
                        }
                    }
                    if (importer == null)
                    {
                        string rootPathWithoutHive;
                        RegistryKey key = Regis3.OpenRegistryHive(filename, out rootPathWithoutHive, CurrentRegistryView);
                        if (key != null)
                        {
                            importer = new RegistryImporter(key, rootPathWithoutHive);
                        }
                        else
                        {
                            importer = RegFile.CreateImporterFromFile(filename, Options);
                        }
                    }
                    Files.Add(importer.Import());
                }
                catch (System.Data.SyntaxErrorException e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }
            }
            if (CompareAgainstRegistry)
            {
                Files.Add(new RegistryImportRelativeToExistingRegKeyEntry(Files[0], CurrentRegistryView).Import());
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
                    RegDiff rc = new RegDiff(file1, Filenames[i], file2, Filenames[j], Aliases);
                    if (!Quiet)
                    {
                        Console.WriteLine(rc.ToString());
                    }

                    RegFileExportOptions options = RegFileExportOptions.None;
                    if (NoEmptyKeys)
                        options |= RegFileExportOptions.NoEmptyKeys;

                    if (!string.IsNullOrEmpty(DiffFile))
                    {
                        Console.WriteLine("Writing {0}...", DiffFile);
                        CreateRegFileExporter(DiffFile).Export(rc.CreateDiffKeyEntry(), DiffFile, options);
                        Console.WriteLine();
                    }
                    if (!string.IsNullOrEmpty(MergeFile))
                    {
                        Console.WriteLine("Writing {0}...", MergeFile);
                        CreateRegFileExporter(MergeFile).Export(rc.CreateMergeKeyEntry(), MergeFile, options);
                        Console.WriteLine();
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
                Console.WriteLine("Writing {0}...", MergeFile);

                RegFileExportOptions options = RegFileExportOptions.None;
                if (NoEmptyKeys)
                    options |= RegFileExportOptions.NoEmptyKeys;

                CreateRegFileExporter(MergeFile).Export(regKeyEntry, MergeFile, options);
                Console.WriteLine();
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
                regKeyEntry.WriteToTheRegistry(registryWriteOptions, env, CurrentRegistryView);
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

        private IRegistryExporter CreateRegFileExporter(string filename)
        {
            // allow the user to specify a filename ending in .xml rather than having to explicitly saying /XML
            if( filename.EndsWith(".xml", StringComparison.OrdinalIgnoreCase ))
                return new XmlRegFileExporter();

            if (FileFormatXML)
                return new XmlRegFileExporter();

            if (FileFormat4)
                return new RegFileFormat4Exporter();

            return new RegFileFormat5Exporter();
        }

        public static int Main(string[] args)
        {
            return new regdiff().Run(args);
        }
    }
}
