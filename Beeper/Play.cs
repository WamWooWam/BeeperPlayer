using Beeper.Common;
using Beeper.Common.Models;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WamWooWam.Core;

namespace Beeper
{
    class Play
    {
        static List<bool> TrackTasksReady = new List<bool>();
        static List<Thread> TrackTasks = new List<Thread>();
        static bool Ready { get { return TrackTasksReady.Count == TrackTasks.Count; } }
        static bool Playing = false;

        /// <summary>
        /// Plays a BeeperFile using method specified by file.
        /// </summary>
        public static void PlayBeeperFile(PreparedFile FileToPlay)
        {
            Program.AppState.BasicState = BasicState.SpawningThreads; // Sets the app's basic state (aids error reporting)

            foreach (PreparedSection section in FileToPlay.Sections) // Go through each section
            {
                ConsolePlus.WriteHeading($"Section: {section.Title} - {section.TotalBeeps} Beeps"); // Output track sync info (Helps on Dual Cores)
                foreach (PreparedTrack track in section.Tracks) // Go through each track
                {
                    Thread TrackTask = new Thread(() => // Create a task for the track
                    {
                        // Initialise player based on output settings
                        TrackTasksReady.Add(true); // Say the task is ready.
                        while (!Ready || !Playing) { Thread.Sleep(10); } // Wait until other tasks are ready. TODO: This is ugly af.
                        for (var i = 1; i <= section.Loops; i++) // For each loop
                        {
                            // Play all beeps in the track
                                foreach (PreparedBeep beep in track.Beeps)
                                {
                                    Console.WriteLine($" Playing {track.SignalType} @ {beep.Frequency}Hz for {beep.Duration}ms."); // Output beep info to console

                                    beep.Player.Play(); // Begin playing the output
                                    Thread.Sleep(beep.TotalDuration);
                                    // Rinse and repeat.   
                                }
                            
                        }

                    });
                    TrackTasks.Add(TrackTask); // Add task to track tasks
                }
                Parallel.ForEach(TrackTasks, t => t.Start());
                Program.AppState.BasicState = BasicState.PreparingThreads;
                while (!Ready) { Thread.Sleep(5); }
                Playing = true;
                while (TrackTasks.Any(t => t.IsAlive)) { Thread.Sleep(5); }
                TrackTasksReady.Clear();
                TrackTasks.Clear();
            }
        }

        /// <summary>
        /// Stops all running playback threads
        /// Used to prevent issues in case of a crash
        /// </summary>
        public static void StopAllThreads()
        {
            if (TrackTasks.Any(t => t.IsAlive))
            {
                foreach (Thread thread in TrackTasks)
                {
                    thread.Abort();
                }
            }
        }

        /// <summary>
        /// Prepares a beeper file to be played.
        /// </summary>
        /// <param name="Original">The beeper file that needs converting</param>
        /// <returns>The converted, ready to play file.</returns>
        public static PreparedFile PrepareBeeperFile(BeeperFile Original)
        {
            // Initially create new file and copy metadata
            var PreparedFile = new PreparedFile();
            PreparedFile.Metadata = Original.Metadata;
            PreparedFile.Sections = new List<PreparedSection>();
            foreach (BeeperSection section in Original.Sections) // Run through each section
            {
                for (var i = 1; i <= section.Loops; i++) // and it's loops
                {
                    // Create a new section and copy some values
                    PreparedSection newSection = new PreparedSection();
                    newSection.Loops = section.Loops;
                    newSection.Title = section.Title;
                    newSection.Tracks = new List<PreparedTrack>();

                    foreach (BeeperTrack track in section.Tracks) // Run through each track
                    {
                        // Create new track and copy values
                        PreparedTrack newTrack = new PreparedTrack();
                        newTrack.Volume = track.Volume;
                        newTrack.SignalType = track.SignalType;
                        newTrack.Pan = track.Pan;
                        newTrack.Title = track.Title;
                        newTrack.Beeps = new List<PreparedBeep>();
                        foreach (BeeperBeep beep in track.Beeps) // Run through each beep
                        {
                            // Create a new beep and copy values
                            PreparedBeep newBeep = new PreparedBeep();
                            newBeep.Attack = beep.Attack;
                            newBeep.Decay = beep.Decay;
                            newBeep.Frequency = beep.Frequency;
                            newBeep.PauseAfter = beep.PauseAfter;

                            // Initialise an IWavePlayer (for output)
                            IWavePlayer waveOut = new WaveOut();
                            if (Program.AppState.Output == Output.Asio)
                                waveOut = new AsioOut();
                            else if (Program.AppState.Output == Output.DirectSound)
                                waveOut = new DirectSoundOut();
                            else if (Program.AppState.Output == Output.File)
                            {
                                // TODO: File output
                            }
                            else if (Program.AppState.Output == Output.Wasapi)
                                waveOut = new WasapiOut();
                            else if (Program.AppState.Output == Output.WaveOut)
                                waveOut = new WaveOut();

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

                            if (beep.Attack != 0 && beep.Decay != 0)
                            {
                                // Should manage Attack, Sustain, Decay and Release, it doesn't
                                var asioProvider = new Test.SampleProviders.AdsrSampleProvider(panProvider.ToMono());
                                asioProvider.AttackSeconds = beep.Attack / 1000;
                                asioProvider.DecaySeconds = beep.Attack / 1000;
                                //asioProvider.Take(TimeSpan.FromMilliseconds(beep.TotalDuration)); // Unneeded, doesn't really work.
                                waveOut.Init(asioProvider); // Initialises wave out
                                newBeep.SampleProvider = asioProvider; // Saves sample provider (just in case)
                            }
                            else
                            {
                                waveOut.Init(panProvider); // Initialises wave out
                                newBeep.SampleProvider = panProvider; // Saves sample provider (just in case)
                            }
                            newBeep.Player = waveOut; // Saves the player
                            newTrack.Beeps.Add(newBeep); // Adds the newly prepared beep to the list
                        }
                        newSection.Tracks.Add(newTrack); // Rebuilds tracks
                    }
                    PreparedFile.Sections.Add(newSection); // Rebuilds sections
                }
            }
            return PreparedFile; // Return the complete file.
        }
    }
}
