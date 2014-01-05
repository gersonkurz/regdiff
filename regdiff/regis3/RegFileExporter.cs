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

using System.Text;
using System.IO;

namespace com.tikumo.regis3
{
    /// <summary>
    /// This is an exporter that takes a RegKeyEntry and exports it. 
    /// The class is abstract, because version 4 and version 5 have different text encoding, so you need
    /// a derived class to actually export the registry.
    /// </summary>
    public abstract class RegFileExporter : IRegistryExporter
    {
        private readonly string Header;
        private readonly Encoding FileEncoding;

        /// <summary>
        /// This constructor creates an exporter with a fixed header and encoding
        /// </summary>
        /// <param name="header"></param>
        /// <param name="fileEncoding"></param>
        protected RegFileExporter(string header, Encoding fileEncoding)
        {
            Header = header;
            FileEncoding = fileEncoding;
        }

        /// <summary>
        /// Export the given registry key to a file
        /// </summary>
        /// <param name="key">Registry key previously imported (or constructed in memory)</param>
        /// <param name="filename">Filename to save the key in</param>
        /// /// <param name="options">Export options</param>
        public void Export(RegKeyEntry key, string filename, RegFileExportOptions options)
        {
            using (StreamWriter sw = new StreamWriter(File.Open(filename, FileMode.Create), FileEncoding))
            {
                try
                {
                    Export(key, sw, options);
                }
                finally
                {
                    sw.Close();
                }
            }
        }

        /// <summary>
        /// Export the given registry key to a text stream
        /// </summary>
        /// <param name="key">Registry key previously imported (or constructed in memory)</param>
        /// <param name="file">Output text stream</param>
        /// /// <param name="options">Export options</param>
        public void Export(RegKeyEntry key, TextWriter file, RegFileExportOptions options)
        {
            file.WriteLine(Header);
            file.WriteLine();
            key.WriteRegFileFormat(file, options);
        }
    }
}
