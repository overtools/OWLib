using DataTool.Flag;

namespace DataTool.SaveLogic.Unlock {
    public static class AnimationItem {
        public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock) {
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, unlock.GUID);

            var context = new Combo.SaveContext(info) {
                m_saveAnimationEffects = false // todo: unsupported here due to relative paths used by OWEffect
            };
            Combo.Save(flags, directory, context);
            Combo.SaveAllAnimations(flags, directory, context);
        }
    }
}
