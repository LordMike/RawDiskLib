using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RawDiskLib.Helpers
{
    internal static class Win32Helper
    {
        private const int ERROR_INSUFFICIENT_BUFFER = 122;
        private const int ERROR_SUCCSS = 0;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int QueryDosDevice(string lpDeviceName, byte[] lpTargetPath, int ucchMax);

        public static string[] GetAllDevices()
        {
            int returnSize;

            byte[] bytes = new byte[16000];
            do
            {
                returnSize = QueryDosDevice(null, bytes, bytes.Length);
                int error = Marshal.GetLastWin32Error();

                if (returnSize > 0 || error == ERROR_SUCCSS)
                    break;

                if (error != ERROR_INSUFFICIENT_BUFFER)
                    throw new Exception("Unable to query DOS devices");

                bytes = new byte[bytes.Length * 2];
            } while (true);

            // Parse as list of null-seperated ANSI strings
            List<string> res = new List<string>();
            StringBuilder sb = new StringBuilder(200);

            for (int i = 0; i < returnSize; i++)
            {
                if (bytes[i] != 0)
                    sb.Append((char)bytes[i]);
                else if (bytes[i] == 0 && sb.Length > 0)
                {
                    res.Add(sb.ToString());
                    sb.Clear();
                }
            }

            return res.ToArray();
        }
    }
}
