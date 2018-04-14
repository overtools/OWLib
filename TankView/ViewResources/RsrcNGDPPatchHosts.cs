using TankView.ObjectModel;

namespace TankView.ViewResources
{
    public class RsrcNGDPPatchHosts : ObservableHashCollection<PatchHost>
    {
        public RsrcNGDPPatchHosts()
        {
            Add(new PatchHost("us.patch.battle.net:1119", "Blizzard Americas"));
            Add(new PatchHost("kr.patch.battle.net:1119", "Blizzard Korea"));
            Add(new PatchHost("eu.patch.battle.net:1119", "Blizzard Europe"));
            Add(new PatchHost("cn.patch.battle.net:1119", "Blizzard China"));
            Add(new PatchHost("tw.patch.battle.net:1119", "Blizzard Taiwan"));
        }
    }
}
