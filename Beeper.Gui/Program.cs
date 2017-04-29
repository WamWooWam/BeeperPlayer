using Beeper.Common;
using Beeper.Common.Models;
using Beeper.Gui.Models;
using Beeper.Gui.Properties;
using Beeper.Gui.Tools;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Threading;

namespace Beeper.Gui
{
    public static class Program
    {
        static App application = new App();
        static MainWindow startupWindow;
        public static AppState AppState = new AppState(); // Creates a new app state
        public static GuiConfig Config = new GuiConfig(); // Manages configuration
        public static string CurrentDurectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); // App data directory
        public static ResourceManager ResourceManager = new ResourceManager(typeof(Resources));
        public static List<string> TemporaryFiles = new List<string>();
        public static List<string> TemporaryDirectories = new List<string>();
        public static List<Thread> TemporaryThreads = new List<Thread>();

        [STAThread]
        public static void Main(string[] args)
        {
            // Check if the degugger is attached
            if (!Debugger.IsAttached)
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException; // If not, set exception handler

            // If reset is passed through the command line
            // TODO: Settings window & GUI option for this
            if (args.Contains("reset"))
            { 
                try
                {
                    // Creates new configuration
                    File.Delete(Path.Combine(CurrentDurectory, "Resources", "Config.json"));
                    ProcessStartInfo info = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location);
                    info.Arguments = "has-reset";
                    Process.Start(info);
                    Environment.Exit(0);
                }
                catch
                {
                    ProcessStartInfo info = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location);
                    info.UseShellExecute = true;
                    info.Verb = "runas";
                    info.Arguments = "reset";
                    Process.Start(info);
                    Environment.Exit(0);
                }
            }

            // On successful reset
            if (args.Contains("has-reset"))
            {
                // Let the user know the reset succeeded
                TaskDialog hasReset = new TaskDialog();
                hasReset.WindowTitle = ResourceManager.GetString("GUISettingsResetTitle");
                hasReset.Content = ResourceManager.GetString("GUISettingsReset");
                hasReset.Buttons.Add(new TaskDialogButton(ButtonType.Ok));
                hasReset.MainIcon = TaskDialogIcon.Information;
                hasReset.ShowDialog();
            }

            // Sets default config options.
            Config.ErrorReporter = new ErrorReporter(); // Manages error reporting settings
            Config.FirstRun = true; // Manages first run
            Config.OutputMethod = Output.DirectSound; // Manages default output
            Config.RecentlyEditedFiles = new List<RecentFileModel>();
            Config.RecentlyPlayedFiles = new List<RecentFileModel>();
            Config.Volume = 1F;

            // Sets app Instance ID
            // Used for disgnostics & usage data
            if (Config.InstanceID == null)
            {
                Random random = new Random();
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";
                Config.InstanceID = new string(Enumerable.Repeat(chars, 12)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
            }

            if (File.Exists(Path.Combine(CurrentDurectory, "Resources", "Config.json"))) // Checks for configuration file
            {
                try
                {
                    // Loads configuration
                    Config = JsonConvert.DeserializeObject<GuiConfig>(
                        File.ReadAllText(Path.Combine(CurrentDurectory,"Resources", "Config.json")));
                }
                catch
                {
                    // Creates new configuration
                    Config.Save();
                }
            }
            else
            {
                try
                {
                    // Creates new configuration
                    File.WriteAllText(Path.Combine(CurrentDurectory, "Resources", "Config.json"),
                        JsonConvert.SerializeObject(Config, Formatting.Indented));
                }
                catch
                {
                    // Relaunches app as administrator
                    // Helps if app is installed to Program Files and it can't access config.json
                    TaskDialog dialog = new TaskDialog();
                    dialog.WindowTitle = ResourceManager.GetString("GUIUnableToWriteConfigTitle");
                    dialog.MainInstruction = ResourceManager.GetString("GUIUnableToWriteConfigInstruction");
                    dialog.Content = ResourceManager.GetString("GUIUnableToWriteConfig");
                    dialog.MainIcon = TaskDialogIcon.Warning;
                    dialog.AllowDialogCancellation = true;
                    dialog.ButtonStyle = TaskDialogButtonStyle.CommandLinks;
                    TaskDialogButton relaunchAsAdmin = new TaskDialogButton(ResourceManager.GetString("RelaunchAsAdministrator"));
                    relaunchAsAdmin.ElevationRequired = true;
                    dialog.Buttons.Add(relaunchAsAdmin);
                    dialog.Buttons.Add(new TaskDialogButton(ButtonType.Cancel));

                    if (dialog.Show() == relaunchAsAdmin)
                    {
                        ProcessStartInfo info = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location);
                        info.UseShellExecute = true;
                        info.Verb = "runas";
                        info.Arguments = "firstrun";
                        Process.Start(info);
                        Environment.Exit(0);
                    }
                    else
                    {
                        Environment.Exit(0);
                    }
                }
            }

            Config.Save();

            // TODO: Themes?
            ImageResources.LoadImageResources(Path.Combine(CurrentDurectory, "Resources", "Images"));

            application.Exit += Application_Exit;

            startupWindow = new MainWindow();

            if (!Config.DontAskAgain)
            {
                TaskDialog dialog = new TaskDialog();
                dialog.WindowTitle = ResourceManager.GetString("GUIWarningTitle");
                dialog.MainInstruction = ResourceManager.GetString("GUIWarningInstruction");
                dialog.Content = ResourceManager.GetString("GUIWarningMessage");
                dialog.AllowDialogCancellation = true;
                dialog.MainIcon = TaskDialogIcon.Warning;
                dialog.ButtonStyle = TaskDialogButtonStyle.CommandLinks;

                TaskDialogButton runCLI = new TaskDialogButton(ResourceManager.GetString("RunCLI"));
                TaskDialogButton runGUI = new TaskDialogButton(ResourceManager.GetString("RunGUI"));
                dialog.Buttons.Add(runCLI);
                dialog.Buttons.Add(runGUI);

                dialog.Buttons.Add(new TaskDialogButton(ButtonType.Cancel));
                dialog.IsVerificationChecked = false;
                dialog.VerificationText = ResourceManager.GetString("DontAskMeAgain");
                dialog.MainIcon = TaskDialogIcon.Warning;

                TaskDialogButton result = dialog.ShowDialog();

                if (result == runCLI)
                {
                    try
                    {
                        Process.Start(Path.Combine(CurrentDurectory, "Beeper.exe"));
                    }
                    catch (Win32Exception)
                    {
                        TaskDialog couldntFindCLI = new TaskDialog();
                        couldntFindCLI.WindowTitle = ResourceManager.GetString("GUICantFindCLITitle");
                        couldntFindCLI.Content = ResourceManager.GetString("GUICantFindCLI");
                        couldntFindCLI.MainInstruction = ResourceManager.GetString("GUICantFindCLIInstruction");
                        couldntFindCLI.MainIcon = TaskDialogIcon.Error;
                        couldntFindCLI.AllowDialogCancellation = false;
                        couldntFindCLI.ShowDialog();
                    }
                }
                else if (result == runGUI)
                {
                    Config.DontAskAgain = dialog.IsVerificationChecked;
                    Config.Save();
                    RunGUI(args);
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                RunGUI(args);
            }
        }

        private static void RunGUI(string[] args)
        {
            string ToPlay = null;
            // Looks for a file within command line args
            foreach (string arg in args) // Runs through all command line arguments
            {
                if (File.Exists(arg)) // If that file exists
                {
                    ToPlay = arg; // Open it
                    break; // Break out of loop
                }
            }

            if (ToPlay != null)
            {
                try
                {
                    PlayBeeperFile(ToPlay, true);
                }
                catch (Exception ex)
                {
                    TaskDialog errorDialog = new TaskDialog();
                    errorDialog.WindowTitle = "Invalid BeeperFile";
                    errorDialog.MainIcon = TaskDialogIcon.Error;
                    errorDialog.Buttons.Add(new TaskDialogButton(ButtonType.Ok));
                    TaskDialogButton MoreDetails = new TaskDialogButton("More Details");
                    errorDialog.Buttons.Add(MoreDetails);
                    errorDialog.Content = "I couldn't load that file because it's invalid.";
                    if (errorDialog.ShowDialog() == MoreDetails)
                    {
                        string TempPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName())) + ".json";
                        File.WriteAllText(TempPath, JsonConvert.SerializeObject(ex, Formatting.Indented));
                        Process.Start(TempPath);
                        TemporaryFiles.Add(TempPath);
                    }
                    Environment.Exit(0);
                }
            }
            else
            { application.Run(startupWindow); }
        }

        // Cleanup on shutdown
        private static void Application_Exit(object sender, System.Windows.ExitEventArgs e)
        {
            foreach (string file in TemporaryFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
            foreach (string directory in TemporaryDirectories)
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch { }
            }

            Config.Save();
        }

        public static void PlayBeeperFile(string ToPlay, bool startup = false)
        {
            Tuple<BeeperFile, string, BeeperFileType> LoadedFile = Load.LoadFromFile(ToPlay);
            PreparedFile PreparedFile = Prepare.PrepareBeeperFile(LoadedFile.Item1, Config.OutputMethod, LoadedFile.Item2, ToPlay);
            MainWindow.AddRecentlyPlayedFile(LoadedFile.Item1, ToPlay);
            if (LoadedFile.Item3 == BeeperFileType.Zip)
                TemporaryDirectories.Add(LoadedFile.Item2);

            Player player = new Player(PreparedFile, LoadedFile.Item1.Metadata);
            if (startup)
            {
                application.Run(player);
            }
            else
            {
                player.Show();
                Application.Current.MainWindow = player;
                startupWindow.Close();
            }
        }

        /// <summary>
        /// When shit goes down and an exception goes unhandled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ReportError(e.ExceptionObject as Exception);
        }

        /// <summary>
        /// This method takes a lot of data about the app's current state and environment, combines and serialises it
        /// then pushes it to my server, It helps me fix issues and is much more reliable than, say an issue tracker.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static void ReportError(Exception ex)
        {
            // Gather error data
            string report = JsonConvert.SerializeObject(new ErrorReportGenerator(ex), new JsonSerializerSettings() { Formatting = Formatting.Indented });
            string FileName = "beeper-crash-" + DateTime.Now.ToString().Replace('/', '-').Replace(':', '-') + ".json";
            string FilePath = Path.Combine(Path.GetTempPath(), FileName);
            // Save exception details
            File.WriteAllText(FilePath, report);
            TemporaryFiles.Add(FilePath);
            // Attempt to report crash so I can fix it          
            if (Config.ErrorReporter.ReportCrashes)
            {
                TaskDialog errorDiag = new TaskDialog();
                errorDiag.WindowTitle = ResourceManager.GetString("CrashDialogTitle");
                errorDiag.MainInstruction = ResourceManager.GetString("CrashDialogInstruction");
                errorDiag.Content = ResourceManager.GetString("CrashDialogContent");
                errorDiag.AllowDialogCancellation = false;
                errorDiag.MainIcon = TaskDialogIcon.Error;
                errorDiag.ButtonStyle = TaskDialogButtonStyle.CommandLinks;
                TaskDialogButton sendReport = new TaskDialogButton(ResourceManager.GetString("CrashDialogSendReport"));
                TaskDialogButton dontSendReport = new TaskDialogButton(ResourceManager.GetString("CrashDialogDontSend"));
                errorDiag.Buttons.Add(sendReport);
                errorDiag.Buttons.Add(dontSendReport);
                errorDiag.Footer = $"<a href=\"details\">{ResourceManager.GetString("ShowDetails")}</a> - <a href=\"privacy\">{ResourceManager.GetString("Privacy")}</a>";
                errorDiag.EnableHyperlinks = true;
                errorDiag.HyperlinkClicked += (object sender, HyperlinkClickedEventArgs e) =>
                {
                    if (e.Href == "details")
                        Process.Start(FilePath);
                };
                if (errorDiag.Show() == sendReport)
                {
                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            Dictionary<string, string> values = new Dictionary<string, string>
                            {
                                { "Report", report },
                                { "Instance", Config.InstanceID }
                            };

                            FormUrlEncodedContent content = new FormUrlEncodedContent(values);
                            HttpResponseMessage response = client.PostAsync("https://wamwoowam.cf/apps/beeper/reporterror", content).Result;
                            if (response.IsSuccessStatusCode)
                            {
                                TaskDialog successDialog = new TaskDialog();
                                successDialog.WindowTitle = "Report sent successfully!";
                                successDialog.Content = "The error report was sent successfully!";
                                successDialog.MainIcon = TaskDialogIcon.Information;
                                successDialog.Buttons.Add(new TaskDialogButton(ButtonType.Ok));
                                successDialog.Show();
                            }
                        }
                    }
                    catch { /* Don't shout about it not working */ }
                }
            }

            Environment.Exit(0);
        }
    }
}
