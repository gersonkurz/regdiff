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
    /// This is a special importer that is used when comparing .REG files to values in the registry: rather than specifying an
    /// actual registry key to import, only keys existing in the import key are read.
    /// </summary>
    public class RegistryImportRelativeToExistingRegKeyEntry : IRegistryImporter
    {
        private readonly RegKeyEntry Result;

        /// <summary>
        /// Return the newly imported registry key
        /// </summary>
        /// <returns></returns>
        public RegKeyEntry Import()
        {
            return Result;
        }

        /// <summary>
        /// This constructor creates a registry importer for an existing registry key
        /// </summary>
        /// <param name="existingRegistry">Existing registry key</param>
        /// <param name="registryView">Type of registry you want to see (32-bit, 64-bit, default).</param>
        public RegistryImportRelativeToExistingRegKeyEntry(RegKeyEntry existingRegistry, RegistryView registryView)
        {
            Result = new RegKeyEntry(null, existingRegistry.Path);

            string rootPath = existingRegistry.Path;
            string rootPathWithoutHive;
            RegistryKey rootKey = Regis3.OpenRegistryHive(rootPath, out rootPathWithoutHive, registryView);
            using (RegistryKey key = rootKey.OpenSubKey(rootPathWithoutHive))
            {
                ImportRecursive(Result, key, existingRegistry);
            }
        }

        private void ImportRecursive(RegKeyEntry parent, RegistryKey key, RegKeyEntry relativeKey)
        {
            foreach (var name in key.GetSubKeyNames())
            {
                string keyName = name.ToLower();
                if (relativeKey.Keys.ContainsKey(keyName))
                {
                    RegKeyEntry entry = new RegKeyEntry(parent, name);
                    parent.Keys[name.ToLower()] = entry;
                    try
                    {
                        using (RegistryKey subkey = key.OpenSubKey(name))
                        {
                            ImportRecursive(entry, subkey, relativeKey.Keys[keyName]);
                        }
                    }
                    catch (System.Security.SecurityException)
                    {
                        // ignore
                    }
                }
            }

            foreach (var name in key.GetValueNames())
            {
                parent.Values[name.ToLower()] = new RegValueEntry(key, name);
            }
        }
    }
}
