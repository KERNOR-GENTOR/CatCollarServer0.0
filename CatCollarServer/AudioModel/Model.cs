using CatCollarServer.Algorytm;
using CatCollarServer.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CatCollarServer.AudioModel
{
    public class Model
    {
        public uint Id { get; set; }
        public string Text { get; private set; }
        public List<MFCCSample> Samples { get; set; }

        public Model(string t)
        {
            Text = t;
            Samples = new List<MFCCSample>();
        }

        public void Write(ref BinaryWriter writer)
        {
            writer.Write(Serialization.Serialize(Id), 0, sizeof(uint));

            uint textSize = (uint)Text.Length;
            writer.Write(Serialization.Serialize(textSize), 0, sizeof(uint));
            writer.Write(Text.ToArray(), 0, (int)(sizeof(char) * textSize));

            uint sampleCount = (uint)Samples.Count;
            writer.Write(Serialization.Serialize(sampleCount), 0, sizeof(uint));

            foreach (MFCCSample sample in Samples)
            {
                writer.Write(Serialization.Serialize(sample.size), 0, sizeof(uint));
                writer.Write(Serialization.Serialize(sample.data), 0, (int)(sizeof(double) * sample.size));
            }
        }

        public void Read(ref BinaryReader reader)
        {
            reader.Read(Serialization.Serialize(Id), 0, sizeof(uint));

            byte[] textSizeBuffer = new byte[sizeof(uint)];
            reader.Read(textSizeBuffer, 0, sizeof(uint));
            uint textSize = Serialization.Deserialize<uint>(textSizeBuffer);

            char[] textChars = new char[textSize + 1];
            reader.Read(textChars, 0, (int)(sizeof(char) * textSize));

            Text = new string(textChars);

            byte[] samplesCountBuffer = new byte[sizeof(uint)];
            reader.Read(samplesCountBuffer, 0, sizeof(uint));
            uint samplesCount = Serialization.Deserialize<uint>(samplesCountBuffer);

            MFCCSample sample;
            for(uint i = 0; i < samplesCount; i++)
            {
                byte[] sampleSizeBuffer = new byte[sizeof(uint)];
                reader.Read(sampleSizeBuffer, 0, sizeof(uint));
                sample.size = Serialization.Deserialize<uint>(sampleSizeBuffer);

                byte[] sampleDataBuffer = new byte[sample.size * sizeof(double)];
                reader.Read(sampleDataBuffer, 0, (int)(sample.size * sizeof(double)));
                sample.data = Serialization.Deserialize<double[]>(sampleDataBuffer);

                Samples.Add(sample);
            }
        }
    }
}
