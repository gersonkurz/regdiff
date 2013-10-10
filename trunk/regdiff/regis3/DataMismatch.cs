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
    /// When comparing two registry files, this class identifies a mismatch in the data of two registry values that are located
    /// in the same key path.
    /// </summary>
    public class DataMismatch
    {
        /// <summary>
        /// Parent registry key path (by definition, the same in both registry trees)
        /// </summary>
        public readonly RegKeyEntry Key;

        /// <summary>
        /// Data in first registry file
        /// </summary>
        public readonly RegValueEntry Value1;

        /// <summary>
        /// Data in the second registry file
        /// </summary>
        public readonly RegValueEntry Value2;

        /// <summary>
        /// This constructor identifies a mismatch in the data of two registry values that are located
        /// in the same key path.
        /// </summary>
        /// <param name="key">Parent registry key path (by definition, the same in both registry trees)</param>
        /// <param name="value1">Data in first registry file</param>
        /// <param name="value2">Data in the second registry file</param>
        public DataMismatch(RegKeyEntry key, RegValueEntry value1, RegValueEntry value2)
        {
            Key = key;
            Value1 = value1;
            Value2 = value2;
        }
    }
}
