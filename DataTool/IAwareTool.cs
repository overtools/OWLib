using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DataTool.WPF;

namespace DataTool {
    public interface IAwareTool : ITool {
        Task<Control> GetToolControl(ProgressWorker worker, SynchronizationContext context, Window window);
    }
}
