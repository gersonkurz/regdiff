using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tikumo.regis3
{
    public class DataMismatch
    {
        public readonly RegKeyEntry Key;
        public readonly RegValueEntry Value1;
        public readonly RegValueEntry Value2;

        public DataMismatch(RegKeyEntry key, RegValueEntry value1, RegValueEntry value2)
        {
            Key = key;
            Value1 = value1;
            Value2 = value2;
        }
    }
}
