using System.ComponentModel;
using TankView.Properties;

namespace TankView.ViewModel {
    public class ExtractionSettings : INotifyPropertyChanged {
        private bool _convertSounds = Settings.Default.ConvertSounds;
        private bool _convertImages = Settings.Default.ConvertImages;

        public bool EnableConvertSounds {
            get => _convertSounds;
            set {
                _convertSounds = value;
                Settings.Default.ConvertSounds = value;
                Settings.Default.Save();
                NotifyPropertyChanged(nameof(EnableConvertSounds));
            }
        }
        
        public bool EnableConvertImages {
            get => _convertImages;
            set {
                _convertImages = value;
                Settings.Default.ConvertImages = value;
                Settings.Default.Save();
                NotifyPropertyChanged(nameof(EnableConvertImages));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
