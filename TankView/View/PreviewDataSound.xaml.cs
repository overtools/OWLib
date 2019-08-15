using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DataTool.WPF;
using TankView.ViewModel;

namespace TankView.View {
    public partial class PreviewDataSound : UserControl, INotifyPropertyChanged, IDisposable {
        public SynchronizationContext ViewContext { get; }
        private WaveOutEvent outputDevice;
        private VorbisWaveReader vorbis;
        public ProgressInfo ProgressInfo { get; set; }
        private ProgressWorker _progressWorker = new ProgressWorker();
        
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public PreviewDataSound() {
            InitializeComponent();
            ViewContext = SynchronizationContext.Current;
            ProgressInfo = new ProgressInfo();
            
            _progressWorker.OnProgress += UpdateProgress;
            
            var timer = new System.Timers.Timer();
            timer.Interval = 300;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        public void Dispose() {
            CleanUp();
        }

        public void SetAudio(Stream data) {
            CleanUp();
            try {
                outputDevice = new WaveOutEvent();
                vorbis = new VorbisWaveReader(data);
                outputDevice.Volume = 1.0f;
                outputDevice.Init(vorbis);
                _progressWorker.ReportProgress(0, $"00:00/{new DateTime(vorbis.TotalTime.Ticks):mm:ss}");
            } catch (Exception ex) {
                Debugger.Log(0, "[TankView.Sound.SetAudio]", $"Error setting audio! {ex.Message}\n");
                // ignored
            }
        }

        private void OnStopped(object sender, StoppedEventArgs e) {
            CleanUp();
        }

        private void CleanUp() {
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

            try {
                outputDevice.Play();
            } catch (Exception ex) {
                Debugger.Log(0, "[TankView.Sound.Play]", $"Error setting audio! {ex.Message}\n");
                // ignored
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
        
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdateProgressBar();
        }
        
        private void UpdateProgressBar()
        {
            if (outputDevice.PlaybackState == PlaybackState.Playing) {
                var progress = (int) Math.Round(((float) vorbis.CurrentTime.Ticks / (float) vorbis.TotalTime.Ticks) * 1000);
                _progressWorker.ReportProgress(progress, $"{new DateTime(vorbis.CurrentTime.Ticks):mm:ss}/{new DateTime(vorbis.TotalTime.Ticks):mm:ss}");
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
