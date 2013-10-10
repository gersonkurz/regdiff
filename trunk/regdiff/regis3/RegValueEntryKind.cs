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
    /// This class defines the known registry value types. It includes more types than the standard .Net enum RegistryValueKind
    /// </summary>
    public enum RegValueEntryKind
    {
        /// <summary>
        /// Registry value kind unknown or invalid
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// No value kind associated
        /// </summary>
        None = 0,

        /// <summary>
        /// "String Zero-Terminated": a string
        /// </summary>
        SZ = 1,

        /// <summary>
        /// "Expandable String": a string that can include environment variable specifications. By default,
        /// Windows will implicitly expand the string when reading it.
        /// </summary>
        ExpandSZ = 2,

        /// <summary>
        /// Binary data
        /// </summary>
        Binary = 3,

        /// <summary>
        /// A (unsigned) integer (little endian = intel default)
        /// </summary>
        DWord = 4,

        /// <summary>
        /// A (unsigned) integer (big endian). Not sure if I have ever seen one of these beasts live in the wild
        /// </summary>
        DWordBigEndian = 5,

        /// <summary>
        /// Registry link
        /// </summary>
        Link = 6,

        /// <summary>
        /// A list of multiple strings.
        /// </summary>
        MultiSZ = 7,

        /// <summary>
        /// A Windows NT resource list
        /// </summary>
        ResourceList = 8,

        /// <summary>
        /// A Windows NT full resource descriptor
        /// </summary>
        FullResourceDescriptor = 9,

        /// <summary>
        /// A Windows NT resource requirements list
        /// </summary>
        ResourceRequirementsList = 10,

        /// <summary>
        /// A 64-bit integer (little endian = intel default)
        /// </summary>
        QWord = 11,
    }
}
