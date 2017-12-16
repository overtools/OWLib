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
                STUEffectReference effect;

                switch (item.Unlock) {
                    case PlayerIcon icon:
                        effect = icon.Effect;
                        type = "Icons";
                        break;
                    case Spray spray:
                        effect = spray.Effect;
                        type = "Sprays";
                        break;
                    default:
                        continue;
                }

                if (effect == null) continue;

                if (!textures.ContainsKey(type))
                    textures[type] = new Dictionary<ulong, List<TextureInfo>>();

                textures[type] = FindLogic.Texture.FindTextures(textures[type], effect.EffectLook, name, true);
            }

            foreach (var groupPair in textures) {
                if (groupPair.Value?.Count == 0) continue;
                var output = Path.Combine(basePath, containerName, heroName ?? "", groupPair.Key, folderName);
                Texture.Save(flags, output, groupPair.Value);
            }
        }


        public static void SaveItem(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, ItemInfo item) {
            string name = GetValidFilename(item.Name);
            string type;
            Dictionary<ulong, List<TextureInfo>> textures = new Dictionary<ulong, List<TextureInfo>>();
            STUEffectReference effect;

            switch (item.Unlock) {
                case PlayerIcon icon:
                    effect = icon.Effect;
                    type = "Icons";
                    break;
                case Spray spray:
                    effect = spray.Effect;
                    type = "Sprays";
                    break;
                default:
                    return;
            }

            if (effect == null) return;

            textures = FindLogic.Texture.FindTextures(textures, effect.EffectLook, name, true);
            
            string output = Path.Combine(basePath, containerName, heroName ?? "", type, folderName);
            Texture.Save(flags, output, textures);
        }
        
    }
}