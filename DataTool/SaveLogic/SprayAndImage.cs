using System.Collections.Generic;
using System.IO;
using static DataTool.Helper.IO;
using DataTool.Flag;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.ToolLogic.Extract;
using static DataTool.Helper.STUHelper;
using STULib.Types;
using STULib.Types.Generic;
using STULib.Types.STUUnlock;

namespace DataTool.SaveLogic {
    public class SprayAndImage {
        public static void SaveItems(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, IEnumerable<ulong> items) {
            var ext = "dds";
            if (flags is ExtractFlags extractFlags) {

            }

            Dictionary<ulong, List<TextureInfo>> textures = new Dictionary<ulong, List<TextureInfo>>();
            foreach (var key in items) {
                ItemInfo item = GatherUnlock(key);

                var name = GetValidFilename(item.Name);
                var type = GetValidFilename(item.Type);

                STUDecalReference decal = null;
                if (item.Unlock is Spray)
                    decal = ((Spray) item.Unlock).Decal;

                if (item.Unlock is PlayerIcon)
                    decal = ((PlayerIcon) item.Unlock).CosmeticUnknownDecal;

                if (decal == null) continue;

                textures = FindLogic.Texture.FindTextures(textures,  decal.DecalResource, name, type, true);
            }

            var output = Path.Combine(basePath, containerName, folderName);
            SaveLogic.Texture.Save(null, output, textures);
        }
    }
}