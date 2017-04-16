using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beeper.Common.Statistics
{
    public class FileLoad
    {
        public long FileRead { get; set; }
        public long JsonParse { get; set; }
        public long FilePrep { get; set; }
        public long Total { get { return FileRead + JsonParse + FilePrep; } }
    }
}
