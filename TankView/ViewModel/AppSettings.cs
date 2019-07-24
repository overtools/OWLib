using System.ComponentModel;
using TankView.Properties;

namespace TankView.ViewModel {
    public class AppSettings : INotifyPropertyChanged {
        private bool _darkMode = Settings.Default.DarkMode;
        private bool _convertSounds = Settings.Default.ConvertSounds;


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
        
        public bool EnableConvertSounds {
            get => _convertSounds;
            set {
                _convertSounds = value;
                Settings.Default.ConvertSounds = value;
                Settings.Default.Save();
                NotifyPropertyChanged(nameof(EnableConvertSounds));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
