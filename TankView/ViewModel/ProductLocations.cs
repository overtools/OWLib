using System.Collections.Generic;
using System.IO;
using TankView.ObjectModel;
using TACTLib.Agent;
using TACTLib.Agent.Protobuf;

namespace TankView.ViewModel {
    public class ProductLocations : ObservableHashCollection<ProductLocation> {
        private static Dictionary<string, string> KnownUIDs = new Dictionary<string, string> {
            {"prometheus", "Live"},
            {"prometheus_dev", "Development"},
            {"prometheus_test", "Public Test"},
            {"prometheus_tournament", "Professional"},
            {"prometheus_viewer", "Viewer"}
        };

        public ProductLocations() {
            try {
                AgentDatabase pdb = new AgentDatabase();
                foreach (ProductInstall install in pdb.Data.ProductInstall) {
                    if (KnownUIDs.ContainsKey(install.Uid) && Directory.Exists(install.Settings.InstallPath)) {
                        Add(new ProductLocation(KnownUIDs[install.Uid], Path.GetFullPath(install.Settings.InstallPath)));
                    }
                }
            } catch { }
        }
    }
}

