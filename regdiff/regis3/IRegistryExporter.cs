﻿// Copyright (c) 2013, Gerson Kurz
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

using System.Collections.Generic;
using System.IO;

namespace regis3
{
    /// <summary>
    /// This is the export interface supported by all regis3 exporter functions: given a RegKeyEntry, create a file or a string.
    /// </summary>
    public interface IRegistryExporter
    {
        /// <summary>
        /// Given a registry key description, create a file
        /// </summary>
        /// <param name="key">Existing registry key description</param>
        /// <param name="filename">Filename to be created</param>
        /// <param name="options">Export options</param>
        void Export(RegKeyEntry key, string filename, RegFileExportOptions options);

        /// <summary>
        /// Given a registry key description, write a file to a stream
        /// </summary>
        /// <param name="key">Existing registry key description</param>
        /// <param name="file">Stream to be written to</param>
        /// /// <param name="options">Export options</param>
        void Export(RegKeyEntry key, TextWriter file, RegFileExportOptions options);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Sets replacement lookup. </summary>
        ///
        /// <param name="replacement">  The replacement.</param>
        ///-------------------------------------------------------------------------------------------------
        void SetReplacementLookup(Dictionary<string, string> replacement);
    }
}
