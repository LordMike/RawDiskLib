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
    }
}