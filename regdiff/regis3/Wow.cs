using System;
using System.Runtime.InteropServices;

namespace com.tikumo.regis3
{
    /// <summary>
    /// Helper class for Windows-on-Windows support (32-bit processes on 64-bit Windows)
    /// </summary>
    public static class Wow
    {
        /// <summary>
        /// Check if this process is 64-bit
        /// </summary>
        public static bool Is64BitProcess
        {
            get { return IntPtr.Size == 8; }
        }

        /// <summary>
        /// Check if this OS is 64-bit
        /// </summary>
        public static bool Is64BitOperatingSystem
        {
            get
            {
                // Clearly if this is a 64-bit process we must be on a 64-bit OS.
                if (Is64BitProcess)
                    return true;
                // Ok, so we are a 32-bit process, but is the OS 64-bit?
                // If we are running under Wow64 than the OS is 64-bit.
                bool isWow64;
                return ModuleContainsFunction("kernel32.dll", "IsWow64Process") && IsWow64Process(GetCurrentProcess(), out isWow64) && isWow64;
            }
        }

        
        private static bool ModuleContainsFunction(string moduleName, string methodName)
        {
            IntPtr hModule = GetModuleHandle(moduleName);
            if (hModule != IntPtr.Zero)
                return GetProcAddress(hModule, methodName) != IntPtr.Zero;
            return false;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool isWow64);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private extern static IntPtr GetCurrentProcess();
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private extern static IntPtr GetModuleHandle(string moduleName);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private extern static IntPtr GetProcAddress(IntPtr hModule, string methodName);
    }
}
