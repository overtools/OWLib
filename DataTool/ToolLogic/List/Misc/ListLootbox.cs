using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.JSON;
using static DataTool.Program;

namespace DataTool.ToolLogic.List.Misc;

[Tool("list-lootbox", Description = "List lootboxes", CustomFlags = typeof(ListFlags), IsSensitive = true)]
public class ListLootbox : JSONTool, ITool {
    public void Parse(ICLIFlags toolFlags) {
        var flags = (ListFlags) toolFlags;
        var lootboxes = GetLootboxes();

        if (flags.JSON) {
            OutputJSON(lootboxes, flags);
            return;
        }

        foreach (var lootbox in lootboxes) {
            Log($"{lootbox.Type}");
            if (!flags.Simplify) {
                if (lootbox.ShopCards != null) {
                    foreach (var shopCard in lootbox.ShopCards) {
                        Log($"\t{shopCard.Text}");
                    }
                }
            }
        }
    }

    public List<LootBox> GetLootboxes() {
        var @return = new List<LootBox>();

        foreach (ulong key in TrackedFiles[0xCF]) {
            var lootbox = LootBox.Load(key);
            if (lootbox == null) continue;
            @return.Add(lootbox);
        }

        return @return;
    }
}