using System.ComponentModel;
using System.Threading;
using System.Windows.Controls;

namespace TankView {
    /// <summary>
    /// Interaction logic for DataToolSimView.xaml
    /// </summary>
    public partial class DataToolSimView {
        private string _moduleName = "DataTool";

        public string ModuleName {
            get => _moduleName;
            set {
                _moduleName = value;
                NotifyPropertyChanged(nameof(ModuleName));
            }
        }

        private Control _control;
        public Control DataToolControl {
            get => _control;
            set {
                _control = value;
                ViewContext.Send((x) => {
                    if (!(x is Control ctrl)) return;
                    MainSource.Children.Clear();
                    MainSource.Children.Add(ctrl);
                    NotifyPropertyChanged(nameof(DataToolControl));
                }, value);
            }
        }

        public SynchronizationContext ViewContext { get; }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        
        public DataToolSimView() {
            InitializeComponent();
            ViewContext = SynchronizationContext.Current;
        }
    }
}
