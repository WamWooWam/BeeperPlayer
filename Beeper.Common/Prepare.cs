using Beeper.Common.Models;
using Beeper.Test.SampleProviders;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamWooWam.Core;

namespace Beeper.Common
{
    public static class Prepare
    {
        /// <summary>
        /// Prepares a beeper file to be played.
        /// </summary>
        /// <param name="Original">The beeper file that needs converting</param>
        /// <returns>The converted, ready to play file.</returns>
        public static PreparedFile PrepareBeeperFile(BeeperFile Original, Output Method, string OutputDirectory)
        {
            // Initially create new file and copy metadata
            var PreparedFile = new PreparedFile()
            {
                Samples = new List<ISampleProvider>(),
                Sections = new List<List<ISampleProvider>>()
            };
            List<ISampleProvider> mixers = new List<ISampleProvider>();
            foreach (BeeperSection section in Original.Sections) // Run through each section
            {
                for (var i = 1; i <= section.Loops; i++) // and it's loops
                {
                    // Create a new section and copy some values
                    List<ISampleProvider> samples = new List<ISampleProvider>();
                    List<ISampleProvider> mix = new List<ISampleProvider>();
                    foreach (BeeperTrack track in section.Tracks) // Run through each track
                    {
                        // Create a new BeeperSampleProvider
                        foreach (BeeperBeep beep in track.Beeps) // Run through each beep
                        {
                            samples.Add(new BeeperSampleProvider(beep, track, Original, OutputDirectory));
                        }

                        ConcatenatingSampleProvider concentrate = new ConcatenatingSampleProvider(samples);
                        VolumeSampleProvider volume = new VolumeSampleProvider(concentrate);
                        volume.Volume = track.Volume;
                        mix.Add(volume);

                        PreparedFile.Samples.AddRange(samples);
                        samples.Clear();
                        ConsolePlus.WriteDebug($@"      Prepared track ""{track.Title}""!");
                    }
                    // Mixes tracks
                    mixers.Add(new MixingSampleProvider(mix)); // Rebuilds sections
                    ConsolePlus.WriteDebug($@"   Prepared section ""{section.Title}"" loop {i}!");
                }
                // Mixes sections
                ConcatenatingSampleProvider finalMix = new ConcatenatingSampleProvider(mixers);
                PreparedFile.Provider = finalMix;

                if (Method != Output.File)
                {
                    IWavePlayer Player = new WaveOut();
                    switch (Method)
                    {
                        case Output.Asio:
                            Player = new AsioOut();
                            break;
                        case Output.DirectSound:
                            Player = new DirectSoundOut();
                            break;
                        case Output.Wasapi:
                            Player = new WasapiOut();
                            break;
                        case default(Output):
                            Player = new WaveOut();
                            break;
                    }
                    Player.Init(finalMix.ToWaveProvider());
                    PreparedFile.Player = Player;
                }
            }
            return PreparedFile; // Return the complete file.
        }
    }
}
