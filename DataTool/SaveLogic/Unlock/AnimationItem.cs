using DataTool.Flag;

namespace DataTool.SaveLogic.Unlock {
    public static class AnimationItem {
        public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock) {
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, unlock.GUID);
            
            info.SaveConfig.SaveAnimationEffects = false;  // todo: unsupported here due to relative paths used by OWEffect
            
            Combo.Save(flags, directory, info);
            Combo.SaveAllAnimations(flags, directory, info);
        }
    }
}