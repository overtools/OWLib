using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using TankView.ViewModel;
using Timer = System.Timers.Timer;

namespace TankView.View {
    public partial class PreviewDataSound : UserControl, INotifyPropertyChanged, IDisposable {
        public SynchronizationContext ViewContext { get; }
        private WaveOutEvent outputDevice;
        private VorbisWaveReader vorbis;
        public ProgressInfo ProgressInfo { get; set; }
        private BackgroundWorker _worker;
        private Timer _timer;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public PreviewDataSound() {
            InitializeComponent();
            ViewContext = SynchronizationContext.Current;
            ProgressInfo = new ProgressInfo();
        }

        public void Dispose() {
            CleanUp();
        }

        public void CreateProgressWorker() {
            _timer = new Timer { Interval = 120 };
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();

            _worker = new BackgroundWorker { WorkerReportsProgress = true };
            _worker.ProgressChanged += UpdateProgress;
        }

        public void SetAudio(Stream data) {
            CleanUp();
            CreateProgressWorker();

            try {
                outputDevice = new WaveOutEvent();
                vorbis = new VorbisWaveReader(data);
                outputDevice.Volume = 0.8f;
                outputDevice.Init(vorbis);
                _worker.ReportProgress(0, $"00:00/{new DateTime(vorbis.TotalTime.Ticks):mm:ss}");
            } catch (Exception ex) {
                Debugger.Log(0, "[TankView.Sound.SetAudio]", $"Error setting audio! {ex.Message}\n");
                // ignored
            }
        }

        private void CleanUp() {
            if (_worker != null) {
                _worker.Dispose();
                _worker = null;
            }

            if (_timer != null) {
                _timer.Dispose();
                _timer = null;
            }

            if (outputDevice != null) {
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
            }

            if (vorbis != null) {
                vorbis.Dispose();
                vorbis = null;
            }
        }

        public void Play(object sender, RoutedEventArgs e) {
            if (outputDevice == null || outputDevice.PlaybackState == PlaybackState.Playing) {
                return;
            }

            if (vorbis == null) {
                _worker.ReportProgress(0, "An error occured playing this sound");
                return;
            }

            try {
                if (outputDevice.PlaybackState == PlaybackState.Stopped) {
                    vorbis.Position = 0;
                }

                outputDevice.Play();
            } catch (Exception ex) {
                _worker.ReportProgress(0, "An error occured playing this sound");
                Debugger.Log(0, "[TankView.Sound.Play]", $"Error setting audio! {ex.Message}\n");
            }
        }

        private void Stop(object sender, RoutedEventArgs e) {
            if (outputDevice == null || outputDevice.PlaybackState == PlaybackState.Stopped) {
                return;
            }

            outputDevice.Stop();
            vorbis.Position = 0;
        }

        private void Pause(object sender, RoutedEventArgs e) {
            if (outputDevice == null || outputDevice.PlaybackState == PlaybackState.Paused) {
                return;
            }

            outputDevice.Pause();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            UpdateProgressBar();
        }

        private void UpdateProgressBar() {
            if (outputDevice == null) {
                _worker.ReportProgress(0, "");
            } else if (outputDevice.PlaybackState == PlaybackState.Playing) {
                var progress = (int) Math.Round(((float) vorbis.CurrentTime.Ticks / (float) vorbis.TotalTime.Ticks) * 1000);
                _worker.ReportProgress(progress, $"{new DateTime(vorbis.CurrentTime.Ticks):mm:ss}/{new DateTime(vorbis.TotalTime.Ticks):mm:ss}");
            }
        }

        private void UpdateProgress(object sender, ProgressChangedEventArgs @event) {
            ViewContext.Send(x => {
                if (!(x is ProgressChangedEventArgs evt)) return;
                if (evt.UserState != null && evt.UserState is string state) {
                    ProgressInfo.State = state;
                }

                ProgressInfo.Percentage = evt.ProgressPercentage;
            }, @event);

            NotifyPropertyChanged(nameof(ProgressInfo));
        }
    }
}
