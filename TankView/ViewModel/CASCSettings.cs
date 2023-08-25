using System.ComponentModel;
using TankView.Properties;
using TACTLib.Client.HandlerArgs;

namespace TankView.ViewModel {
    public class CASCSettings : INotifyPropertyChanged {
        public CASCSettings() {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
