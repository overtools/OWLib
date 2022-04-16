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
                info.m_textures.First(x => x.Value.m_loose).Value.m_name = unlock.Name;
                directory = Path.GetFullPath(Path.Combine(directory, ".."));
            } catch {
                // animated spray - no main image

                saveAllTextures = true;
            }

            var context = new Combo.SaveContext(info);

            Combo.SaveLooseTextures(flags, directory, context);
            if (!saveAllTextures) {
                return;
            }

            Combo.SaveAllMaterials(flags, directory, context);
            Combo.Save(flags, directory, context);
        }
    }
}
