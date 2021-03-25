using System;
using System.Diagnostics;
using System.IO;
using DeviceIOControlLib.Objects.Disk;
using DeviceIOControlLib.Wrapper;
using Microsoft.Win32.SafeHandles;
using RawDiskLib.Helpers;

namespace RawDiskLib
{
    public class RawDisk : IDisposable
    {
        private FileAccess _access;
        private FileStream _diskFs;
        private DiskDeviceWrapper _deviceIo;
        private DISK_GEOMETRY _diskInfo;
        private int _sectorsPrCluster;

        public long SizeBytes { get; private set; }

        public long ClusterCount => SizeBytes / ClusterSize;

        public int ClusterSize { get; private set; }

        public long SectorCount => SizeBytes / _diskInfo.BytesPerSector;

        public int SectorSize => _diskInfo.BytesPerSector;

        public string DosDeviceName { get; private set; }

        public DISK_GEOMETRY DiskInfo => _diskInfo;

        /// <summary>
        /// The actual handle behind the scenes. Used for other Win32 calls.
        /// Do not close this.
        /// </summary>
        public SafeFileHandle DiskHandle { get; private set; }

        public RawDisk(string devicePath, FileAccess access = FileAccess.Read)
        {
            if (string.IsNullOrEmpty(devicePath) )
                throw new ArgumentException("DevicePath must be valid", nameof(devicePath));
            if ((access & FileAccess.Read) == 0)
                throw new ArgumentException("Access must include read");

            InitiateCommon(devicePath, access);
            InitateDevice();
        }

        public RawDisk(DiskNumberType type, int number, FileAccess access = FileAccess.Read)
        {
            if (number < 0)
                throw new ArgumentException("Invalid number");
            if ((access & FileAccess.Read) == 0)
                throw new ArgumentException("Access must include read");

            string path;
            switch (type)
            {
                case DiskNumberType.PhysicalDisk:
                    path = $@"\\.\GLOBALROOT\Device\Harddisk{number}\Partition0";
                    break;
                case DiskNumberType.Volume:
                    path = $@"\\.\GLOBALROOT\Device\HarddiskVolume{number}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            InitiateCommon(path, access);
            InitateDevice();
        }

        public RawDisk(char driveLetter, FileAccess access = FileAccess.Read)
        {
            if (!char.IsLetter(driveLetter))
                throw new ArgumentException("Invalid drive letter");
            if ((access & FileAccess.Read) == 0)
                throw new ArgumentException("Access must include read");

            driveLetter = char.ToUpper(driveLetter);

            string dosName = $@"\\.\{driveLetter}:";
            InitiateCommon(dosName, access);
            InitateVolume(driveLetter);
        }

        public RawDisk(DriveInfo drive, FileAccess access = FileAccess.Read)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));
            if ((access & FileAccess.Read) == 0)
                throw new ArgumentException("Access must include read");

            char driveLetter = drive.Name.ToUpper()[0];

            string dosName = $@"\\.\{driveLetter}:";
            InitiateCommon(dosName, access);
            InitateVolume(driveLetter);
        }

        private void InitiateCommon(string dosName, FileAccess access)
        {
            Debug.WriteLine("Initiating with " + dosName);

            DiskHandle = PlatformShim.CreateDeviceHandle(dosName, access);
            DosDeviceName = dosName;

            if (DiskHandle.IsInvalid)
                throw new ArgumentException("Invalid diskName: " + dosName);

            _access = access;

            _deviceIo = new DiskDeviceWrapper(DiskHandle);
            _diskFs = new FileStream(DiskHandle, _access);

            _diskInfo = _deviceIo.DiskGetDriveGeometry();
            SizeBytes = _deviceIo.DiskGetLengthInfo();
        }

        private void InitateDevice()
        {
            Debug.WriteLine("Initiating type Device");

            ClusterSize = _diskInfo.BytesPerSector;
            _sectorsPrCluster = ClusterSize / _diskInfo.BytesPerSector;
        }

        private void InitateVolume(char driveLetter)
        {
            Debug.WriteLine("Initiating type Volume");

            uint sectorsPerCluster, bytesPerSector, numberOfFreeClusters, numberOfClusters;
            bool success = PlatformShim.GetDiskFreeSpace(driveLetter + ":", out sectorsPerCluster, out bytesPerSector, out numberOfFreeClusters, out numberOfClusters);

            if (success)
            {
                ClusterSize = (int)(bytesPerSector * sectorsPerCluster);
                _sectorsPrCluster = (int)sectorsPerCluster;
            }
        }

        public void WriteClusters(byte[] data, long cluster)
        {
            int clusters = data.Length / ClusterSize;

            if (data.Length % ClusterSize != 0)
                throw new ArgumentException("Data length");
            if (cluster < 0 || cluster + clusters > ClusterCount)
                throw new ArgumentException("Out of bounds", nameof(cluster));

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
            if (clusters <= 0)
                throw new ArgumentException("Clusters must be larger than 0", nameof(clusters));
            if (cluster < 0 || cluster + clusters > ClusterCount)
                throw new ArgumentException("Out of bounds", nameof(cluster));
            if (buffer.Length - bufferOffset < clusters * ClusterSize)
                throw new ArgumentException("Buffer not large enough", nameof(buffer));
            if (!(0 <= bufferOffset && bufferOffset <= buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(bufferOffset));

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
            if (sectors <= 0)
                throw new ArgumentException("Sectors must be larger than 0", nameof(sectors));
            if (sector < 0 || sector + sectors > SectorCount)
                throw new ArgumentException("Out of bounds", nameof(sector));
            if (buffer.Length - bufferOffset < sectors * SectorSize)
                throw new ArgumentException("Buffer not large enough", nameof(buffer));
            if (!(0 <= bufferOffset && bufferOffset <= buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(bufferOffset));

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
            SafeFileHandle diskHandle = PlatformShim.CreateDeviceHandle(DosDeviceName, _access);
            FileStream diskFs = new FileStream(diskHandle, _access);

            return new RawDiskStream(diskFs, SectorSize, SizeBytes);
        }

        public void Dispose()
        {
            if (!DiskHandle.IsClosed)
                DiskHandle.Dispose();
        }
    }
}
