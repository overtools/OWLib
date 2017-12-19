using System.Collections.Generic;
using System.IO;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic.Unlock {
    public class Portrait {
        public static void SaveItems(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, List<ItemInfo> items) {
            var textures = new Dictionary<string, Dictionary<ulong, List<TextureInfo>>>();
            foreach (var item in items) {
                if (!(item.Unlock is STULib.Types.STUUnlock.Portrait)) continue;

                var name = GetValidFilename(item.Name);
                var unlock = (STULib.Types.STUUnlock.Portrait) item.Unlock;
                var tier = unlock.Tier.ToString();

                if (!textures.ContainsKey(tier))
                    textures[tier] = new Dictionary<ulong, List<TextureInfo>>();


                textures[tier] = FindLogic.Texture.FindTextures(textures[tier], unlock.BorderImage, name, true);
                textures[tier] = FindLogic.Texture.FindTextures(textures[tier], unlock.StarImage, name, true);
            }

            foreach (var groupPair in textures) {
                if (groupPair.Value?.Count == 0) continue;
                var output = Path.Combine(basePath, containerName, "Portraits", groupPair.Key);
                Texture.Save(null, output, groupPair.Value);
            }
        }
    }
}