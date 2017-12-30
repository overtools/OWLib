using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using OWLib;
using STULib.Types;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

// ReSharper disable SuspiciousTypeConversion.Global

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
                    ParseJSON(unlocks, flags);
                    return;
                }
            }

            foreach (KeyValuePair<string,  HashSet<ItemInfo>> groupPair in unlocks) {
                if (groupPair.Value?.Count == 0) continue;

                if (groupPair.Value != null) {
                    Log(groupPair.Key);
                    foreach (ItemInfo unlock in groupPair.Value) {
                        if (unlock?.Name == null) continue;
                        Log("\t{0} ({1} {2})", unlock.Name, unlock.Rarity, unlock.Type);
                    }
                }
            }
        }

        public Dictionary<string, HashSet<ItemInfo>> GetUnlocks() {
            Dictionary<string, HashSet<ItemInfo>> @return = new Dictionary<string, HashSet<ItemInfo>>();
            foreach (ulong key in TrackedFiles[0x54]) {
                STUGlobalInventoryMaster invMaster = GetInstance<STUGlobalInventoryMaster>(key);
                if (invMaster == null) continue;

                @return["Achievement"] = GatherUnlocks(invMaster.AchievementUnlocks?.Unlocks?.Select(it => (ulong)it));

                @return["Standard"] = new HashSet<ItemInfo>();

                if (invMaster.LevelUnlocks != null) {
                    foreach (STUAdditionalUnlocks levelUnlocks in invMaster.LevelUnlocks) {
                        if (levelUnlocks?.Unlocks == null) continue;

                        foreach (ItemInfo info in GatherUnlocks(levelUnlocks.Unlocks.Select(it => (ulong)it))) {
                            @return["Standard"].Add(info);
                        }
                    }
                }

                if (invMaster.EventGeneralUnlocks != null) {
                    foreach (STUHeroUnlocks.STULootBoxUnlocks eventUnlocks in invMaster.EventGeneralUnlocks) {
                        if (eventUnlocks?.Unlocks?.Unlocks == null) continue;

                        string eventKey = $"Event/{ItemEvents.GetInstance().EventsNormal[(uint)eventUnlocks.Event]}";

                        if (!@return.ContainsKey(eventKey))
                            @return[eventKey] = new HashSet<ItemInfo>();

                        foreach (ItemInfo info in GatherUnlocks(eventUnlocks.Unlocks.Unlocks.Select(it => (ulong)it))) {
                            @return[eventKey].Add(info);
                        }
                    }
                }
            }

            return @return;
        }
    }
}
