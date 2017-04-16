using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;

namespace Beeper.Common.Models
{
    public class PreparedFile
    {
        public IWavePlayer Player { get; set; }
        public ISampleProvider Provider { get; set; }
        public List<List<ISampleProvider>> Sections { get; set; }
        public List<ISampleProvider> Samples { get; set; }
    }
}
