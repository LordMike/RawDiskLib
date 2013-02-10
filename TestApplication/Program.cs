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
        const ConsoleColor DefaultColor = ConsoleColor.White;
        const int SectorsToRead = 100;

        static void Main(string[] args)
        {
            List<string> devs = Utils.GetAvailableDrives();

            Console.ForegroundColor = DefaultColor;

            // Volumes (C:, E: ..)
            {
                List<char> devices = devs.Where(s => s.Length == 2 && s.EndsWith(":")).Select(s => s[0]).ToList();
                foreach (char device in devices)
                {
                    Console.WriteLine("Trying volume, " + device + ": .. ");

                    try
                    {
                        RawDisk disk = new RawDisk(device);
                        PresentResult(disk);
                    }
                    catch (Win32Exception exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: " + new Win32Exception(exception.NativeErrorCode).Message);
                        Console.ForegroundColor = DefaultColor;
                    }

                    Console.WriteLine();
                }
            }

            Console.WriteLine("Done with Volumes, type ENTER to proceed.");
            Console.ReadLine();
            Console.WriteLine();
            Console.WriteLine();

            // Physical Drives (PhysicalDrive0, PhysicalDrive1 ..)
            {
                List<int> devices = devs.Where(s => s.StartsWith("PhysicalDrive")).Select(s => int.Parse(s.Substring("PhysicalDrive".Length))).ToList();
                foreach (int device in devices)
                {
                    Console.WriteLine("Trying PhysicalDrive" + device + ": .. ");

                    try
                    {
                        RawDisk disk = new RawDisk(DiskNumberType.PhysicalDisk, device);
                        PresentResult(disk);
                    }
                    catch (Win32Exception exception)
                    {
                        Console.WriteLine("Error: " + new Win32Exception(exception.NativeErrorCode).Message);
                    }

                    Console.WriteLine();
                }
            }

            Console.WriteLine("Done with Physical Drives, type ENTER to proceed.");
            Console.ReadLine();
            Console.WriteLine();
            Console.WriteLine();

            // Harddisk Volumes (HarddiskVolume1, HarddiskVolume2 ..)
            {
                List<int> devices = devs.Where(s => s.StartsWith("HarddiskVolume")).Select(s => int.Parse(s.Substring("HarddiskVolume".Length))).ToList();
                foreach (int device in devices)
                {
                    Console.WriteLine("Trying HarddiskVolume" + device + ": .. ");

                    try
                    {
                        RawDisk disk = new RawDisk(DiskNumberType.Volume, device);
                        PresentResult(disk);
                    }
                    catch (Win32Exception exception)
                    {
                        Console.WriteLine("Error: " + new Win32Exception(exception.NativeErrorCode).Message);
                    }

                    Console.WriteLine();
                }
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static void PresentResult(RawDisk disk)
        {
            byte[] data = disk.Read(0, (int)Math.Min(disk.SectorCount, SectorsToRead));

            string fatType = Encoding.ASCII.GetString(data, 82, 8);     // Extended FAT parameters have a display name here.
            bool isFat = fatType.StartsWith("FAT");
            bool isNTFS = Encoding.ASCII.GetString(data, 3, 4) == "NTFS";

            // Optimization, if it's a known FS, we know it's not all zeroes.
            bool allZero = (!isNTFS || !isFat) && data.All(s => s == 0);

            Console.WriteLine("Size in bytes : {0:N0}", disk.SizeBytes);
            Console.WriteLine("Sectors       : {0:N0}", disk.SectorCount);
            Console.WriteLine("Sectorsize    : {0:N0}", disk.SectorSize);

            if (!isFat && !isNTFS)
                Console.ForegroundColor = ConsoleColor.Red;
            
            if (isNTFS)
                Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("Is NTFS       : {0}", isNTFS);
            Console.ForegroundColor = DefaultColor;

            if (!isFat && !isNTFS)
                Console.ForegroundColor = ConsoleColor.Red;

            if (isFat)
                Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("Is FAT        : {0}", isFat ? fatType : "False");
            Console.ForegroundColor = DefaultColor;

            if (!allZero)
                Console.ForegroundColor = ConsoleColor.Green;
            else
                Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine("All bytes zero: {0}", allZero);
            Console.ForegroundColor = DefaultColor;
        }
    }
}
