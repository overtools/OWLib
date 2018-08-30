using TankView.ObjectModel;

namespace TankView.ViewModel {
    public class NGDPPatchHosts : ObservableHashCollection<PatchHost> {
        public NGDPPatchHosts() {
            Add(new PatchHost("us.patch.battle.net:1119", "Blizzard Americas"));
            Add(new PatchHost("kr.patch.battle.net:1119", "Blizzard Korea"));
            Add(new PatchHost("eu.patch.battle.net:1119", "Blizzard Europe"));
            Add(new PatchHost("cn.patch.battle.net:1119", "Blizzard China"));
            Add(new PatchHost("tw.patch.battle.net:1119", "Blizzard Taiwan"));
        }
    }
}
