using System.IO;
using DataTool.Flag;
using DataTool.Helper;
using TankLib.STU.Types;

namespace DataTool.SaveLogic.Unlock {
    public static class NameCard {
        public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock) {
            STU_DB1B05B5 nameCard = (STU_DB1B05B5) unlock.STU;

            string name = IO.GetCleanString(nameCard.m_name);

            directory = Path.GetFullPath(Path.Combine(directory, ".."));

            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();

            // smaller version for the name plate ui
            // if (nameCard.m_0CAFC9BA != null) {
            //     FindLogic.Combo.Find(info, nameCard.m_0CAFC9BA);
            //     info.SetTextureName(nameCard.m_0CAFC9BA, name);
            // }

            // larger version for the career page
            if (nameCard.m_C5B31BBA != null) {
                FindLogic.Combo.Find(info, nameCard.m_C5B31BBA);
                info.SetTextureName(nameCard.m_C5B31BBA, name);
            }

            var context = new Combo.SaveContext(info);
            Combo.SaveLooseTextures(flags, Path.Combine(directory), context);
        }
    }
}