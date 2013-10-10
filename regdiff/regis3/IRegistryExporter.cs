using System.IO;

namespace com.tikumo.regis3
{
    /// <summary>
    /// This is the export interface supported by all regis3 exporter functions: given a RegKeyEntry, create a file or a string.
    /// </summary>
    public interface IRegistryExporter
    {
        /// <summary>
        /// Given a registry key description, create a file
        /// </summary>
        /// <param name="key">Existing registry key description</param>
        /// <param name="filename">Filename to be created</param>
        void Export(RegKeyEntry key, string filename);

        /// <summary>
        /// Given a registry key description, write a file to a stream
        /// </summary>
        /// <param name="key">Existing registry key description</param>
        /// <param name="file">Stream to be written to</param>
        void Export(RegKeyEntry key, TextWriter file);
    }
}
