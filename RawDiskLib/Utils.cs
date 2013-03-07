using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RawDiskLib.Helpers;

namespace RawDiskLib
{
    public static class Utils
    {
        public static List<string> GetAvailableDrives()
        {
            string[] drives = Win32Helper.GetAllDevices();

            Regex rgxDriveLetter = new Regex(@"^[A-Z]:$");
            Regex rgxHarddisk = new Regex(@"^PhysicalDrive[0-9]+$");
            Regex rgxPartition = new Regex(@"^HarddiskVolume[0-9]+$");

            return drives.Where(s => rgxDriveLetter.IsMatch(s) || rgxHarddisk.IsMatch(s) || rgxPartition.IsMatch(s)).ToList();
        }

        public static char[] GetAllAvailableVolumes()
        {
            string[] drives = Win32Helper.GetAllDevices();

            Regex rgxDriveLetter = new Regex(@"^[A-Z]:$");

            return drives.Where(s => rgxDriveLetter.IsMatch(s)).Select(s => s[0]).ToArray();
        }

        public static int[] GetAllAvailableDrives(DiskNumberType type)
        {
            string[] drives = Win32Helper.GetAllDevices();

            Regex rgxHarddisk = new Regex(@"^PhysicalDrive[0-9]+$");
            Regex rgxPartition = new Regex(@"^HarddiskVolume[0-9]+$");

            switch (type)
            {
                case DiskNumberType.PhysicalDisk:
                    return drives.Where(s => rgxHarddisk.IsMatch(s)).Select(s => int.Parse(s.Substring("PhysicalDrive".Length))).ToArray();
                case DiskNumberType.Volume:
                    return drives.Where(s => rgxPartition.IsMatch(s)).Select(s => int.Parse(s.Substring("HarddiskVolume".Length))).ToArray();
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }
    }
}