using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-lootbox", Description = "List lootboxes", TrackTypes = new ushort[] {0xCF}, CustomFlags = typeof(ListFlags))]
    public class ListLoobox : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            var lootboxes = GetLootboxes();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(lootboxes, flags);
                    return;
                }

            var iD = new IndentHelper();
            foreach (var lootboxSet in lootboxes) {
                Log($"{iD}{lootboxSet.Key}");
                foreach (var lootbox in lootboxSet.Value)
                    Log($"{iD+1}{lootbox}");
            }
        }

        public Dictionary<string, List<string>> GetLootboxes() {
            Dictionary<string, List<string>> @return = new Dictionary<string, List<string>>();

            foreach (ulong key in TrackedFiles[0xCF]) {
                var lootbox = GetInstance<STULootbox>(key);

                if (lootbox == null) continue;

                @return[lootbox.EventNameNormal] = lootbox.ShopCards.Select(l => GetString(l.Text) ?? "Unknown").ToList();
            }

            return @return;
        }
    }
}