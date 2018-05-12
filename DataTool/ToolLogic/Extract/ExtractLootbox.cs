using System;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using TankLib.STU.Types;
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
                STULootBox lootbox = GetInstanceNew<STULootBox>(key);
                if (lootbox == null) continue;
                
                string name = GetValidFilename(lootbox.m_7AB4E3F8.ToString()) ?? $"Unknown{GUID.Index(key):X}";

                Combo.ComboInfo info = Combo.Find(null, lootbox.m_B2F9D222);  // 003
                Combo.Find(info, lootbox.m_CBE2DADD);  // 003
                Combo.Find(info, lootbox.m_3970E137);  // 00D
                Combo.Find(info, lootbox.m_FEC3ED62);  // 00D
                Combo.Find(info, lootbox.m_FFE7768F);  // 00D
                Combo.Find(info, lootbox.m_9B180535);  // 01A
                Combo.Find(info, lootbox.m_modelLook);
                
                Combo.Find(info, 288230376151716950);  // coin chest, todo
                // 00000000315A.00C in 000000001456.003 (288230376151716950)
                
                foreach (STULootBoxShopCard lootboxShopCard in lootbox.m_shopCards) {
                    Combo.Find(info, lootboxShopCard.m_87EACF5F);  // 004
                }
                SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(basePath, Container, name, "ShopCards"), info);
                SaveLogic.Combo.Save(flags, Path.Combine(basePath, Container, name), info);
            }
        }
    }
}