using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Beeper.Gui.Models
{
    public class ImageResource
    {
        public string Key { get; set; }
        public BitmapImage Base { get; set; }
        public int Size { get; set; }
    }
}
