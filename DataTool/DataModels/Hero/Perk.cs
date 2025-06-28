using DataTool.DataModels.Hero;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.DataModels {
    public class Perk {
        public teResourceGUID GUID { get; set; }
        public Loadout Loadout { get; set; }
        
        public Perk(STU_42B75C40 perk, ulong guid = default) {
            Init(perk, guid);
        }
        
        public void Init(STU_42B75C40 perk, ulong guid = default) {
            GUID = (teResourceGUID)guid;
            Loadout = Loadout.Load(perk.m_loadout);
        }

        public static Perk Load(ulong guid) {
            STU_DF0481B0 baseType = STUHelper.GetInstance<STU_DF0481B0>(guid);
            if (baseType is not STU_42B75C40 perk) {
                // is a talent (or null)
                return null;
            }

            return new Perk(perk, guid);
        }
    }
}
