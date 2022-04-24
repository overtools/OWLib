using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TACTLib;
using TankLib;
using TankLib.STU;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    /// <summary>
    /// Unlock data model
    /// </summary>
    [DataContract]
    public class Unlock {
        [DataMember]
        public teResourceGUID GUID;

        /// <summary>
        /// Name of this unlock
        /// </summary>
        [DataMember]
        public string Name;

        /// <summary>
        /// DataTool enum for the type of Unlock
        /// </summary>
        [DataMember]
        public UnlockType Type;

        /// <summary>
        /// Unlock rarity
        /// </summary>
        /// <see cref="STUUnlockRarity"/>
        [DataMember]
        public STUUnlockRarity Rarity;

        /// <summary>
        /// Description of this unlock
        /// </summary>
        [DataMember]
        public string Description;

        /// <summary>
        /// Where this unlock can be obtained from
        /// </summary>
        /// <example>"Available in Halloween Loot Boxes"</example>
        [DataMember]
        public string AvailableIn;

        /// <summary>
        /// If the Unlock is a Skin, the GUID of the SkinTheme
        /// </summary>
        [DataMember]
        public teResourceGUID SkinThemeGUID;

        /// <summary>
        /// Array of categories the Unlock belongs to that the Hero Gallery & Career Profile filtering options use
        /// </summary>
        [DataMember]
        public string[] Categories;

        /// <summary>
        /// If the Unlock is a form of Currency, the amount of currency it is
        /// </summary>
        [DataMember]
        public int Currency;

        [DataMember]
        public Enum_BABC4175 LootBoxType;

        [DataMember]
        public bool IsEsportsUnlock;

        [DataMember]
        public string EsportsTeam;

        /// <summary>
        /// Internal Unlock STU
        /// </summary>
        [IgnoreDataMember]
        public STU_3021DDED STU;

        /// <summary>
        /// Whether this is a "normal" Unlock like a skin, emote, voiceline, pose, icon, etc and not something like a Lootbox or Currency.
        /// </summary>
        [IgnoreDataMember]
        public bool IsTraditionalUnlock;

        // These types are specific to certain unlocks so don't show them unless we're on that unlock
        public bool ShouldSerializeLootBoxType() => Type == UnlockType.Lootbox;
        public bool ShouldSerializeSkinThemeGUID() => Type == UnlockType.Skin;
        public bool ShouldSerializeEsportsTeam() => IsEsportsUnlock;
        public bool ShouldSerializeCurrency() => Type == UnlockType.CompetitiveCurrency || Type == UnlockType.Currency || Type == UnlockType.OWLToken;

        // These only really apply to "normal" unlocks and can be removed from others
        public bool ShouldSerializeAvailableIn() => IsTraditionalUnlock;
        public bool ShouldSerializeCategories() => IsTraditionalUnlock;

        public Unlock(STU_3021DDED unlock, ulong guid) {
            Init(unlock, guid);
        }

        public Unlock(ulong guid) {
            var unlock = GetInstance<STU_3021DDED>(guid);
            if (unlock == null) return;
            Init(unlock, guid);
        }

        private void Init(STU_3021DDED unlock, ulong guid) {
            GUID = (teResourceGUID) guid;
            STU = unlock;

            Name = GetString(unlock.m_name)?.TrimEnd(' '); // ffs blizz, why do the names end in a space sometimes
            AvailableIn = GetString(unlock.m_53145FAF);
            Rarity = unlock.m_rarity;
            Description = GetDescriptionString(unlock.m_3446F580);
            Type = GetTypeForUnlock(unlock);

            IsTraditionalUnlock =
                Type == UnlockType.Icon || Type == UnlockType.Spray ||
                Type == UnlockType.Skin || Type == UnlockType.HighlightIntro ||
                Type == UnlockType.VictoryPose || Type == UnlockType.VoiceLine ||
                Type == UnlockType.Emote;

            if (unlock.m_BEE9BCDA != null)
                Categories = unlock.m_BEE9BCDA.Select(x => GetGUIDName(x.GUID)).ToArray();

            // Lootbox and currency unlocks have some additional relevant data
            switch (unlock) {
                case STUUnlock_CompetitiveCurrency stu:
                    Currency = stu.m_760BF18E;
                    break;
                case STUUnlock_Currency stu:
                    Currency = stu.m_currency;
                    break;
                case STUUnlock_OWLToken stu:
                    Currency = stu.m_63A026AF;
                    break;
                case STUUnlock_LootBox stu:
                    Rarity = stu.m_2F922165;
                    LootBoxType = stu.m_lootboxType;
                    break;
                case STUUnlock_SkinTheme stu:
                    SkinThemeGUID = stu.m_skinTheme;
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
        /// Returns the UnlockType for an Unlock
        /// </summary>
        /// <param name="unlock">Source unlock</param>
        /// <returns>Friendly type name</returns>
        private static UnlockType GetTypeForUnlock(STUUnlock unlock) {
            return GetUnlockType(unlock.GetType());
        }

        /// <summary>
        /// Returns the UnlockType for a STUUnlock Type
        /// </summary>
        /// <param name="type">unlock stu type</param>
        /// <returns></returns>
        public static UnlockType GetUnlockType(Type type) {
            if (type == typeof(STUUnlock_SkinTheme)) {
                return UnlockType.Skin;
            }

            if (type == typeof(STUUnlock_AvatarPortrait)) {
                return UnlockType.Icon;
            }

            if (type == typeof(STUUnlock_Emote)) {
                return UnlockType.Emote;
            }

            if (type == typeof(STUUnlock_Pose)) {
                return UnlockType.VictoryPose;
            }

            if (type == typeof(STUUnlock_VoiceLine)) {
                return UnlockType.VoiceLine;
            }

            if (type == typeof(STUUnlock_SprayPaint)) {
                return UnlockType.Spray;
            }

            if (type == typeof(STUUnlock_Currency)) {
                return UnlockType.Currency;
            }

            if (type == typeof(STUUnlock_PortraitFrame)) {
                return UnlockType.PortraitFrame;
            }

            if (type == typeof(STUUnlock_Weapon)) {
                return UnlockType.WeaponSkin;
            }

            if (type == typeof(STUUnlock_POTGAnimation)) {
                return UnlockType.HighlightIntro;
            }

            if (type == typeof(STUUnlock_HeroMod)) {
                return UnlockType.HeroMod;
            }

            if (type == typeof(STUUnlock_CompetitiveCurrency)) {
                return UnlockType.CompetitiveCurrency;
            }

            if (type == typeof(STUUnlock_OWLToken)) {
                return UnlockType.OWLToken;
            }

            if (type == typeof(STUUnlock_LootBox)) {
                return UnlockType.Lootbox;
            }

            Logger.Debug("Unlock", $"Unknown unlock type ${type}");
            return UnlockType.Unknown;
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
        WeaponSkin,
        Lootbox,
        PortraitFrame, // borders
        Currency, // gold
        CompetitiveCurrency, // competitive points
        OWLToken,
        HeroMod, // wot? unused?
    }
}