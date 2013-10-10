using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tikumo.regis3
{
    /// <summary>
    /// Available options when exporting a RegKeyEntry tree back to the registry
    /// </summary>
    [System.Flags]
    public enum RegistryWriteOptions
    {
        /// <summary>
        /// Export the data recursively. If omitted, export only the top level keys
        /// </summary>
        Recursive = (1<<0),

        /// <summary>
        /// Grant all access to everyone. I know I am lazy and one of these days hackers will probably take me down, but
        /// it is a lot easier this way ;)
        /// </summary>
        AllAccessForEveryone = (1<<1),
    }
}
