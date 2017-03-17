using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beeper.Common.Models
{
    public class Config
    {
        public string InstanceID { get; set; }
        public bool FirstRun { get; set; }
        public Accuracy TimingAccuracy { get; set; }
        public ErrorReporter ErrorReporter { get; set; }
    }

    public class Accuracy
    {
        public Accuracy()
        {
            EnableOnTheFlyAdjustment = true;
            EnableCache = true;
            AccuracyThreshold = 5;
        }

        /// <summary>
        /// Enables/disables on the fly timing adjustments, improves accuracy
        /// of timings by modifying how long sounds play and pause for
        /// </summary>
        public bool EnableOnTheFlyAdjustment { get; set; }
        /// <summary>
        /// Enables/disables caching of adjustments.
        /// </summary>
        public bool EnableCache { get; set; }
        /// <summary>
        /// The number of milliseconds larger or smaller a time can be
        /// before it is considered inaccurate
        /// </summary>
        public int AccuracyThreshold { get; set; }
    }

    public class AppState
    {
        public string[] CommandLineArgs { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public BasicState BasicState { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Output Output { get; set; }
        public string FileString { get; set; }
        public BeeperFile LoadedFile { get; set; }
        public bool IsCaching { get; set; }
    }

    public class EnvironmentInfo
    {
        public EnvironmentInfo()
        {
            CommandLine = Environment.CommandLine;
            CommandLineArgs = Environment.GetCommandLineArgs();
            EnvironmentVariables = Environment.GetEnvironmentVariables();
            Is64bit = Environment.Is64BitProcess;
            OS = Environment.OSVersion;
            ProcessorCount = Environment.ProcessorCount;
            FrameworkVersion = Environment.Version;
        }

        public string CommandLine { get; set; }
        public string[] CommandLineArgs { get; set; }
        public System.Collections.IDictionary EnvironmentVariables { get; set; }
        public bool Is64bit { get; set; }
        public OperatingSystem OS { get; set; }
        public int ProcessorCount { get; set; }
        public Version FrameworkVersion { get; set; }
    }

    /// <summary>
    /// The very basic state of the app. A general run-down of what it's doing.
    /// </summary>
    public enum BasicState { PreLoading, SearchingForFile, LoadingFile, SpawningThreads, PreparingThreads, PrePlay, Playing, PostPlay, Scaffolding, UpgradingFile }

    public enum Output { WaveOut, DirectSound, Asio, Wasapi, File }

    /// <summary>
    /// Configuration for error reporter
    /// </summary>
    public class ErrorReporter
    {
        public ErrorReporter()
        {
            ReportCrashes = true;
            IncludeConfig = true;
            IncludeEnvironmentInfo = true;
            IncludeLoadedFile = true;
            UseHttps = true;
            ReportHostname = "wamwoowam.cf";
            ReportPath = "/Apps/Error/Report";
        }

        public bool ReportCrashes { get; set; }
        public bool IncludeEnvironmentInfo { get; set; }
        public bool IncludeLoadedFile { get; set; }
        public bool IncludeConfig { get; set; }
        public string ReportHostname { get; set; }
        public string ReportPath { get; set; }
        public bool UseHttps { get; set; }
    }

    public class ErrorReport
    {
        public object Exception { get; set; }
        public AppState AppState { get; set; }
        public Config Config { get; set; }
        public PlaySynchroniser PlaySync { get; set; }
        public EnvironmentInfo EnvironmentInfo { get; set; }
    }
}
