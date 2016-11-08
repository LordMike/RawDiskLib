using System;
using System.Collections.Generic;
using System.ComponentModel;
using RawDiskLib;

namespace TestApplication.Examples
{
    public class ExampleVolumes : ExampleBase
    {
        public override string Name
        {
            get { return "Present Volumes"; }
        }

        public override void Execute()
        {
            IEnumerable<char> volumeDrives = Utils.GetAllAvailableVolumes();

            foreach (char device in volumeDrives)
            {
                Console.WriteLine("Trying volume, " + device + ": .. ");

                try
                {
                    using (RawDisk disk = new RawDisk(device))
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