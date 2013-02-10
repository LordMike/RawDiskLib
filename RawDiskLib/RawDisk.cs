using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DeviceIOControlLib;
using Microsoft.Win32.SafeHandles;
using RawDiskLib.Helpers;
using FileAttributes = System.IO.FileAttributes;

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

    public class RawDisk : IDisposable
    {
        private SafeFileHandle _diskHandle;
        private FileStream _diskFs;
        private DeviceIOControlWrapper _deviceIo;
        private DISK_GEOMETRY _diskInfo;

        private long _deviceLength;

        public long SizeBytes
        {
            get { return _deviceLength; }
        }
        public long SectorCount
        {
            get { return _deviceLength / SectorSize; }
        }
        public int SectorSize
        {
            get { return _diskInfo.BytesPerSector; }
        }

        public string DosDeviceName { get; private set; }

        public DISK_GEOMETRY DiskInfo
        {
            get { return _diskInfo; }
        }

        public RawDisk(char driveLetter)
        {
            if (!char.IsLetter(driveLetter))
                throw new ArgumentException("Invalid drive letter");

            driveLetter = char.ToUpper(driveLetter);

            string path = string.Format(@"\\.\{0}:", driveLetter);
            InitateVolume(path);
        }
        public RawDisk(DiskNumberType type, int number)
        {
            if (number < 0)
                throw new ArgumentException("Invalid number");

            string path;
            switch (type)
            {
                case DiskNumberType.PhysicalDisk:
                    path = string.Format(@"\\.\GLOBALROOT\Device\Harddisk{0}\DR{0}", number);
                    break;
                case DiskNumberType.Volume:
                    path = string.Format(@"\\.\GLOBALROOT\Device\HarddiskVolume{0}", number);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
            InitateDevice(path);
        }
        public RawDisk(DriveInfo drive)
        {
            if (drive == null)
                throw new ArgumentNullException("drive");

            char driveLetter = drive.Name.ToUpper()[0];

            string path = string.Format(@"\\.\{0}:", driveLetter);
            InitateVolume(path);
        }

        private void InitateDevice(string dosName)
        {
            Debug.WriteLine("Initiating with " + dosName);

            _diskHandle = Win32Helper.CreateFile(dosName, FileAccess.Read, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
            DosDeviceName = dosName;

            if (_diskHandle.IsInvalid)
                throw new ArgumentException("Invalid diskName: " + dosName);

            _deviceIo = new DeviceIOControlWrapper(_diskHandle);
            _diskFs = new FileStream(_diskHandle, FileAccess.Read);

            _diskInfo = _deviceIo.DiskGetDriveGeometry();
            _deviceLength = _deviceIo.DiskGetLengthInfo();
        }
        private void InitateVolume(string dosName)
        {
            Debug.WriteLine("Initiating with " + dosName);

            _diskHandle = Win32Helper.CreateFile(dosName, FileAccess.Read, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
            DosDeviceName = dosName;

            if (_diskHandle.IsInvalid)
                throw new ArgumentException("Invalid diskName: " + dosName);

            _deviceIo = new DeviceIOControlWrapper(_diskHandle);
            _diskFs = new FileStream(_diskHandle, FileAccess.Read);

            _diskInfo = _deviceIo.DiskGetDriveGeometry();
            _deviceLength = _deviceIo.DiskGetLengthInfo();
        }

        public byte[] Read(long sector, int sectors)
        {
            if (sectors < 1)
                throw new ArgumentException("sectors");
            if (sector < 0 || sector + sectors > SectorCount)
                throw new ArgumentException("Out of bounds");

            long offsetBytes = sector * SectorSize;
            long newOffset = _diskFs.Seek(offsetBytes, SeekOrigin.Begin);

            Debug.Assert(newOffset == offsetBytes);

            byte[] data = new byte[SectorSize * sectors];
            int wasRead = _diskFs.Read(data, 0, data.Length);

            if (wasRead == 0)
                throw new EndOfStreamException();

            return data;
        }

        public void Dispose()
        {
            if (!_diskHandle.IsClosed)
                _diskHandle.Close();
        }
    }

    public enum DiskNumberType
    {
        PhysicalDisk,
        Volume,
    }
}
