using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using OWLib;
using TankLib.STU.Types;
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
            Dictionary<string, List<string>> lootboxes = GetLootboxes();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(lootboxes, flags);
                    return;
                }

            IndentHelper iD = new IndentHelper();
            foreach (KeyValuePair<string, List<string>> lootboxSet in lootboxes) {
                Log($"{iD}{lootboxSet.Key}");
                foreach (string lootbox in lootboxSet.Value)
                    Log($"{iD+1}{lootbox}");

                Log();
            }
        }

        public Dictionary<string, List<string>> GetLootboxes() {
            Dictionary<string, List<string>> @return = new Dictionary<string, List<string>>();

            foreach (ulong key in TrackedFiles[0xCF]) {
                STULootBox lootbox = GetInstanceNew<STULootBox>(key);

                if (lootbox == null) continue;

                @return[ItemEvents.GetInstance().GetEventNormal((uint)lootbox.m_lootboxType)] = lootbox.m_shopCards.Select(l => GetString(l.m_cardText) ?? "Unknown").ToList();
            }

            return @return;
        }
    }
}