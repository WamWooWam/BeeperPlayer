using Beeper.Common.Models;
using Beeper.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Beeper.Common;
using WamWooWam.Core;
using System.Diagnostics;

namespace Beeper.UI
{
    /// <summary>
    /// The Command Line Interface for the player.
    /// TODO: Multilingual support.
    /// </summary>
    public static class CLI
    {
        // Gets the console's launch colour, resets on close.
        static ConsoleColor LaunchColour = Console.ForegroundColor;

        public static void RunCLI(string[] args)
        {
            Program.AppState.BasicState = BasicState.PreLoading; // Sets the app's basic state, used for crash reports

            Console.Clear(); // Clears console
            Console.Title = "BeeperPlayer"; // Sets console title
            Console.ForegroundColor = ConsoleColor.White; // Sets console colour
            Console.WriteLine(); // Writes a new line to the console
            ConsolePlus.WriteLine("---- BeeperPlayer Version 1.0 Rev. 1 ----", ConsoleColor.Yellow); // Writes a header.
            Console.WriteLine();
            if (args.Contains("create"))
            {
                // Manages creation of a new file, makes composition easier.
                Program.AppState.BasicState = BasicState.Scaffolding;
                ConsolePlus.WriteLine("Scaffolding...");
                BeeperFile NewFile = BeeperFile.Create();
                var FileString = JsonConvert.SerializeObject(NewFile, Formatting.Indented);
                ConsolePlus.Write(FileString);
                Console.WriteLine();
                ConsolePlus.Write("Type a filename: ");
                var FileName = Console.ReadLine();
                File.WriteAllText(FileName, FileString); // TODO: More input valiation
                ConsolePlus.WriteLine("A new BeeperFile has been writen to 'newbeep.beep'.");
                Console.ReadKey();
            }
            else if (args.Contains("help"))
            {
                // Writes help to the console
                ConsolePlus.WriteHeading("Help", false);
                ConsolePlus.WriteSubHeading("Command Line Usage", " Basic usage: 'beeper [path] [arguments]'");
                ConsolePlus.WriteLine(" create  : Creates a new BeeperFile called 'newbeep.beep'");
                ConsolePlus.WriteLine(" debug   : Shows debug messages and parser errors, useful if composing.");
                ConsolePlus.WriteLine(" reset   : Resets all settings.");
                Console.WriteLine();
                ConsolePlus.WriteLine(" Arguments must be in lowercase.", ConsoleColor.Red);
                ConsolePlus.WriteLine(" Arguments can be specified any order, but are parsed in the following order:", ConsoleColor.Green);
                ConsolePlus.WriteLine(" reset, debug, create, help, [file to open]");
            }
            else
            {
                Program.AppState.BasicState = BasicState.SearchingForFile;
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
                    // Loads file from JSON
                    Program.AppState.FileString = Path.GetDirectoryName(ToPlay);
                    Program.AppState.BasicState = BasicState.LoadingFile;
                    ConsolePlus.WriteLine($@"Loading file ""{ToPlay}""...");
                    var LoadedFile = Load.LoadFromFile(ToPlay);
                    Program.AppState.LoadedFile = LoadedFile.Item1;
                    Program.AppState.ExtractPath = LoadedFile.Item2;
                    var FileToPlay = Prepare.PrepareBeeperFile(Program.AppState.LoadedFile, Program.AppState.Output, Program.AppState.ExtractPath);
                    ConsolePlus.WriteLine($@"Loaded BeeperFile from ""{ToPlay}""!");
                    // Manages file upgrades
                    // TODO: Incremental changes
                    if (Program.AppState.LoadedFile.Metadata.Version == null || Program.AppState.LoadedFile.Metadata.Version < 3)
                    {
                        ConsolePlus.WriteHeading("File Needs Upgrade", colour: ConsoleColor.Red);
                        ConsolePlus.WriteLine("The file you're trying to load is a bit old and is in need of a good update!");
                        ConsolePlus.WriteLine("UPGRADING WILL IRREVERSIBLY MODIFY THE FILE.", ConsoleColor.Red);
                        ConsolePlus.WriteLine("Don't mean to scare you, but it will!");
                        ConsolePlus.WriteLine("Press enter to upgrade the file, or any key to exit.");
                        if (Console.ReadKey().Key == ConsoleKey.Enter)
                        {
                            Program.AppState.LoadedFile.Metadata.Version = 2;
                            if (File.Exists(ToPlay + ".bak"))
                                File.Delete(ToPlay + ".bak");
                            File.Move(ToPlay, ToPlay + ".bak");
                            File.WriteAllText(ToPlay, JsonConvert.SerializeObject(Program.AppState.LoadedFile, Formatting.Indented));
                        }
                        else
                        {
                            Environment.Exit(0);
                            Console.ForegroundColor = LaunchColour;
                        }
                    }

                    // Manages output method
                    if (args.Contains("asio"))
                        Program.AppState.Output = Output.Asio;
                    else if (args.Contains("directsound") || args.Contains("ds"))
                        Program.AppState.Output = Output.DirectSound;
                    else if (args.Contains("file"))
                        Program.AppState.Output = Output.File;
                    else if (args.Contains("wasapi"))
                        Program.AppState.Output = Output.Wasapi;
                    else if (args.Contains("waveout") || args.Contains("wo"))
                        Program.AppState.Output = Output.WaveOut;
                    else
                        Program.AppState.Output = Output.DirectSound;

                    // Outputs file information
                    ConsolePlus.WriteHeading("File Information");
                    ConsolePlus.WriteLine($"Song Title         : {Program.AppState.LoadedFile.Metadata.Title}");
                    ConsolePlus.WriteLine($"Song Artist        : {Program.AppState.LoadedFile.Metadata.Artist}");
                    ConsolePlus.WriteLine($"Composer           : {Program.AppState.LoadedFile.Metadata.FileCreator}");
                    ConsolePlus.WriteLine($"Total Beeps        : {Program.AppState.LoadedFile.TotalBeeps}");
                    ConsolePlus.WriteLine($"Duration           : {Program.AppState.LoadedFile.Duration.ToString(@"hh\:mm\:ss")}");
                    // Sets console title
                    Console.Title = $"BeeperPlayer | {Program.AppState.LoadedFile.Metadata.Title} - {Program.AppState.LoadedFile.Metadata.Artist}";
                    Console.WriteLine();
                    if (Program.AppState.Output != Output.File)
                    {
                        Console.WriteLine();
                        // Waits to play
                        ConsolePlus.WriteLine("Press any key to play...", ConsoleColor.Green);
                        Console.WriteLine();
                        Console.ReadKey();
                        Play.PlayPreparedBeeperFile(Program.AppState.LoadedFile, FileToPlay); // Plays the beeper file
                    }
                    else
                    {
                        ConsolePlus.WriteLine("Enter an output filename");
                        Console.Write(" ");
                        var OutputFile = Console.ReadLine();
                        Console.WriteLine();
                        if (String.IsNullOrEmpty(Path.GetExtension(OutputFile)))
                            OutputFile += ".wav";
                        bool Overwrite = true;
                        if (File.Exists(OutputFile))
                        {
                            ConsolePlus.WriteLine($@"The file ""{OutputFile}"" already exists. Do you want to overwrite it?");
                            while (true)
                            {
                                var key = Console.ReadKey();
                                Console.WriteLine();
                                if (key.KeyChar == 'y')
                                {
                                    Overwrite = true;
                                    break;
                                }
                                else if (key.KeyChar == 'n')
                                {
                                    Overwrite = false;
                                    break;
                                }
                                else
                                    ConsolePlus.WriteLine("I said, Y/N you twat!");
                            }
                        }
                        if (Overwrite)
                        {
                            File.Delete(OutputFile);
                            Export.ExportBeeperFile(FileToPlay, OutputFile);
                        }
                        else
                        {
                            ConsolePlus.Write("Press any key to exit...", ConsoleColor.Green);
                            Console.ReadKey();
                            Console.ForegroundColor = LaunchColour;
                            Environment.Exit(0);
                        }
                    }
                }
                else
                {
                    // Manages no file specified
                    ConsolePlus.WriteLine("You didn't tell me to open a file or the file you told me about doesn't actually exist!", ConsoleColor.Red);
                    ConsolePlus.WriteLine("I'm pretty useless without a file to open, type a path to a file and I'll open it, Press enter to exit.");
                    ConsolePlus.WriteLine(@"If you need me to help, type ""help"" now.");
                    Console.Write(" ");
                    string FileToOpen = Console.ReadLine();
                    if (!string.IsNullOrEmpty(FileToOpen))
                    {
                        ConsolePlus.WriteLine("OK! Let's try this then!");
                        var list = args.ToList();
                        list.Add(FileToOpen);
                        RunCLI(list.ToArray());
                    }
                    else
                    {
                        ConsolePlus.WriteLine("Alrighty then, see you later!");
                    }
                }
            }
            Console.WriteLine();
            ConsolePlus.Write("Press any key to exit...", ConsoleColor.Green);
            Console.ReadKey();
            Console.ForegroundColor = LaunchColour;
            Environment.Exit(0);
        }
    }
}
