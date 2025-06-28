﻿using DataTool.DataModels;
using DataTool.Flag;
using DataTool.JSON;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-general-unlocks", Description = "List general unlocks", CustomFlags = typeof(ListFlags))]
    public class ListGeneralUnlocks : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ListFlags) toolFlags;
            var unlocks = GetPlayerProgression();

            if (flags.JSON) {
                if (flags.Flatten) {
                    OutputJSON(unlocks.IterateUnlocks(), flags);
                } else {
                    OutputJSON(unlocks, flags);
                }

                return;
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

        public static PlayerProgression GetPlayerProgression() {
            foreach (ulong key in TrackedFiles[0x54]) {
                STUGenericSettings_PlayerProgression playerProgression = GetInstance<STUGenericSettings_PlayerProgression>(key);
                if (playerProgression == null) continue;

                return new PlayerProgression(playerProgression);
            }

            return null;
        }
    }
}
