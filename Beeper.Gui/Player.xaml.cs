using Beeper.Common.Models;
using Beeper.Gui.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using NAudio.Wave;
using System.IO;
using Beeper.Common;
using System.Threading;
using System.ComponentModel;
using NAudio.Wave.SampleProviders;

namespace Beeper.Gui
{
    /// <summary>
    /// Interaction logic for Player.xaml
    /// </summary>
    public partial class Player : Window
    {
        BeeperMeta Metadata; // The file's metadata
        PreparedFile FileToPlay; // The file being played
        TimeSpan Duration; // The duration of the file being played
        WaveFileReader reader; // Wave Reader, handles duration and scrubbing
        VolumeSampleProvider provider; // A volume provider to handle volume
        IWavePlayer player; // A generic IWavePlayer (Type configurable)
        PlaybackState State => player.PlaybackState; // the state of the player
        int TargetFPS = 30; // Target UI framerate
        Thread backgroundUpdateThread; // Updates on screen UI

        /// <summary>
        /// Main entry point for window. Handles basic setup.
        /// </summary>
        /// <param name="fileToPlay">The file to play (pre-prepared)</param>
        /// <param name="metadata">The metadata of the file to be played.</param>
        public Player(PreparedFile fileToPlay, BeeperMeta metadata)
        {
            InitializeComponent(); // Initialises the window
            IsEnabled = false; // Disables UI until ready to play/playing

            Metadata = metadata; // Assign paramaters to local variables.
            FileToPlay = fileToPlay;

            // Initialises images
            playImage.Source = ImageResources.GetImageResource("playButton", 64);
            stopImage.Source = ImageResources.GetImageResource("stopButton", 48);
            volumeHighImage.Source = ImageResources.GetImageResource("volumeHigh", 24);
            volumeLowImage.Source = ImageResources.GetImageResource("volumeLow", 24);

            // Sets misc. UI text.
            trackTitle.Text = Metadata?.Title;
            trackAlbum.Text = Metadata?.Album;
            trackComposer.Text = Metadata?.Composer;

            Title += ": " + metadata?.Title + " - " + metadata?.Album;
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BackgroundWorker preparePlayWorker = new BackgroundWorker();
            preparePlayWorker.DoWork += PreparePlayWorker;
            preparePlayWorker.RunWorkerCompleted += PreparePlayWorker_RunWorkerCompleted;
            preparePlayWorker.RunWorkerAsync();

            if (Metadata.AlbumArt != null)
            {
                loadingLabel.Content = "Loading...";
                BackgroundWorker prepareAlbumArtWorker = new BackgroundWorker();
                prepareAlbumArtWorker.DoWork += PrepareAlbumArtWorker_DoWork;
                prepareAlbumArtWorker.RunWorkerCompleted += PrepareAlbumArtWorker_RunWorkerCompleted;
                prepareAlbumArtWorker.RunWorkerAsync();
            }
        }

        private void PreparePlayWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            playBar.Maximum = Duration.Ticks;
            Play();
        }

        private void BackgroundUpdateThread()
        {
            try
            {
                while (Dispatcher.Invoke(() => { return IsActive; }))
                {
                    while (State == PlaybackState.Playing)
                    {
                        try
                        {
                            playBar.Dispatcher.Invoke(() => playBar.Value = reader.CurrentTime.Ticks);
                            elapsedTime.Dispatcher.Invoke(() => elapsedTime.Text = reader.CurrentTime.ToString(@"mm\:ss"));
                            remainingTime.Dispatcher.Invoke(() => remainingTime.Text = "-" + (Duration - reader.CurrentTime).ToString(@"mm\:ss"));
                        }
                        catch
                        {
                            Stop();
                            break;
                        }
                        Thread.Sleep(1000 / TargetFPS);
                    }
                    Thread.Sleep(100);
                }
            }
            catch
            {
                Stop();
            }
        }

