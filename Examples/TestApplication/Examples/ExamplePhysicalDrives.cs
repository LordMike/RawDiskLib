using System;
using System.Collections.Generic;
using System.ComponentModel;
using RawDiskLib;

namespace TestApplication.Examples
{
    public class ExamplePhysicalDrives : ExampleBase
    {
        public override string Name
        {
            get { return "Present Physical Drives"; }
        }

        public override void Execute()
        {
            IEnumerable<int> physicalDrives = Utils.GetAllAvailableDrives(DiskNumberType.PhysicalDisk);

            foreach (int device in physicalDrives)
            {
                Console.WriteLine("Trying PhysicalDrive" + device + ": .. ");

                try
                {
                    using (RawDisk disk = new RawDisk(DiskNumberType.PhysicalDisk, device))
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