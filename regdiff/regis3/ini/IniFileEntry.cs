using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tikumo.regis3.ini
{
    public class IniFileEntry
    {
        public string Name;
        public string Data;
        public string Comment;

        public IniFileEntry(string name, string data, string comment)
        {
            Name = name;
            Data = data;
            Comment = comment;
        }

        public bool HasComment
        {
            get
            {
                return !string.IsNullOrEmpty(Comment);
            }
        }
    }
}
