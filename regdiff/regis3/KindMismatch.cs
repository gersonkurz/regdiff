using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tikumo.regis3
{
    /// <summary>
    /// This is a helper class that represents a value that exists in both keys, but has different value kinds (and by definition
    /// different values) in each of them.
    /// </summary>
    public class KindMismatch
    {
        /// <summary>
        /// Key where the value was located
        /// </summary>
        public readonly RegKeyEntry Key;

        /// <summary>
        /// Value as it exists in the 1st key
        /// </summary>
        public readonly RegValueEntry Value1;

        /// <summary>
        /// Value as it exists in the 2nd key
        /// </summary>
        public readonly RegValueEntry Value2;

        /// <summary>
        /// The constructor creates an object that represents a value that exists in both keys, but has different value kinds (and by definition
        /// different values) in each of them.
        /// </summary>
        /// <param name="key">Key where the value was located</param>
        /// <param name="value1">Value as it exists in the 1st key</param>
        /// <param name="value2">Value as it exists in the 2nd key</param>
        public KindMismatch(RegKeyEntry key, RegValueEntry value1, RegValueEntry value2)
        {
            Key = key;
            Value1 = value1;
            Value2 = value2;
        }
    }
}
