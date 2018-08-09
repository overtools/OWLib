using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.JSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TankLib.STU;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
	/// <summary>
	/// Unlock data model
	/// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class Unlock {
		/// <summary>
		/// Name of this unlock
		/// </summary>
	    public string Name;
		
		/// <summary>
		/// Description of this unlock
		/// </summary>
		public string Description;
		
		/// <summary>
		/// Unlock rarity
		/// </summary>
		/// <see cref="STUUnlockRarity"/>
		[JsonConverter(typeof(StringEnumConverter))]
	    public STUUnlockRarity Rarity;
		
		/// <summary>
		/// Where this unlock can be obtained from
		/// </summary>
		/// <example>"Available in Halloween Loot Boxes"</example>
	    public string AvailableIn;

		/// <summary>
		/// Friendly type name
		/// </summary>
	    public string Type;
	    
		/// <summary>
		/// Internal StructuredData
		/// </summary>
	    [JsonIgnore]
	    public STUUnlock STU;

		[JsonConverter(typeof(GUIDConverter))]
		public ulong GUID;

	    public Unlock(STUUnlock unlock, ulong guid) {
		    Init(unlock, guid);
	    }
		
		public Unlock(ulong guid) {
			var unlock = GetInstance<STUUnlock>(guid);
			if (unlock == null) return;
			Init(unlock, guid);
		}

		private void Init(STUUnlock unlock, ulong guid) {
			Name = GetString(unlock.m_name)?.TrimEnd(' '); // ffs blizz, why do the names end in a space sometimes
			AvailableIn = GetString(unlock.m_53145FAF);
			Rarity = unlock.m_rarity;
			Description = GetDescriptionString(unlock.m_3446F580);

			GUID = guid;
			STU = unlock;

			Type = GetTypeName(unlock);
		}

		public string GetName() {
			return Name?.Replace(".", "") ?? GetFileName(GUID);
		}

	    /// <summary>
	    /// Get user friendly type name
	    /// </summary>
	    /// <param name="unlock">Source unlock</param>
	    /// <returns>Friendly type name</returns>
	    /// <exception cref="NotImplementedException">Unlock type is unknown</exception>
	    private static string GetTypeName(STUUnlock unlock) {
		    return GetTypeName(unlock.GetType());
	    }

		public static string GetTypeName(Type type) {
			if (type == typeof(STUUnlock_SkinTheme)) {
				return "Skin";
			}
			if (type == typeof(STUUnlock_AvatarPortrait)) {
				return "Icon";
			}
			if (type == typeof(STUUnlock_Emote)) {
				return "Emote";
			}
			if (type == typeof(STUUnlock_Pose)) {
				return "VictoryPose";
			}
			if (type == typeof(STUUnlock_VoiceLine)) {
				return "VoiceLine";
			}
			if (type == typeof(STUUnlock_SprayPaint)) {
				return "Spray";
			}
			if (type == typeof(STUUnlock_Currency)) {
				return "Currency";
			}
			if (type == typeof(STUUnlock_PortraitFrame)) {
				return "PortraitFrame";
			}
			if (type == typeof(STUUnlock_Weapon)) {
				return "WeaponSkin";
			}
			if (type == typeof(STUUnlock_POTGAnimation)) {
				return "HighlightIntro";
			}
			if (type == typeof(STUUnlock_HeroMod)) {
				return "HeroMod";  // wtf
			}

			throw new NotImplementedException($"Unknown Unlock Type: {type}");
		}

	    /// <summary>
	    /// Get an array of <see cref="Unlock"/> from an array of GUIDs
	    /// </summary>
	    /// <param name="guids">GUID collection</param>
	    /// <returns>Array of <see cref="Unlock"/></returns>
	    public static Unlock[] GetArray(IEnumerable<ulong> guids) {
		    if (guids == null) return null;
		    List<Unlock> unlocks = new List<Unlock>();
		    foreach (ulong guid in guids) {
			    STUUnlock stu = GetInstance<STUUnlock>(guid);
			    if (stu == null) continue;
			    Unlock unlock = new Unlock(stu, guid);
			    unlocks.Add(unlock);
		    }
		    return unlocks.ToArray();
	    }
		
		/// <summary>Get an array of <see cref="Unlock"/> from STUUnlocks</summary>
		/// <inheritdoc cref="GetArray(System.Collections.Generic.IEnumerable{ulong})"/>>
	    public static Unlock[] GetArray(STUUnlocks unlocks) {
		    return GetArray(unlocks?.m_unlocks);
	    }

		public static Unlock[] GetArray(teStructuredDataAssetRef<STUUnlock>[] unlocks) {
			return GetArray(unlocks?.Select(x => (ulong) x));
		}
	}

	public static class UnlockData {
		//public static readonly ulong[] SummerGames2016 = new ulong[] {0, 1, 2, 3};
		//public static readonly ulong[] SummerGames2017 = new ulong[] {0, 1, 2, 3};
		public static readonly ulong[] SummerGames2018 = {
			0x250000000001716,
			0x250000000001A8B,
			0x25000000000170B,
			0x250000000001A88,
			0x2500000000015F2,
			0x250000000001A89,
			0x250000000001A8A,
			0x2500000000011AE,
			0x2500000000016D2,
			0x250000000001A86,
			0x250000000001A87,
			0x250000000001A5A,
			0x250000000001ABE,
			0x250000000001952,
			0x250000000001A84,
			0x250000000001A51,
			0x2500000000011D7,
			0x2500000000011B6,
			0x250000000001ABF,
			0x250000000001062,
			0x250000000001A85,
			0x250000000001ABB,
			0x250000000001A3C,
			0x250000000001A67,
			0x250000000001A66,
			0x250000000001ABD,
			0x250000000001A58,
			0x250000000001A64,
			0x250000000001A6A,
			0x250000000001A6D,
			0x250000000001A3A,
			0x250000000001AB3,
			0x250000000001AB4,
			0x250000000001A3B,
			0x250000000001A6B,
			0x250000000001A68,
			0x250000000001AB1,
			0x250000000001AB2,
			0x250000000001A6C,
			0x250000000001A69,
			0x250000000001AAF,
			0x250000000001AB0,
			0x2500000000012A5,
			0x250000000001A7E,
			0x250000000001A81,
			0x250000000001A77,
			0x2500000000013A5,
			0x250000000001A7B,
			0x250000000001A7A,
			0x2500000000013A6,
			0x250000000001A7C,
			0x250000000001A79,
			0x250000000001A78,
			0x250000000001A7F,
			0x250000000001A82,
			0x250000000001A80,
			0x250000000001A7D,
			0x250000000001A83,
			0x250000000001A8C
		};
	}
}
