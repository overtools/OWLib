using System;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.JSON;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-general-unlocks", Description = "List general unlocks", TrackTypes = new ushort[] { 0x54 }, CustomFlags = typeof(ListFlags))]
    public class ListGeneralUnlocks : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            var unlocks = GetUnlocks();

            if (toolFlags is ListFlags flags) {
                if (flags.JSON) {
                    OutputJSON(unlocks, flags);
                    return;
                }
            }
            
            ListHeroUnlocks.DisplayUnlocks("Other", unlocks.OtherUnlocks);

            if (unlocks.LootBoxesUnlocks != null) {
                foreach (LootBoxUnlocks lootBoxUnlocks in unlocks.LootBoxesUnlocks) {
                    string boxName = LootBox.GetName(lootBoxUnlocks.LootBoxType);
                        
                    ListHeroUnlocks.DisplayUnlocks(boxName, lootBoxUnlocks.Unlocks);
                }
            }

            if (unlocks.AdditionalUnlocks != null) {
                foreach (AdditionalUnlocks additionalUnlocks in unlocks.AdditionalUnlocks) {
                    ListHeroUnlocks.DisplayUnlocks($"Level {additionalUnlocks.Level}", additionalUnlocks.Unlocks);
                }
            }
        }

        public PlayerProgression GetUnlocks() {
            foreach (ulong key in TrackedFiles[0x54]) {
                STUGenericSettings_PlayerProgression playerProgression = GetInstance<STUGenericSettings_PlayerProgression>(key);
                if (playerProgression == null) continue;

                return new PlayerProgression(playerProgression);
            }
            return null;
        }
    }
}
