using System;
using System.Linq;
using DataTool.Flag;
using OWLib;
using STULib.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-general", Description = "Extract general sprays and icons", TrackTypes = new ushort[] {0x54}, CustomFlags = typeof(ExtractFlags))]
    public class ExtractGeneral : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

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

            foreach (var key in TrackedFiles[0x54]) {
                STUGlobalInventoryMaster invMaster = GetInstance<STUGlobalInventoryMaster>(key);
                if (invMaster == null) continue;

                SaveLogic.SprayAndImage.SaveItems(basePath, null, "General", "Achievements", flags, invMaster.AchievementUnlocks?.Unlocks?.Select(it => (ulong)it));

                if (invMaster.EventGeneralUnlocks != null) {
                    foreach (var eventUnlocks in invMaster.EventGeneralUnlocks) {
                        if (eventUnlocks?.Data?.Unlocks == null) continue;

                        var eventKey = ItemEvents.GetInstance().EventsNormal[(uint)eventUnlocks.Event];
                        var unlocks = eventUnlocks.Data.Unlocks.Select(it => (ulong) it);
                        SaveLogic.SprayAndImage.SaveItems(basePath, null, "General", eventKey, flags, unlocks);
                    }
                }

                /* Requires a bit more work
                if (invMaster.LevelUnlocks != null) {
                    foreach (var levelUnlocks in invMaster.LevelUnlocks) {
                        if (levelUnlocks?.Unlocks == null) continue;

                        var unlocks = levelUnlocks.Unlocks.Select(it => (ulong)it);
                        SaveLogic.Portrait.SaveItems(basePath, null, "General", "Standard", flags, unlocks);
                    }
                }
                */
            }
        }
    }
}