using System;
using System.IO;
using DataTool.DataModels;
using DataTool.Flag;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-general", Description = "Extract general unlocks", CustomFlags = typeof(ExtractFlags))]
    public class ExtractGeneral : ITool {
        public void Parse(ICLIFlags toolFlags) {
            GetGeneralUnlocks(toolFlags);
        }

        public void GetGeneralUnlocks(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            string path = Path.Combine(basePath, "General");

            foreach (var key in TrackedFiles[0x54]) {
                STUGenericSettings_PlayerProgression progression = GetInstance<STUGenericSettings_PlayerProgression>(key);
                if (progression == null) continue;
                
                PlayerProgression playerProgression = new PlayerProgression(progression);

                if (playerProgression.LootBoxesUnlocks != null) {
                    foreach (LootBoxUnlocks lootBoxUnlocks in playerProgression.LootBoxesUnlocks) {
                        string boxName = LootBox.GetName(lootBoxUnlocks.LootBoxType);
                        ExtractHeroUnlocks.SaveUnlocks(flags, lootBoxUnlocks.Unlocks, path, boxName, null, null, null, null);
                    }
                }

                if (playerProgression.AdditionalUnlocks != null) {
                    foreach (AdditionalUnlocks additionalUnlocks in playerProgression.AdditionalUnlocks) {
                        ExtractHeroUnlocks.SaveUnlocks(flags, additionalUnlocks.Unlocks, path, "Standard", null, null, null, null);
                    }
                }

                if (playerProgression.OtherUnlocks != null) {
                    ExtractHeroUnlocks.SaveUnlocks(flags, playerProgression.OtherUnlocks, path, "Achievement", null, null, null, null);
                }
            }
        }
    }
}