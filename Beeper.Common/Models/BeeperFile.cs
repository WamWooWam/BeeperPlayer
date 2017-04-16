using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;
using NAudio.Wave.SampleProviders;

namespace Beeper.Common.Models
{
    public class BeeperFile
    {
        public static BeeperFile Create()
        {
            var Metadata = new BeeperMeta()
            {
                Title = "Example Title",
                Artist = "Example Artist",
                FileCreator = Environment.UserName,
                Version = 2
            };
            var Section = new BeeperSection
            {
                Title = "Example Section",
                Tracks = new List<BeeperTrack>()
            };
            var Track = new BeeperTrack()
            {
                Title = "ExampleTrack",
                SignalType = SignalGeneratorType.Square,
                Beeps = new List<BeeperBeep>(),
                Volume = 0.25F
            };
            var Beep = new BeeperBeep()
            {
                Duration = 1000,
                Frequency = 1000,
                PauseAfter = 100,
            };
            Track.Beeps.Add(Beep);
            Section.Tracks.Add(Track);
            var Sections = new List<BeeperSection>();
            Sections.Add(Section);
            return new BeeperFile
            {
                Metadata = Metadata,
                Sections = Sections
            };
        }

        public static BeeperFile Upgrade(BeeperFile Original)
        {
            var NewFile = JsonConvert.DeserializeObject<BeeperFile>(JsonConvert.SerializeObject(Original));
            NewFile.Metadata.Version = 3;
            return NewFile;
        }

        public BeeperMeta Metadata { get; set; }
        public List<BeeperSection> Sections { get; set; }
        public List<BeeperInstrument> Instruments { get; set; }

        [JsonIgnore]
        public int TotalBeeps
        {
            get
            {
                var i = 0;
                foreach (BeeperSection section in Sections)
                {
                    i += section.TotalBeeps;
                }
                return i;
            }
        }

        [JsonIgnore]
        public TimeSpan Duration
        {
            get
            {
                var i = 0;
                foreach (BeeperSection section in Sections)
                {
                    var t = 0;
                    foreach (BeeperTrack track in section.Tracks)
                    {
                        foreach (BeeperBeep beep in track.Beeps)
                        {
                            t += (beep.TotalDuration * section.Loops);
                        }
                    }
                    t /= section.Tracks.Count;
                    i += t;
                }
                return TimeSpan.FromMilliseconds(i);
            }
        }
    }

    public class BeeperMeta
    {
        public int? Version { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string FileCreator { get; set; }
    }

    public class BeeperSection
    {
        public string Title { get; set; }
        public int Loops { get; set; }
        public List<BeeperTrack> Tracks { get; set; }
        [JsonIgnore]
        public int TotalBeeps
        {
            get
            {
                var i = 0;
                foreach (var track in Tracks)
                {
                    i += track.Beeps.Count * Loops;
                }
                return i;
            }
        }
    }

    public class BeeperInstrument
    {
        public int Attack { get; set; }
        public int Decay { get; set; }
        public float SustainLevel { get; set; } = 1.0F;
        public int Release { get; set; }
    }

    public class BeeperTrack
    {
        public string Title { get; set; }
        public float Volume { get; set; }
        public float Pan { get; set; }
        public int? Instrument { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public SignalGeneratorType SignalType { get; set; }
        public List<BeeperBeep> Beeps { get; set; }
    }

    public class BeeperBeep
    {
        public int? PauseBefore { get; set; }
        public int Frequency { get; set; }
        public int Duration { get; set; }
        public int? PauseAfter { get; set; }
        public string Sample { get; set; }
        [JsonIgnore]
        public int TotalDuration { get { return Duration + PauseAfter.GetValueOrDefault() + PauseBefore.GetValueOrDefault(); } }
    }

    public enum BeeperFileType { Json, Zip }
}
