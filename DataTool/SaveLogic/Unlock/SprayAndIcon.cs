using System.IO;
using System.Linq;
using DataTool.Flag;

namespace DataTool.SaveLogic.Unlock {
    public static class SprayAndIcon {
        public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock) {
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, unlock.GUID);

            bool saveAllTextures = false;
            try {
                info.Textures.First(x => x.Value.Loose).Value.Name = unlock.Name;
                directory = Path.GetFullPath(Path.Combine(directory, ".."));
            } catch {
                // animated spray - no main image

                saveAllTextures = true;
            }

            Combo.SaveLooseTextures(flags, directory, info);
            if (!saveAllTextures) return;
            Combo.SaveAllMaterials(flags, directory, info);
            Combo.Save(flags, directory, info);
        }
    }
}