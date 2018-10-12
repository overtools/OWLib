using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DataTool;
using DataTool.WPF;
using TankView.ViewModel;
using TACTLib.Agent.Protobuf;

namespace TankView {
    public partial class DataToolProgressTransition : Window {
        public SynchronizationContext ViewContext { get; }
        
        public ProgressInfo ProgressInfo { get; set; }
        private ProgressWorker _progressWorker = new ProgressWorker();
        public DataToolProgressTransition(IAwareTool tool) {
            InitializeComponent();

            ViewContext = SynchronizationContext.Current;

            _progressWorker.OnProgress += UpdateProgress;
            
            Task.Factory.StartNew(async () => {
                var control = await tool.GetToolControl(_progressWorker);
                var window = new DataToolSimView {
                    ModuleName = tool.GetType().GetCustomAttributes<ToolAttribute>().FirstOrDefault()?.Name,
                    DataToolControl = control,
                    Owner = Owner
                };
                window.Show();
                Close();
            }).Start();
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

