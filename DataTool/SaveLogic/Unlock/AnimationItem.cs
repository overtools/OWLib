using System.IO;
using DataTool.DataModels;
using DataTool.Flag;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic.Unlock {
    public class AnimationItem {
        public static void SaveItem(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, ItemInfo item) {
            if (item == null) return;
            string properType = item.Type;
            switch (item.Type) {
                case "Pose":
                    properType = "Victory Pose";
                    break;
            }
            
            string output = Path.Combine(basePath, containerName, heroName ?? "", properType, folderName, GetValidFilename(item.Name).Replace(".", ""));

            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, item.GUID);
            
            info.SaveConfig.SaveAnimationEffects = false;  // todo: unsupported here due to relative paths used by OWEffect
            
            Combo.Save(flags, output, info);
            Combo.SaveAllAnimations(flags, output, info);
        }
    }
}