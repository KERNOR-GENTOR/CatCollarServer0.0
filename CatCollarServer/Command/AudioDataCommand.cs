using CatCollarServer.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;

namespace CatCollarServer.Command
{
    public static class AudioDataCommand
    {
        const string output_folder = "..\\Output";

        public static bool ReadData(ref Context context, in string inputFile)
        {
            if (inputFile == null)
            {
                Console.WriteLine("Input file is not specified");
                return false;
            }

            Console.WriteLine("Reading WAV data");
            WavData wavData = WavData.ReadFromFile(inputFile);

            if (wavData != null)
            {
                context.WavData = wavData;
            }

            return wavData != null;
        }

        public static bool SplitIntoFiles(ref Context context, string outputFolder)
        {
            int counter = 1;
            Console.WriteLine("Splitting data into separate words");

            if (context.WavData == null)
            {
                Console.WriteLine("Input data is not specified");
                return false;
            }

            // Determine results directory name
            string folder = output_folder;
            if (outputFolder != null)
            {
                folder = outputFolder;
            }
            Console.WriteLine("Output directory is: {0}", folder);

            // Check if output directory exists or can be created
            if (!InitOutputDirectory(folder))
            {
                return false;
            }

            // Create the Processor
            Processor processor = new Processor(context.WavData);
            processor.Init();
            context.Processor = processor;

            // Split wav data into words
            processor.DivideIntoWords();

            //Save result
            Console.WriteLine("Words: {0}", processor.Words.Count);

            foreach (Word word in processor.Words)
            {
                //!!!тут варто буде подумати над іменами, напевно
                string fileName = folder + "//" + counter.ToString() + ".wav";
                Console.WriteLine(fileName);
                processor.SaveWordAsAudio(fileName, word);
                counter++;
            }
            Console.WriteLine("Complete!");
            return true;
        }

        private static bool InitOutputDirectory(string folder)
        {
            if (!File.Exists(folder))
            {
                try
                {
                    //For operating system Windows
                    Directory.CreateDirectory(folder);
                }
                catch
                {
                    Console.WriteLine("Directory {0} can't be created", folder);
                    return false;
                }
            }
            else if (File.GetAttributes(folder).HasFlag(FileAttributes.Directory))
            {
                return true;
            }
            else
            {
                Console.WriteLine("File {0} is not a directory", folder);
                return false;
            }

            return true;
        }
    }
}
