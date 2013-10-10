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
using System.Text;

namespace com.tikumo.regis3
{
    /// <summary>
    /// This is a generic parser of file content. Both the .REG parser and the .INI parser are concrete implementations of this class.
    /// The basic idea is that the parser is always in a well-known state, and in each state only a subset of new states are allowed.
    /// </summary>
    public abstract class AbstractFileParser
    {
        /// <summary>
        /// Callback definition for parser states
        /// </summary>
        /// <param name="c">Character read at current position in text file</param>
        protected delegate void ExpectFunc(char c);

        /// <summary>
        /// This is the current parser state. Derived classes initialize and maintain this.
        /// </summary>
        protected ExpectFunc ParserState;

        /// <summary>
        /// Current line number
        /// </summary>
        protected int LineNumber;

        /// <summary>
        /// Current column number
        /// </summary>
        protected int ColumnNumber;

        /// <summary>
        /// Buffer used by parser states to collect longer strings
        /// </summary>
        protected readonly StringBuilder Buffer = new StringBuilder();

        /// <summary>
        /// Current zero-based offeset in CurrentContent
        /// </summary>
        private int CurrentIndex;

        /// <summary>
        /// Current content being parsed
        /// </summary>
        private string CurrentContent;

        /// <summary>
        /// This function should be used to create SyntaxError exceptions. It provides a generic helptext as well as contextual information
        /// (such as line / column number, and file context) in the trace data.
        /// 
        /// Typical use in a parser would be something like this:
        /// 
        /// throw SyntaxError("error message goes here")
        /// </summary>
        /// <param name="context">Context pattern</param>
        /// <param name="args">Context arguments</param>
        /// <returns>A System.Data.SyntaxErrorException exception</returns>
        protected Exception SyntaxError(string context, params object[] args)
        {
            StringBuilder errorMessage = new StringBuilder();
            errorMessage.AppendFormat("ERROR, parser failed at line {0}, col {1}", LineNumber, ColumnNumber);
            errorMessage.AppendLine();
            errorMessage.AppendFormat(context, args);
            errorMessage.AppendLine();

            int StartIndex = CurrentIndex;
            while ((StartIndex >= 0) && (CurrentContent[StartIndex] != '\n'))
            {
                --StartIndex;
            }
            if (StartIndex < 0)
                StartIndex = 0;
            else
                ++StartIndex;
            int StopIndex = CurrentIndex;
            while ((StopIndex < CurrentContent.Length) && (CurrentContent[StopIndex] != '\r') && (CurrentContent[StopIndex] != '\n'))
            {
                ++StopIndex;
            }
            if (StopIndex >= CurrentContent.Length)
            {
                StopIndex = CurrentContent.Length - 1;
            }


            errorMessage.Append(">> ");
            errorMessage.Append(CurrentContent.Substring(StartIndex, StopIndex - StartIndex));

            return new System.Data.SyntaxErrorException(errorMessage.ToString());
        }

        /// <summary>
        /// Start parsing the given content from an initial state
        /// </summary>
        /// <param name="content">Text content</param>
        /// <param name="initialFunc">Initial parser state</param>
        protected void Parse(string content, ExpectFunc initialFunc)
        {
            CurrentIndex = 0;
            CurrentContent = content;
            ParserState = initialFunc;
            LineNumber = 1;
            ColumnNumber = 0;

            foreach (char c in content)
            {
                ++ColumnNumber;
                ParserState(c);
                ++CurrentIndex;
            }
        }
    }
}
