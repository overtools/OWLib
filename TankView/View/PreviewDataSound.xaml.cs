using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace TankView.View {
    public partial class PreviewDataSound : UserControl, IDisposable {
        private WaveOutEvent outputDevice;
        private VorbisWaveReader vorbis;

        public PreviewDataSound() {
            InitializeComponent();
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
    }
}
