using System;
using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using TankLib.STU.Types;
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
            List<LootBox> lootboxes = GetLootboxes();

            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(lootboxes, flags);
                    return;
                }

            foreach (LootBox lootbox in lootboxes) {
                Log($"{lootbox.Name}");
                if (lootbox.ShopCards != null) {
                    foreach (LootBoxShopCard shopCard in lootbox.ShopCards) {
                        Log($"\t{shopCard.Text}");
                    }
                }
            }
        }

        public List<LootBox> GetLootboxes() {
            List<LootBox> @return = new List<LootBox>();

            foreach (ulong key in TrackedFiles[0xCF]) {
                STULootBox lootbox = GetInstance<STULootBox>(key);
                if (lootbox == null) continue;

                @return.Add(new LootBox(lootbox));
            }

            return @return;
        }
    }
}