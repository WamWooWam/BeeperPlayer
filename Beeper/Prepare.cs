using Beeper.Common.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamWooWam.Core;

namespace Beeper
{
    class Prepare
    {


        /// <summary>
        /// Prepares a beeper file to be played.
        /// </summary>
        /// <param name="Original">The beeper file that needs converting</param>
        /// <returns>The converted, ready to play file.</returns>
        public static PreparedFile PrepareBeeperFile(BeeperFile Original)
        {

            Stopwatch watcher = new Stopwatch();
            watcher.Start();
            if (Program.Config.Debug)
                Console.WriteLine();
            // Initially create new file and copy metadata
            var PreparedFile = new PreparedFile();
            PreparedFile.Metadata = Original.Metadata;
            PreparedFile.Sections = new List<PreparedSection>();
            ConsolePlus.WriteDebug("Preparing File...");
            foreach (BeeperSection section in Original.Sections) // Run through each section
            {
                for (var i = 1; i <= section.Loops; i++) // and it's loops
                {
                    ConsolePlus.WriteDebug($@"   Preparing section ""{section.Title}"" loop {i}...");
                    // Create a new section and copy some values
                    PreparedSection newSection = new PreparedSection();
                    newSection.Loops = section.Loops;
                    newSection.Title = section.Title;
                    newSection.Tracks = new List<PreparedTrack>();
                    newSection.Index = Original.Sections.IndexOf(section);
                    List<ISampleProvider> samples = new List<ISampleProvider>();
                    foreach (BeeperTrack track in section.Tracks) // Run through each track
                    {
                        PreparedTrack newTrack = new PreparedTrack();
                        newTrack.Index = section.Tracks.IndexOf(track);
                        ConsolePlus.WriteDebug($@"      Preparing track ""{track.Title}""...");
                        // Create new track and copy values
                        foreach (BeeperBeep beep in track.Beeps) // Run through each beep
                        {
                            ISampleProvider SampleProvider = new SignalGenerator();
                            if (Program.Config.Debug)
                                ConsolePlus.Write($@"          Preparing {track.SignalType} @ {beep.Frequency}hz for {beep.Duration}ms...", ConsoleColor.Cyan);
                            // Prepare the signal generators and wave players needed for this beep

                            var signalGenerator = new SignalGenerator(); // Initialise the signal generator
                            signalGenerator.Gain = track.Volume; // Set gain
                            signalGenerator.Type = track.SignalType; // Set signal type
                            signalGenerator.Frequency = beep.Frequency;

                            // Manages duration
                            var duration = new OffsetSampleProvider(signalGenerator);
                            duration.Take = TimeSpan.FromMilliseconds(beep.Duration);

                            // Manages pause after beep
                            var pause = new OffsetSampleProvider(duration);
                            pause.LeadOut = TimeSpan.FromMilliseconds(beep.PauseAfter);

                            // Manages panning
                            var panProvider = new PanningSampleProvider(pause.ToMono());
                            panProvider.Pan = track.Pan;

                            samples.Add(panProvider);
                            if (Program.Config.Debug)
                            {
                                ConsolePlus.Write($@"Done!", ConsoleColor.Green);
                                Console.WriteLine();
                            }
                        }
                        ConsolePlus.WriteDebug($@"      Prepared track ""{track.Title}""!");
                        ConcatenatingSampleProvider concentrate = new ConcatenatingSampleProvider(samples);
                        if (Program.AppState.Output != Output.File)
                        {
                            IWavePlayer Player = new WaveOut();
                            switch (Program.AppState.Output)
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
                            Player.Init(concentrate);
                            newTrack.Player = Player;
                        }
                        newTrack.Provider = concentrate;
                        newSection.Tracks.Add(newTrack);
                        samples.Clear();
                    }

                    PreparedFile.Sections.Add(newSection); // Rebuilds sections
                    ConsolePlus.WriteDebug($@"   Prepared section ""{section.Title}"" loop {i}!");
                }
            }
            ConsolePlus.WriteDebug($"File Prepared in {watcher.ElapsedMilliseconds}ms!");
            watcher.Reset();
            return PreparedFile; // Return the complete file.
        }
    }
}
