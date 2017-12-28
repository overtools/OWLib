using System;
using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using STULib.Types;
using STULib.Types.Generic;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using Combo = DataTool.FindLogic.Combo;
using Sound = DataTool.FindLogic.Sound;

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
                SaveLogic.Combo.Save(flags, Path.Combine(basePath, Container, name), info);

                Combo.ComboInfo shopCardInfo = new Combo.ComboInfo();
                foreach (STULootBoxShopCard lootboxShopCard in lootbox.ShopCards) {
                    Combo.Find(shopCardInfo, lootboxShopCard.Texture);
                }
                foreach (Combo.TextureInfoNew textureInfo in shopCardInfo.Textures.Values) {
                    SaveLogic.Combo.SaveTexture(flags, Path.Combine(basePath, Container, name, "ShopCards"), shopCardInfo, textureInfo.GUID);
                }
                
                
                Dictionary<ulong, List<SoundInfo>> music = new Dictionary<ulong, List<SoundInfo>>();
                foreach (Common.STUGUID guid in new [] {lootbox.Effect1, lootbox.Effect2, lootbox.Effect3, lootbox.Entity2, lootbox.Entity}) {
                    music = Sound.FindSounds(music, guid, null, true);
                }
            
                SaveLogic.Sound.Save(toolFlags, Path.Combine(basePath, $"{Container}", name, "Music"), music);
            }
        }
    }
}