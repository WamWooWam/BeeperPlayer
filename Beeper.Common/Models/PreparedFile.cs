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
        public new List<PreparedTrack> Tracks { get; set; }
        [JsonIgnore]
        public new int TotalBeeps
        {
            get
            {
                var i = 0;
                foreach (var track in Tracks)
                {
                    i += track.Beeps.Count;
                }
                return i;
            }
        }
    }

    public class PreparedTrack : BeeperTrack
    {
        public new List<PreparedBeep> Beeps { get; set; }
    }

    public class PreparedBeep : BeeperBeep
    {
        public ISampleProvider SampleProvider { get; set; }
        public IWavePlayer Player { get; set; }
    }
}
