using CatCollarServer.Audio;
using CatCollarServer.AudioModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CatCollarServer.Command
{
    public class Context
    {
        public WavData WavData { get; set; }
        public Processor Processor { get; set; }
        public Storage Storage { get; set; }

        public Context()
        {
            WavData = null;
            Processor = null;
            Storage = new Storage();
        }
    }
}
