using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Xml;
using System.Globalization;

namespace com.tikumo.regis3
{
    public class RegFileParser : AbstractFileParser
    {
        private RegKeyEntry Result;
        private readonly RegFileImportOptions Options;
        private RegKeyEntry CurrentKey;
        private RegValueEntry CurrentValue;
        private RegValueEntryKind CurrentDataKind = RegValueEntryKind.Unknown;
        private readonly string HeaderId;
        private int NumberOfClosingBracketsExpected;

        public RegFileParser(string expectedHeader, RegFileImportOptions options)
        {
            Options = options;
            Result = new RegKeyEntry(null, null);
            HeaderId = expectedHeader;
            NumberOfClosingBracketsExpected = 0;
        }

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

        public RegKeyEntry Import()
        {
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
        private bool ExpectHeader(char c)
        {
            if (c == '\r')
            {
                string header = Buffer.ToString();
                if( !header.Equals(HeaderId) )
                {
                    Trace.TraceError("ERROR: .REG file expected header: '{0}', got '{1}' instead", HeaderId, header);
                    return false;
                }
                ParserState = ExpectNewline;
                return true;
            }
            else
            {
                Buffer.Append(c);
                return true;
            }
        }

        private bool ExpectCarriageReturn(char c)
        {
            if (c == '\r')
            {
                ParserState = ExpectStartOfLine;
                return true;
            }
            else if (c == ' ' || c == '\t' )
            {
                return true;
            }
            else if (c == '#')
            {
                ParserState = ExpectCommentUntilEndOfLine;
                return true;
            }
            else if (c == ';')
            {
                ParserState = ExpectCommentUntilEndOfLine;
                return true;
            }
            else
            {
                Trace.TraceError("ERROR, expected carriage return but got '{0}' instead", c);
                return false;
            }
        }

        private bool ExpectNewline(char c)
        {
            if (c == '\n')
            {
                ++LineNumber;
                ParserState = ExpectStartOfLine;
                return true;
            }
            else
            {
                Trace.TraceError("ERROR, expected newline but got '{0}' instead", c); 
                return false;
            }
        }

        private bool ExpectStartOfLine(char c)
        {
            if (c == '\r')
            {
                return true;
            }
            else if (c == '\n')
            {
                ++LineNumber;
                return true;
            }
            else if (c == '[')
            {
                Buffer.Clear();
                NumberOfClosingBracketsExpected = 0;
                ParserState = ExpectKeyPath;
                return true;
            }
            else if (c == '@')
            {
                CurrentValue = CurrentKey.FindOrCreateValue(null);
                ParserState = ExpectEqualSign;
                return true;
            }
            else if (c == '"')
            {
                Buffer.Clear();
                ParserState = ExpectValueNameDefinition;
                return true;
            }
            else if ((c == '#') && AllowHashtagComments)
            {
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
                return true;
            }
            else if ((c == ';') && AllowSemicolonComments)
            {
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
                return true;
            }
            else
            {
                Trace.TraceError("ERROR, don't support values yet; '{0}'", c);
                return false;
            }
        }

        private bool ExpectCommentUntilEndOfLine(char c)
        {
            if (c == '\n')
            {
                ++LineNumber;
                ParserState = ExpectStartOfLine;
            }
            return true;
        }

        private bool ExpectValueNameDefinition(char c)
        {
            if (c == '"')
            {
                CurrentValue = CurrentKey.FindOrCreateValue(Buffer.ToString());
                ParserState = ExpectEqualSign;
                return true;
            }
            else if (c == '\\')
            {
                ParserState = ExpectQuotedCharInStringValueNameDefinition;
                return true;
            }
            else
            {
                Buffer.Append(c);
                return true;
            }
        }

        private bool ExpectQuotedCharInStringValueNameDefinition(char c)
        {
            Buffer.Append(c);
            ParserState = ExpectValueNameDefinition;
            return true;
        }

        private bool ExpectEqualSign(char c)
        {
            if (c == '=')
            {
                Buffer.Clear();
                ParserState = ExpectStartOfValueDefinition;
                return true;
            }
            else
            {
                Trace.TraceError("ERROR, expected '=', got '{0}' instead.", c);
                return false;
            }
        }

        private bool ExpectStartOfValueDefinition(char c)
        {
            if (c == '"')
            {
                ParserState = ExpectStringValueDefinition;
                Buffer.Clear();
                return true;
            }
            else if (c == ':')
            {
                string typename = Buffer.ToString().ToLower();
                Buffer.Clear();
                if (typename.Equals("dword", StringComparison.OrdinalIgnoreCase))
                {
                    ParserState = ExpectHexIntegerValue;
                    return true;
                }
                else if( typename.StartsWith("hex(") && typename.EndsWith(")") )
                {
                    string[] tokens = typename.Split('(', ')');
                    if (tokens.Length >= 2)
                    {
                        int kindValueAsInt = 0;
                        if (!int.TryParse(tokens[1], NumberStyles.HexNumber, null, out kindValueAsInt))
                        {
                            Trace.TraceError("ERROR, '{0}' is not a valid hex() kind ", tokens[1]);
                            return false;
                        }
                        RegValueEntryKind kind = (RegValueEntryKind)kindValueAsInt;
                        Buffer.Clear();
                        CurrentDataKind = kind;
                        ParserState = ExpectStartOfMultiByteValueDefinition;
                        return true;
                    }
                }
                else if (typename.Equals("hex"))
                {
                    CurrentDataKind = RegValueEntryKind.Binary;
                    ParserState = ExpectStartOfMultiByteValueDefinition;
                    return true;
                }
                Trace.TraceError("ERROR, value type '{0}' not supported", typename);
                return false;
            }
            else
            {
                Buffer.Append(c);
                return true;
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

        private bool DecodeCurrentHexValue()
        {
            int result = 0;
            string value = Buffer.ToString();
            Buffer.Clear();

            if (int.TryParse(value, NumberStyles.HexNumber, null, out result))
            {
                CurrentValue.SetIntValue(result);
                ParserState = ExpectNewline;
                Buffer.Clear();
                return true;
            }
            Trace.TraceError("ERROR, '{0}' is not a valid integer", value);
            return false;
        }

        private bool ExpectHexIntegerValue(char c)
        {
            if (c == '\r')
            {
                return DecodeCurrentHexValue();
            }
            else if ("0123456789ABCDEFabcdef".IndexOf(c) >= 0)
            {
                Buffer.Append(c);
                return true;
            }
            else if (IsWhitespace(c))
            {
                return true;
            }
            else if ((c == '#') && AllowHashtagComments)
            {
                if (!DecodeCurrentHexValue())
                    return false;

                ParserState = ExpectCommentUntilEndOfLine;
                return true;
            }
            else if ((c == ';') && AllowHashtagComments)
            {
                if (!DecodeCurrentHexValue())
                    return false;

                ParserState = ExpectCommentUntilEndOfLine;
                return true;
            }
            else if ((c == '$') && AllowVariableNamesForNonStringVariables)
            {
                Buffer.Append(c);
                ParserState = ExpectVariableDefinedHexIntegerValue;
                return true;
            }
            else
            {
                Trace.TraceError("ERROR, '{0}' is not a valid hex digit", c);
                return false;
            }
        }

        private bool DecodeCurrentVariableDefinedHexValue()
        {
            CurrentValue.SetEscapedIntValue(Buffer.ToString());
            ParserState = ExpectNewline;
            Buffer.Clear();
            return true;
        }

        private bool ExpectVariableDefinedHexIntegerValue(char c)
        {
            if (c == '\r')
            {
                return DecodeCurrentVariableDefinedHexValue();
            }
            else if ((c == '#') && AllowHashtagComments)
            {
                if (!DecodeCurrentVariableDefinedHexValue())
                    return false;

                ParserState = ExpectCommentUntilEndOfLine;
                return true;
            }
            else if ((c == ';') && AllowHashtagComments)
            {
                if (!DecodeCurrentVariableDefinedHexValue())
                    return false;

                ParserState = ExpectCommentUntilEndOfLine;
                return true;
            }
            else
            {
                Buffer.Append(c);
                return true;
            }
        }

        private bool ExpectStringValueDefinition(char c)
        {
            if (c == '"')
            {
                CurrentValue.SetStringValue(Buffer.ToString());
                ParserState = ExpectCarriageReturn;
                return true;
            }
            else if (c == '\\')
            {
                ParserState = ExpectQuotedCharInStringValueDefinition;
                return true;
            }
            else
            {
                Buffer.Append(c);
                return true;
            }
        }

        private bool ExpectQuotedCharInStringValueDefinition(char c)
        {
            Buffer.Append(c);
            ParserState = ExpectStringValueDefinition;
            return true;
        }

        private bool ExpectStartOfMultiByteValueDefinition(char c)
        {
            if (c == '\r')
            {
                return ExpectMultiByteValueDefinition(c);
            }
            int result;
            string value = c.ToString();
            if (int.TryParse(value, NumberStyles.HexNumber, null, out result))
            {
                Buffer.Append(c);
                ParserState = ExpectMultiByteValueDefinition;
                return true;
            }
            Trace.TraceError("ERROR, expected byte-oriented hex(2) definition");
            return false;
        }

        private byte[] CreateByteArrayFromString(string input)
        {
            if ((input.Length % 2) != 0)
            {
                input += "0";
                Trace.Assert(input.Length % 2 == 0);
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

        private bool ExpectMultiByteValueDefinition(char c)
        {
            if (c == ',')
            {
                // decode and feed current byte
                return true;
            }
            else if (c == '\\')
            {
                ParserState = ExpectNewlineFollowedByMultiByteValueDefinition;
                return true;
            }
            else if ((c == ' ') || (c == '\t'))
            {
                // ignore whitespace
                return true;
            }
            else if( c == '\r' )
            {
                CurrentValue.SetBinaryType(CurrentDataKind, CreateByteArrayFromString(Buffer.ToString()));
                ParserState = ExpectNewline;
                return true;
            }
            else if (c == '\n')
            {
                Trace.Assert(false);
                return false;
            }
            else
            {
                Buffer.Append(c);
                return true;
            }
        }

        private bool ExpectNewlineFollowedByMultiByteValueDefinition(char c)
        {
            if (c == '\r')
            {
                return true;
            }
            else if (c == '\n')
            {
                ++LineNumber;
                ParserState = ExpectMultiByteValueDefinition;
                return true;
            }
            else
            {
                Trace.TraceError("ERROR, expected newline to follow trailing backslash");
                return false;
            }
        }

        private bool ExpectKeyPath(char c)
        {
            if (c == '[')
            {
                ++NumberOfClosingBracketsExpected;
                Buffer.Append(c);
                return true;
            }
            else if (c == ']')
            {
                if (0 == NumberOfClosingBracketsExpected)
                {
                    CurrentKey = Result.FindOrCreateKey(Buffer.ToString());
                    Trace.Assert(CurrentKey != null);
                    ParserState = ExpectCarriageReturn;
                    return true;
                }
                else if (NumberOfClosingBracketsExpected > 0)
                {
                    --NumberOfClosingBracketsExpected;
                    Buffer.Append(c);
                    return true;
                }
                else
                {
                    Trace.Assert(false);
                    return false;
                }
                
            }
            else
            {
                Buffer.Append(c);
                return true;
            }
        }

        #endregion


    }
}
