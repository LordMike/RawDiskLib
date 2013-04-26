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

        private FileAccess _access;
        private SafeFileHandle _diskHandle;
        private FileStream _diskFs;
        private DeviceIOControlWrapper _deviceIo;
        private DISK_GEOMETRY _diskInfo;

        private long _deviceLength;
        private int _clusterSize;
        private int _sectorsPrCluster;

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

        public RawDisk(DiskNumberType type, int number, FileAccess access = FileAccess.Read)
        {
            if (number < 0)
                throw new ArgumentException("Invalid number");
            if (!access.HasFlag(FileAccess.Read))
                throw new ArgumentException("Access must include read");

            string path;
            switch (type)
            {
                case DiskNumberType.PhysicalDisk:
                    path = string.Format(@"\\.\GLOBALROOT\Device\Harddisk{0}\Partition0", number);
                    break;
                case DiskNumberType.Volume:
                    path = string.Format(@"\\.\GLOBALROOT\Device\HarddiskVolume{0}", number);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }

            InitiateCommon(path, access);
            InitateDevice();
        }

        public RawDisk(char driveLetter, FileAccess access = FileAccess.Read)
        {
            if (!char.IsLetter(driveLetter))
                throw new ArgumentException("Invalid drive letter");
            if (!access.HasFlag(FileAccess.Read))
                throw new ArgumentException("Access must include read");

            driveLetter = char.ToUpper(driveLetter);

            string dosName = string.Format(@"\\.\{0}:", driveLetter);
            InitiateCommon(dosName, access);
            InitateVolume(driveLetter);
        }

        public RawDisk(DriveInfo drive, FileAccess access = FileAccess.Read)
        {
            if (drive == null)
                throw new ArgumentNullException("drive");
            if (!access.HasFlag(FileAccess.Read))
                throw new ArgumentException("Access must include read");

            char driveLetter = drive.Name.ToUpper()[0];

            string dosName = string.Format(@"\\.\{0}:", driveLetter);
            InitiateCommon(dosName, access);
            InitateVolume(driveLetter);
        }

        private void InitiateCommon(string dosName, FileAccess access)
        {
            Debug.WriteLine("Initiating with " + dosName);

            _diskHandle = CreateDeviceHandle(dosName, access);
            DosDeviceName = dosName;

            if (_diskHandle.IsInvalid)
                throw new ArgumentException("Invalid diskName: " + dosName);

            _access = access;

            _deviceIo = new DeviceIOControlWrapper(_diskHandle);
            _diskFs = new FileStream(_diskHandle, _access);

            _diskInfo = _deviceIo.DiskGetDriveGeometry();
            _deviceLength = _deviceIo.DiskGetLengthInfo();
        }

        private void InitateDevice()
        {
            Debug.WriteLine("Initiating type Device");

            _clusterSize = _diskInfo.BytesPerSector;
            _sectorsPrCluster = _clusterSize / _diskInfo.BytesPerSector;
        }

        private void InitateVolume(char driveLetter)
        {
            Debug.WriteLine("Initiating type Volume");

            uint sectorsPerCluster, bytesPerSector, numberOfFreeClusters, numberOfClusters;
            bool success = GetDiskFreeSpace(driveLetter + ":", out sectorsPerCluster, out bytesPerSector, out numberOfFreeClusters, out numberOfClusters);

            if (success)
            {
                _clusterSize = (int)(bytesPerSector * sectorsPerCluster);
                _sectorsPrCluster = (int)sectorsPerCluster;
            }
        }

        public void WriteClusters(byte[] data, long cluster)
        {
            int clusters = data.Length / ClusterSize;

            if (data.Length % ClusterSize != 0)
                throw new ArgumentException("Data length");
            if (cluster < 0 || cluster + clusters > ClusterCount)
                throw new ArgumentException("Out of bounds");

            long offsetBytes = cluster * ClusterSize;

            long actualOffset = _diskFs.Position;
            if (_diskFs.Position != offsetBytes)
                actualOffset = _diskFs.Seek(offsetBytes, SeekOrigin.Begin);

            Debug.Assert(actualOffset == offsetBytes);

            _diskFs.Write(data, 0, data.Length);
        }

        public byte[] ReadClusters(long cluster, int clusters)
        {
            byte[] data = new byte[ClusterSize * clusters];
            ReadClusters(data, 0, cluster, clusters);

            return data;
        }

        public int ReadClusters(byte[] buffer, int bufferOffset, long cluster, int clusters)
        {
            if (clusters < 1)
                throw new ArgumentException("clusters");
            if (cluster < 0 || cluster + clusters > ClusterCount)
                throw new ArgumentException("Out of bounds");
            if (buffer.Length - bufferOffset < clusters * ClusterSize)
                throw new ArgumentException("Buffer not large enough");
            if (!(0 <= bufferOffset && bufferOffset <= buffer.Length))
                throw new ArgumentOutOfRangeException("bufferOffset");

            return ReadSectors(buffer, bufferOffset, cluster * _sectorsPrCluster, clusters * _sectorsPrCluster);
        }

        public byte[] ReadSectors(long sector, int sectors)
        {
            byte[] data = new byte[SectorSize * sectors];
            ReadSectors(data, 0, sector, sectors);

            return data;
        }

        public int ReadSectors(byte[] buffer, int bufferOffset, long sector, int sectors)
        {
            if (sectors < 1)
                throw new ArgumentException("sectors");
            if (sector < 0 || sector + sectors > SectorCount)
                throw new ArgumentException("Out of bounds");
            if (buffer.Length - bufferOffset < sectors * SectorSize)
                throw new ArgumentException("Buffer not large enough");
            if (!(0 <= bufferOffset && bufferOffset <= buffer.Length))
                throw new ArgumentOutOfRangeException("bufferOffset");

            long offsetBytes = sector * SectorSize;

            long actualOffset = _diskFs.Position;
            if (_diskFs.Position != offsetBytes)
                actualOffset = _diskFs.Seek(offsetBytes, SeekOrigin.Begin);

            Debug.Assert(actualOffset == offsetBytes);

            int wasRead = _diskFs.Read(buffer, bufferOffset, sectors * SectorSize);

            return wasRead;
        }

        public RawDiskStream CreateDiskStream()
        {
            SafeFileHandle diskHandle = CreateDeviceHandle(DosDeviceName, _access);
            FileStream diskFs = new FileStream(diskHandle, _access);

            return new RawDiskStream(diskFs, SectorSize, SizeBytes);
        }

        private static SafeFileHandle CreateDeviceHandle(string dosName, FileAccess access)
        {
            return Win32Helper.CreateFile(dosName, access, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
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
