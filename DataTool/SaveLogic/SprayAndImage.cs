using System.Collections.Generic;
using System.IO;
using static DataTool.Helper.IO;
using DataTool.Flag;
using DataTool.FindLogic;
using static DataTool.Helper.STUHelper;
using STULib.Types;
using STULib.Types.STUUnlock;

namespace DataTool.SaveLogic {
    public class SprayAndImage {
        public static void SaveItems(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, IEnumerable<ulong> items) {
            Dictionary<string, Dictionary<ulong, List<TextureInfo>>> textures = new Dictionary<string, Dictionary<ulong, List<TextureInfo>>>();
            foreach (var key in items) {
                var item = GatherUnlock(key);
                var name = GetValidFilename(item.Name);
                string type = null;
                STUDecalReference decal = null;

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

                if (!textures.ContainsKey(type))
                    textures[type] = new Dictionary<ulong, List<TextureInfo>>();

                if (decal == null) continue;
                textures[type] = FindLogic.Texture.FindTextures(textures[type], decal.DecalResource, name, true);
            }

            foreach (KeyValuePair<string, Dictionary<ulong, List<TextureInfo>>> groupPair in textures) {
                if (groupPair.Value?.Count == 0) continue;
                var output = Path.Combine(basePath, containerName, folderName, groupPair.Key);
                Texture.Save(null, output, groupPair.Value);
            }
        }
    }
}