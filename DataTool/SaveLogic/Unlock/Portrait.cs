using System.Collections.Generic;
using System.IO;
using DataTool.DataModels;
using DataTool.Flag;

namespace DataTool.SaveLogic.Unlock {
    public class Portrait {
        public static void SaveItems(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, IEnumerable<ItemInfo> items) {
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            
            foreach (ItemInfo item in items) {
                if (item == null) continue;
                if (!(item.Unlock is STULib.Types.STUUnlock.Portrait unlock)) continue;
                string tier = unlock.Tier.ToString();

                if (unlock.StarImage != null) {
                    FindLogic.Combo.Find(info, unlock.StarImage);
                    info.SetTextureName(unlock.StarImage, $"Star - {unlock.Star}");
                }

                if (unlock.BorderImage != null) {
                    FindLogic.Combo.Find(info, unlock.BorderImage);
                    int borderNum = unlock.Level - unlock.Star * 10 - (int)unlock.Tier * 10;

                    if ((int) unlock.Tier > 1) {
                        borderNum -= 50 * ((int) unlock.Tier - 1);
                    }
                    borderNum -= 1;
                    
                    info.SetTextureName(unlock.BorderImage, $"Border - {borderNum}");
                }
                
                Combo.SaveLooseTextures(flags, Path.Combine(basePath, containerName, "Portraits", tier), info); 
                info.Textures = new Dictionary<ulong, FindLogic.Combo.TextureInfoNew>();  // quite naughty
            }
        }
    }
}