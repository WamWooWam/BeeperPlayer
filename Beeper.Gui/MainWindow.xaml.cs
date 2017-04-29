using Beeper.Gui.Tools;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using Newtonsoft.Json;
using Beeper.Common.Models;
using WamWooWam.Core.Collections;
using Beeper.Common;
using Ookii.Dialogs.Wpf;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;
using Beeper.Gui.Models;
using Beeper.Gui.Controls;

namespace Beeper.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            openImage.Source = ImageResources.GetImageResource("openFile", 48);
            editExistingImage.Source = ImageResources.GetImageResource("editFile", 48);
            createNewImage.Source = ImageResources.GetImageResource("newFile", 48);
            RefreshView();
        }

        private void RefreshView()
        {
            recentlyPlayedFiles.Items.Clear();
            foreach (RecentFileModel file in Program.Config.RecentlyPlayedFiles.ToArray().Reverse())
            {
                RecentlyOpenedFileControl control = new RecentlyOpenedFileControl(file);
                recentlyPlayedFiles.Items.Add(control);
            }
            if (Program.Config.RecentlyPlayedFiles.Count == 0)
            {
                recentlyPlayedFilesText.Content = "Recent files will appear here";
            }
        }

        public static void ExternalRemovePlayedFile(string filePath)
        {
            Program.Config.RecentlyPlayedFiles.RemoveAll(p => p.FilePath == filePath);
        }

        /// <summary>
        /// Externally runs PlayFile routine.
        /// </summary>
        /// <param name="File"></param>
        public void ExternalPlayFile(string File)
        {
            try
            {
                Program.PlayBeeperFile(File);
            }
            catch (Exception ex)
            {
                if (ex is DirectoryNotFoundException | ex is FileNotFoundException)
                {
                    Ookii.Dialogs.Wpf.TaskDialog dialog = new Ookii.Dialogs.Wpf.TaskDialog();
                    dialog.WindowTitle = "I can't find that!";
                    dialog.MainIcon = TaskDialogIcon.Information;
                    Ookii.Dialogs.Wpf.TaskDialogButton yesButton = new Ookii.Dialogs.Wpf.TaskDialogButton(ButtonType.Yes);
                    dialog.Buttons.Add(yesButton);
                    dialog.Buttons.Add(new Ookii.Dialogs.Wpf.TaskDialogButton(ButtonType.No));
                    dialog.Content = "Sorry! I couldn't open that file because it doesn't exist or isn't where I left it. Should I remove it from the list?";
                    if (dialog.ShowDialog() == yesButton)
                    {
                        Program.Config.RecentlyPlayedFiles.RemoveAll(f => f.FilePath == File);
                        RefreshView();
                    }
                }
                else
                {
                    Ookii.Dialogs.Wpf.TaskDialog errorDialog = new Ookii.Dialogs.Wpf.TaskDialog();
                    errorDialog.WindowTitle = "Invalid BeeperFile";
                    errorDialog.MainIcon = TaskDialogIcon.Error;
                    errorDialog.Buttons.Add(new Ookii.Dialogs.Wpf.TaskDialogButton(ButtonType.Ok));
                    Ookii.Dialogs.Wpf.TaskDialogButton MoreDetails = new Ookii.Dialogs.Wpf.TaskDialogButton("More Details");
                    errorDialog.Buttons.Add(MoreDetails);
                    errorDialog.Content = "I couldn't load that file because it's invalid.";
                    if (errorDialog.ShowDialog() == MoreDetails)
                    {
                        string TempPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName())) + ".json";
                        System.IO.File.WriteAllText(TempPath, JsonConvert.SerializeObject(ex, Formatting.Indented));
                        Process.Start(TempPath);
                        Program.TemporaryFiles.Add(TempPath);
                    }
                }
            }
        }

        private void openButon_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog open = new CommonOpenFileDialog();
            open.Filters.Add(new CommonFileDialogFilter("BeeperPlayer Audio Files", ".beep"));
            if (open.ShowDialog() == CommonFileDialogResult.Ok)
            {
                try
                {
                    Program.PlayBeeperFile(open.FileName);
                }
                catch (Exception ex)
                {
                    Ookii.Dialogs.Wpf.TaskDialog errorDialog = new Ookii.Dialogs.Wpf.TaskDialog();
                    errorDialog.WindowTitle = "Invalid BeeperFile";
                    errorDialog.MainIcon = TaskDialogIcon.Error;
                    errorDialog.Buttons.Add(new Ookii.Dialogs.Wpf.TaskDialogButton(ButtonType.Ok));
                    Ookii.Dialogs.Wpf.TaskDialogButton MoreDetails = new Ookii.Dialogs.Wpf.TaskDialogButton("More Details");
                    errorDialog.Buttons.Add(MoreDetails);
                    errorDialog.Content = "I couldn't load that file because it's invalid.";
                    if (errorDialog.ShowDialog() == MoreDetails)
                    {
                        string TempPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName())) + ".json";
                        File.WriteAllText(TempPath, JsonConvert.SerializeObject(ex, Formatting.Indented));
                        Process.Start(TempPath);
                        Program.TemporaryFiles.Add(TempPath);
                    }
                }
            }
        }

        public static void AddRecentlyPlayedFile(BeeperFile file, string Path)
        {
            foreach (RecentFileModel model in Program.Config.RecentlyPlayedFiles.Where(m => m.FilePath == Path).ToArray())
            {
                Program.Config.RecentlyPlayedFiles.Remove(model);
            }
            Program.Config.RecentlyPlayedFiles.Add(new RecentFileModel() { FilePath = Path, DisplayName = file.Metadata.Title + " - " + file.Metadata.Album });
            Program.Config.Save();
            //RefreshView();
        }
    }
}
