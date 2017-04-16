using Beeper.Common.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beeper.Common
{
    public class BeeperSampleProvider : ISampleProvider
    {
        public WaveFormat WaveFormat => Source.WaveFormat;

        public ISampleProvider Source { get; private set; }
        public BeeperBeep Beep { get; private set; }

        public BeeperSampleProvider(BeeperBeep beep, BeeperTrack track, BeeperFile file, string OutputDirectory)
        {
            Beep = beep;
            if (!string.IsNullOrEmpty(beep.Sample))
            {
                if (File.Exists(Path.Combine(OutputDirectory, "samples", beep.Sample + ".wav")))
                {
                    // Loads sample from disk
                    WaveFileReader reader = new WaveFileReader(Path.Combine(OutputDirectory, "samples", beep.Sample + ".wav"));
                    Source = reader.ToSampleProvider();

                    // Manages pause after beep
                    var pause = new OffsetSampleProvider(Source);
                    pause.LeadOut = TimeSpan.FromMilliseconds(beep.PauseAfter.GetValueOrDefault());
                    Source = pause;

                    // Manages panning
                    var panProvider = new PanningSampleProvider(Source.ToMono());
                    panProvider.Pan = track.Pan;

                    Source = panProvider;
                }
                else
                {
                    throw new FileNotFoundException();
                }
            }
            else
            {
                BeeperInstrument instrument = new BeeperInstrument();
                if (track.Instrument != null && track.Instrument != 0)
                    instrument = file.Instruments[track.Instrument.GetValueOrDefault() - 1];
                // Prepare the signal generators and wave players needed for this beep
                SignalGenerator signalGenerator = new SignalGenerator(); // Initialise the signal generator
                signalGenerator.Type = track.SignalType; // Set signal type
                signalGenerator.Frequency = beep.Frequency;
                Source = signalGenerator;
                if (track.SignalType == SignalGeneratorType.White)
                {
                    var noiseMixer = new MixingSampleProvider(signalGenerator.WaveFormat);
                    for (var j = 1; j <= beep.Frequency; j++) // and it's loops
                    {
                        var noiseGenerator = new SignalGenerator(); // Initialise the signal generator
                        noiseGenerator.Gain = track.Volume / beep.Frequency; // Set gain
                        noiseGenerator.Type = track.SignalType; // Set signal type
                        noiseMixer.AddMixerInput(noiseGenerator);
                        Source = noiseMixer;
                    }
                }

                // ADSR Envelopes.
                var adsr = new Test.SampleProviders.AdsrSampleProvider(Source.ToMono());
                adsr.adsr.AttackRate = (instrument.Attack / 1000F) * 44100F; // Attack is in milliseconds. This 1000 has to be a float
                adsr.adsr.DecayRate = (instrument.Decay / 1000F) * 44100F;
                adsr.adsr.SustainLevel = instrument.SustainLevel;
                adsr.adsr.ReleaseRate = (instrument.Release / 1000F) * 44100F;
                Source = adsr;

                // Manages duration
                var duration = new OffsetSampleProvider(Source);
                duration.Take = TimeSpan.FromMilliseconds(beep.Duration);
                Source = duration;

                // Manages pause after beep
                var pause = new OffsetSampleProvider(Source);
                pause.LeadOut = TimeSpan.FromMilliseconds(beep.PauseAfter.GetValueOrDefault());
                Source = pause;

                // Manages panning
                var panProvider = new PanningSampleProvider(Source.ToMono());
                panProvider.Pan = track.Pan;
                Source = panProvider;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return Source.Read(buffer, offset, count);
        }
    }
}
