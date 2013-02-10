using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using RawDiskLib;
using RawDiskLib.Helpers;
using System.Linq;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> devs = Utils.GetAvailableDrives();

            // Volumes (C:, E: ..)
            {
                List<char> devices = devs.Where(s => s.Length == 2 && s.EndsWith(":")).Select(s => s[0]).ToList();
                foreach (char device in devices)
                {
                    Console.Write("Trying volume, " + device + ": .. ");

                    try
                    {
                        RawDisk dik = new RawDisk(device);
                        byte[] data = dik.Read(0, 10);

                        Console.WriteLine("Ok, got " + data.Length + " bytes");
                    }
                    catch (Win32Exception exception)
                    {
                        Console.WriteLine("Error: " + exception.Message);
                    }
                }
            }

            // Physical Drives (PhysicalDrive0, PhysicalDrive1 ..)
            {
                List<int> devices = devs.Where(s => s.StartsWith("PhysicalDrive")).Select(s => int.Parse(s.Substring("PhysicalDrive".Length))).ToList();
                foreach (int device in devices)
                {
                    Console.Write("Trying PhysicalDrive" + device + ": .. ");

                    try
                    {
                        RawDisk dik = new RawDisk(DiskNumberType.PhysicalDisk, device);
                        byte[] data = dik.Read(0, 10);

                        Console.WriteLine("Ok, got " + data.Length + " bytes");
                    }
                    catch (Win32Exception exception)
                    {
                        Console.WriteLine("Error: " + exception.Message);
                    }
                }
            }

            // Harddisk Volumes (HarddiskVolume1, HarddiskVolume2 ..)
            {
                List<int> devices = devs.Where(s => s.StartsWith("HarddiskVolume")).Select(s => int.Parse(s.Substring("HarddiskVolume".Length))).ToList();
                foreach (int device in devices)
                {
                    Console.Write("Trying HarddiskVolume" + device + ": .. ");

                    try
                    {
                        RawDisk dik = new RawDisk(DiskNumberType.Volume, device);
                        byte[] data = dik.Read(0, 10);

                        Console.WriteLine("Ok, got " + data.Length + " bytes");
                    }
                    catch (Win32Exception exception)
                    {
                        Console.WriteLine("Error: " + exception.Message);
                    }
                }
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
