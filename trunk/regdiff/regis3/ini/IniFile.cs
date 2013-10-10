using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace com.tikumo.regis3.ini
{
    public class IniFile : IniFileSection
    {
        public IniFile()
        {
        }

        public void Read(string filename)
        {
            new IniFileParser(this).Parse(File.ReadAllText(filename));
        }
    }
}
