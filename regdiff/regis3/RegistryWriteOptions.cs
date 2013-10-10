using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tikumo.regis3
{
    [System.Flags]
    public enum RegistryWriteOptions
    {
        Recursive = (1<<0),

        AllAccessForEveryone = (1<<1),
    }
}
