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

namespace com.tikumo.regis3.ini
{
    /// <summary>
    /// This class implements a parser for .INI files
    /// </summary>
    public class IniFileParser : AbstractFileParser
    {
        private readonly IniFileSection RootSection;
        private IniFileSection CurrentSection;
        private int NumberOfClosingBracketsExpected;
        private delegate IniFileSection CreateSectionFromNameHandler(string sectionName);
        private CreateSectionFromNameHandler CreateSectionFromName;
        private string CurrentValueName;
        private string CurrentValueData;
        private string CurrentComment;
        private readonly IniFileOptions Options;

        /// <summary>
        /// The default constructor creates a root section (typically a IniFile instance) and a set of parse options
        /// </summary>
        /// <param name="rootSection">Root section</param>
        /// <param name="options">Parse options</param>
        public IniFileParser(
            IniFileSection rootSection, 
            IniFileOptions options = IniFileOptions.KeepComments | IniFileOptions.KeepFlat | IniFileOptions.StripEmptyLines)
        {
            RootSection = rootSection;
            Options = options;
            CurrentSection = rootSection;
            CreateSectionFromName = CreateFlatSectionFromName;
        }

        private bool KeepComments
        {
            get
            {
                return (Options & IniFileOptions.KeepComments) != 0;
            }
        }

        private bool KeepFlat
        {
            get
            {
                return (Options & IniFileOptions.KeepFlat) != 0;
            }
        }

        private bool StripEmptyLines
        {
            get
            {
                return (Options & IniFileOptions.StripEmptyLines) != 0;
            }
        }

        /// <summary>
        /// Parse .INI file content
        /// </summary>
        /// <param name="content">.INI file content</param>
        public void Parse(string content)
        {
            Parse(content, ExpectStartOfLine);
        }

        private IniFileSection CreateFlatSectionFromName(string sectionName)
        {
            return new IniFileSection(sectionName, RootSection);
        }

        private void AddNewLine()
        {
            if (StripEmptyLines && string.IsNullOrEmpty(CurrentValueName))
            {
                // ignore this line
            }
            else
            {
                if (!KeepComments)
                    CurrentComment = null;

                CurrentSection.Entries.Add(new IniFileEntry(CurrentValueName, CurrentValueData, CurrentComment));
            }
            CurrentValueName = null;
            CurrentValueData = null;
            CurrentComment = null;
        }

        #region Parser States
        private void ExpectStartOfLine(char c)
        {
            if (c == '\r')
            {
            }
            else if (c == '\n')
            {
                ++LineNumber;
            }
            else if (c == '[')
            {
                Buffer.Clear();
                NumberOfClosingBracketsExpected = 0;
                ParserState = ExpectSectionName;
            }
            else if ( (c == ' ') || (c == '\t') )
            {
            }
            else if (c == '#')
            {
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
            }
            else if (c == ';')
            {
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
            }
            else
            {
                ParserState = ExpectValueNameDefinition;
                Buffer.Clear();
                Buffer.Append(c);
            }
        }

        private void ExpectSectionName(char c)
        {
            if (c == '[')
            {
                ++NumberOfClosingBracketsExpected;
                Buffer.Append(c);
            }
            else if (c == ']')
            {
                if (0 == NumberOfClosingBracketsExpected)
                {
                    CurrentSection = CreateSectionFromName(Buffer.ToString());
                    Buffer.Clear();
                    ParserState = ExpectCarriageReturn;
                }
                else if (NumberOfClosingBracketsExpected > 0)
                {
                    --NumberOfClosingBracketsExpected;
                    Buffer.Append(c);
                }
                else
                {
                    throw SyntaxError("Too many closing square brackets");
                }
            }
            else
            {
                Buffer.Append(c);
            }
        }

        private void ExpectCarriageReturn(char c)
        {
            if (c == '\r')
            {
                ParserState = ExpectStartOfLine;
            }
            else if (c == ' ' || c == '\t')
            {
            }
            else if (c == '#')
            {
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
            }
            else if (c == ';')
            {
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
            }
            else
            {
                throw SyntaxError("ERROR, expected carriage return but got '{0}' instead", c);
            }
        }

        private void ExpectNewline(char c)
        {
            if (c == '\n')
            {
                ++LineNumber;
                ParserState = ExpectStartOfLine;
            }
            else
            {
                throw SyntaxError("ERROR, expected newline but got '{0}' instead", c);
            }
        }

        private void ExpectCommentUntilEndOfLine(char c)
        {
            if (c == '\r')
            {
                CurrentComment = Buffer.ToString().Trim();
                AddNewLine();
                ++LineNumber;
                ParserState = ExpectNewline;
            }
            else 
            {
                Buffer.Append(c);
            }
        }

        private void ExpectValueNameDefinition(char c)
        {
            if (c == '=')
            {
                CurrentValueName = Buffer.ToString().Trim();
                Buffer.Clear();
                ParserState = ExpectValueDataDefinition;
            }
            else if (c == '\\')
            {
                ParserState = ExpectQuotedCharInStringValueNameDefinition;
            }
            else
            {
                Buffer.Append(c);
            }
        }

        private void ExpectQuotedCharInStringValueNameDefinition(char c)
        {
            Buffer.Append(c);
            ParserState = ExpectValueNameDefinition;
        }

        private void ExpectValueDataDefinition(char c)
        {
            if (c == '\r')
            {
                CurrentValueData = Buffer.ToString().Trim();
                AddNewLine();
                Buffer.Clear();
                ParserState = ExpectNewline;
            }
            else if (c == '#')
            {
                CurrentValueData = Buffer.ToString().Trim();
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
            }
            else if (c == ';')
            {
                CurrentValueData = Buffer.ToString().Trim();
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
            }
            else if (c == '\\')
            {
                ParserState = ExpectQuotedCharInStringValueNameDefinition;
            }
            else
            {
                Buffer.Append(c);
            }
        }
        #endregion


    }
}
