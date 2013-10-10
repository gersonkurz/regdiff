using System;

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
