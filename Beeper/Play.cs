using Beeper.Common;
using Beeper.Common.Models;
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
        public static PlaySynchroniser Sync;

        /// <summary>
        /// Plays a BeeperFile using method specified by file.
        /// </summary>
        public static void PlayBeeperFile()
        {
            BeeperFile FileToPlay = Program.AppState.LoadedFile; // Gets the file to play
            Sync = new PlaySynchroniser(Program.Config); // Initialises the synchroniser to use
            Program.AppState.BasicState = BasicState.SpawningThreads; // Sets the app's basic state (aids error reporting)
            foreach (BeeperSection section in FileToPlay.Sections) // Go through each section
            {
                foreach (BeeperTrack track in section.Tracks) // Go through each track
                {
                    Thread TrackTask = new Thread(() => // Create a task for the track
                    {
                        ConsolePlus.WriteDebug("SYNC", $@"Synchronising task for ""{track.Title}""..."); // Output track sync info (Helps on Dual Cores)
                        var signalGenerator = new SignalGenerator(); // Initialise the signal generator
                        signalGenerator.Gain = track.Volume; // Set gain
                        signalGenerator.Type = track.SignalType; // Set volume
                        var panProvider = new PanningSampleProvider(signalGenerator.ToMono()); // Initialise pan provider (faux stereo)
                        panProvider.Pan = track.Pan; // Set panning

                        // Initialise player based on output settings
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

                        waveOut.Init(panProvider); // Initialise with providers

                        TrackTasksReady.Add(true); // Say the task is ready.
                        while (!Ready || !Playing) { Thread.Sleep(5); } // Wait until other tasks are ready. TODO: This is ugly af.
                        for (var i = 1; i <= section.Loops; i++) // For each loop
                        {
                            // Play all beeps in the track
                            PlayBeeps(track.Beeps, track, signalGenerator, waveOut, Sync);
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
        /// Plays a series of beeps
        /// TODO: Make less ugly.
        /// </summary>
        /// <param name="Beeps">The list of beeps to pay</param>
        /// <param name="track">The track containing the beeps</param>
        /// <param name="signalGenerator">The signal generator to use</param>
        /// <param name="waveOut">The IWavePlayer to use</param>
        /// <param name="Sync">The PlaySynchroniser to use</param>
        public static void PlayBeeps(List<BeeperBeep> Beeps, BeeperTrack track, SignalGenerator signalGenerator, IWavePlayer waveOut, PlaySynchroniser Sync)
        {
            // Initialise a stopwatch to time how long the beep takes
            // vs how long it actually takes
            // TODO: Asyncronously work out the difference between how lomg a beep is taking
            //       vs. how long it should be taking and inteligently compensate for that.
            Stopwatch TimeWatcher = new Stopwatch();
            foreach (BeeperBeep beep in Beeps)
            {
                TimeWatcher.Start(); // Begin timing the beep
                signalGenerator.Frequency = beep.Frequency; // Set the output frequency
                Console.WriteLine();
                Console.Write($" Playing {track.SignalType} @ {beep.Frequency}Hz for {beep.Duration}ms."); // Output beep info to console
                if (Program.Config.TimingAccuracy.EnableOnTheFlyAdjustment)
                    Console.Write($" Adjusting by {Sync.PlayPercentageReduction * -100}% ({(beep.Duration - Sync.GetCompensatedPlayTime(beep)).ToString("+0;-#")}ms)");

                waveOut.Play(); // Begin playing the output

                if (Sync.GetCompensatedPlayTime(beep) > 0)
                    Thread.Sleep(Sync.GetCompensatedPlayTime(beep)); // Wait for duration

                // Hack to work around DSound issues
                // TODO: Work properly.
                if (Program.AppState.Output == Output.DirectSound)
                    waveOut.Pause(); // Stop the output
                else
                    waveOut.Stop();

                // Submit play time to synchroniser
                if (Program.Config.TimingAccuracy.EnableOnTheFlyAdjustment)
                    Sync.SubmitPlayTime(beep.Duration, TimeWatcher.ElapsedMilliseconds);

                ConsolePlus.WriteDebug("PLAY", $"Waiting for {beep.PauseAfter}ms"); // Output more debugging info
                if (Sync.GetCompensatedPauseTime(beep) > 0) // Prevents crashes on stupidly slow systems.
                    Thread.Sleep(Sync.GetCompensatedPauseTime(beep)); // Wait for pause     

                if (Program.Config.TimingAccuracy.EnableOnTheFlyAdjustment)
                {
                    ConsolePlus.WriteDebug("SYNC", $"Play took {TimeWatcher.ElapsedMilliseconds}ms, expected {beep.TotalDuration}ms"); // Output as debugging info.
                    TimeWatcher.Stop();
                    Sync.EvaluateTime(beep.TotalDuration, (int)TimeWatcher.ElapsedMilliseconds); // Write evaluation of play time
                    TimeWatcher.Reset(); // Reset
                }
                // Rinse and repeat.   
            }
        }
    }
}
