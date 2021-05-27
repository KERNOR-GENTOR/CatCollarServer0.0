using CatCollarServer.Command;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CatCollarServer
{
    public static class Recorder
    {
        private const string output_folder = "..\\CatCollarServer\\Output\\";
        private static List<string> sourceList;
        private static List<WaveInEvent> sourceStreams = new List<WaveInEvent>();
        private static List<WaveFileWriter> waveWriters = new List<WaveFileWriter>();
        private static List<System.Windows.Forms.Timer> timers = new List<System.Windows.Forms.Timer>();

        public static void Run()
        {
            //List<WaveInCapabilities> sources = new List<WaveInCapabilities>();
            //for (int i = 0; i < WaveIn.DeviceCount; i++)
            //{
            //    Console.WriteLine("Found " + WaveIn.GetCapabilities(i).ProductName);
            //    sources.Add(WaveIn.GetCapabilities(i));
            //}

            //sourceList = new List<string>();
            //sourceStreams = new List<WaveInEvent>();
            //waveWriters = new List<WaveFileWriter>();
            //timers = new List<System.Windows.Forms.Timer>();
            //foreach (var source in sources)
            //{
            //    sourceList.Add(source.ProductName);
            //    sourceStreams.Insert(sourceList.IndexOf(source.ProductName), new WaveInEvent());
            //    waveWriters.Insert(sourceList.IndexOf(source.ProductName), null);
            //    timers.Insert(sourceList.IndexOf(source.ProductName), new System.Windows.Forms.Timer());
            //    Console.WriteLine($"Added {source.ProductName}.");
            //}
            //CommandFacad.Devices = sourceList;

            Parallel.Invoke(DevicesLoop);
            Parallel.ForEach(sourceList, RecordingLoop);

            //List<Task> tasks = new List<Task>();

            //foreach (string source in sourcelist)
            //{
            //    tasks.Add(new Task(() => ForeverLoop(source)));
            //}
            //foreach (Task task in tasks)
            //{
            //    task.RunSynchronously();
            //}
            //foreach (Task task in tasks)
            //{
            //    task.Wait();
            //}
        }

        private static object deviceLock = new object();

        private static void DevicesLoop()
        {
            while (true)
            {
                lock (deviceLock)
                {
                    SearchDevices();
                }
                Thread.Sleep(5000);
            }
        }

        private static void SearchDevices()
        {
            List<WaveInCapabilities> sources = new List<WaveInCapabilities>();
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                Console.WriteLine("Found " + WaveIn.GetCapabilities(i).ProductName);
                sources.Add(WaveIn.GetCapabilities(i));
            }

            sourceList = new List<string>();
            sourceStreams = new List<WaveInEvent>();
            waveWriters = new List<WaveFileWriter>();
            timers = new List<System.Windows.Forms.Timer>();
            foreach (var source in sources)
            {
                sourceList.Add(source.ProductName);
                sourceStreams.Insert(sourceList.IndexOf(source.ProductName), new WaveInEvent());
                waveWriters.Insert(sourceList.IndexOf(source.ProductName), null);
                timers.Insert(sourceList.IndexOf(source.ProductName), new System.Windows.Forms.Timer());
                Console.WriteLine($"Added {source.ProductName}.");
            }
            CommandFacad.Devices = sourceList;
        }

        private static object audioLock = new object();

        private static void RecordingLoop(string device)
        {
            while (true)
            {
                lock(audioLock)
                {
                    StartRecording(device);
                }
                Thread.Sleep(1000);
            }
        }

        private static void StartRecording(string device/*, ref WaveFileWriter waveWriter*/)
        {
            Console.WriteLine($"Start {device}.");

            //var sourceStream = new WaveInEvent(); //Can not use common WaveIn here
            //System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

            while (true)
            {
                if (sourceList != null)
                {
                    break;
                }
            }

            int deviceNumber = sourceList.IndexOf(device);

            try
            {
                sourceStreams[deviceNumber].DeviceNumber = deviceNumber;
                sourceStreams[deviceNumber].WaveFormat = new WaveFormat(44100, WaveIn.GetCapabilities(deviceNumber).Channels);
                sourceStreams[deviceNumber].DataAvailable += new EventHandler<WaveInEventArgs>((sender, e) => sourceStream_Data(sender, e, deviceNumber));
            }
            catch
            {
                sourceList.Remove(device);
                CommandFacad.Devices.Remove(device);
                return;
            }
            timers[deviceNumber].Interval = 1000; //set the interval to 1 second.

            Console.WriteLine("Start writing into " + device + ".wav");

            //using (Stream stream = File.Open(output_folder + device + ".wav", FileMode.Create))
            //{
            //    waveWriters[deviceNumber] = new WaveFileWriter(stream, sourceStreams[deviceNumber].WaveFormat);
            //}
            //try
            //{
            //    sourceStreams[deviceNumber].StartRecording();
            //}
            //catch
            //{ }
            try
            {
                using (Stream stream = File.Open(output_folder + device + ".wav", FileMode.Create))
                {
                    waveWriters[deviceNumber] = new WaveFileWriter(stream, sourceStreams[deviceNumber].WaveFormat);
                }
                sourceStreams[deviceNumber].StartRecording();
            }
            catch
            {
                Thread.Sleep(1000);
            }

            timers[deviceNumber].Enabled = true;

            timers[deviceNumber].Tick += new EventHandler((sender, e) =>
            {
                Console.WriteLine("Stop writing into " + device + ".wav");
                StopTimer(sender, e, deviceNumber);
            });
        }
        private static void sourceStream_Data(object sender, WaveInEventArgs e, int deviceNumber)
        {
            if (waveWriters[deviceNumber] == null)
                return;
            try
            {
                //offset is 0 because written the entire array of data
                waveWriters[deviceNumber].WriteData(e.Buffer, 0, e.BytesRecorded);
                waveWriters[deviceNumber].Flush();
            }
            catch
            {
                return;
            }
        }

        private static void StopTimer(object sender, EventArgs e, int deviceNumber)
        {
            sourceStreams[deviceNumber].StopRecording();
            sourceStreams[deviceNumber].Dispose();
            sourceStreams[deviceNumber] = null;
            waveWriters[deviceNumber].Close();
            waveWriters[deviceNumber] = null;

            timers[deviceNumber].Enabled = false;
        }
    }
}
