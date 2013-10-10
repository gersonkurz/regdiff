
namespace com.tikumo.regis3
{
    /// <summary>
    /// This class defines the known registry value types. It includes more types than the standard .Net enum RegistryValueKind
    /// </summary>
    public enum RegValueEntryKind
    {
        /// <summary>
        /// Registry value kind unknown or invalid
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// No value kind associated
        /// </summary>
        None = 0,

        /// <summary>
        /// "String Zero-Terminated": a string
        /// </summary>
        SZ = 1,

        /// <summary>
        /// "Expandable String": a string that can include environment variable specifications. By default,
        /// Windows will implicitly expand the string when reading it.
        /// </summary>
        ExpandSZ = 2,

        /// <summary>
        /// Binary data
        /// </summary>
        Binary = 3,

        /// <summary>
        /// A (unsigned) integer (little endian = intel default)
        /// </summary>
        DWord = 4,

        /// <summary>
        /// A (unsigned) integer (big endian). Not sure if I have ever seen one of these beasts live in the wild
        /// </summary>
        DWordBigEndian = 5,

        /// <summary>
        /// Registry link
        /// </summary>
        Link = 6,

        /// <summary>
        /// A list of multiple strings.
        /// </summary>
        MultiSZ = 7,

        /// <summary>
        /// A Windows NT resource list
        /// </summary>
        ResourceList = 8,

        /// <summary>
        /// A Windows NT full resource descriptor
        /// </summary>
        FullResourceDescriptor = 9,

        /// <summary>
        /// A Windows NT resource requirements list
        /// </summary>
        ResourceRequirementsList = 10,

        /// <summary>
        /// A 64-bit integer (little endian = intel default)
        /// </summary>
        QWord = 11,
    }
}
