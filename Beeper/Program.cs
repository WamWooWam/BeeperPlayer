using Beeper.Common.Models;
using Beeper.Properties;
using Beeper.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using Beeper.Common;
using System.Diagnostics;
using WamWooWam.Core;

namespace Beeper
{
    class Program
    {
        public static AppState AppState = new AppState(); // Creates a new app state
        public static Config Config = new Config(); // Manages configuration
        public static string CurrentDurectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); // App data directory

        static void Main(string[] args)
        {
            if (args.Contains("reset"))
            {
                Settings.Default.Reset();
                Settings.Default.Save();
                Settings.Default.Reload();
                ConsolePlus.WriteLine("Settings Reset", ConsoleColor.Green);
                Console.WriteLine();
            }

            Config.ErrorReporter = new ErrorReporter(); // Manages error reporting settings
            Config.FirstRun = true; // Manages first run
            ConsolePlus.Debug = Config.Debug; // Manages debug

            if (!args.Contains("vsdebug"))
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException; // Sets exception handler

            if (File.Exists(Path.Combine(CurrentDurectory, "config.json"))) // Checks for configuration file
            {
                try
                {
                    // Loads configuration
                    ConsolePlus.WriteLine("Loading config...");
                    Config = JsonConvert.DeserializeObject<Config>(
                        File.ReadAllText(Path.Combine(CurrentDurectory, "config.json")));
                }
                catch
                {
                    // Creates new configuration
                    ConsolePlus.WriteLine("Unable to load config, creating new.", ConsoleColor.Red);
                    File.WriteAllText(Path.Combine(CurrentDurectory, "config.json"),
                        JsonConvert.SerializeObject(Config, Formatting.Indented));
                }
            }
            else
            {
                try
                {
                    // Creates new configuration
                    ConsolePlus.WriteLine("Unable to load config, creating new.", ConsoleColor.Red);
                    File.WriteAllText(Path.Combine(CurrentDurectory, "config.json"),
                        JsonConvert.SerializeObject(Config, Formatting.Indented));
                }
                catch
                {
                    // Relaunches app as administrator
                    // Helps if app is installed to Program Files and it can't access config.json
                    ProcessStartInfo info = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location);
                    info.UseShellExecute = true;
                    info.Verb = "runas";
                    info.Arguments = "firstrun";
                    Process.Start(info);
                    Environment.Exit(0);
                }
            }

            // Sets app Instance ID
            // Used for disgnostics & usage data
            if (Config.InstanceID == null)
            {
                Random random = new Random();
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";
                Config.InstanceID = new string(Enumerable.Repeat(chars, 12)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
            }

            if (Config.FirstRun)
            {
                if (args.Contains("firstrun"))
                {
                    // Statistics information, needed for legal reasons
                    Console.Clear();
                    ConsolePlus.WriteHeading("Beeper Statistics");
                    ConsolePlus.WriteLine("Hello! Welcome to BeeperPlayer! Thanks for downloading!");
                    ConsolePlus.WriteLine("For legal reasons, I need to tell you some stuff before you use the app.");
                    ConsolePlus.WriteLine("With your permission, if the app crashes, it will send some envronment and usage data, in order to make development faster, and hopefully fix the issue you've encountered.");
                    ConsolePlus.WriteLine("This data includes:");
                    ConsolePlus.WriteLine(" - Command Line Arguments");
                    ConsolePlus.WriteLine(" - Environment Variables");
                    ConsolePlus.WriteLine(" - Currently opened files");
                    ConsolePlus.WriteLine(" - App configuration");
                    Console.WriteLine();
                    Console.WriteLine();
                    while (true)
                    {
                        ConsolePlus.Write("Type Y/N to agree/disagree: ");
                        var key = Console.ReadKey();
                        Console.WriteLine();
                        if (key.KeyChar == 'y')
                        {
                            ConsolePlus.WriteLine("Thanks for allowing stats to be sent. Lets get on with this then, shal we?");
                            Config.ErrorReporter.ReportCrashes = true;
                            break;
                        }
                        if (key.KeyChar == 'n')
                        {
                            ConsolePlus.WriteLine($"That's perfectly fine. I respect your opinion, {Environment.UserName}! Let's do this!");
                            Config.ErrorReporter.ReportCrashes = false;
                            break;
                        }
                        ConsolePlus.WriteLine("I said, Y/N you twat!");
                    }
                    Config.FirstRun = false;
                    File.WriteAllText(Path.Combine(CurrentDurectory, "config.json"),
                        JsonConvert.SerializeObject(Config, Formatting.Indented));
                    ConsolePlus.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
                else
                {
                    ProcessStartInfo info = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location);
                    info.UseShellExecute = true;
                    info.Verb = "runas";
                    info.Arguments = "firstrun";
                    Process.Start(info);
                    Environment.Exit(0);
                }
            }

            // Saves command line arguments
            AppState.CommandLineArgs = args;
            if (args.Contains("debug") || Config.Debug)
            {
                Config.Debug = true;
                ConsolePlus.Debug = true;
                Config.Save(CurrentDurectory);
                ConsolePlus.WriteLine($"     --- DEBUG MODE ENABLED --- ", ConsoleColor.Red);
            }
                CLI.RunCLI(args);
   
        }

