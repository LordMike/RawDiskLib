using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DeviceIOControlLib;
using Microsoft.Win32.SafeHandles;
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

    public class RawDisk : IDisposable
    {
        private SafeFileHandle _diskHandle;
        private DeviceIOControlWrapper _deviceIo;
        private DISK_GEOMETRY_EX _diskInfo;
        private PARTITION_INFORMATION_EX _partitionInfo;

        public long SizeBytes
        {
            get { return _partitionInfo.PartitionLength; }
        }
        public long SectorCount
        {
            get { return _partitionInfo.PartitionLength / SectorSize; }
        }
        public int SectorSize
        {
            get { return _diskInfo.Geometry.BytesPerSector; }
        }

        public long DiskOffsetSectors
        {
            get { return _partitionInfo.StartingOffset / SectorSize; }
        }

        public bool IsPartition
        {
            get
            {
                return (_partitionInfo.PartitionStyle == PartitionStyle.PARTITION_STYLE_MBR && _partitionInfo.DriveLayoutInformaiton.Mbr.RecognizedPartition) ||
                        (_partitionInfo.PartitionStyle == PartitionStyle.PARTITION_STYLE_GPT);
            }
        }

        public string DosDeviceName { get; private set; }

        public PARTITION_INFORMATION_EX PartitionInfo
        {
            get { return _partitionInfo; }
        }
        public DISK_GEOMETRY_EX DiskInfo
        {
            get { return _diskInfo; }
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
            Initate(path);
        }
        public RawDisk(char driveLetter)
        {
            if (!char.IsLetter(driveLetter))
                throw new ArgumentException("Invalid drive letter");

            driveLetter = char.ToUpper(driveLetter);

            string path = string.Format(@"\\.\{0}:", driveLetter);
            Initate(path);
        }
        public RawDisk(DriveInfo drive)
        {
            if (drive == null)
                throw new ArgumentNullException("drive");

            char driveLetter = drive.Name.ToUpper()[0];

            string path = string.Format(@"\\.\{0}:", driveLetter);
            Initate(path);
        }

        private void Initate(string dosName)
        {
            Debug.WriteLine("Initiating with " + dosName);

            _diskHandle = Win32Helper.CreateFile(dosName, FileAccess.Read, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
            DosDeviceName = dosName;

            if (_diskHandle.IsInvalid)
                throw new ArgumentException("Invalid diskName: " + dosName);

            _deviceIo = new DeviceIOControlWrapper(_diskHandle);

            _diskInfo = _deviceIo.DiskGetDriveGeometryEx();
            _partitionInfo = _deviceIo.DiskGetPartitionInfoEx();
        }

        public byte[] Read(long sector, int sectors)
        {
            if (sectors < 1)
                throw new ArgumentException("sectors");
            if (sector < 0 || sector + sectors > SectorCount)
                throw new ArgumentException("Out of bounds");

            long offsetBytes = sector * SectorSize;

            using (FileStream fs = new FileStream(_diskHandle, FileAccess.Read))
            {
                long newOffset = fs.Seek(offsetBytes, SeekOrigin.Begin);

                byte[] data = new byte[SectorSize * sectors];
                int wasRead = fs.Read(data, 0, data.Length);

                if (wasRead == 0)
                    throw new EndOfStreamException();

                return data;
            }
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
