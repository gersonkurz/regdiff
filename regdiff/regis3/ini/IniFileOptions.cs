using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tikumo.regis3.ini
{
    [System.Flags]
    public enum IniFileOptions
    {
        /// <summary>
        /// Keep comments when reading .INI files. Comments can start with '#' or ';'. This is the default
        /// </summary>
        KeepComments = (1<<0),

        /// <summary>
        /// Keep sections flag (i.e. no subsections). This is the default
        /// </summary>
        KeepFlat = (1<<1),

        /// <summary>
        /// Strip empty lines (NB: that includes lines with only comments). This is the default
        /// </summary>
        StripEmptyLines = (1<<2),
    }
}
