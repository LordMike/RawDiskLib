using System;
using System.Collections.Generic;
using System.ComponentModel;
using RawDiskLib;

namespace TestApplication.Examples
{
    public class ExampleHarddiskVolumes : ExampleBase
    {
        public override string Name
        {
            get { return "Present Harddisk Volumes"; }
        }

        public override void Execute()
        {
            IEnumerable<int> harddiskVolumes = Utils.GetAllAvailableDrives(DiskNumberType.Volume);

            foreach (int device in harddiskVolumes)
            {
                Console.WriteLine("Trying HarddiskVolume" + device + ": .. ");

                try
                {
                    using (RawDisk disk = new RawDisk(DiskNumberType.Volume, device))
                    {
                        ExampleUtilities.PresentResult(disk);
                    }
                }
                catch (Win32Exception exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: " + new Win32Exception(exception.NativeErrorCode).Message);
                    Console.ForegroundColor = ExampleUtilities.DefaultColor;
                }

                Console.WriteLine();
            }
        }
    }
}