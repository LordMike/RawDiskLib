using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RawDiskLib.Helpers
{
    internal static partial class Win32Helper
    {
        private const int ERROR_INSUFFICIENT_BUFFER = 122;
        private const int ERROR_SUCCSS = 0;

#if NET7_0_OR_GREATER
        [LibraryImport("kernel32.dll", EntryPoint = "QueryDosDeviceW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial int QueryDosDevice(string lpDeviceName, char[] lpTargetPath, int ucchMax);
#else
        [DllImport("kernel32.dll", EntryPoint = "QueryDosDeviceW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int QueryDosDevice(string lpDeviceName, char[] lpTargetPath, int ucchMax);
#endif

        public static string[] GetAllDevices()
        {
            int returnSize;

            char[] chars = new char[16000];
            do
            {
                returnSize = QueryDosDevice(null, chars, chars.Length);
                int error = Marshal.GetLastWin32Error();

                if (returnSize > 0 || error == ERROR_SUCCSS)
                    break;

                if (error != ERROR_INSUFFICIENT_BUFFER)
                    throw new Exception("Unable to query DOS devices");

                chars = new char[chars.Length * 2];
            } while (true);

            // Parse as list of null-seperated UTF-16 strings
            List<string> res = new List<string>();
            StringBuilder sb = new StringBuilder(200);

            for (int i = 0; i < returnSize; i++)
            {
                if (chars[i] != '\0')
                    sb.Append(chars[i]);
                else if (sb.Length > 0)
                {
                    res.Add(sb.ToString());
                    sb.Clear();
                }
            }

            return res.ToArray();
        }
    }
}
