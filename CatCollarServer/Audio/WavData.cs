using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
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
                ms.Position = 0;
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        public static T Deserialize<T>(byte[] param)
        {
            using (MemoryStream ms = new MemoryStream(param))
            {
                IFormatter br = new BinaryFormatter();
                ms.Position = 0;
                return (T)br.Deserialize(ms);
            }
        }

        public static WavHeader DeserializeHeader(byte[] vs)
        {
            WavHeader header = new WavHeader();
            int startPoint = 0;

            byte[] bufferInt = new byte[sizeof(uint)];
            byte[] bufferShr = new byte[sizeof(ushort)];

            Array.Copy(vs, startPoint, header.RIFF, 0, 4);
            startPoint += 4;


            Array.Copy(vs, startPoint, bufferInt, 0, sizeof(uint));
            header.ChunkSize = BitConverter.ToUInt32(bufferInt);//Deserialize<int>(bufferInt);
            startPoint += sizeof(uint);

            Array.Copy(vs, startPoint, header.Wave, 0, 4);
            startPoint += 4;

            Array.Copy(vs, startPoint, header.FMT, 0, 4);
            startPoint += 4;

            Array.Copy(vs, startPoint, bufferInt, 0, sizeof(uint));
            header.Subchunk1Size = BitConverter.ToUInt32(bufferInt);//Deserialize<int>(bufferInt);
            startPoint += sizeof(uint);

            Array.Copy(vs, startPoint, bufferShr, 0, sizeof(ushort));
            header.AudioFormat = BitConverter.ToUInt16(bufferShr);//Deserialize<ushort>(bufferShr);
            startPoint += sizeof(ushort);

            Array.Copy(vs, startPoint, bufferShr, 0, sizeof(ushort));
            header.NumberOfChan = BitConverter.ToUInt16(bufferShr);//Deserialize<ushort>(bufferShr);
            startPoint += sizeof(ushort);

            Array.Copy(vs, startPoint, bufferInt, 0, sizeof(uint));
            header.SamplesPerSec = BitConverter.ToUInt32(bufferInt);//Deserialize<int>(bufferInt);
            startPoint += sizeof(uint);

            Array.Copy(vs, startPoint, bufferInt, 0, sizeof(uint));
            header.BytesPerSec = BitConverter.ToUInt32(bufferInt);//Deserialize<int>(bufferInt);
            startPoint += sizeof(uint);

            Array.Copy(vs, startPoint, bufferShr, 0, sizeof(ushort));
            header.BlockAlign = BitConverter.ToUInt16(bufferShr);//Deserialize<ushort>(bufferShr);
            startPoint += sizeof(ushort);

            Array.Copy(vs, startPoint, bufferShr, 0, sizeof(ushort));
            header.BitsPerSample = BitConverter.ToUInt16(bufferShr);//Deserialize<ushort>(bufferShr);
            startPoint += sizeof(ushort);

            Array.Copy(vs, startPoint, header.Data, 0, 4);
            startPoint += 4;

            Array.Copy(vs, startPoint, bufferInt, 0, sizeof(uint));
            header.Subchunk2Size = BitConverter.ToUInt32(bufferInt);//Deserialize<int>(bufferInt);

            return header;
        }
    }

    [Serializable()]
    public class WavHeader : ISerializable
    {
        public byte[] RIFF; // RIFF Header
        public uint ChunkSize; // RIFF Chunk Size
        public byte[] Wave; // WAVE Header

        public byte[] FMT; // FMT header
        public uint Subchunk1Size; // Size of the fmt chunk
        public ushort AudioFormat; // Audio format 1=PCM (Other formats are unsupported)
        public ushort NumberOfChan; // Number of channels 1=Mono, 2=Stereo
        public uint SamplesPerSec; // Sampling Frequency in Hz
        public uint BytesPerSec; // bytes per second
        public ushort BlockAlign; // 2=16-bit mono, 4=16-bit stereo
        public ushort BitsPerSample; // Number of bits per sample

        // The data below depends on audioFormat, but we work only with PCM cases
        public byte[] Data; // DATA header
        public uint Subchunk2Size; // Sampled data length

        public WavHeader()
        {
            RIFF = new byte[4];
            ChunkSize = new uint();
            Wave = new byte[4];
            FMT = new byte[4];
            Subchunk1Size = new uint();
            AudioFormat = new ushort();
            NumberOfChan = new ushort();
            SamplesPerSec = new uint();
            BytesPerSec = new uint();
            BlockAlign = new ushort();
            BitsPerSample = new ushort();
            Data = new byte[4];
            Subchunk2Size = new uint();
        }

        //Deserialization constructor.
        public WavHeader(SerializationInfo info, StreamingContext ctxt) : this()
        {
            //Get the values from info and assign them to the appropriate properties
            RIFF = (byte[])info.GetValue("RIFF", RIFF.GetType());
            ChunkSize = (uint)info.GetValue("ChunkSize", ChunkSize.GetType());
            Wave = (byte[])info.GetValue("Wave", Wave.GetType());
            FMT = (byte[])info.GetValue("FMT", FMT.GetType());
            Subchunk1Size = (uint)info.GetValue("Subchunk1Size", Subchunk1Size.GetType());
            AudioFormat = (ushort)info.GetValue("AudioFormat", AudioFormat.GetType());
            NumberOfChan = (ushort)info.GetValue("NumberOfChan", NumberOfChan.GetType());
            SamplesPerSec = (uint)info.GetValue("SamplesPerSec", SamplesPerSec.GetType());
            BytesPerSec = (uint)info.GetValue("BytesPerSec", BytesPerSec.GetType());
            BlockAlign = (ushort)info.GetValue("BlockAlign", BlockAlign.GetType());
            BitsPerSample = (ushort)info.GetValue("BitsPerSample", BlockAlign.GetType());
            Data = (byte[])info.GetValue("Data", Data.GetType());
            Subchunk2Size = (uint)info.GetValue("Subchunk2Size", Subchunk2Size.GetType());
        }

        //Serialization function.
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            //Add the values to info and name them
            info.AddValue("RIFF", RIFF);
            info.AddValue("ChunkSize", ChunkSize);
            info.AddValue("Wave", Wave);
            info.AddValue("FMT", FMT);
            info.AddValue("Subchunk1Size", Subchunk1Size);
            info.AddValue("AudioFormat", AudioFormat);
            info.AddValue("NumberOfChan", NumberOfChan);
            info.AddValue("SamplesPerSec", SamplesPerSec);
            info.AddValue("BytesPerSec", BytesPerSec);
            info.AddValue("BlockAlign", BlockAlign);
            info.AddValue("BitsPerSample", BitsPerSample);
            info.AddValue("Data", Data);
            info.AddValue("Subchunk2Size", Subchunk2Size);
        }
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

        public const int WavHeaderSizeOf = 44;

        public static WavData ReadFromFile(in string filePathName)
        {
            WavHeader wavHeader = new WavHeader();

            string path = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, filePathName);

            try
            {
                //Read file
                BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                //Deserialize header
                wavHeader = Serialization.DeserializeHeader(reader.ReadBytes(WavHeaderSizeOf));

                WavData wavData = new WavData(wavHeader);

                ReadData(ref reader, wavHeader, ref wavData);
                reader.BaseStream.Close();
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
            string riff = System.Text.ASCIIEncoding.ASCII.GetString(header.RIFF),
                wave = System.Text.ASCIIEncoding.ASCII.GetString(header.Wave);
            if (String.Compare(riff, "RIFF") != 0
                || String.Compare(wave, "WAVE") != 0)
            {
                Console.WriteLine("Invalid RIFF/WAVE format");
                return false;
            }

            if (header.AudioFormat != 1)
            {
                Console.WriteLine("nvalid WAV format: only PCM audio format is supported");
                return false;
            }

            if (header.NumberOfChan > 2)
            {
                Console.WriteLine("Invalid WAV format: only 1 or 2 channels audio is supported");
                return false;
            }

            ulong bitsPerChannel = (ulong)(header.BitsPerSample / header.NumberOfChan);
            if (bitsPerChannel != 16)
            {
                Console.WriteLine("Invalid WAV format: only 16 - bit per channel is supported");
                return false;
            }

            if (header.Subchunk2Size > 0)
            {
                Console.WriteLine("File too big");
                return false;
            }

            return true;
        }

        public static void ReadData(ref BinaryReader reader, WavHeader header, ref WavData wavFile)
        {
            short value, minValue = 0, maxValue = 0;
            short value16, valueLeft16, valueRight16;

            var bytesPerSample = header.BitsPerSample / 8;
            ulong numberOfSamplesXChannels = (ulong)(header.Subchunk2Size / (header.NumberOfChan * bytesPerSample));

            wavFile.RawData = new short[numberOfSamplesXChannels];

            uint sampleNumber = 0;
            for (; sampleNumber < numberOfSamplesXChannels && reader.BaseStream.CanRead; sampleNumber++)
            {
                try
                {
                    if (header.NumberOfChan == 1)
                    {
                        value16 = BitConverter.ToInt16(reader.ReadBytes(sizeof(short)));
                        value = value16;
                    }
                    else
                    {
                        byte[] left = reader.ReadBytes(sizeof(short));
                        if (left.Length == 0)
                        {
                            break;
                        }
                        valueLeft16 = BitConverter.ToInt16(left);
                        
                        byte[] right = reader.ReadBytes(sizeof(short));
                        if (right.Length == 0)
                        {
                            break;
                        }
                        valueRight16 = BitConverter.ToInt16(right);

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
                catch { }
            }
            //sampleNumber++;

            // Normalization
            wavFile.NormalizedData = new double[sampleNumber];
            double maxAbs = Math.Max(MathF.Abs(minValue), MathF.Abs(maxValue));

            for (int i = 0; i < sampleNumber; i++)
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

    //class WavFile
    //{
    //    public int samplesPerSecond { get; set; }
    //    public int samplesTotalCount { get; set; }
    //    public string wavFilename { get; private set; }
    //    public byte[] metaData { get; set; }


    //    public WavFile(string filename)
    //    {
    //        samplesTotalCount = samplesPerSecond = -1;
    //        wavFilename = filename;
    //        metaData = null;
    //    }


    //    /// <summary>
    //    /// Reading all samples of a 16-bit stereo wav file into arrays.
    //    /// </summary>

    //    public bool ReadData(ref double[] L, ref double[] R, string path)
    //    {
    //        try
    //        {
    //            BinaryReader reader = new BinaryReader(File.Open(path + wavFilename, FileMode.Open));

    //            // header (8 + 4 bytes):

    //            byte[] riffId = reader.ReadBytes(4);    // "RIFF"
    //            int fileSize = reader.ReadInt32();      // size of entire file
    //            byte[] typeId = reader.ReadBytes(4);    // "WAVE"

    //            if (Encoding.ASCII.GetString(typeId) != "WAVE") return false;

    //            // chunk 1 (8 + 16 or 18 bytes):

    //            byte[] fmtId = reader.ReadBytes(4);     // "fmt "
    //            int fmtSize = reader.ReadInt32();       // size of chunk in bytes
    //            int fmtCode = reader.ReadInt16();       // 1 - for PCM
    //            int channels = reader.ReadInt16();      // 1 - mono, 2 - stereo
    //            int sampleRate = reader.ReadInt32();    // sample rate per second
    //            int byteRate = reader.ReadInt32();      // bytes per second
    //            int dataAlign = reader.ReadInt16();     // data align
    //            int bitDepth = reader.ReadInt16();      // 8, 16, 24, 32, 64 bits

    //            if (fmtCode != 1) return false;     // not PCM
    //            if (channels != 2) return false;    // only Stereo files in this version
    //            if (bitDepth != 16) return false;   // only 16-bit in this version

    //            if (fmtSize == 18) // fmt chunk can be 16 or 18 bytes
    //            {
    //                int fmtExtraSize = reader.ReadInt16();  // read extra bytes size
    //                reader.ReadBytes(fmtExtraSize);         // skip over "INFO" chunk
    //            }

    //            // chunk 2 (8 bytes):

    //            byte[] dataId = reader.ReadBytes(4);    // "data"
    //            int dataSize = reader.ReadInt32();      // size of audio data

    //            Debug.Assert(Encoding.ASCII.GetString(dataId) == "data", "Data chunk not found!");

    //            samplesPerSecond = sampleRate;                  // sample rate (usually 44100)
    //            samplesTotalCount = dataSize / (bitDepth / 8);  // total samples count in audio data

    //            // audio data:

    //            L = R = new double[samplesTotalCount / 2];

    //            for (int i = 0, s = 0; i < samplesTotalCount; i += 2)
    //            {
    //                L[s] = Convert.ToDouble(reader.ReadInt16());
    //                R[s] = Convert.ToDouble(reader.ReadInt16());
    //                s++;
    //            }

    //            // metadata:

    //            long moreBytes = reader.BaseStream.Length - reader.BaseStream.Position;

    //            if (moreBytes > 0)
    //            {
    //                metaData = reader.ReadBytes((int)moreBytes);
    //            }

    //            reader.Close();
    //        }
    //        catch
    //        {
    //            Debug.Fail("Failed to read file.");
    //            return false;
    //        }

    //        return true;
    //    }


    //    /// <summary>
    //    /// Writing all 16-bit stereo samples from arrays into wav file.
    //    /// </summary>

    //    public bool WriteData(double[] L, double[] R, string path)
    //    {
    //        Debug.Assert((samplesTotalCount != -1) && (samplesPerSecond != -1),
    //            "No sample count or sample rate info!");

    //        try
    //        {
    //            BinaryWriter writer = new BinaryWriter(File.Create(path + іwavFilename));

    //            int fileSize = 44 + samplesTotalCount * 2;

    //            if (metaData != null)
    //            {
    //                fileSize += metaData.Length;
    //            }

    //            // header:

    //            writer.Write(Encoding.ASCII.GetBytes("RIFF"));  // "RIFF"
    //            writer.Write((Int32)fileSize);                  // size of entire file with 16-bit data
    //            writer.Write(Encoding.ASCII.GetBytes("WAVE"));  // "WAVE"

    //            // chunk 1:

    //            writer.Write(Encoding.ASCII.GetBytes("fmt "));  // "fmt "
    //            writer.Write((Int32)16);                        // size of chunk in bytes
    //            writer.Write((Int16)1);                         // 1 - for PCM
    //            writer.Write((Int16)2);                         // only Stereo files in this version
    //            writer.Write((Int32)samplesPerSecond);          // sample rate per second (usually 44100)
    //            writer.Write((Int32)(4 * samplesPerSecond));    // bytes per second (usually 176400)
    //            writer.Write((Int16)4);                         // data align 4 bytes (2 bytes sample stereo)
    //            writer.Write((Int16)16);                        // only 16-bit in this version

    //            // chunk 2:

    //            writer.Write(Encoding.ASCII.GetBytes("data"));  // "data"
    //            writer.Write((Int32)(samplesTotalCount * 2));   // size of audio data 16-bit

    //            // audio data:

    //            for (int i = 0, s = 0; i < samplesTotalCount; i += 2)
    //            {
    //                writer.Write(Convert.ToInt16(L[s]));
    //                writer.Write(Convert.ToInt16(R[s]));
    //                s++;
    //            }

    //            // metadata:

    //            if (metaData != null)
    //            {
    //                writer.Write(metaData);
    //            }

    //            writer.Flush();
    //            writer.Close();
    //        }
    //        catch
    //        {
    //            Debug.Fail("Failed to write file.");
    //            return false;
    //        }

    //        return true;
    //    }
    //}
}
