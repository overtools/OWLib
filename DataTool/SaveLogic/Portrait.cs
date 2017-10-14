using System.Collections.Generic;
using static DataTool.Helper.IO;
using DataTool.Flag;
using DataTool.FindLogic;
using static DataTool.Helper.STUHelper;
using STULib.Types;

namespace DataTool.SaveLogic {
    public class Portrait {
        public static void SaveItems(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, IEnumerable<ulong> items) {
            Dictionary<string, Dictionary<ulong, List<TextureInfo>>> textures = new Dictionary<string, Dictionary<ulong, List<TextureInfo>>>();
            foreach (var key in items) {
                var item = GatherUnlock(key);
                var name = GetValidFilename(item.Name);

                var unlock = ((STULib.Types.STUUnlock.Portrait) item.Unlock);
                var borderDecal = new STUDecalReference { DecalResource = unlock.BorderImage };
                var starDecal = new STUDecalReference { DecalResource = unlock.StarImage };

                //textures = FindLogic.Texture.FindTextures(textures, borderDecal.DecalResource, name, true);
            }
        }
    }
}