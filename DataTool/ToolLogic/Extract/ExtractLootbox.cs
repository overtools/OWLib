using System;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-lootbox", Description = "Extract lootbox models", TrackTypes = new ushort[] {0xCF}, CustomFlags = typeof(ExtractFlags))]
    public class ExtractLootbox : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetLootboxes(toolFlags);
        }

        public const string Container = "Lootboxes";

        public void GetLootboxes(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }
            
            
            foreach (ulong key in TrackedFiles[0xCF]) {
                STULootbox lootbox = GetInstance<STULootbox>(key);
                if (lootbox == null) continue;
                
                string name = GetValidFilename(lootbox.Event.ToString()) ?? $"Unknown{GUID.Index(key):X}";

                Combo.ComboInfo info = Combo.Find(null, lootbox.Entity);
                Combo.Find(info, lootbox.Entity2);
                Combo.Find(info, lootbox.Effect1);
                Combo.Find(info, lootbox.Effect2);
                Combo.Find(info, lootbox.Effect3);
                Combo.Find(info, lootbox.ModelLook);
                Combo.Find(info, lootbox.Look2);
                
                Combo.Find(info, 288230376151716950);  // coin chest, todo
                // 00000000315A.00C in 000000001456.003 (288230376151716950)
                
                foreach (STULootBoxShopCard lootboxShopCard in lootbox.ShopCards) {
                    Combo.Find(info, lootboxShopCard.Texture);
                }
                SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(basePath, Container, name, "ShopCards"), info);
                SaveLogic.Combo.Save(flags, Path.Combine(basePath, Container, name), info);
            }
        }
    }
}