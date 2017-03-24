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
    public class PreparedFile : BeeperFile
    {
        public new List<PreparedSection> Sections { get; set; }
    }

    public class PreparedSection : BeeperSection
    {
        public int Index { get; set; }
        public new List<PreparedTrack> Tracks { get; set; }
    }

    public class PreparedTrack
    {
        public IWavePlayer Player { get; set; }
        public ISampleProvider Provider { get; set; }
        public int Index { get; set; }
    }
}
