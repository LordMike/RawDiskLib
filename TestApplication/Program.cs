using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using DeviceIOControlLib;
using System.Linq;
using RawDiskLib;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            RawDisk disk = new RawDisk('C');

            byte[] ntfsBootSector = disk.Read(0, 1);

            // First 3 bytes is some random thing, then the next 4 bytes should contain "NTFS" in ascii
            string first8Bytes = Encoding.ASCII.GetString(ntfsBootSector, 3, 4);
            Console.WriteLine(first8Bytes);
        }
    }
}
