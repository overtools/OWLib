using System.Collections.Generic;
using System.Linq;
using TACTLib;
using TankLib;
using TankLib.STU;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    public class Unlock {
        public teResourceGUID GUID { get; set; }

        /// <summary>
        /// Name of this unlock
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// DataTool enum for the type of Unlock
        /// </summary>
        public UnlockType Type { get; set; }

        /// <summary>
        /// Unlock rarity
        /// </summary>
        public STUUnlockRarity Rarity { get; set; }

        /// <summary>
        /// Description of this unlock
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Where this unlock can be obtained from
        /// </summary>
        /// <example>"Available in Shop"</example>
        public string AvailableIn { get; set; }

        /// <summary>
        /// Battle.net Product Id
        /// </summary>
        public long ProductId { get; set; }

        /// <summary>
        /// If the Unlock is a Skin, the GUID of the SkinTheme
        /// </summary>
        public teResourceGUID SkinThemeGUID { get; set; }

        /// <summary>
        /// If the Unlock is a Hero, the GUID of the Hero
        /// </summary>
        public teResourceGUID HeroGUID { get; set; }

        /// <summary>
        /// If this unlock belongs to an ESports Team
        /// </summary>
        public bool IsEsportsUnlock { get; set; }

        /// <summary>
        /// If this unlock belongs to an ESports Team, the name of the team
        /// </summary>
        public string EsportsTeam { get; set; }

        /// <summary>
        /// Array of categories the Unlock belongs to that the Hero Gallery & Career Profile filtering options use
        /// </summary>
        public string[] Categories { get; set; }

        /// <summary>
        ///  If the Unlock is a form of Currency or XP, the amount granted
        /// </summary>
        public int? Amount { get; set; }

        public Enum_BABC4175 LootBoxType { get; set; }

        /// <summary>
        /// Whether this is a "normal" Unlock like a skin, emote, voiceline, pose, icon, etc and not something like a Lootbox or Currency.
        /// </summary>
        internal bool IsTraditionalUnlock { get; set; }
        internal STU_3021DDED STU { get; set; }

        // These types are specific to certain unlocks so don't show them unless we're on that unlock
        public bool ShouldSerializeLootBoxType() => Type == UnlockType.Lootbox;
        public bool ShouldSerializeSkinThemeGUID() => Type == UnlockType.Skin;
        public bool ShouldSerializeHeroGUID() => Type == UnlockType.Hero;
        public bool ShouldSerializeIsEsportsUnlock() => IsEsportsUnlock || Type == UnlockType.Skin;
        public bool ShouldSerializeEsportsTeam() => IsEsportsUnlock;
        public bool ShouldSerializeAmount() => Amount != null;

        // These only really apply to "normal" unlocks and can be removed from others
        public bool ShouldSerializeAvailableIn() => IsTraditionalUnlock;
        public bool ShouldSerializeCategories() => IsTraditionalUnlock;

        public Unlock(STU_3021DDED unlock, ulong key = default) {
            Init(unlock, key);
        }

        public Unlock(ulong key) {
            var unlock = GetInstance<STU_3021DDED>(key);
            Init(unlock, key);
        }

        private void Init(STU_3021DDED unlock, ulong key) {
            if (unlock == null) return;

            GUID = (teResourceGUID) key;
            STU = unlock;

            Name = GetCleanString(unlock.m_name);
            AvailableIn = GetString(unlock.m_53145FAF);
            Rarity = unlock.m_rarity;
            Description = GetDescriptionString(unlock.m_3446F580);
            Type = GetUnlockType(unlock);
            ProductId = unlock.m_00B16A0B;

            IsTraditionalUnlock =
                Type == UnlockType.Icon || Type == UnlockType.Spray ||
                Type == UnlockType.Skin || Type == UnlockType.HighlightIntro ||
                Type == UnlockType.VictoryPose || Type == UnlockType.VoiceLine ||
                Type == UnlockType.Emote || Type == UnlockType.Souvenir ||
                Type == UnlockType.NameCard || Type == UnlockType.PlayerTitle ||
                Type == UnlockType.WeaponCharm || Type == UnlockType.WeaponVariant ||
                Type == UnlockType.WeaponSkin;

            if (unlock.m_BEE9BCDA != null) {
                Categories = unlock.m_BEE9BCDA
                    .Where(x => x.GUID != 0)
                    .Select(x => GetGUIDName(x.GUID)).ToArray();
            }

            // Lootbox and currency unlocks have some additional relevant data
            switch (unlock) {
                case STUUnlock_CompetitiveCurrency stu:
                    Amount = stu.m_760BF18E;
                    break;
                case STUUnlock_Currency stu:
                    Amount = stu.m_currency;
                    break;
                case STUUnlock_OWLToken stu:
                    Amount = stu.m_63A026AF;
                    break;
                case STUUnlock_LootBox stu:
                    Rarity = stu.m_2F922165;
                    LootBoxType = stu.m_lootboxType;
                    break;
                case STUUnlock_SkinTheme stu:
                    SkinThemeGUID = stu.m_skinTheme;
                    break;
                case STU_C3C6FD9E stu:
                    HeroGUID = stu.m_hero;
                    break;
                case STU_514C0F6B stu:
                    Amount = (int) stu.m_amount;
                    break;
                case STU_7A1A4764 stu:
                    Amount = stu.m_E0A45C1B;
                    break;
            }

            if (unlock.m_0B1BA7C1 != null) {
                var teamDefinition = new TeamDefinition(unlock.m_0B1BA7C1);
                if (teamDefinition.Id != 0) {
                    IsEsportsUnlock = true;
                    EsportsTeam = teamDefinition.FullName;
                }
            }
        }

        public string GetName() {
            return Name ?? GetFileName(GUID);
        }

        /// <summary>
        /// Returns the UnlockType for a STUUnlock Type
        /// </summary>
        /// <param name="stu">the unlock stu to get the type for</param>
        public static UnlockType GetUnlockType(STUInstance stu) {
            var unlockType = stu switch
            {
                STUUnlock_SkinTheme _ => UnlockType.Skin,
                STUUnlock_AvatarPortrait _ => UnlockType.Icon,
                STU_A458D547 _ => UnlockType.Souvenir, // has to be before emote because it inherits from it
                STUUnlock_Emote _ => UnlockType.Emote,
                STUUnlock_Pose _ => UnlockType.VictoryPose,
                STUUnlock_VoiceLine _ => UnlockType.VoiceLine,
                STUUnlock_SprayPaint _ => UnlockType.Spray,
                STUUnlock_Currency _ => UnlockType.Currency,
                STUUnlock_PortraitFrame _ => UnlockType.PortraitFrame,
                STUUnlock_Weapon _ => UnlockType.WeaponVariant,
                STUUnlock_POTGAnimation _ => UnlockType.HighlightIntro,
                STUUnlock_CompetitiveCurrency _ => UnlockType.CompetitiveCurrency,
                STUUnlock_OWLToken _ => UnlockType.OWLToken,
                STUUnlock_LootBox _ => UnlockType.Lootbox,
                STU_6A808718 _ => UnlockType.WeaponCharm,
                STU_C3C6FD9E _ => UnlockType.Hero,
                STU_7A1A4764 _ => UnlockType.BattlePassXP,
                STU_DB1B05B5 _ => UnlockType.NameCard,
                STU_514C0F6B _ => UnlockType.OverwatchCoins,
                STU_52AB57E9 _ => UnlockType.PlayerTitle,
                STU_AD84E2AA _ => UnlockType.BattlePass,
                STU_184D5944 _ => UnlockType.SeasonXPBoost,
                STU_1EB22BDB _ => UnlockType.Unknown,
                STU_80C1169E _ => UnlockType.BattlePassTierSkip,
                STU_3F17D547 _ => UnlockType.SkinComponent,
                STU_A85D31BF _ => UnlockType.StoryMission,
                STU_2448F3AA _ => UnlockType.WeaponSkin,
                _ => UnlockType.Unknown
            };

            if (unlockType == UnlockType.Unknown) {
                Logger.Debug("Unlock", $"Unknown unlock type {stu.GetType()}");
            }

            return unlockType;
        }

        /// <summary>
        /// Get an array of <see cref="Unlock"/> from an array of GUIDs
        /// </summary>
        /// <param name="guids">GUID collection</param>
        /// <returns>Array of <see cref="Unlock"/></returns>
        public static Unlock[] GetArray(IEnumerable<ulong> guids) {
            if (guids == null) return new Unlock[0];
            List<Unlock> unlocks = new List<Unlock>();
            foreach (ulong guid in guids) {
                STU_3021DDED stu = GetInstance<STU_3021DDED>(guid);
                if (stu == null) continue;
                Unlock unlock = new Unlock(stu, guid);
                unlocks.Add(unlock);
            }

            return unlocks.ToArray();
        }

        /// <summary>Get an array of <see cref="Unlock"/> from STUUnlocks</summary>
        /// <inheritdoc cref="GetArray(System.Collections.Generic.IEnumerable{ulong})"/>
        public static Unlock[] GetArray(STUUnlocks unlocks) {
            return GetArray(unlocks?.m_unlocks);
        }

        public static Unlock[] GetArray(teStructuredDataAssetRef<STUUnlock>[] unlocks) {
            return GetArray(unlocks?.Select(x => (ulong) x));
        }

        public UnlockLite ToLiteUnlock() {
            return UnlockLite.FromUnlock(this);
        }

        public STU_3021DDED GetSTU() => STU;
    }

    public class UnlockLite {
        public teResourceGUID GUID { get; set; }
        public string Name { get; set; }
        public UnlockType Type { get; set; }
        public STUUnlockRarity Rarity { get; set; }
        public int? Amount { get; set; }

        public bool ShouldSerializeAmount() => Amount != null;

        public static UnlockLite FromUnlock(Unlock unlock) {
            return new UnlockLite {
                GUID = unlock.GUID,
                Name = unlock.Name,
                Type = unlock.Type,
                Rarity = unlock.Rarity,
                Amount = unlock.Amount
            };
        }
    }

    public enum UnlockType {
        Unknown, // :david:
        Skin,
        Icon,
        Spray,
        Emote,
        VictoryPose,
        HighlightIntro,
        VoiceLine,
        WeaponVariant, // competitive reward
        Lootbox,
        PortraitFrame, // borders
        Currency, // legacy credits
        CompetitiveCurrency, // competitive points
        OWLToken,
        OverwatchCoins,
        SeasonXPBoost,
        WeaponCharm,
        PlayerTitle,
        BattlePass,
        BattlePassXP,
        Souvenir,
        NameCard,
        Hero,
        SkinComponent,
        BattlePassTierSkip,
        StoryMission,
        WeaponSkin, // ow2 weapon skin
    }
}