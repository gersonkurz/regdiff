using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tikumo.regis3
{
    public abstract class AbstractFileParser
    {
        protected delegate void ExpectFunc(char c);
        protected ExpectFunc ParserState;
        protected int LineNumber;
        protected int ColumnNumber;
        protected readonly StringBuilder Buffer = new StringBuilder();
        protected int CurrentIndex;
        protected string CurrentContent;

        protected AbstractFileParser()
        {
        }

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