        /// <summary>
        /// When shit goes down and an exception goes unhandled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            await ReportError(e.ExceptionObject as Exception);
        }

        /// <summary>
        /// This method takes a lot of data about the app's current state and environment, combines and serialises it
        /// then pushes it to my server, It helps me fix issues and is much more reliable than, say an issue tracker.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static async Task ReportError(Exception ex)
        {
            // Gather error data
            string report = JsonConvert.SerializeObject(new ErrorReportGenerator(ex), new JsonSerializerSettings() { Formatting = Formatting.Indented });
            string FileName = "beeper-crash-" + DateTime.Now.ToString().Replace('/', '-').Replace(':', '-') + ".json";
            string FilePath = Path.Combine(CurrentDurectory, @"crash", FileName);
            // Check for and create a directory for crash dumps.
            if (!Directory.Exists(Path.Combine(CurrentDurectory, @"crash")))
                Directory.CreateDirectory(Path.Combine(CurrentDurectory, @"crash"));
            // Save exception details
            File.WriteAllText(FilePath, report);
            // Attempt to report crash so I can fix it          
            if (Config.ErrorReporter.ReportCrashes)
            {
                Console.Clear();
                ConsolePlus.WriteHeading("BeeperPlayer has Crashed!", colour: ConsoleColor.Red);
                ConsolePlus.WriteLine("I'm sorry! It looks like I've crashed!");
                ConsolePlus.WriteLine("If you let me, I'll send an error report to help me sort things out.");
                ConsolePlus.WriteLine("Little to no personal info is send and it's all sent encrypted over HTTPS.");
                Console.WriteLine();
                bool IsReporting = false;
                while (true)
                {
                    ConsolePlus.Write("Do you want to send an error report? Press V to view what will be sent. (Y/N/V): ");
                    var key = Console.ReadKey();
                    Console.WriteLine();
                    Console.WriteLine();
                    if (key.KeyChar == 'y')
                    {
                        ConsolePlus.WriteLine("Thank you! With this info, I can properly diagnose this issue and, fingers crossed, sort it out!");
                        IsReporting = true;
                        break;
                    }
                    else if (key.KeyChar == 'n')
                    {
                        ConsolePlus.WriteLine($"That's perfectly fine. I respect your opinion, {Environment.UserName}! Goodbye!");
                        IsReporting = false;
                        break;
                    }
                    else if (key.KeyChar == 'v')
                        Process.Start(FilePath);
                    else
                        ConsolePlus.WriteLine("I said, Y/N you twat! Press V if you want more info on what's being sent.");
                }
                if (IsReporting)
                {
                    Console.WriteLine();
                    try
                    {
                        ConsolePlus.WriteLine("Sending error report...");
                        using (var client = new HttpClient())
                        {
                            var values = new Dictionary<string, string>
                            {
                                { "Report", report },
                                { "Instance", Config.InstanceID }
                            };

                            var content = new FormUrlEncodedContent(values);
                            var response = await client.PostAsync("https://wamwoowam.cf/apps/beeper/reporterror", content);
                        }
                        ConsolePlus.WriteLine("Error report sent!");
                    }
                    catch { /* Don't shout about it not working */ }
                }
            }
            Console.WriteLine();
            ConsolePlus.Write("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
