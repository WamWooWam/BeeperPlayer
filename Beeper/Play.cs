using Beeper.Common;
using Beeper.Common.Models;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WamWooWam.Core;

namespace Beeper
{
    class Play
    {
        /// <summary>
        /// Plays a BeeperFile using method specified by file.
        /// </summary>
        public static void PlayBeeperFile(PreparedFile FileToPlay)
        {
            Program.AppState.BasicState = BasicState.Playing;
            foreach (PreparedSection section in FileToPlay.Sections) // Go through each section
            {
                var OriginalSection = Program.AppState.LoadedFile.Sections[section.Index];
                ConsolePlus.WriteHeading($"Section: {OriginalSection.Title} - {OriginalSection.TotalBeeps / OriginalSection.Loops} Beeps"); // Output track sync info (Helps on Dual Cores)
                Parallel.ForEach(section.Tracks, t =>
                {
                    var OriginalTrack = OriginalSection.Tracks[t.Index];
                    // Initialise player based on output settings     
                    t.Player.Play();
                    foreach (BeeperBeep beep in OriginalTrack.Beeps)
                    {
                        Console.WriteLine($" Playing {OriginalTrack.SignalType} @ {beep.Frequency}Hz for {beep.Duration}ms w/ pause of {beep.PauseAfter}ms"); // Output beep info to console
                        Thread.Sleep(beep.TotalDuration); // Wait for the output to complete  
                    }
                });
            }
        }
    }
}
