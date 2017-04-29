using System;
using System.Threading;
using NAudio.Dsp;
using NAudio.Wave;

namespace Beeper.Test.SampleProviders
{
    /// <summary>
    /// ADSR sample provider allowing you to specify attack, decay, sustain and release values
    /// </summary>
    public class AdsrSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        public readonly EnvelopeGenerator adsr;

        /// <summary>
        /// Creates a new AdsrSampleProvider with default values
        /// </summary>
        public AdsrSampleProvider(ISampleProvider source)
        {
            if (source.WaveFormat.Channels > 1) throw new ArgumentException("Currently only supports mono inputs");
            this.source = source;
            adsr = new EnvelopeGenerator();
            adsr.Gate(true);
        }

        /// <summary>
        /// Reads audio from this sample provider
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            if (adsr.State == EnvelopeGenerator.EnvelopeState.Idle) return 0; // we've finished
            int samples = source.Read(buffer, offset, count);
            for (int n = 0; n < samples; n++)
            {
                buffer[offset++] *= adsr.Process();
            }
            return samples;
        }


        /// <summary>
        /// Enters the Release phase
        /// </summary>
        public void Stop()
        {
            adsr.Gate(false);
        }

        /// <summary>
        /// The output WaveFormat
        /// </summary>
        public WaveFormat WaveFormat { get { return source.WaveFormat; } }
    }
}