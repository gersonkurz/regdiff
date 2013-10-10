using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace com.tikumo.regis3.ini
{
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

        public IniFileParser(IniFileSection rootSection, IniFileOptions options =
            IniFileOptions.KeepComments | IniFileOptions.KeepFlat | IniFileOptions.StripEmptyLines)
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
                ParserState = ExpectSectionName;
                return true;
            }
            else if ( (c == ' ') || (c == '\t') )
            {
                return true;
            }
            else if (c == '#')
            {
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
                return true;
            }
            else if (c == ';')
            {
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
                return true;
            }
            else
            {
                ParserState = ExpectValueNameDefinition;
                Buffer.Clear();
                Buffer.Append(c);
                return true;
            }
        }

        private bool ExpectSectionName(char c)
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
                    CurrentSection = CreateSectionFromName(Buffer.ToString());
                    Buffer.Clear();
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

        private bool ExpectCarriageReturn(char c)
        {
            if (c == '\r')
            {
                ParserState = ExpectStartOfLine;
                return true;
            }
            else if (c == ' ' || c == '\t')
            {
                return true;
            }
            else if (c == '#')
            {
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
                return true;
            }
            else if (c == ';')
            {
                Buffer.Clear();
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

        private bool ExpectCommentUntilEndOfLine(char c)
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
            return true;
        }

        private bool ExpectValueNameDefinition(char c)
        {
            if (c == '=')
            {
                CurrentValueName = Buffer.ToString().Trim();
                Buffer.Clear();
                ParserState = ExpectValueDataDefinition;
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
        
        private bool ExpectValueDataDefinition(char c)
        {
            if (c == '\r')
            {
                CurrentValueData = Buffer.ToString().Trim();
                AddNewLine();
                Buffer.Clear();
                ParserState = ExpectNewline;
                return true;
            }
            else if (c == '#')
            {
                CurrentValueData = Buffer.ToString().Trim();
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
                return true;
            }
            else if (c == ';')
            {
                CurrentValueData = Buffer.ToString().Trim();
                Buffer.Clear();
                ParserState = ExpectCommentUntilEndOfLine;
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
        #endregion


    }
}
