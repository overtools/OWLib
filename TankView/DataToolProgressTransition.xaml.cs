using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DataTool;
using DataTool.WPF;
using TankView.ViewModel;

namespace TankView {
    public partial class DataToolProgressTransition : Window {
        public SynchronizationContext ViewContext { get; }
        
        public ProgressInfo ProgressInfo { get; set; } = new ProgressInfo();
        private ProgressWorker _progressWorker = new ProgressWorker();

        public DataToolProgressTransition(IAwareTool tool) {
            InitializeComponent();

            ViewContext = SynchronizationContext.Current;

            _progressWorker.OnProgress += UpdateProgress;

            var window = new DataToolSimView {
                ModuleName = tool.GetType().GetCustomAttributes<ToolAttribute>().FirstOrDefault()?.Name ?? "DataTool",
                Owner = Owner,
                Visibility = Visibility.Hidden
            };
            
            var t = new Thread(() => {
                var control = tool.GetToolControl(_progressWorker, window.ViewContext).GetAwaiter().GetResult();
                window.DataToolControl = control;
                window.ViewContext.Send(x => {
                    if (!(x is DataToolSimView view)) return;
                    view.Visibility = Visibility.Visible;
                    view.Show();
                }, window);
                ViewContext.Send(x => {
                    Close();
                }, this);
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        private void UpdateProgress(object sender, ProgressChangedEventArgs @event) {
            ViewContext.Send(x => {
                if (!(x is ProgressChangedEventArgs evt)) return;
                if (evt.UserState != null && evt.UserState is string state) {
                    ProgressInfo.State = state;
                }

                ProgressInfo.Percentage = evt.ProgressPercentage;
            }, @event);
        }
    }
}

