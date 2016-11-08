using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using RawDiskLib;

namespace TestApplication.Examples
{
    public class ExampleHashDrives : ExampleBase
    {
        public override string Name
        {
            get { return "Hash Drives"; }
        }

        public override void Execute()
        {
            // 512 MB
            const long lengthTodo = 512L * 1024 * 1024;

            IEnumerable<char> volumeDrives = Utils.GetAllAvailableVolumes();

            foreach (char drive in volumeDrives)
            {
                Console.WriteLine("Hashing " + drive + ":");

                try
                {
                    using (RawDisk disk = new RawDisk(drive))
                    {
                        long diskReadLength = Math.Min(disk.SizeBytes, lengthTodo);
                        long totalRead = 0;

                        // Read in ~16M chunks
                        int increment = (int)(((16f * 1024f * 1024f) / disk.SectorSize) * disk.SectorSize);

                        Console.WriteLine("Reading {0:N0} B in {1:N0} B chunks. Chunks: {2:N0}", diskReadLength, increment, diskReadLength / increment);

                        byte[] input = new byte[increment];
                        byte[] output = new byte[increment];

                        MD5 md5 = MD5.Create();

                        Stopwatch sw = new Stopwatch();

                        using (RawDiskStream diskFs = disk.CreateDiskStream())
                        {
                            sw.Start();
                            while (true)
                            {
                                int read = (int)Math.Min(increment, diskReadLength - diskFs.Position);
                                int actualRead = diskFs.Read(input, 0, read);

                                if (actualRead == 0)
                                {
                                    Console.CursorTop++;
                                    if (totalRead == diskReadLength)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("Done! Read {0:N0} in total", totalRead);
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Done! Read {0:N0} in total (not expected)", totalRead);
                                    }
                                    Console.ForegroundColor = ExampleUtilities.DefaultColor;

                                    break;
                                }

                                md5.TransformBlock(input, 0, actualRead, output, 0);

                                totalRead += actualRead;

                                Console.WriteLine("Position: {0:N0} B, Progress: {1:#0.000%}, AvgSpeed: {2:N2} MB/s", diskFs.Position, diskFs.Position * 1f / diskReadLength, (diskFs.Position / 1048576f) / sw.Elapsed.TotalSeconds);
                                Console.CursorTop--;
                            }
                            sw.Stop();
                        }

                        md5.TransformFinalBlock(new byte[0], 0, 0);

                        Console.Write("Computed MD5: ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(BitConverter.ToString(md5.Hash).Replace("-", ""));
                        Console.ForegroundColor = ExampleUtilities.DefaultColor;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: " + ex.Message);
                    Console.ForegroundColor = ExampleUtilities.DefaultColor;
                }

                Console.WriteLine();
            }
        }
    }
}