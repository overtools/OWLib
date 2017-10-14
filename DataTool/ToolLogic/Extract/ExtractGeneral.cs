using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels;
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

                var achivementUnlocks = invMaster.AchievementUnlocks?.Unlocks?.Select(it => GatherUnlock((ulong) it)).ToList();
                SaveLogic.Unlock.SprayAndImage.SaveItems(basePath, null, "General", "Achievements", flags, achivementUnlocks);

                if (invMaster.EventGeneralUnlocks != null) {
                    foreach (var eventUnlocks in invMaster.EventGeneralUnlocks) {
                        if (eventUnlocks?.Data?.Unlocks == null) continue;

                        var eventKey = ItemEvents.GetInstance().EventsNormal[(uint)eventUnlocks.Event];
                        var unlocks = eventUnlocks.Data.Unlocks.Select(it => GatherUnlock((ulong) it)).ToList();
                        SaveLogic.Unlock.SprayAndImage.SaveItems(basePath, null, "General", eventKey, flags, unlocks);
                    }
                }

                if (invMaster.LevelUnlocks != null) {
                    var unlocks = new HashSet<ItemInfo>();
                    foreach (var levelUnlocks in invMaster.LevelUnlocks) {
                        if (levelUnlocks?.Unlocks == null) continue;
                        foreach (var unlock in levelUnlocks.Unlocks)
                            unlocks.Add(GatherUnlock(unlock));
                    }

                    SaveLogic.Unlock.SprayAndImage.SaveItems(basePath, null, "General", "Standard", flags, unlocks.ToList());
                    SaveLogic.Unlock.Portrait.SaveItems(basePath, null, "General", "Standard", flags, unlocks.ToList());
                }
            }
        }
    }
}