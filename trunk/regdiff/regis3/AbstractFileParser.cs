using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tikumo.regis3
{
    public abstract class AbstractFileParser
    {
        protected delegate bool ExpectFunc(char c);
        protected ExpectFunc ParserState;
        protected int LineNumber;
        protected int ColumnNumber;
        protected readonly StringBuilder Buffer = new StringBuilder();

        protected AbstractFileParser()
        {
        }

        protected void Parse(string content, ExpectFunc initialFunc)
        {
            int index = 0;

            ParserState = initialFunc;
            LineNumber = 1;
            ColumnNumber = 0;

            foreach (char c in content)
            {
                ++ColumnNumber;
                if (!ParserState(c))
                {
                    StringBuilder errorMessage = new StringBuilder();
                    errorMessage.AppendFormat("ERROR, parser failed at line {0}, col {1}", LineNumber, ColumnNumber);
                    errorMessage.AppendLine();

                    int StartIndex = index;
                    while ((StartIndex >= 0) && (content[StartIndex] != '\n'))
                    {
                        --StartIndex;
                    }
                    if (StartIndex < 0)
                        StartIndex = 0;
                    else
                        ++StartIndex;
                    int StopIndex = index;
                    while ((StopIndex < content.Length) && (content[StopIndex] != '\r') && (content[StopIndex] != '\n'))
                    {
                        ++StopIndex;
                    }
                    if (StopIndex >= content.Length)
                    {
                        StopIndex = content.Length - 1;
                    }


                    errorMessage.Append(">> ");
                    errorMessage.Append(content.Substring(StartIndex, StopIndex - StartIndex));

                    throw new SyntaxErrorException(errorMessage.ToString());
                }
                ++index;
            }
        }
    }
}
