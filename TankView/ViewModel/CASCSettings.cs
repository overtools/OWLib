using System.ComponentModel;
using TankView.Properties;
using TACTLib.Client.HandlerArgs;

namespace TankView.ViewModel {
    public class CASCSettings : INotifyPropertyChanged {
        private bool _apm = Settings.Default.CacheAPM;

        public bool APM {
            get => _apm;
            set {
                _apm = value;
                Settings.Default.CacheAPM = value;
                Settings.Default.Save();
                ((ClientCreateArgs_Tank)MainWindow.ClientArgs.HandlerArgs).CacheAPM = value;
                NotifyPropertyChanged(nameof(APM));
            }
        }
        
        public CASCSettings() {
            ((ClientCreateArgs_Tank)MainWindow.ClientArgs.HandlerArgs).CacheAPM = APM;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
