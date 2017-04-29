using Beeper.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Converters;

namespace Beeper.Gui.Models
{
    public class GuiConfig : Config
    {
        [JsonIgnore]
        public string SaveLocation => Path.Combine(Program.CurrentDurectory, "Resources", "Config.json");
        public bool DontAskAgain { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Output OutputMethod { get; set; }

        public float Volume { get; set; }

        public List<RecentFileModel> RecentlyPlayedFiles { get; set; } = new List<RecentFileModel>();
        public List<RecentFileModel> RecentlyEditedFiles { get; set; } = new List<RecentFileModel>();

        public void Save()
        {
            File.WriteAllText(SaveLocation, JsonConvert.SerializeObject(Program.Config, Formatting.Indented));
        }
    }
}
