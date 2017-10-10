using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.Flag;
using STULib;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;
using DataTool.Models;
using OWLib;
using STULib.Types.Generic;

// ReSharper disable SuspiciousTypeConversion.Global

namespace DataTool.ToolLogic.List {
    [Tool("list-general-unlocks", Description = "List general unlocks", TrackTypes = new ushort[] { 0x54 }, CustomFlags = typeof(ListFlags))]
    public class ListGeneralUnlocks : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            var unlocks = GatherUnlocks();

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

        public Dictionary<string, HashSet<ItemInfo>> GatherUnlocks() {
            Dictionary<string, HashSet<ItemInfo>> @return = new Dictionary<string, HashSet<ItemInfo>>();
            foreach (ulong key in TrackedFiles[0x54]) {
                STUGlobalInventoryMaster invMaster = GetInstance<STUGlobalInventoryMaster>(key);
                if (invMaster == null) continue;

                @return["Achievement"] = GatherUnlocks(invMaster.AchievementUnlocks?.Unlocks?.Select(it => (ulong)it));

                @return["Standard/Common"] = new HashSet<ItemInfo>();

                if (invMaster.StandardUnlocks != null) {
                    foreach (STUGlobInvStandardUnlocks levelUnlocks in invMaster.StandardUnlocks) {
                        if (levelUnlocks?.Unlocks == null) continue;

                        foreach (ItemInfo info in GatherUnlocks(levelUnlocks.Unlocks.Select(it => (ulong)it))) {
                            @return["Standard/Common"].Add(info);
                        }
                    }
                }

                if (invMaster.EventGeneralUnlocks != null) {
                    foreach (STUHeroUnlocks.EventUnlockInfo eventUnlocks in invMaster.EventGeneralUnlocks) {
                        if (eventUnlocks?.Data?.Unlocks == null) continue;

                        string eventKey = $"Event/{ItemEvents.GetInstance().EventsNormal[(uint)eventUnlocks.Event]}";

                        if (!@return.ContainsKey(eventKey)) {
                            @return[eventKey] = new HashSet<ItemInfo>();
                        }

                        foreach (ItemInfo info in GatherUnlocks(eventUnlocks.Data.Unlocks.Select(it => (ulong)it))) {
                            @return[eventKey].Add(info);
                        }
                    }
                }
            }

            return @return;
        }

        public HashSet<ItemInfo> GatherUnlocks(IEnumerable<ulong> GUIDs) {
            HashSet<ItemInfo> @return = new HashSet<ItemInfo>();
            foreach (ulong GUID in GUIDs) {
                ItemInfo unlock = GatherUnlock(GUID);
                if (unlock == null) continue;

                @return.Add(unlock);
            }
            return @return;
        }
    }
}
