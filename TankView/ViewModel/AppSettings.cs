using System.ComponentModel;
using TankView.Properties;

namespace TankView.ViewModel {
    public class AppSettings : INotifyPropertyChanged {
        private bool _darkMode = Settings.Default.DarkMode;
        private int _audioVolume = Settings.Default.AudioVolume;

        public bool EnableDarkMode {
            get => _darkMode;
            set {
                _darkMode = value;
                Settings.Default.DarkMode = value;
                Settings.Default.Save();
                (App.Current as App)?.SetDarkMode(EnableDarkMode);
                NotifyPropertyChanged(nameof(EnableDarkMode));
            }
        }

        public int AudioVolume {
            get => _audioVolume;
            set {
                _audioVolume = value;
                Settings.Default.AudioVolume = value;
                Settings.Default.Save();
                NotifyPropertyChanged(nameof(AudioVolume));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
