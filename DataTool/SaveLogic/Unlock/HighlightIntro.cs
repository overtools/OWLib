using DataTool.Flag;
using DataTool.Helper;
using TankLib.Helpers;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;

namespace DataTool.SaveLogic.Unlock {
    public static class HighlightIntro {
        public static void Save(ICLIFlags flags, string directory, DataModels.Unlock unlock, STUHero hero) {
            if (unlock.GetSTU() is STUUnlock_POTGAnimation { m_type: POTGType.Default }) {
                SaveHeroicIntro(flags, directory, hero);
                return;
            }
            
            AnimationItem.Save(flags, directory, unlock);
        }

        private static void SaveHeroicIntro(ICLIFlags flags, string directory, STUHero hero) {
            if (hero == null) {
                // sanity
                Logger.Error(nameof(HighlightIntro), "No hero associated to highlight intro");
                return;
            }
            
            // heroic highlight intros do not use the animation field
            // instead it uses the hero select entity which has a blend tree

            var heroSelectEntity = STUHelper.GetInstance<STUModelComponent>(hero.m_322C521A);
            if (heroSelectEntity == null) {
                Logger.Error(nameof(HighlightIntro), "Unable to load heroic animation entity");
                return;
            }
            
            // we don't really want to extract the whole hero
            // so select only the blend tree
            var comboInfo = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(comboInfo, heroSelectEntity.m_animBlendTreeSet);

            var saveContext = new Combo.SaveContext(comboInfo) {
                m_saveAnimationEffects = false // not supported in loose mode
            };
            Combo.Save(flags, directory, saveContext);
            Combo.SaveAllAnimations(flags, directory, saveContext);
        }
    }
}
