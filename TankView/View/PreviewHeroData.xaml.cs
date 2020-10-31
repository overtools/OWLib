using System.Windows.Controls;
using DataTool.DataModels;
using DataTool.DataModels.Hero;
using TankLib;
using TankView.ViewModel;

namespace TankView.View {
    public partial class PreviewHeroData : UserControl {

        public Hero Hero { get; set; }
        public string GUIDString { get; set; }

        public PreviewHeroData(GUIDEntry guidEntry, Hero hero) {
            GUIDString = teResourceGUID.AsString(guidEntry.GUID);
            Hero = hero;
            InitializeComponent();
        }
    }
}
