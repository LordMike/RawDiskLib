using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using DeviceIOControlLib;
using Microsoft.Win32.SafeHandles;
using RawDiskLib.Helpers;
using FileAttributes = System.IO.FileAttributes;

namespace RawDiskLib
{
    public class RawDisk : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetDiskFreeSpace(string lpRootPathName,
           out uint lpSectorsPerCluster,
           out uint lpBytesPerSector,
           out uint lpNumberOfFreeClusters,
           out uint lpTotalNumberOfClusters);

        private SafeFileHandle _diskHandle;
        private FileStream _diskFs;
        private DeviceIOControlWrapper _deviceIo;
        private DISK_GEOMETRY _diskInfo;

        private long _deviceLength;
        private int _clusterSize;

        public long SizeBytes
        {
            get { return _deviceLength; }
        }
        public long ClusterCount
        {
            get { return _deviceLength / _clusterSize; }
        }
        public int ClusterSize
        {
            get { return _clusterSize; }
        }

        public long SectorCount
        {
            get { return _deviceLength / _diskInfo.BytesPerSector; }
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
        public RawDisk(char driveLetter)
        {
            if (!char.IsLetter(driveLetter))
                throw new ArgumentException("Invalid drive letter");

            driveLetter = char.ToUpper(driveLetter);

            InitateVolume(driveLetter);
        }
        public RawDisk(DriveInfo drive)
        {
            if (drive == null)
                throw new ArgumentNullException("drive");

            char driveLetter = drive.Name.ToUpper()[0];

            InitateVolume(driveLetter);
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
            _clusterSize = _diskInfo.BytesPerSector;
        }
        private void InitateVolume(char driveLetter)
        {
            string dosName = string.Format(@"\\.\{0}:", driveLetter);
            Debug.WriteLine("Initiating with " + dosName);

            _diskHandle = Win32Helper.CreateFile(dosName, FileAccess.Read, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
            DosDeviceName = dosName;

            if (_diskHandle.IsInvalid)
                throw new ArgumentException("Invalid diskName: " + dosName);

            _deviceIo = new DeviceIOControlWrapper(_diskHandle);
            _diskFs = new FileStream(_diskHandle, FileAccess.Read);

            _diskInfo = _deviceIo.DiskGetDriveGeometry();
            _deviceLength = _deviceIo.DiskGetLengthInfo();

            uint sectorsPerCluster, bytesPerSector, numberOfFreeClusters, numberOfClusters;
            bool success = GetDiskFreeSpace(driveLetter + ":", out sectorsPerCluster, out bytesPerSector, out numberOfFreeClusters, out numberOfClusters);

            if (success)
            {
                _clusterSize = (int)(bytesPerSector * sectorsPerCluster);
            }
        }

        public byte[] ReadClusters(long cluster, int clusters)
        {
            if (clusters < 1)
                throw new ArgumentException("clusters");
            if (cluster < 0 || cluster + clusters > ClusterCount)
                throw new ArgumentException("Out of bounds");

            long offsetBytes = cluster * ClusterSize;
            long newOffset = _diskFs.Seek(offsetBytes, SeekOrigin.Begin);

            Debug.Assert(newOffset == offsetBytes);

            byte[] data = new byte[ClusterSize * clusters];
            int wasRead = _diskFs.Read(data, 0, data.Length);

            if (wasRead == 0)
                throw new EndOfStreamException();

            return data;
        }

        public byte[] ReadSectors(long sector, int sectors)
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
