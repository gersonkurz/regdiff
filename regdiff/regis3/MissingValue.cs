using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.tikumo.regis3
{
    /// <summary>
    /// This is a helper class that represents a missing value in a key
    /// </summary>
    public class MissingValue
    {
        /// <summary>
        /// Key where the value was located
        /// </summary>
        public readonly RegKeyEntry Key;

        /// <summary>
        /// Original value
        /// </summary>
        public readonly RegValueEntry Value;

        /// <summary>
        /// The constructor creates an object that represents a missing value in a key
        /// </summary>
        /// <param name="key">Key where the value was located</param>
        /// <param name="value">Original value</param>
        public MissingValue(RegKeyEntry key, RegValueEntry value)
        {
            Key = key;
            Value = value;
        }
    }
}
