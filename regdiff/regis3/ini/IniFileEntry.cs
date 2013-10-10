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

namespace com.tikumo.regis3.ini
{
    /// <summary>
    /// This class represents a key/value assignment pair in an .INI file
    /// </summary>
    public class IniFileEntry
    {
        /// <summary>
        /// Name of the value
        /// </summary>
        public string Name;

        /// <summary>
        /// Data of the value (can be null if options permit)
        /// </summary>
        public string Data;

        /// <summary>
        /// Optional comment (can be null)
        /// </summary>
        public string Comment;

        /// <summary>
        /// Create a new key/value assignment pair
        /// </summary>
        /// <param name="name">Name of the value</param>
        /// <param name="data">Data of the value (can be null if options permit)</param>
        /// <param name="comment">Optional comment (can be null)</param>
        public IniFileEntry(string name, string data, string comment)
        {
            Name = name;
            Data = data;
            Comment = comment;
        }

        /// <summary>
        /// Flag indicating whether or not the value has a comment
        /// </summary>
        public bool HasComment
        {
            get
            {
                return !string.IsNullOrEmpty(Comment);
            }
        }
    }
}
