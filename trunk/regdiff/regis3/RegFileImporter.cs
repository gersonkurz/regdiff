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

namespace com.tikumo.regis3
{
    /// <summary>
    /// This is the importer for .REG files.
    /// The class is abstract, because version 4 and version 5 have different text encoding, so you need
    /// a derived class to actually import the registry.
    /// </summary>
    public abstract class RegFileImporter : IRegistryImporter
    {
        private RegKeyEntry Result;
        private readonly RegFileParser Parser;
        private readonly string Content;

        /// <summary>
        /// The constructor takes the content of a .REG file, the expected header (=first line of the .REG file) and import options (if any)
        /// </summary>
        /// <param name="content">Content of the .REG file</param>
        /// <param name="expectedHeader">Expected header</param>
        /// <param name="options">Import options</param>
        public RegFileImporter(string content, string expectedHeader, RegFileImportOptions options)
        {
            Parser = new RegFileParser(expectedHeader, options);
            Content = content;
            Result = null;
        }

        /// <summary>
        /// Import the content of the .REG file and return the RegKeyEntry created from it
        /// </summary>
        /// <returns></returns>
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
