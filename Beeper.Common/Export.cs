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

namespace Beeper.Common
{
    public static class Export
    {
        public static void ExportBeeperFile(PreparedFile toExport, string Filename)
        {
            bool ExportToMP3 = (Path.GetExtension(Filename) == ".mp3");
            if (ExportToMP3)
                ConsolePlus.WriteHeading($"Exporting to MP3 File");
            else
                ConsolePlus.WriteHeading($"Exporting to Wave File");

            var mediaType = MediaFoundationEncoder.SelectMediaType(AudioSubtypes.MFAudioFormat_MP3, new WaveFormat(44100, 2), 192000);
            if (ExportToMP3 && mediaType == null)
            {
                ConsolePlus.WriteLine("You seem to want to export to MP3, but your system doesn't support it.");
                ConsolePlus.WriteLine("BeeperPlayer will now exit");
            }
            else
            {
                // TODO: Why do I need to make convert to a wave provider and then back to a sample provider
                //       to stop it doing strange things?
                OffsetSampleProvider offset = new OffsetSampleProvider(toExport.Provider.ToWaveProvider().ToSampleProvider());
                offset.DelayBy = TimeSpan.FromMilliseconds(500);
                offset.LeadOut = TimeSpan.FromMilliseconds(500);
                ConsolePlus.Write($@"Exporting file to ""{Filename}""...");
                if (ExportToMP3)
                {
                    MediaFoundationApi.Startup();
                    MediaFoundationEncoder.EncodeToMp3(offset.ToWaveProvider(), Filename);
                }
                else
                    WaveFileWriter.CreateWaveFile(Filename, offset.ToWaveProvider());

                ConsolePlus.Write($@"Done!", ConsoleColor.Green);
                Console.WriteLine();
            }
        }
    }
}
