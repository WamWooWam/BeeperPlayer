using Beeper.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WamWooWam.Core;

namespace Beeper.Common
{
    public static class Play
    {
        public static void PlayBeeperFile(BeeperFile ToPlay)
        {
            var PreparedFile = Prepare.PrepareBeeperFile(ToPlay, Output.DirectSound, null);
            PreparedFile.Player.Play();
            OutputToConsole(ToPlay);
        }

        public static void PlayPreparedBeeperFile(BeeperFile ToPlay, PreparedFile Prepared)
        {
            Prepared.Player.Play();

            OutputToConsole(ToPlay);
        }

        public static void OutputToConsole(BeeperFile ToPlay)
        {
            foreach (BeeperSection section in ToPlay.Sections) // Go through each section
            {
                for (var i = 1; i <= section.Loops; i++) // and it's loops
                {
                    ConsolePlus.WriteHeading($"Section: {section.Title} (Loop {i}) - {section.TotalBeeps / section.Loops} Beeps"); // Output section information
                    Parallel.ForEach(section.Tracks, t =>
                    {
                        foreach (BeeperBeep beep in t.Beeps) // Gives illusion of beeps being played individually, in fact they're one long continous thing
                        {
                            Thread.Sleep(beep.PauseBefore.GetValueOrDefault());
                            if (string.IsNullOrEmpty(beep.Sample))
                                ConsolePlus.WriteLine($"Playing {t.SignalType} @ {beep.Frequency}Hz for {beep.Duration}ms w/ pause of {beep.PauseAfter}ms"); // Output beep info to console
                            else
                                ConsolePlus.WriteLine($"Playing \"{beep.Sample}\" w/ pause of {beep.PauseAfter}ms"); // Output beep info to console
                            Thread.Sleep(beep.TotalDuration); // Wait for the output to complete  
                        }
                    });
                }
            }
        }
    }
}
