using Beeper.Gui.Models;
using Beeper.Gui.Tools;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Beeper.Gui.Controls
{
    /// <summary>
    /// Interaction logic for RecentlyOpenedFileControl.xaml
    /// </summary>
    public partial class RecentlyOpenedFileControl : ListViewItem
    {
        public RecentFileModel RecentFile { get; set; }

        public RecentlyOpenedFileControl()
        {
            InitializeComponent();
            beeperIcon.Source = ImageResources.GetImageResource("beeperLogo", 32);
            openImage.Source = ImageResources.GetImageResource("openFile", 16);
            removeImage.Source = ImageResources.GetImageResource("removeFile", 16);
        }

        public RecentlyOpenedFileControl(RecentFileModel recentFile)
        {
            InitializeComponent();
            RecentFile = recentFile;
            beeperIcon.Source = ImageResources.GetImageResource("beeperLogo", 32);
            openImage.Source = ImageResources.GetImageResource("openFile", 16);
            removeImage.Source = ImageResources.GetImageResource("removeFile", 16);
            fileName.Text = recentFile.DisplayName;
            filePath.Text = recentFile.FilePath;
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow).ExternalPlayFile(RecentFile.FilePath);
        }

        private void openMenuItem_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow).ExternalPlayFile(RecentFile.FilePath);
        }

        private void removeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ExternalRemovePlayedFile(RecentFile.FilePath);
            (Parent as ListView).Items.Remove(this);
        }
    }
}
