using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using Newtonsoft.Json;
using STULib.Types;
using STULib.Types.Enums;
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
        
        [JsonObject(MemberSerialization.OptOut)]
        public class LootboxInfo {
            public string Event;
            public STUEnumEventID EventId;
            public string Name;
            public List<string> Strings;
            public List<string> Boxes;

            [JsonConverter(typeof(GUIDConverter))]
            public ulong GUID;
            
            public LootboxInfo(ulong guid, string name, string eventName, STUEnumEventID eventId, List<string> strings, List<string> boxes) {
                Event = eventName;
                EventId = eventId;
                Name = name;
                Strings = strings;
                Boxes = boxes;
                GUID = guid;
            }
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
                Log($"{iD}{lootboxSet.Event}");
                foreach (var lootbox in lootboxSet.Boxes)
                    Log($"{iD+1}{lootbox}");

                Log();
            }
        }

        public List<LootboxInfo> GetLootboxes() {
            List<LootboxInfo> lootboxList = new List<LootboxInfo>();

            foreach (ulong key in TrackedFiles[0xCF]) {
                var lootbox = GetInstance<STULootbox>(key);
                
                if (lootbox == null) continue;

                var name = GetString(lootbox.Name);
                var strings = lootbox.Strings.Select(l => GetString(l) ?? "Unknown").ToList();
                var boxes = lootbox.ShopCards.Select(l => GetString(l.Text) ?? "Unknown").ToList();

                lootboxList.Add(new LootboxInfo(key, name, lootbox.EventNameNormal, lootbox.Event, strings, boxes));
            }

            return lootboxList.OrderBy(l => (int) l.EventId).ToList();
        }
    }
}