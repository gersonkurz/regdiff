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
using System.IO;

namespace com.tikumo.regis3
{
    /// <summary>
    /// This is a helper class that takes a .REG filename (or the content of a .REG file) and identifies the correct parser.
    /// Parsers distinguish between Registry Format 4 (ANSI) and 5 (Unicode)
    /// </summary>
    public static class RegFile
    {
        /// <summary>
        /// Given a filename, identify the proper registry importer
        /// </summary>
        /// <param name="filename">Filename (including path)</param>
        /// <param name="options">Registry import options</param>
        /// <returns>Registry importer suitable for this file</returns>
        public static IRegistryImporter CreateImporterFromFile(string filename, RegFileImportOptions options)
        {
            return CreateImporterFromString(File.ReadAllText(filename), options);
        }

        /// <summary>
        /// Given the content of a .REG file, identify the proper registry importer
        /// </summary>
        /// <param name="content">File content</param>
        /// <param name="options">Registry import options</param>
        /// <returns>Registry importer suitable for this file</returns>
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
