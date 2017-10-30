using System.Collections.Generic;
using System.IO;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using STULib.Types.Generic;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic.Unlock {
    public class AnimationItem {
        public static void SaveItem(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, ItemInfo item) {
            // if (item.Type != "Pose") return;
            
            HashSet<AnimationInfo> animations = new HashSet<AnimationInfo>();
            HashSet<ModelInfo> models = new HashSet<ModelInfo>();
            animations = FindLogic.Animation.FindAnimations(animations, models, new Common.STUGUID(item.GUID));

            string properType = item.Type;
            switch (item.Type) {
                case "Pose":
                    properType = "Victory Pose";
                    break;
            }
            
            string output = Path.Combine(basePath, containerName, heroName ?? "", properType, folderName, GetValidFilename(item.Name).Replace(".", ""));

            foreach (ModelInfo model in models) {
                Model.Save(flags, Path.Combine(output, "Models"), model, $"VictoryPose {item.Name}_{GUID.Index(model.GUID)}");
            }
            Animation.Save(flags, Path.Combine(output, "Animations"), animations);
        }
    }
}