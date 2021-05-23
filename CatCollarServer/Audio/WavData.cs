using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace CatCollarServer.Audio
{
    public static class Serialization
    {
        public static byte[] Serialize(object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        public static T Deserialize<T>(byte[] param)
        {
            using (MemoryStream ms = new MemoryStream(param))
            {
                IFormatter br = new BinaryFormatter();
                return (T)br.Deserialize(ms);
            }
        }
    }
    public class WavHeader
    {
        public string RIFF = new string(new char[4]); // RIFF Header
        public uint ChunkSize; // RIFF Chunk Size
        public string Wave = new string(new char[4]); // WAVE Header

        public string FMT = new string(new char[4]); // FMT header
        public uint Subchunk1Size; // Size of the fmt chunk
        public ushort AudioFormat; // Audio format 1=PCM (Other formats are unsupported)
        public ushort NumberOfChan; // Number of channels 1=Mono, 2=Stereo
        public uint SamplesPerSec; // Sampling Frequency in Hz
        public uint BytesPerSec; // bytes per second
        public ushort BlockAlign; // 2=16-bit mono, 4=16-bit stereo
        public ushort BitsPerSample; // Number of bits per sample

        // The data below depends on audioFormat, but we work only with PCM cases
        public string Data = new string(new char[4]); // DATA header
        public uint Subchunk2Size; // Sampled data length
    }

    public class WavData : ICloneable
    {
        public WavHeader Header { get; private set; }
        public short[] RawData { get; private set; }
        public double[] NormalizedData { get; private set; }

        public short MaxValue { get; private set; }
        public short MinValue { get; private set; }
        public uint NumberOfSamples { get; private set; }
        public WavData(WavHeader header)
        {
            Header = header;
            RawData = null;
            NormalizedData = null;

            MaxValue = 0;
            MinValue = 0;
            NumberOfSamples = 0;
        }

        public const int WavHeaderSizeOf = sizeof(char) * 4 * 4 + sizeof(uint) * 5 + sizeof(ushort) * 4;

        public static WavData ReadFromFile(in string filePathName)
        {
            WavHeader wavHeader = new WavHeader();

            string path = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, filePathName);

            try
            {
                //Read file
                BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));

                //Deserialize header
                wavHeader = Serialization.Deserialize<WavHeader>(reader.ReadBytes(WavHeaderSizeOf));

                WavData wavData = new WavData(wavHeader);

                ReadData(reader, wavHeader, ref wavData);
                reader.Close();

                return wavData;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(e.Message);
            }
        }

        public bool CheckHeader(in WavHeader header)
        {
            if(String.Compare(header.RIFF, 0, "RIFF", 0, sizeof(char)*4) != 0
                || String.Compare(header.Wave, 0, "WAVE", 0, sizeof(char) * 4) != 0)
            {
                Console.WriteLine("Invalid RIFF/WAVE format");
                return false;
            }

            if(header.AudioFormat != 1)
            {
                Console.WriteLine("nvalid WAV format: only PCM audio format is supported");
                return false;
            }

            if (header.NumberOfChan > 2)
            {
                Console.WriteLine("Invalid WAV format: only 1 or 2 channels audio is supported");
                return false;
            }

            ulong bitsPerChannel = (ulong) (header.BitsPerSample / header.NumberOfChan);
            if (bitsPerChannel != 16)
            {
                Console.WriteLine("Invalid WAV format: only 16 - bit per channel is supported");
                return false;
            }

            if(header.Subchunk2Size > 0)
            {
                Console.WriteLine("File too big");
                return false;
            }

            return true;
        }
    
        public static void ReadData(BinaryReader reader, WavHeader header, ref WavData wavFile)
        {
            short value, minValue = 0, maxValue = 0;
            short value16, valueLeft16, valueRight16;

            int bytesPerSample = (int)(header.BitsPerSample / 8);
            ulong numberOfSamplesXChannels = (ulong)(header.Subchunk2Size / (header.NumberOfChan * bytesPerSample));

            wavFile.RawData = new short[numberOfSamplesXChannels];

            uint sampleNumber = 0;
            for (; sampleNumber < numberOfSamplesXChannels && !reader.BaseStream.CanRead; sampleNumber++)
            {
                if (header.NumberOfChan == 1)
                {
                    value16 = Serialization.Deserialize<short>(reader.ReadBytes(sizeof(short)));
                    value = value16;
                }
                else
                {
                    valueLeft16 = Serialization.Deserialize<short>(reader.ReadBytes(sizeof(short)));
                    valueRight16 = Serialization.Deserialize<short>(reader.ReadBytes(sizeof(short)));
                    value = (short)((Math.Abs(valueLeft16) + Math.Abs(valueRight16)) / 2);
                }

                if (maxValue < value)
                {
                    maxValue = value;
                }

                if (minValue > value)
                {
                    minValue = value;
                }

                wavFile.RawData[sampleNumber] = value;
            }
            sampleNumber++;

            // Normalization
            wavFile.NormalizedData = new double[sampleNumber];
            double maxAbs = Math.Max(MathF.Abs(minValue), MathF.Abs(maxValue));

            for (uint i = 0; i < sampleNumber; i++)
            {
                wavFile.NormalizedData[i] = wavFile.RawData[i] / maxAbs;
            }

            // Update values
            wavFile.MinValue = minValue;
            wavFile.MaxValue = maxValue;
            wavFile.NumberOfSamples = sampleNumber;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
