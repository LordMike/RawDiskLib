using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RawDiskLib;

namespace TestApplication.Examples
{
    public class ExampleWriteDrive : ExampleBase
    {
        public override string Name
        {
            get { return "Write to a drive"; }
        }

        public override void Execute()
        {
            char[] volumeDrives = Utils.GetAllAvailableVolumes();
            int[] harddiskVolumes = Utils.GetAllAvailableDrives(DiskNumberType.Volume);

            Console.WriteLine("You need to enter a volume on which to write and read. Note that this volume will be useless afterwards - do not chose anything by test volumes!");
            Console.WriteLine("Select volume:");
            List<int> options = new List<int>();

            foreach (int harddiskVolume in harddiskVolumes)
            {
                try
                {
                    using (RawDisk disk = new RawDisk(DiskNumberType.Volume, harddiskVolume))
                    {
                        char[] driveLetters = DiskHelper.GetDriveLetters(disk.DosDeviceName.Remove(0, @"\\.\GLOBALROOT".Length)).Where(volumeDrives.Contains).ToArray();

                        Console.WriteLine("  {0:N0}: {1:N0} Bytes, Drive Letters: {2}", harddiskVolume, disk.SizeBytes, string.Join(", ", driveLetters));
                        options.Add(harddiskVolume);
                    }
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
            Console.ForegroundColor = ExampleUtilities.DefaultColor;

            bool allSuccess = false;

            string confirm = Console.ReadLine();
            if (confirm == "YES")
            {
                Console.WriteLine("Confirmed - starting");

                using (RawDisk disk = new RawDisk(DiskNumberType.Volume, selectedVol, FileAccess.ReadWrite))
                {
                    long chunks = disk.ClusterCount / ExampleUtilities.ClustersToRead;

                    Console.WriteLine("Disk {0} is {1:N0} Bytes large.", disk.DosDeviceName, disk.SizeBytes);
                    Console.WriteLine("Beginning random write in {0:N0} cluster chunks of {1:N0} Bytes each", ExampleUtilities.ClustersToRead, disk.ClusterSize);
                    Console.WriteLine(" # of chunks: {0:N0}", chunks);

                    byte[] chunkData = new byte[disk.ClusterSize * ExampleUtilities.ClustersToRead];
                    byte[] readData = new byte[disk.ClusterSize * ExampleUtilities.ClustersToRead];
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

                            disk.WriteClusters(chunkData, chunk * ExampleUtilities.ClustersToRead);

                            // Read
                            Console.Write("Reading ... ");

                            disk.ReadClusters(readData, 0, chunk * ExampleUtilities.ClustersToRead, (int) ExampleUtilities.ClustersToRead);

                            // Check
                            if (chunkData.SequenceEqual(readData))
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("Confirmed!");
                                Console.ForegroundColor = ExampleUtilities.DefaultColor;
                            }
                            else
                            {
                                allSuccess = false;

                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Failed!");
                                Console.ForegroundColor = ExampleUtilities.DefaultColor;

                                Console.WriteLine("Presse enter to proceed.");
                                Console.ReadLine();
                            }
                        }
                        catch (Exception ex)
                        {
                            allSuccess = false;

                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error: " + ex.Message);
                            Console.ForegroundColor = ExampleUtilities.DefaultColor;

                            Console.WriteLine("Presse enter to proceed.");
                            Console.ReadLine();

                            Console.CursorTop--;
                        }

                        Console.CursorTop--;
                        Console.CursorLeft = 0;
                        Console.Write(blankLine);
                        Console.CursorTop--;
                        Console.CursorLeft = 0;
                    }
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
                Console.ForegroundColor = ExampleUtilities.DefaultColor;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Some chunks were unable to be read correctly.");
                Console.ForegroundColor = ExampleUtilities.DefaultColor;
            }
        }
    }
}