using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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

        public DataToolProgressTransition(IAwareTool tool) : this(tool.GetToolControl, tool.GetType().GetCustomAttributes<ToolAttribute>().FirstOrDefault()?.Name ?? "DataTool") { }

        public DataToolProgressTransition(Func<ProgressWorker, SynchronizationContext, Window, Task<Control>> fn, string name) {
            InitializeComponent();

            ViewContext = SynchronizationContext.Current;

            _progressWorker.OnProgress += UpdateProgress;

            var window = new DataToolSimView {
                ModuleName = name,
                Visibility = Visibility.Hidden
            };

            var t = new Thread(() => {
                Control control = null;
                try {
                    control = fn(_progressWorker, window.ViewContext, window).GetAwaiter().GetResult();
                } catch(Exception e) {
                    MessageBox.Show(e.Message, "DataTool Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                }
                if (control == null) {
                    window.ViewContext.Send(x => { window.Close(); }, this);
                    ViewContext.Send(x => { Close(); }, this);
                    return;
                }
                window.DataToolControl = control;
                window.ViewContext.Send(x => {
                    if (!(x is DataToolSimView view)) return;
                    view.Visibility = Visibility.Visible;
                    view.Show();
                }, window);
                ViewContext.Send(x => { Close(); }, this);
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
