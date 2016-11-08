using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DeviceIOControlLib.Objects.FileSystem;
using DeviceIOControlLib.Wrapper;
using Microsoft.Win32.SafeHandles;
using RawDiskLib;
using TestApplication.Utilities;
using FileAttributes = System.IO.FileAttributes;

namespace TestApplication.Examples
{
    internal static class ExampleUtilities
    {
        public const ConsoleColor DefaultColor = ConsoleColor.Gray;
        public const long ClustersToRead = 100;

        public static void PresentResult(RawDisk disk)
        {
            byte[] data = disk.ReadClusters(0, (int)Math.Min(disk.ClusterCount, ClustersToRead));

            string fatType = Encoding.ASCII.GetString(data, 82, 8);     // Extended FAT parameters have a display name here.
            bool isFat = fatType.StartsWith("FAT");
            bool isNTFS = Encoding.ASCII.GetString(data, 3, 4) == "NTFS";

            // Optimization, if it's a known FS, we know it's not all zeroes.
            bool allZero = (!isNTFS || !isFat) && data.All(s => s == 0);

            Console.WriteLine("Size in bytes : {0:N0}", disk.SizeBytes);
            Console.WriteLine("Sectors       : {0:N0}", disk.ClusterCount);
            Console.WriteLine("SectorSize    : {0:N0}", disk.SectorSize);
            Console.WriteLine("ClusterCount  : {0:N0}", disk.ClusterCount);
            Console.WriteLine("ClusterSize   : {0:N0}", disk.ClusterSize);

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

        public static void CopyFile(string sourceFile, string dstFile)
        {
            // FileAttributes 0x20000000L = FILE_FLAG_NO_BUFFERING
            SafeFileHandle fileHandle = Win32Helper.CreateFile(sourceFile, (FileAccess)Program.FILE_READ_ATTRIBUTES, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal | (FileAttributes)0x20000000L, IntPtr.Zero);

            if (fileHandle.IsInvalid)
                throw new ArgumentException("Invalid file: " + sourceFile);

            //var driveWrapper = new DeviceIOControlWrapper(driveHandle);
            FilesystemDeviceWrapper fileWrapper = new FilesystemDeviceWrapper(fileHandle);

            FileExtentInfo[] extents = fileWrapper.FileSystemGetRetrievalPointers();
            decimal totalSize = extents.Sum(s => (decimal)s.Size);
            decimal copiedBytes = 0;

            using (RawDisk disk = new RawDisk(char.ToUpper(sourceFile[0])))
            {
                // Write to the source file
                using (FileStream fs = new FileStream(dstFile, FileMode.Create))
                {
                    // Copy all extents
                    foreach (FileExtentInfo fileExtentInfo in extents)
                    {
                        // Copy chunks of data
                        for (ulong offset = 0; offset < fileExtentInfo.Size; offset += 10000)
                        {
                            int currentSizeBytes = (int)Math.Min(10000, fileExtentInfo.Size - offset);
                            byte[] data = disk.ReadClusters((long)(fileExtentInfo.Lcn + offset), currentSizeBytes);
                            fs.Write(data, 0, data.Length);

                            copiedBytes += currentSizeBytes;
                        }
                    }
                }
            }

            Debug.Assert(copiedBytes == totalSize);
        }
    }
}