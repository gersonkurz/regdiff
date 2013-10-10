using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tikumo.regis3
{
    public class SyntaxErrorException : Exception
    {
        public SyntaxErrorException(string msg)
            :   base(msg)
        {
        }
    }
}
