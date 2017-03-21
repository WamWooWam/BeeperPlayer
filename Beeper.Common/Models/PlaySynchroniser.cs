using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamWooWam.Core;

namespace Beeper.Common.Models
{
    /// <summary>
    /// Basic play synchroniser implementation
    /// TODO: Be actually smart about it, do clever things, proper averadges and shit.
    /// </summary>
    public class PlaySynchroniser
    {
        public PlaySynchroniser(Config config)
        {
            PlayPercentageReduction = 0;
            AveragePlayLatency = 0;
            // TODO: Why does this work? When doesn't it? Should this be calculated
            //       prior to play?
            AverageBeepDuration = 1;
            Configuration = config;
        }

        static Config Configuration { get; set; }
        public float PlayPercentageReduction { get; set; }
        public float AveragePlayLatency { get; set; }
        public float AverageBeepDuration { get; set; }

        /// <summary>
        /// Calculates the percentage a beep needs to be compensated by.
        /// </summary>
        /// <param name="beep">The beep we're calculating this for</param>
        /// <returns>The percentage adjustment that the beep needs.</returns>
        public float GetCompensationPercentage(BeeperBeep beep)
        {
            // Add this beep to the average length of beeps in the file
            AverageBeepDuration += beep.TotalDuration;
            AverageBeepDuration /= 2;

            // Get the percentage difference between this beep and the average beep.
            var PercentageDiff = (AverageBeepDuration - beep.TotalDuration) / AverageBeepDuration;
            // Add this to the normal reduction
            PercentageDiff += PlayPercentageReduction;
            return PercentageDiff / 2; // Return the average of the two
        }

        /// <summary>
        /// Calculates the time a beep needs to play for, adjusted based on 
        /// previous playback information.
        /// <summary>
        /// <param name="beep">The beep that's being played</param>
        /// <returns>The amount of time to pause between play and stop</returns>
        public int GetCompensatedPlayTime(BeeperBeep beep)
        {
            if (Configuration.TimingAccuracy.EnableOnTheFlyAdjustment)
            {
                if (beep.Duration * GetCompensationPercentage(beep) > 0)
                    return beep.Duration - (int)(beep.Duration * GetCompensationPercentage(beep));
                else
                    return beep.Duration + (int)(beep.Duration * GetCompensationPercentage(beep));
            }
            else
                return beep.Duration;
        }

        /// <summary>
        /// Calculates the time we need to pause for between beeps. Mostly here
        /// because I can, not strictly needed but good for accuracy.
        /// </summary>
        /// <param name="beep">The beep that's being played</param>
        /// <returns>The amount of time to pause for between this and the next beep.</returns>
        public int GetCompensatedPauseTime(BeeperBeep beep)
        {
            if (Configuration.TimingAccuracy.EnableOnTheFlyAdjustment)
                return beep.PauseAfter - (int)(beep.PauseAfter * GetCompensationPercentage(beep));
            else
                return beep.PauseAfter;

        }

        /// <summary>
        /// Submits the time it took to play and works out the difference to
        /// better sync later plays.
        /// TODO: Cleanup, be smart.
        /// </summary>
        /// <param name="Expected">The amount of time it should've taken to perform an action.</param>
        /// <param name="Actual">The amount of time it actually took to do it.</param>
        public void SubmitPlayTime(int Expected, long Actual)
        {
            float Difference = Actual - Expected; // Get the difference between how long the beep took vs how long it should've taken
            float PercentageDifference = Difference / Expected; // Calculate to percentage
                                                                // Ensure beep is long enough not to cause issues
            if (Expected > 20 && Math.Abs(Actual - Expected) > Configuration.TimingAccuracy.AccuracyThreshold / 2)
            {
                // if enabled
                if (Configuration.TimingAccuracy.EnableOnTheFlyAdjustment)
                {
                    // Set the reduction in play percentage
                    if (PlayPercentageReduction == 0)
                        PlayPercentageReduction += PercentageDifference;
                    else
                    {
                        PlayPercentageReduction += PercentageDifference;
                        PlayPercentageReduction /= 2;
                    }
                }

                // Set the average play difference
                if (AveragePlayLatency == 0)
                    AveragePlayLatency += Difference;
                else
                {
                    AveragePlayLatency += Difference;
                    AveragePlayLatency /= 2;
                }
            }
        }

        /// <summary>
        /// Evaluates the final time
        /// </summary>
        /// <param name="TotalDuration">The total final duration, timed.</param>
        /// <param name="ActualDuration">The duration it should've taken, calculated.</param>
        public void EvaluateTime(int TotalDuration, int ActualDuration)
        {
            if (Configuration.TimingAccuracy.EnableOnTheFlyAdjustment)
            {
                float Difference = ActualDuration - TotalDuration; // Get the difference between how long the beep took vs how long it should've taken
                float PercentageDifference = Difference / TotalDuration; // Calculate to percentage

                if (PlayPercentageReduction == 0)
                    PlayPercentageReduction = PercentageDifference;
                if (AveragePlayLatency == 0)
                    AveragePlayLatency = Difference;

                if (Math.Abs(TotalDuration - ActualDuration) <= Configuration.TimingAccuracy.AccuracyThreshold)
                    ConsolePlus.WriteDebug("SYNC", $"Play time within ± {Configuration.TimingAccuracy.AccuracyThreshold}ms! (out by {(ActualDuration - TotalDuration).ToString("+0;-#")}ms)", ConsoleColor.Green);
                else
                    ConsolePlus.WriteDebug("SYNC", $"Play time outside ± {Configuration.TimingAccuracy.AccuracyThreshold}ms, Attempting to compensate! (out by {(ActualDuration - TotalDuration).ToString("+0;-#")}ms)", ConsoleColor.Red);
            }
        }
    }
}
