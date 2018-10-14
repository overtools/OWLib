using System.ComponentModel;
using TankView.Properties;
using TACTLib.Client.HandlerArgs;

namespace TankView.ViewModel {
    public class AppSettings : INotifyPropertyChanged {
        private bool _darkMode = Settings.Default.DarkMode;


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

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
