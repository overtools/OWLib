#nullable enable
using System.Collections.Generic;
using System.Linq;
using DataTool.Helper;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;

namespace DataTool.DataModels {
    public class ProgressionUnlocks {
        /// <summary>
        /// "Other" Unlocks. Common examples are OWL skins and achievement rewards
        /// </summary>
        public Unlock[]? OtherUnlocks { get; set; }

        /// <summary>
        /// Unknown Unlocks
        /// </summary>
        public Unlock[]? UnknownUnlocks { get; set; }

        /// <summary>
        /// Unlocks granted at a specific level
        /// </summary>
        public LevelUnlocks[]? LevelUnlocks { get; set; }

        /// <summary>
        /// Loot Box specific unlocks
        /// </summary>
        public LootBoxUnlocks[]? LootBoxesUnlocks { get; set; }

        public ProgressionUnlocks(STUHero hero) {
            var unlocks = STUHelper.GetInstance<STUProgressionUnlocks>(hero.m_heroProgression);
            Init(unlocks);
        }

        public ProgressionUnlocks(ulong guid) {
            var unlocks = STUHelper.GetInstance<STUProgressionUnlocks>(guid);
            Init(unlocks);
        }

        private void Init(STUProgressionUnlocks? progressionUnlocks) {
            if (progressionUnlocks == null) return;

            if (progressionUnlocks.m_lootBoxesUnlocks != null) {
                LootBoxesUnlocks = new LootBoxUnlocks[progressionUnlocks.m_lootBoxesUnlocks.Length];

                for (int i = 0; i < progressionUnlocks.m_lootBoxesUnlocks.Length; i++) {
                    STULootBoxUnlocks lootBoxUnlocks = progressionUnlocks.m_lootBoxesUnlocks[i];
                    LootBoxesUnlocks[i] = new LootBoxUnlocks(lootBoxUnlocks);
                }
            }

            if (progressionUnlocks.m_7846C401 != null) {
                LevelUnlocks = new LevelUnlocks[progressionUnlocks.m_7846C401.Length];
                for (int i = 0; i < LevelUnlocks.Length; i++) {
                    var levelUnlocks = progressionUnlocks.m_7846C401[i];
                    LevelUnlocks[i] = new LevelUnlocks(levelUnlocks);
                }
            }

            OtherUnlocks = Unlock.GetArray(progressionUnlocks.m_otherUnlocks);
            UnknownUnlocks = Unlock.GetArray(progressionUnlocks.m_9135A4B2);
        }

        public IEnumerable<Unlock> IterateUnlocks() {
            if (LootBoxesUnlocks != null) {
                foreach (LootBoxUnlocks lootBoxUnlocks in LootBoxesUnlocks) {
                    foreach (Unlock VARIABLE in lootBoxUnlocks.Unlocks) {
                        yield return VARIABLE;
                    }
                }
            }

            if (LevelUnlocks != null) {
                foreach (LevelUnlocks levelUnlocks in LevelUnlocks) {
                    foreach (Unlock unlock in levelUnlocks.Unlocks) {
                        yield return unlock;
                    }
                }
            }

            if (UnknownUnlocks != null) {
                foreach (Unlock unlock in UnknownUnlocks) {
                    yield return unlock;
                }
            }

            if (OtherUnlocks != null) {
                foreach (Unlock unlock in OtherUnlocks) {
                    yield return unlock;
                }
            }
        }

        public IEnumerable<Unlock> GetUnlocksOfType(UnlockType type) {
            return IterateUnlocks().Where(x => x.Type == type);
        }
    }

    public class LootBoxUnlocks {
        public Unlock[] Unlocks { get; set; }
        public Enum_BABC4175 LootBoxType { get; set; }

        public LootBoxUnlocks(STULootBoxUnlocks lootBoxUnlocks) {
            LootBoxType = lootBoxUnlocks.m_lootBoxType;
            Unlocks = Unlock.GetArray(lootBoxUnlocks.m_unlocks);
        }
    }

    public class LevelUnlocks {
        public Unlock[] Unlocks { get; set; }
        public int Level { get; set; }

        public LevelUnlocks(STU_1757E817 levelUnlocks) {
            Level = levelUnlocks.m_level;
            Unlocks = Unlock.GetArray(levelUnlocks.m_unlocks);
        }
    }
}
