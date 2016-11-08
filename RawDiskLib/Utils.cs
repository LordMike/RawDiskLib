using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RawDiskLib.Helpers;

namespace RawDiskLib
{
    public static class Utils
    {
        static readonly Regex RgxDriveLetter = new Regex(@"^[A-Z]:$");
        static readonly Regex RgxHarddisk = new Regex(@"^PhysicalDrive[0-9]+$");
        static readonly Regex RgxPartition = new Regex(@"^HarddiskVolume[0-9]+$");
        
        public static IEnumerable<string> GetAvailableDrives()
        {
            string[] drives = Win32Helper.GetAllDevices();

            foreach (string drive in drives)
            {
                if (RgxDriveLetter.IsMatch(drive) || RgxHarddisk.IsMatch(drive) || RgxPartition.IsMatch(drive))
                    yield return drive;
            }
        }

        public static IEnumerable<char> GetAllAvailableVolumes()
        {
            string[] drives = Win32Helper.GetAllDevices();

            foreach (string drive in drives)
            {
                if (RgxDriveLetter.IsMatch(drive))
                    yield return drive[0];
            }
        }

        public static IEnumerable<int> GetAllAvailableDrives(DiskNumberType type)
        {
            string[] drives = Win32Helper.GetAllDevices();

            if (type == DiskNumberType.PhysicalDisk)
            {
                foreach (string drive in drives)
                {
                    if (RgxHarddisk.IsMatch(drive))
                        yield return int.Parse(drive.Substring("PhysicalDrive".Length));
                }
            }
            else if (type == DiskNumberType.Volume)
            {
                foreach (string drive in drives)
                {
                    if (RgxPartition.IsMatch(drive))
                        yield return int.Parse(drive.Substring("HarddiskVolume".Length));
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}