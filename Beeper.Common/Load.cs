using Beeper.Common.Models;
using Ionic.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beeper.Common
{
    public static class Load
    {
        /// <summary>
        /// Loads a BeeperFile from a file on disk.
        /// </summary>
        /// <param name="FilePath">The path of the file to load</param>
        /// <returns>A tuple containing the file itself, the file's directory (for samples) and the type of file loaded.</returns>
        public static Tuple<BeeperFile, string, BeeperFileType> LoadFromFile(string FilePath)
        {
            if (ZipFile.IsZipFile(FilePath)) // If the file is a zip file...
            {
                string ExtractPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
                Directory.CreateDirectory(ExtractPath); // Prepare a temporary directory to extract it to
                using (ZipFile zip = ZipFile.Read(FilePath))
                {
                    zip.ExtractAll(ExtractPath); // Extract the whole file to the temp folder
                }
                BeeperFile LoadedFile = JsonConvert.DeserializeObject<BeeperFile>(File.ReadAllText(Path.Combine(ExtractPath, "file.beep")));
                return new Tuple<BeeperFile, string, BeeperFileType>(LoadedFile, ExtractPath, BeeperFileType.Zip);
            }
            else
            {
                string ExtractPath = Path.GetDirectoryName(FilePath);
                BeeperFile LoadedFile = JsonConvert.DeserializeObject<BeeperFile>(File.ReadAllText(FilePath));
                return new Tuple<BeeperFile, string, BeeperFileType>(LoadedFile, ExtractPath, BeeperFileType.Json);
            }
        }
    }
}
