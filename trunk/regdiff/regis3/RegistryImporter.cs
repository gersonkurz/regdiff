// Copyright (c) 2013, Gerson Kurz
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// Redistributions of source code must retain the above copyright notice, this list
// of conditions and the following disclaimer. Redistributions in binary form must
// reproduce the above copyright notice, this list of conditions and the following
// disclaimer in the documentation and/or other materials provided with the distribution.
// 
// Neither the name regdiff nor the names of its contributors may be used to endorse
// or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
// IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
// NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using Microsoft.Win32;

namespace com.tikumo.regis3
{
    /// <summary>
    /// This is the standard importer to read data from an existing registry (on the local machine, or on a remote machine).
    /// </summary>
    public class RegistryImporter : IRegistryImporter
    {
        private readonly RegKeyEntry Result;

        /// <summary>
        /// The import function returns the RegKeyEntry imported from the registry
        /// </summary>
        /// <returns>The key imported from the registry</returns>
        public RegKeyEntry Import()
        {
            return Result;
        }

        /// <summary>
        /// The default constructor takes a registry root path encoded as a string (for example, HKEY_LOCAL_MACHINE\Software\Microsoft) 
        /// and reads everything under it.
        /// </summary>
        /// <param name="rootPath">Root path</param>
        public RegistryImporter(string rootPath)
        {
            Result = new RegKeyEntry(null, rootPath);

            string rootPathWithoutHive;
            RegistryKey rootKey = Regis3.OpenRegistryHive(rootPath, out rootPathWithoutHive);
            using (RegistryKey key = rootKey.OpenSubKey(rootPathWithoutHive))
            {
                ImportRecursive(Result, key);
            }
        }

        /// <summary>
        /// This constructor takes an existing registry root key and a path relative to that key. You acn use this
        /// if you've already split the path and identified the root key in a step before that (this is what Regdiff does)
        /// </summary>
        /// <param name="rootKey">Registry key identifing root (e.g. HKEY_LOCAL_MACHINE)</param>
        /// <param name="rootPath">Relative registry path (e.g. "Software\Microsoft")</param>
        public RegistryImporter(RegistryKey rootKey, string rootPath)
        {
            Result = new RegKeyEntry(null, rootKey.Name);

            RegKeyEntry parent = Result;
            foreach (string token in rootPath.Split('\\'))
            {
                RegKeyEntry next = new RegKeyEntry(parent, token);
                parent.Keys[token.ToLower()] = next;
                parent = next;
            }

            using (RegistryKey key = rootKey.OpenSubKey(rootPath))
            {
                ImportRecursive(parent, key);
            }
        }

        private void ImportRecursive(RegKeyEntry parent, RegistryKey key)
        {
            foreach (var name in key.GetSubKeyNames())
            {
                RegKeyEntry entry = new RegKeyEntry(parent, name);
                parent.Keys[name.ToLower()] = entry;
                try
                {
                    using (RegistryKey subkey = key.OpenSubKey(name))
                    {
                        ImportRecursive(entry, subkey);
                    }
                }
                catch (System.Security.SecurityException)
                {
                    // ignore
                }
            }

            foreach (var name in key.GetValueNames())
            {
                parent.Values[name.ToLower()] = new RegValueEntry(key, name);
            }
        }
    }
}