        private void PrepareAlbumArtWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loadingLabel.Content = "";
        }

        private async void PrepareAlbumArtWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (Metadata.AlbumArt != null)
            {
                try
                {
                    string CachePath = Path.Combine(Program.CurrentDurectory, @"Resources", "Cache", "Covers", FileToPlay.SHA256) + ".png";
                    Console.WriteLine(CachePath);
                    if (!File.Exists(CachePath))
                    {
                        HttpClient client = new HttpClient();
                        Stream str = await client.GetStreamAsync(Metadata.AlbumArt);
                        using (FileStream fileStream = File.Create(CachePath))
                        {
                            str.CopyTo(fileStream);
                        }
                        albumArt.Dispatcher.Invoke(() => albumArt.Source = ImageResources.GetImageFromFile(CachePath));
                    }
                    else
                    {
                        using (FileStream file = File.OpenRead(CachePath))
                        {
                            albumArt.Dispatcher.Invoke(() => albumArt.Source = ImageResources.GetImageFromFile(CachePath));
                        }
                    }
                }
                catch
                {
                    albumArt.Dispatcher.Invoke(() => albumArt.Source = ImageResources.GetImageResource("mediaFile", 256));
                }
            }
            else
            {
                albumArt.Dispatcher.Invoke(() => albumArt.Source = ImageResources.GetImageResource("mediaFile", 256));
            }
        }

        private void PreparePlayWorker(object sender, DoWorkEventArgs e)
        {
            backgroundUpdateThread = new Thread(() => BackgroundUpdateThread()); // Initialises background threads

            // Initialises a new player based on the player passed within the file.
            player = (IWavePlayer)Activator.CreateInstance(FileToPlay.Player.GetType());
            player.PlaybackStopped += Player_PlaybackStopped;
            FileToPlay.Player.Dispose(); // Disposes of the player to free up memory.       

            reader = new WaveFileReader(Export.ExportBeeperFile(FileToPlay, Padding: false)); // Initialise a reader
            Duration = reader.TotalTime; // Set duration to reader duration

            MeteringSampleProvider meter = new MeteringSampleProvider(reader.ToSampleProvider());
            meter.StreamVolume += Meter_StreamVolume;
            meter.SamplesPerNotification = meter.WaveFormat.SampleRate / 60;

            provider = new VolumeSampleProvider(meter); // Initialise a volume provider
            provider.Volume = Program.Config.Volume; // Set volume based on config file.

            player.Init(provider); // Initialise new player with volume.

            volumeSlider.Dispatcher.Invoke(() => volumeSlider.Value = Program.Config.Volume);
            playBar.Dispatcher.Invoke(() => playBar.IsIndeterminate = false);
            remainingTime.Dispatcher.Invoke(() => remainingTime.Text = "-" + Duration.ToString(@"mm\:ss"));
            Dispatcher.Invoke(() => IsEnabled = true);
        }

        private void Player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            try
            {
                playImage.Dispatcher.Invoke(() => playImage.Source = ImageResources.GetImageResource("playButton", 64));
                stopButton.Dispatcher.Invoke(() => stopButton.IsEnabled = false);
                playBar.Dispatcher.Invoke(() => playBar.Value = reader.CurrentTime.Ticks);
                elapsedTime.Dispatcher.Invoke(() => elapsedTime.Text = reader.CurrentTime.ToString(@"mm\:ss"));
                remainingTime.Dispatcher.Invoke(() => remainingTime.Text = "-" + (Duration - reader.CurrentTime).ToString(@"mm\:ss"));
            }
            catch (TaskCanceledException)
            { /* Fucking task canceled exceptions... */ }
        }

        private void Meter_StreamVolume(object sender, StreamVolumeEventArgs e)
        {
            try
            {
                volumeBarLeft.Dispatcher.Invoke(() => volumeBarLeft.Value = e.MaxSampleValues[0]);
                volumeBarRight.Dispatcher.Invoke(() => volumeBarRight.Value = e.MaxSampleValues[1]);
            }
            catch (TaskCanceledException) { }
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            if (State == PlaybackState.Playing)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        #region Scrubbing
        private void playBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TargetFPS = 120;
            SetPlayPosition(sender as ProgressBar, e);
        }

        private void playBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                SetPlayPosition(sender as ProgressBar, e);
            }
        }

        private void playBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TargetFPS = 30;
            SetPlayPosition(sender as ProgressBar, e);
        }

        #endregion

        private void Window_Closing(object sender, CancelEventArgs e)
        {

        }

        #region Actions
        private void Play()
        {
            playImage.Dispatcher.Invoke(() => playImage.Source = ImageResources.GetImageResource("pauseButton", 64));
            stopButton.Dispatcher.Invoke(() => stopButton.IsEnabled = true);
            if (player.PlaybackState == PlaybackState.Stopped)
                reader.Seek(0, SeekOrigin.Begin);
            player.Play();
            if (backgroundUpdateThread == null || !backgroundUpdateThread.IsAlive)
            {
                backgroundUpdateThread = new Thread(() => BackgroundUpdateThread());
                backgroundUpdateThread.Start();
            }
        }

        private void Pause()
        {
            playImage.Dispatcher.Invoke(() => playImage.Source = ImageResources.GetImageResource("playButton", 64));
            player.Pause();
        }

        private void Stop()
        {
            if (player.PlaybackState != PlaybackState.Paused)
                player.Pause();
            reader.Seek(0, SeekOrigin.Begin);
            Player_PlaybackStopped(null, null);
        }

        private void SetPlayPosition(ProgressBar sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(sender);
            double percentage = pos.X / sender.ActualWidth;
            reader.CurrentTime = TimeSpan.FromMilliseconds(reader.TotalTime.TotalMilliseconds * percentage);
            if (State != PlaybackState.Playing)
                Play();
        }
        #endregion

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (provider != null)
            {
                provider.Volume = (float)e.NewValue;
            }
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Program.Config.Volume = provider.Volume;
            Program.Config.Save();
            Environment.Exit(0); // Fuck WASAPI;
        }
    }
}
