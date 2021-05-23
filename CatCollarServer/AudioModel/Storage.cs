using CatCollarServer.Algorytm;
using CatCollarServer.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CatCollarServer.AudioModel
{
    public class Storage
    {
        private const string storage_examples_file = "..\\Resources\\models.dat";
        private const string storage_header = "DATAS";

        public Dictionary<uint, Model> Models { get; private set; }
        private uint maxId;

        public Storage()
        {
            maxId = 0;
            Models = null;
        }

        public bool Init()
        {
            if (Models != null)
            {
                return true;
            }
            Models = new Dictionary<uint, Model>();

            Console.WriteLine("Loading models from the storage");
            if (File.Exists(storage_examples_file))
            {
                BinaryReader reader = new BinaryReader(File.Open(storage_examples_file, FileMode.Open));
                if (reader == null)
                {
                    Console.WriteLine("Can't access the model's storage");
                }
                char[] header = new char[4];
                reader.Read(header, 0, sizeof(char) * 4);
                if (header.ToString() != storage_header)
                {
                    Console.WriteLine("Invalid storage");
                    return false;
                }

                byte[] maxIdBuffer = new byte[sizeof(uint)];
                reader.Read(maxIdBuffer, 0, sizeof(uint));
                maxId = Serialization.Deserialize<uint>(maxIdBuffer);

                string tmpName = "";
                for (uint i = 0; i < maxId; i++)
                {
                    Model model = new Model(tmpName);
                    model.Read(ref reader);

                    Models.Add(model.Id, model);
                }
                reader.Close();
            }
            else
            {
                BinaryWriter writer = new BinaryWriter(File.Open(storage_examples_file, FileMode.Create));
                Console.WriteLine("Storage not found, creating an empty one");

                writer.Write(Serialization.Serialize(storage_header), 0, sizeof(char) * 4);
                writer.Write(Serialization.Serialize(maxId), 0, sizeof(uint));
                writer.Close();
            }

            return true;
        }

        public uint AddModel(Model model)
        {
            model.Id = ++maxId;
            Models.Add(maxId, model);

            return maxId;
        }

        public void AddSample(uint modelId, Word word)
        {
            Models[modelId].Samples.Add(new MFCCSample() { data = word.MFCC, size = word.MFCCSize });
        }

        //Save models into the file
        public bool Persist()
        {
            BinaryWriter writer = new BinaryWriter(File.Open(storage_examples_file, FileMode.Create));
            if (writer == null)
            {
                Console.WriteLine("Can't access the model's storage");
            }

            writer.Write(storage_header.ToCharArray(), 0, sizeof(char) * 4);
            writer.Write(Serialization.Serialize(maxId), 0, sizeof(uint));

            foreach(var model in Models)
            {
                Model tmpModel = model.Value;
                tmpModel.Write(ref writer);
            }
            writer.Close();
            Console.WriteLine("Done!");
            return true;
        }
    }
}
