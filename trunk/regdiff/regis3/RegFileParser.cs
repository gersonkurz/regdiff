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
using System.Globalization;

namespace com.tikumo.regis3
{
    /// <summary>
    /// This is the default parser for .REG files. It takes a string as input and produces a valid registry tree from it.
    /// </summary>
    public class RegFileParser : AbstractFileParser
    {
        private RegKeyEntry Result;
        private readonly RegFileImportOptions Options;
        private RegKeyEntry CurrentKey;
        private RegValueEntry CurrentValue;
        private RegValueEntryKind CurrentDataKind = RegValueEntryKind.Unknown;
        private readonly string HeaderId;
        private int NumberOfClosingBracketsExpected;

        /// <summary>
        /// The constructor needs an expected header identifying the version of the .REG file, as well as import options
        /// </summary>
        /// <param name="expectedHeader">expected header identifying the version of the .REG file</param>
        /// <param name="options">import options</param>
        public RegFileParser(string expectedHeader, RegFileImportOptions options)
        {
            Options = options;
            Result = new RegKeyEntry(null, null);
            HeaderId = expectedHeader;
            NumberOfClosingBracketsExpected = 0;
        }

        /// <summary>
        /// This method imports a registry key from the text of a .REG file
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public RegKeyEntry Parse(string content)
        {
            Parse(content, ExpectHeader);

            if ((Result.Keys.Count == 1) && (Result.Values.Count == 0))
            {
                foreach (string key in Result.Keys.Keys)
                {
                    Result = Result.Keys[key];
                    break;
                }
            }
            return Result;
        }

        private bool AllowVariableNamesForNonStringVariables
        {
            get
            {
                return (Options & RegFileImportOptions.AllowVariableNamesForNonStringVariables) != 0;
            }
        }

        private bool AllowHashtagComments
        {
            get
            {
                return (Options & RegFileImportOptions.AllowHashtagComments) != 0;
            }
        }

        private bool AllowSemicolonComments
        {
            get
            {
                return (Options & RegFileImportOptions.AllowSemicolonComments) != 0;
            }
        }

