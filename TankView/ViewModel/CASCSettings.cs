using System;
using System.ComponentModel;
using TankView.Properties;

namespace TankView.ViewModel {
    public class CASCSettings : INotifyPropertyChanged {
        private bool _apm = Settings.Default.CacheAPM;

        public bool APM {
            get { return _apm; }
            set {
                _apm = value;
                Settings.Default.CacheAPM = value;
                Settings.Default.Save();
                MainWindow.ClientArgs.Tank.CacheAPM = value;
                NotifyPropertyChanged(nameof(APM));
            }
        }
        
        public CASCSettings() {
            MainWindow.ClientArgs.Tank.CacheAPM = APM;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
