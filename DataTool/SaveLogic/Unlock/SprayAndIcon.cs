using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.ToolLogic.Extract;

namespace DataTool.SaveLogic.Unlock {
    public static class SprayAndIcon {
        public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock) {
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, unlock.GUID);
            
            // hmm, resaving the default spray over and over again (ref'd by SSCE) is kinda bad.

            try {
                info.Textures.First(x => x.Value.Loose).Value.Name = unlock.Name;
            } catch {
                // what
            }

            ExtractFlags extractFlags = flags as ExtractFlags;
            
            if (extractFlags?.SprayOnlyImage == true) {
                directory = Path.GetFullPath(Path.Combine(directory, ".."));
            }

            Combo.SaveLooseTextures(flags, directory, info);
            if (extractFlags?.SprayOnlyImage == true) return;
            Combo.SaveAllMaterials(flags, directory, info);
            Combo.Save(flags, directory, info);
        }
    }
}