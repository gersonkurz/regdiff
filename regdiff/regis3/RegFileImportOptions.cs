using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tikumo.regis3
{
    /// <summary>
    /// Parser options for .REG files
    /// </summary>
    [System.Flags]
    public enum RegFileImportOptions
    {
        /// <summary>
        /// No specific options
        /// </summary>
        None = 0,

        /// <summary>
        /// Allow Hashtag-style line comments.
        /// </summary>
        AllowHashtagComments = (1<<0),

        /// <summary>
        /// Allow Semicolon-style line comments.
        /// </summary>
        AllowSemicolonComments = (1<<1),

        /// <summary>
        /// If this option is set, the parser is more relaxed about whitespaces in the .REG file (Recommended, especially if you manually edit the file yourself.)
        /// </summary>
        IgnoreWhitespaces = (1<<2),

        /// <summary>
        /// If this option is set, a .REG file can have a statement like this:
        /// 
        /// "something"=dword:$$VARIABLE$$
        /// 
        /// where $$VARIABLE$$ is replaced at runtime with the respective -numeric- variable. 
        /// </summary>
        AllowVariableNamesForNonStringVariables = (1<<3),

    }
}
