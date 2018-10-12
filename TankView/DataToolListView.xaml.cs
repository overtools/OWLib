using System.Windows;
using DataTool;
using DataTool.ToolLogic.Extract;

namespace TankView {
    public partial class DataToolListView : Window {
        public DataToolListView() {
            InitializeComponent();
            IAwareTool test = new ExtractHeroUnlocks();
            var transition = new DataToolProgressTransition(test);
            transition.Show();
            Close();
        }
    }
}

