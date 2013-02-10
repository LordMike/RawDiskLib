using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace RawDiskLib.Helpers
{
    public static class Win32Helper
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
           string lpFileName,
           [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
           [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
           IntPtr lpSecurityAttributes,
           [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
           [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
           IntPtr hTemplateFile);

        [DllImport("kernel32.dll")]
        private static extern uint QueryDosDevice(string lpDeviceName, IntPtr lpTargetPath, uint ucchMax);

        public static string[] GetAllDevices()
        {
            // Allocate some memory to get a list of all system devices.
            // Start with a small size and dynamically give more space until we have enough room.
            int returnSize = 0;
            int maxSize = 100;
            string allDevices;
            IntPtr mem;
            string[] retval = null;

            while (returnSize == 0)
            {
                mem = Marshal.AllocHGlobal(maxSize);
                if (mem != IntPtr.Zero)
                {
                    // mem points to memory that needs freeing
                    try
                    {
                        returnSize = (int)QueryDosDevice(null, mem, (uint)maxSize);
                        if (returnSize != 0)
                        {
                            allDevices = Marshal.PtrToStringAnsi(mem, returnSize);
                            retval = allDevices.Split('\0');
                            break;    // not really needed, but makes it more clear...
                        }
                        else if (Marshal.GetLastWin32Error() == 122)
                        //maybe better
                        //else if( Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
                        //ERROR_INSUFFICIENT_BUFFER = 122;
                        {
                            maxSize *= 10;
                        }
                        else
                        {
                            Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(mem);
                    }
                }
                else
                {
                    throw new OutOfMemoryException();
                }
            }
            return retval;
        }

        public static string GetErrorMessage(int errorCode)
        {
            return new Win32Exception(errorCode).Message;
        }
    }
}
