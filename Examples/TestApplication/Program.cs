using System;
using TestApplication.Examples;

namespace TestApplication
{
    public class Program
    {
        public const uint FILE_READ_ATTRIBUTES = (0x0080);
        public const uint FILE_WRITE_ATTRIBUTES = 0x0100;

        private static ConsoleColor _defaultColor;
        const int ClustersToRead = 100;

        static void Main(string[] args)
        {
            ExampleBase[] examples = new ExampleBase[]
                {
                    new ExampleVolumes(),
                    new ExampleHarddiskVolumes(),
                    new ExamplePhysicalDrives(),
                    new ExampleCopyMFT(),
                    new ExampleWriteDrive(),
                    new ExampleHashDrives(),
                };


            for (int index = 0; index < examples.Length; index++)
            {
                ExampleBase example = examples[index];
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Beginning example {0} of {1} '{2}', type 'S' to skip, ENTER to continue.", index + 1, examples.Length, example.Name);
                Console.ForegroundColor = ExampleUtilities.DefaultColor;

                string input = Console.ReadLine();

                if (input.StartsWith("S", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Skipping..");
                    Console.WriteLine();
                    continue;
                }

                Console.Clear();

                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Executing example {0}", example.Name);
                    Console.ForegroundColor = ExampleUtilities.DefaultColor;

                    example.Execute();

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Completed example {0} of {1} '{2}'.", index + 1, examples.Length, example.Name);
                    Console.ForegroundColor = ExampleUtilities.DefaultColor;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Exampled failed");
                    Console.WriteLine("Exception message: " + ex.Message);
                    Console.ForegroundColor = ExampleUtilities.DefaultColor;
                }
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}