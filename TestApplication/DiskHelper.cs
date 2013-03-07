using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TestApplication
{
    public static class DiskHelper
    {
        [DllImport("kernel32.dll")]
        private static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, uint ucchMax);

        public static char[] GetDriveLetters(string driveName)
        {
            List<char> results = new List<char>();

            StringBuilder sb = new StringBuilder(128);
            for (char ch = 'A'; ch < 'Z'; ch++)
            {
                uint result;
                do
                {
                    result = QueryDosDevice(ch + ":", sb, (uint)sb.Capacity);

                    if (result == 122)
                        sb.EnsureCapacity(sb.Capacity * 2);
                } while (result == 122);

                // Contains target?
                string[] drives = sb.ToString().Split('\0');


                if (drives.Any(s => s.Equals(driveName, StringComparison.InvariantCultureIgnoreCase)))
                    results.Add(ch);
            }

            return results.ToArray();
        }
    }
}