using System.Collections.Generic;
using System.IO;
using DataTool.DataModels;
using static DataTool.Helper.IO;
using DataTool.Flag;
using DataTool.FindLogic;
using STULib.Types;
using STULib.Types.STUUnlock;

namespace DataTool.SaveLogic.Unlock {
    public class SprayAndImage {
        public static void SaveItems(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, List<ItemInfo> items) {
            var textures = new Dictionary<string, Dictionary<ulong, List<TextureInfo>>>();
            foreach (var item in items) {
                var name = GetValidFilename(item.Name);
                string type;
                STUDecalReference decal;

                switch (item.Unlock) {
                    case PlayerIcon icon:
                        decal = icon.Decal;
                        type = "Icons";
                        break;
                    case Spray spray:
                        decal = spray.Decal;
                        type = "Sprays";
                        break;
                    default:
                        continue;
                }

                if (decal == null) continue;

                if (!textures.ContainsKey(type))
                    textures[type] = new Dictionary<ulong, List<TextureInfo>>();

                textures[type] = FindLogic.Texture.FindTextures(textures[type], decal.DecalResource, name, true);
            }

            foreach (var groupPair in textures) {
                if (groupPair.Value?.Count == 0) continue;
                var output = Path.Combine(basePath, containerName, heroName, groupPair.Key, folderName);
                Texture.Save(null, output, groupPair.Value);
            }
        }
    }
}