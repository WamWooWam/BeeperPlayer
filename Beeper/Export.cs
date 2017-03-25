using Beeper.Common.Models;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WamWooWam.Core;

namespace Beeper
{
    class Export
    {
        public static void ExportBeeperFile(BeeperFile toExport, string Filename)
        {
            bool ExportToMP3 = (Path.GetExtension(Filename) == ".mp3");
            if (ExportToMP3)
                ConsolePlus.WriteHeading($"Exporting to MP3 File");
            else
                ConsolePlus.WriteHeading($"Exporting to Wave File");

            var mediaType = MediaFoundationEncoder.SelectMediaType( AudioSubtypes.MFAudioFormat_MP3, new WaveFormat(44100, 2), 192000);
            if (ExportToMP3 && mediaType == null)
            {
                ConsolePlus.WriteLine("You seem to want to export to MP3, but your system doesn't support it.");
                ConsolePlus.WriteLine("BeeperPlayer will now exit");
            }
            else
            {
                ConsolePlus.Write("Preparing to export... ");
                var PreparedFile = Prepare.PrepareBeeperFile(Program.AppState.LoadedFile);
                ConsolePlus.Write("Done!", ConsoleColor.Green);
                List<ISampleProvider> sampleProviders = new List<ISampleProvider>();
                WaveFormat format = null;
                Console.WriteLine();
                Console.WriteLine();
                ConsolePlus.Write("Mixing tracks... ");
                foreach (PreparedSection section in PreparedFile.Sections)
                {
                    var mixingProviders = new List<ISampleProvider>();
                    foreach (PreparedTrack track in section.Tracks)
                    {
                        mixingProviders.Add(track.Provider);
                        format = track.Provider.WaveFormat;
                    }
                    MixingSampleProvider mixer = new MixingSampleProvider(mixingProviders);
                    sampleProviders.Add(mixer);
                }
                ConcatenatingSampleProvider sequentialMix = new ConcatenatingSampleProvider(sampleProviders);
                ConsolePlus.Write("Ready!", ConsoleColor.Green);
                Console.WriteLine();
                Console.WriteLine();
                ConsolePlus.WriteLine($@"Exporting file to ""{Filename}""");
                if (ExportToMP3)
                {
                    MediaFoundationApi.Startup();
                    MediaFoundationEncoder.EncodeToMp3(sequentialMix.ToWaveProvider(), Filename);
                }
                else
                    WaveFileWriter.CreateWaveFile(Filename, sequentialMix.ToWaveProvider());
            }
        }
    }
}
