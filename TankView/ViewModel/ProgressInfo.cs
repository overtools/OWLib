using System.ComponentModel;

namespace TankView.ViewModel {
    public class ProgressInfo : INotifyPropertyChanged {
        private string _state = "Idle";
        private int _pc = 0;

        public string State {
            get { return _state; }
            set {
                _state = value;
                NotifyPropertyChanged(nameof(State));
            }
        }

        public int Percentage {
            get { return _pc; }
            set {
                _pc = value;
                NotifyPropertyChanged(nameof(Percentage));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
