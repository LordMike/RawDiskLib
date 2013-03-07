using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using DeviceIOControlLib;
using Microsoft.Win32.SafeHandles;
using RawDiskLib;
using System.Linq;
using FileAttributes = System.IO.FileAttributes;

namespace TestApplication
{
    public class Program
    {
        public const uint FILE_READ_ATTRIBUTES = (0x0080);
        public const uint FILE_WRITE_ATTRIBUTES = 0x0100;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
           string lpFileName,
           [MarshalAs(UnmanagedType.U4)] uint dwDesiredAccess,
           [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
           IntPtr lpSecurityAttributes,
           [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
           [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
           IntPtr hTemplateFile);

        private static ConsoleColor _defaultColor;
        const int ClustersToRead = 100;

        static void Main(string[] args)
        {
            List<string> devs = Utils.GetAvailableDrives();

            List<char> volumeDrives = devs.Where(s => s.Length == 2 && s.EndsWith(":")).Select(s => s[0]).ToList();
            List<int> harddiskVolumes = devs.Where(s => s.StartsWith("HarddiskVolume")).Select(s => int.Parse(s.Substring("HarddiskVolume".Length))).ToList();
            List<int> physicalDrives = devs.Where(s => s.StartsWith("PhysicalDrive")).Select(s => int.Parse(s.Substring("PhysicalDrive".Length))).ToList();

            _defaultColor = Console.ForegroundColor;

            // Volumes (C:, E: ..)
            {
                foreach (char device in volumeDrives)
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
                        Console.ForegroundColor = _defaultColor;
                    }

                    Console.WriteLine();
                }
            }

            Console.WriteLine("Done with Volumes, beginning Physical Drives, type ENTER to proceed.");
            Console.ReadLine();
            Console.Clear();

            // Physical Drives (PhysicalDrive0, PhysicalDrive1 ..)
            {
                foreach (int device in physicalDrives)
                {
                    Console.WriteLine("Trying PhysicalDrive" + device + ": .. ");

                    try
                    {
                        RawDisk disk = new RawDisk(DiskNumberType.PhysicalDisk, device);
                        PresentResult(disk);
                    }
                    catch (Win32Exception exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: " + new Win32Exception(exception.NativeErrorCode).Message);
                        Console.ForegroundColor = _defaultColor;
                    }

                    Console.WriteLine();
                }
            }

            Console.WriteLine("Done with Physical Drives, beginning Harddisk Volumes, type ENTER to proceed.");
            Console.ReadLine();
            Console.Clear();

            // Harddisk Volumes (HarddiskVolume1, HarddiskVolume2 ..)
            {
                foreach (int device in harddiskVolumes)
                {
                    Console.WriteLine("Trying HarddiskVolume" + device + ": .. ");

                    try
                    {
                        RawDisk disk = new RawDisk(DiskNumberType.Volume, device);
                        PresentResult(disk);
                    }
                    catch (Win32Exception exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: " + new Win32Exception(exception.NativeErrorCode).Message);
                        Console.ForegroundColor = _defaultColor;
                    }

                    Console.WriteLine();
                }
            }

            Console.WriteLine("Done with Harddisk Volumes, beginning Copy of $MFT files, type ENTER to proceed.");
            Console.ReadLine();
            Console.Clear();

            // Copy MFT files
            {
                foreach (char device in volumeDrives)
                {
                    Console.WriteLine("Trying to copy MFT off of volume, " + device + ": .. ");

                    try
                    {
                        string dstFile = device + "-MFT.bin";

                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        CopyFile(device + @":\$MFT", dstFile);
                        sw.Stop();

                        long targetFileSize = new FileInfo(dstFile).Length;

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Copied    : {0:N0} bytes to '{1}'", targetFileSize, dstFile);
                        Console.WriteLine("Time used : {0:N1} ms", sw.ElapsedMilliseconds);
                        Console.WriteLine("Speed     : {0:N2} MB/s", (targetFileSize / 1024) / sw.Elapsed.TotalMilliseconds);
                    }
                    catch (Exception exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: " + exception.Message);
                    }

                    Console.ForegroundColor = _defaultColor;
                    Console.WriteLine();
                }
            }

            Console.WriteLine("Done with Copy of $MFT files, beginning Writing, type ENTER to proceed.");
            Console.ReadLine();
            Console.Clear();

            // Writing
            {
                Console.WriteLine("You need to enter a volume on which to write and read. Note that this volume will be useless afterwards - do not chose anything by test volumes!");
                Console.WriteLine("Select volume:");
                List<int> options = new List<int>();

                foreach (int harddiskVolume in harddiskVolumes)
                {
                    try
                    {
                        RawDisk disk = new RawDisk(DiskNumberType.Volume, harddiskVolume);

                        char[] driveLetters = DiskHelper.GetDriveLetters(disk.DosDeviceName.Remove(0, @"\\.\GLOBALROOT".Length)).Where(volumeDrives.Contains).ToArray();

                        Console.WriteLine("  {0:N0}: {1:N0} Bytes, Drive Letters: {2}", harddiskVolume, disk.SizeBytes, string.Join(", ", driveLetters));
                        options.Add(harddiskVolume);
                    }
                    catch (Exception)
                    {
                        // Don't write it
                    }
                }

                string vol;
                int selectedVol;
                do
                {
                    Console.WriteLine("Enter #:");
                    vol = Console.ReadLine();
                } while (!(int.TryParse(vol, out selectedVol) && options.Contains(selectedVol)));

                Console.WriteLine("Selected " + selectedVol + ".");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Are you sure? Type YES.");
                Console.ForegroundColor = _defaultColor;

                bool allSuccess = false;

                string confirm = Console.ReadLine();
                if (confirm == "YES")
                {
                    Console.WriteLine("Confirmed - starting");

                    RawDisk disk = new RawDisk(DiskNumberType.Volume, selectedVol, FileAccess.ReadWrite);

                    long chunks = disk.ClusterCount / ClustersToRead;

                    Console.WriteLine("Disk {0} is {1:N0} Bytes large.", disk.DosDeviceName, disk.SizeBytes);
                    Console.WriteLine("Beginning random write in {0:N0} cluster chunks of {1:N0} Bytes each", ClustersToRead, disk.ClusterSize);
                    Console.WriteLine(" # of chunks: {0:N0}", chunks);

                    byte[] chunkData = new byte[disk.ClusterSize * ClustersToRead];
                    byte[] readData = new byte[disk.ClusterSize * ClustersToRead];
                    Random rand = new Random();

                    StringBuilder blankLineBuilder = new StringBuilder(Console.BufferWidth);
                    for (int i = 0; i < Console.BufferWidth; i++)
                        blankLineBuilder.Append(' ');
                    string blankLine = blankLineBuilder.ToString();

                    allSuccess = true;

                    for (int chunk = 0; chunk < chunks; chunk++)
                    {
                        rand.NextBytes(chunkData);

                        Console.Write("Chunk #{0}: ", chunk);

                        try
                        {
                            // Write
                            Console.Write("Writing ... ");

                            disk.WriteClusters(chunkData, chunk * ClustersToRead);

                            // Read
                            Console.Write("Reading ... ");

                            disk.ReadClusters(readData, 0, chunk * ClustersToRead, ClustersToRead);

                            // Check
                            if (chunkData.SequenceEqual(readData))
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Confirmed!");
                                Console.ForegroundColor = _defaultColor;
                            }
                            else
                            {
                                allSuccess = false;

                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Failed!");
                                Console.ForegroundColor = _defaultColor;

                                Console.WriteLine("Presse enter to proceed.");
                                Console.ReadLine();
                            }
                        }
                        catch (Exception ex)
                        {
                            allSuccess = false;

                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error: " + ex.Message);
                            Console.ForegroundColor = _defaultColor;

                            Console.WriteLine("Presse enter to proceed.");
                            Console.ReadLine();
                        }

                        Console.CursorTop--;
                        Console.CursorLeft = 0;
                        Console.Write(blankLine);
                        Console.CursorTop--;
                        Console.CursorLeft = 0;
                    }
                }
                else
                {
                    Console.WriteLine("Aborted");
                }

                if (allSuccess)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("All chunks were able to write and read successfully.");
                    Console.ForegroundColor = _defaultColor;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Some chunks were unable to be read correctly.");
                    Console.ForegroundColor = _defaultColor;
                }
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static void CopyFile(string sourceFile, string dstFile)
        {
            // FileAttributes 0x20000000L = FILE_FLAG_NO_BUFFERING
            SafeFileHandle fileHandle = CreateFile(sourceFile, FILE_READ_ATTRIBUTES, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal | (FileAttributes)0x20000000L, IntPtr.Zero);

            if (fileHandle.IsInvalid)
                throw new ArgumentException("Invalid file: " + sourceFile);

            //var driveWrapper = new DeviceIOControlWrapper(driveHandle);
            DeviceIOControlWrapper fileWrapper = new DeviceIOControlWrapper(fileHandle);

            FileExtentInfo[] extents = fileWrapper.FileSystemGetRetrievalPointers();
            decimal totalSize = extents.Sum(s => (decimal)s.Size);
            decimal copiedBytes = 0;

            RawDisk disk = new RawDisk(char.ToUpper(sourceFile[0]));

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

            Debug.Assert(copiedBytes == totalSize);
        }

        private static void PresentResult(RawDisk disk)
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
            Console.ForegroundColor = _defaultColor;

            if (!isFat && !isNTFS)
                Console.ForegroundColor = ConsoleColor.Red;

            if (isFat)
                Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("Is FAT        : {0}", isFat ? fatType : "False");
            Console.ForegroundColor = _defaultColor;

            if (!allZero)
                Console.ForegroundColor = ConsoleColor.Green;
            else
                Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine("All bytes zero: {0}", allZero);
            Console.ForegroundColor = _defaultColor;
        }
    }
}