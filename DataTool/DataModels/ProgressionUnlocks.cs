using DataTool.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;

namespace DataTool.DataModels {
    /// <summary>
    /// Progression data model
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class ProgressionUnlocks {
        /// <summary>
        /// "Other" Unlocks. Common examples are OWL skins and achievement rewards
        /// </summary>
        public Unlock[] OtherUnlocks;
        
        /// <summary>
        /// Unknown Unlocks
        /// </summary>
        public Unlock[] UnknownUnlocks;
        
        /// <summary>
        /// Unlocks granted at a specific level
        /// </summary>
        public LevelUnlocks[] LevelUnlocks;
        
        /// <summary>
        /// Loot Box specific unlocks
        /// </summary>
        public LootBoxUnlocks[] LootBoxesUnlocks;

        public ProgressionUnlocks(STUHero hero) {
            var unlocks = STUHelper.GetInstance<STUProgressionUnlocks>(hero.m_heroProgression);
            Init(unlocks);
        }

        private void Init(STUProgressionUnlocks progressionUnlocks) {
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
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class LootBoxUnlocks {
        /// <summary>
        /// Unlocks
        /// </summary>
        public Unlock[] Unlocks;
        
        /// <summary>
        /// Loot Box type
        /// </summary>
        /// <see cref="Enum_BABC4175"/>
       [JsonConverter(typeof(StringEnumConverter))]
        public Enum_BABC4175 LootBoxType;

        public LootBoxUnlocks(STULootBoxUnlocks lootBoxUnlocks) {
            LootBoxType = lootBoxUnlocks.m_lootboxType;
            Unlocks = Unlock.GetArray(lootBoxUnlocks.m_unlocks);
        }
    }

    /// <summary>
    /// Level Unlocks data model
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class LevelUnlocks {
        /// <summary>
        /// Unlocks
        /// </summary>
        public Unlock[] Unlocks;
        
        /// <summary>
        /// Level unlocked at
        /// </summary>
        public int Level;

        public LevelUnlocks(STU_1757E817 levelUnlocks) {
            Level = levelUnlocks.m_level;
            Unlocks = Unlock.GetArray(levelUnlocks.m_unlocks);
        }
    }
}