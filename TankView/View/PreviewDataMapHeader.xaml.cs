using System.Windows.Controls;
using DataTool.DataModels;
using TankLib;
using TankView.ViewModel;

namespace TankView.View {
    public partial class PreviewDataMapHeader : UserControl {

        public MapHeader MapHeader { get; set; }
        public string GUIDString { get; set; }

        public PreviewDataMapHeader(GUIDEntry guidEntry, MapHeader mapHeader) {
            GUIDString = teResourceGUID.AsString(guidEntry.GUID);
            MapHeader = mapHeader;
            InitializeComponent();
        }
    }
}
