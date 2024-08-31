using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.JSON;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-lootbox", Description = "List lootboxes", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListLootbox : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ListFlags) toolFlags;
            var lootboxes = GetLootboxes();

            if (flags.JSON) {
                OutputJSON(lootboxes, flags);
                return;
            }

            foreach (LootBox lootbox in lootboxes) {
                Log($"{lootbox.Type}");
                if (!flags.Simplify) {
                    if (lootbox.ShopCards != null) {
                        foreach (LootBoxShopCard shopCard in lootbox.ShopCards) {
                            Log($"\t{shopCard.Text}");
                        }
                    }
                }
            }
        }

        public List<LootBox> GetLootboxes() {
            var @return = new List<LootBox>();

            foreach (ulong key in TrackedFiles[0xCF]) {
                var lootbox = GetInstance<STULootBox>(key);
                if (lootbox == null) continue;

                @return.Add(new LootBox(lootbox));
            }

            return @return;
        }
    }
}
