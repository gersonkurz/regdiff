
namespace com.tikumo.regis3
{
    /// <summary>
    /// When comparing two registry files, this class identifies a mismatch in the data of two registry values that are located
    /// in the same key path.
    /// </summary>
    public class DataMismatch
    {
        /// <summary>
        /// Parent registry key path (by definition, the same in both registry trees)
        /// </summary>
        public readonly RegKeyEntry Key;

        /// <summary>
        /// Data in first registry file
        /// </summary>
        public readonly RegValueEntry Value1;

        /// <summary>
        /// Data in the second registry file
        /// </summary>
        public readonly RegValueEntry Value2;

        /// <summary>
        /// This constructor identifies a mismatch in the data of two registry values that are located
        /// in the same key path.
        /// </summary>
        /// <param name="key">Parent registry key path (by definition, the same in both registry trees)</param>
        /// <param name="value1">Data in first registry file</param>
        /// <param name="value2">Data in the second registry file</param>
        public DataMismatch(RegKeyEntry key, RegValueEntry value1, RegValueEntry value2)
        {
            Key = key;
            Value1 = value1;
            Value2 = value2;
        }
    }
}