        #region Parser States
        private void ExpectHeader(char c)
        {
            if (c == '\r')
            {
                string header = Buffer.ToString();
                if( !header.Equals(HeaderId) )
                {
                    throw SyntaxError("ERROR: .REG file expected header: '{0}', got '{1}' instead", HeaderId, header);
                }
                ParserState = ExpectNewline;
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
            else if (c == ' ' || c == '\t' )
            {
            }
            else if (c == '#')
            {
                ParserState = ExpectCommentUntilEndOfLine;
            }
            else if (c == ';')
            {
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
                ParserState = ExpectKeyPath;
            }
            else if (c == '@')
            {
                CurrentValue = CurrentKey.FindOrCreateValue(null);
                ParserState = ExpectEqualSign;
            }
            else if (c == '"')
            {
                Buffer.Clear();
                ParserState = ExpectValueNameDefinition;
            }
            else if ((c == '#') && AllowHashtagComments)
            {
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
            }
            else if ((c == ';') && AllowSemicolonComments)
            {
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
            }
            else
            {
                throw SyntaxError("ERROR, don't support values yet; '{0}'", c);
            }
        }

        private void ExpectCommentUntilEndOfLine(char c)
        {
            if (c == '\n')
            {
                ++LineNumber;
                ParserState = ExpectStartOfLine;
            }
        }

        private void ExpectValueNameDefinition(char c)
        {
            if (c == '"')
            {
                CurrentValue = CurrentKey.FindOrCreateValue(Buffer.ToString());
                ParserState = ExpectEqualSign;
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

        private void ExpectEqualSign(char c)
        {
            if (c == '=')
            {
                Buffer.Clear();
                ParserState = ExpectStartOfValueDefinition;
            }
            else
            {
                throw SyntaxError("ERROR, expected '=', got '{0}' instead.", c);
            }
        }

        private void ExpectStartOfValueDefinition(char c)
        {
            if (c == '"')
            {
                ParserState = ExpectStringValueDefinition;
                Buffer.Clear();
            }
            else if (c == ':')
            {
                string typename = Buffer.ToString().ToLower();
                Buffer.Clear();
                if (typename.Equals("dword", StringComparison.OrdinalIgnoreCase))
                {
                    ParserState = ExpectHexIntegerValue;
                }
                else if( typename.StartsWith("hex(") && typename.EndsWith(")") )
                {
                    string[] tokens = typename.Split('(', ')');
                    if (tokens.Length >= 2)
                    {
                        int kindValueAsInt = 0;
                        if (!int.TryParse(tokens[1], NumberStyles.HexNumber, null, out kindValueAsInt))
                        {
                            throw SyntaxError("ERROR, '{0}' is not a valid hex() kind ", tokens[1]);
                        }
                        RegValueEntryKind kind = (RegValueEntryKind)kindValueAsInt;
                        Buffer.Clear();
                        CurrentDataKind = kind;
                        ParserState = ExpectStartOfMultiByteValueDefinition;
                    }
                }
                else if (typename.Equals("hex"))
                {
                    CurrentDataKind = RegValueEntryKind.Binary;
                    ParserState = ExpectStartOfMultiByteValueDefinition;
                }
                else
                {
                    throw SyntaxError("ERROR, value type '{0}' not supported", typename);
                }
            }
            else
            {
                Buffer.Append(c);
            }
        }

        private bool IsWhitespace(char c)
        {
            if ((Options & RegFileImportOptions.IgnoreWhitespaces) != 0)
            {
                return (c == ' ') || (c == '\t');
            }
            return false;
        }

        private void DecodeCurrentHexValue()
        {
            int result = 0;
            string value = Buffer.ToString();
            Buffer.Clear();

            if (int.TryParse(value, NumberStyles.HexNumber, null, out result))
            {
                CurrentValue.SetIntValue(result);
                ParserState = ExpectNewline;
                Buffer.Clear();
            }
            else
            {
                throw SyntaxError("ERROR, '{0}' is not a valid integer", value);
            }
        }

        private void ExpectHexIntegerValue(char c)
        {
            if (c == '\r')
            {
                DecodeCurrentHexValue();
            }
            else if ("0123456789ABCDEFabcdef".IndexOf(c) >= 0)
            {
                Buffer.Append(c);
            }
            else if (IsWhitespace(c))
            {
            }
            else if ((c == '#') && AllowHashtagComments)
            {
                DecodeCurrentHexValue();
                ParserState = ExpectCommentUntilEndOfLine;
            }
            else if ((c == ';') && AllowHashtagComments)
            {
                DecodeCurrentHexValue();
                ParserState = ExpectCommentUntilEndOfLine;
            }
            else if ((c == '$') && AllowVariableNamesForNonStringVariables)
            {
                Buffer.Append(c);
                ParserState = ExpectVariableDefinedHexIntegerValue;
            }
            else
            {
                throw SyntaxError("ERROR, '{0}' is not a valid hex digit", c);
            }
        }

        private void DecodeCurrentVariableDefinedHexValue()
        {
            CurrentValue.SetEscapedIntValue(Buffer.ToString());
            ParserState = ExpectNewline;
            Buffer.Clear();
        }

        private void ExpectVariableDefinedHexIntegerValue(char c)
        {
            if (c == '\r')
            {
                DecodeCurrentVariableDefinedHexValue();
            }
            else if ((c == '#') && AllowHashtagComments)
            {
                DecodeCurrentVariableDefinedHexValue();
                ParserState = ExpectCommentUntilEndOfLine;
            }
            else if ((c == ';') && AllowHashtagComments)
            {
                DecodeCurrentVariableDefinedHexValue();
                ParserState = ExpectCommentUntilEndOfLine;
            }
            else
            {
                Buffer.Append(c);
            }
        }

        private void ExpectStringValueDefinition(char c)
        {
            if (c == '"')
            {
                CurrentValue.SetStringValue(Buffer.ToString());
                ParserState = ExpectCarriageReturn;
            }
            else if (c == '\\')
            {
                ParserState = ExpectQuotedCharInStringValueDefinition;
            }
            else
            {
                Buffer.Append(c);
            }
        }

        private void ExpectQuotedCharInStringValueDefinition(char c)
        {
            Buffer.Append(c);
            ParserState = ExpectStringValueDefinition;
        }

        private void ExpectStartOfMultiByteValueDefinition(char c)
        {
            if (c == '\r')
            {
                ExpectMultiByteValueDefinition(c);
            }
            else
            {
                int result;
                string value = c.ToString();
                if (int.TryParse(value, NumberStyles.HexNumber, null, out result))
                {
                    Buffer.Append(c);
                    ParserState = ExpectMultiByteValueDefinition;
                }
                else
                {
                    throw SyntaxError("ERROR, expected byte-oriented hex(2) definition");
                }
            }
        }

        private byte[] CreateByteArrayFromString(string input)
        {
            if ((input.Length % 2) != 0)
            {
                input += "0";
            }
            byte[] result = new byte[input.Length / 2];
            int j = 0;
            byte value = 0;
            for (int i = 0; i < input.Length; ++i)
            {
                if ((i % 2) == 0)
                {
                    value = (byte)int.Parse(input.Substring(i, 1), NumberStyles.HexNumber);
                    value <<= 4;
                }
                else
                {
                    value |= (byte)int.Parse(input.Substring(i, 1), NumberStyles.HexNumber);
                    result[j++] = value;
                }
            }

            return result;
        }

        private void ExpectMultiByteValueDefinition(char c)
        {
            if (c == ',')
            {
            }
            else if (c == '\\')
            {
                ParserState = ExpectNewlineFollowedByMultiByteValueDefinition;
            }
            else if ((c == ' ') || (c == '\t'))
            {
            }
            else if( c == '\r' )
            {
                CurrentValue.SetBinaryType(CurrentDataKind, CreateByteArrayFromString(Buffer.ToString()));
                ParserState = ExpectNewline;
            }
            else if (c == '\n')
            {
                throw SyntaxError("Got \\n without \\r - registry file is not properly encoded");
            }
            else
            {
                Buffer.Append(c);
            }
        }

        private void ExpectNewlineFollowedByMultiByteValueDefinition(char c)
        {
            if (c == '\r')
            {
            }
            else if (c == '\n')
            {
                ++LineNumber;
                ParserState = ExpectMultiByteValueDefinition;
            }
            else
            {
                throw SyntaxError("ERROR, expected newline to follow trailing backslash");
            }
        }

        private void ExpectKeyPath(char c)
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
                    CurrentKey = Result.FindOrCreateKey(Buffer.ToString());
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

        #endregion


    }
}
