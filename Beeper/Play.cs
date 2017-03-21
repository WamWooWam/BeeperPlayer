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

        /// <summary>
        /// Plays a BeeperFile using method specified by file.
        /// </summary>
        public static void PlayBeeperFile(PreparedFile FileToPlay)
        {
            Program.AppState.BasicState = BasicState.SpawningThreads; // Sets the app's basic state (aids error reporting)

            foreach (PreparedSection section in FileToPlay.Sections) // Go through each section
            {
                ConsolePlus.WriteHeading($"{section.Title} - {section.TotalBeeps} Beeps", colour: ConsoleColor.Cyan);
                foreach (PreparedTrack track in section.Tracks) // Go through each track
                {
                    Thread TrackTask = new Thread(() => // Create a task for the track
                    {
                        // ConsolePlus.WriteDebug("SYNC", $@"Synchronising task for ""{track.Title}""..."); // Output track sync info (Helps on Dual Cores)
                        // Initialise player based on output settings
                        // TrackTasksReady.Add(true); // Say the task is ready.
                        // while (!Ready || !Playing) { Thread.Sleep(5); } // Wait until other tasks are ready. TODO: This is ugly af.
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
                while (TrackTasks.Any(t => t.IsAlive)) { Thread.Sleep(5); }
                TrackTasksReady.Clear();
                TrackTasks.Clear();
            }
        }

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

        public static PreparedFile PrepareBeeperFile(BeeperFile Original)
        {
            var PreparedFile = new PreparedFile();
            PreparedFile.Metadata = Original.Metadata;
            PreparedFile.Sections = new List<PreparedSection>();
            foreach (BeeperSection section in Original.Sections)
            {
                for (var i = 1; i <= section.Loops; i++) // For each loop
                {
                    PreparedSection newSection = new PreparedSection();
                    newSection.Loops = section.Loops;
                    newSection.Title = section.Title;
                    newSection.Tracks = new List<PreparedTrack>();

                    foreach (BeeperTrack track in section.Tracks)
                    {
                        PreparedTrack newTrack = new PreparedTrack();
                        newTrack.Volume = track.Volume;
                        newTrack.SignalType = track.SignalType;
                        newTrack.Pan = track.Pan;
                        newTrack.Title = track.Title;
                        newTrack.Beeps = new List<PreparedBeep>();
                        foreach (BeeperBeep beep in track.Beeps)
                        {
                            PreparedBeep newBeep = new PreparedBeep();
                            newBeep.Attack = beep.Attack;
                            newBeep.Decay = beep.Decay;
                            newBeep.Frequency = beep.Frequency;
                            newBeep.PauseAfter = beep.PauseAfter;

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

                            var signalGenerator = new SignalGenerator(); // Initialise the signal generator
                            signalGenerator.Gain = track.Volume; // Set gain
                            signalGenerator.Type = track.SignalType; // Set signal type
                            signalGenerator.Frequency = beep.Frequency;

                            var duration = new OffsetSampleProvider(signalGenerator);
                            duration.Take = TimeSpan.FromMilliseconds(beep.Duration);

                            var pause = new OffsetSampleProvider(duration);
                            pause.LeadOut = TimeSpan.FromMilliseconds(beep.PauseAfter);

                            var panProvider = new PanningSampleProvider(pause.ToMono());
                            panProvider.Pan = track.Pan;

                            if (beep.Attack != 0 && beep.Decay != 0)
                            {
                                var asioProvider = new Test.SampleProviders.AdsrSampleProvider(panProvider.ToMono());
                                asioProvider.AttackSeconds = beep.Attack / 1000;
                                asioProvider.DecaySeconds = beep.Attack / 1000;
                                //asioProvider.Take(TimeSpan.FromMilliseconds(beep.TotalDuration));
                                waveOut.Init(asioProvider);
                                newBeep.SampleProvider = asioProvider;
                            }
                            else
                            {
                                waveOut.Init(panProvider);
                                newBeep.SampleProvider = panProvider;
                            }

                            newBeep.Player = waveOut;
                            newTrack.Beeps.Add(newBeep);
                        }
                        newSection.Tracks.Add(newTrack);
                    }
                    PreparedFile.Sections.Add(newSection);
                }
            }
            return PreparedFile;
        }

        /// <summary>
        /// Plays a series of beeps
        /// TODO: Make less ugly.
        /// </summary>
        /// <param name="Beeps">The list of beeps to pay</param>
        /// <param name="track">The track containing the beeps</param>
        /// <param name="signalGenerator">The signal generator to use</param>
        /// <param name="waveOut">The IWavePlayer to use</param>
        /// <param name="Sync">The PlaySynchroniser to use</param>
        public static void PlayBeeps(List<BeeperBeep> Beeps, BeeperTrack track, SignalGenerator signalGenerator, IWavePlayer waveOut)
        {
            if (track.SignalType == SignalGeneratorType.White || track.SignalType == SignalGeneratorType.Pink)
            {
                // Massively better player, doesn't work with noise. FFS.
                // This is made even more annoying by the fact that noise is generally used as drums
                // so keeping it in sync properly is very important.
                ConsolePlus.WriteLine("Skipping Noise Track", ConsoleColor.Red);
            }
            else
            {
                foreach (PreparedBeep beep in Beeps)
                {
                    Console.WriteLine($" Playing {track.SignalType} @ {beep.Frequency}Hz for {beep.Duration}ms."); // Output beep info to console

                    beep.Player.Play(); // Begin playing the output
                    Thread.Sleep(beep.TotalDuration);
                    // Rinse and repeat.   
                }
            }
        }
    }
}
