using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Beeper.Gui.Controls
{
    /// <summary>
    /// Interaction logic for RecentFile.xaml
    /// </summary>
    public partial class RecentFile : UserControl
    {
        public RecentFile()
        {
            InitializeComponent();
            beeperIcon.Source = new BitmapImage(new Uri("file:///" + Path.Combine(Directory.GetCurrentDirectory(), "Resources/beeperlogo32.png"), UriKind.Absolute));
        }

        public RecentFile(RecentFile )
    }
}
