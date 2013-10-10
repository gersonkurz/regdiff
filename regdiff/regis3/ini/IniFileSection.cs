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
using System.Text;

namespace com.tikumo.regis3.ini
{
    /// <summary>
    /// This class represents a section in an .INI file. INI files can be organized to have recursive sections, so for example
    /// 
    /// [foo\bar\blub] 
    /// 
    /// could either be represented as a single section named 'foo\bar\blub', or three sections named 'foo', 'bar' and 'blub' respectively.
    /// </summary>
    public class IniFileSection
    {
        /// <summary>
        /// List of child sections. Can be empty for flat files
        /// </summary>
        public readonly List<IniFileSection> Sections = new List<IniFileSection>();

        /// <summary>
        /// List of entries in this section. 
        /// </summary>
        public readonly List<IniFileEntry> Entries = new List<IniFileEntry>();

        /// <summary>
        /// Name of this section
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Parent section or null if this is the root section
        /// </summary>
        public readonly IniFileSection Parent;

        /// <summary>
        /// The default constructor creates an unamed section without a parent. Used only by the public IniFile constructor.
        /// </summary>
        protected IniFileSection()
        {
            Name = null;
            Parent = null;
        }

        /// <summary>
        /// This constructor creates a named section with a parent
        /// </summary>
        /// <param name="name">Name of this section</param>
        /// <param name="parent">Parent section. Must not be null</param>
        public IniFileSection(string name, IniFileSection parent)
        {
            Name = name;
            Parent = parent;
            Parent.Sections.Add(this);
        }

        /// <summary>
        /// Create a string description of this instance
        /// </summary>
        /// <returns>String description of this instance</returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();

            foreach (IniFileSection section in Sections)
            {
                output.AppendLine(section.ToString());
            }

            if (Parent != null)
            {
                Console.WriteLine("[{0}]", Name);
            }

            foreach (IniFileEntry entry in Entries)
            {
                if (entry.HasComment)
                {
                    Console.WriteLine("{0}={1} # {2}",
                        entry.Name, entry.Data, entry.Comment);
                }
                else
                {
                    Console.WriteLine("{0}={1}",
                        entry.Name, entry.Data);
                }
            }
            
            return output.ToString();
        }
    }
}
