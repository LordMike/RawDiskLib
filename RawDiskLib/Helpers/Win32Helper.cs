using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace RawDiskLib.Helpers
{
    internal static class Win32Helper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int QueryDosDevice(string lpDeviceName, byte[] lpTargetPath, int ucchMax);

        public static string[] GetAllDevices()
        {
            int returnSize;

            byte[] bytes = new byte[16000];
            do
            {
                returnSize = QueryDosDevice(null, bytes, bytes.Length);

                if (returnSize != 0)
                    break;

                int error = Marshal.GetLastWin32Error();
                string s = new Win32Exception(error).Message;

                if (error == 122)
                    bytes = new byte[bytes.Length * 2];

            } while (true);

            // Parse
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
