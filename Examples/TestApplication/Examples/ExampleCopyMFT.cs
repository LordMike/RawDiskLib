using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using RawDiskLib;

namespace TestApplication.Examples
{
    public class ExampleCopyMFT : ExampleBase
    {
        public override string Name
        {
            get { return "Copy $MFT's"; }
        }

        public override void Execute()
        {
            IEnumerable<char> volumeDrives = Utils.GetAllAvailableVolumes();

            foreach (char device in volumeDrives)
            {
                Console.WriteLine("Trying to copy MFT off of volume, " + device + ": .. ");

                try
                {
                    string dstFile = device + "-MFT.bin";

                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    ExampleUtilities.CopyFile(device + @":\$MFT", dstFile);
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

                Console.ForegroundColor = ExampleUtilities.DefaultColor;
                Console.WriteLine();
            }
        }
    }
}