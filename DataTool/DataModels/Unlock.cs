using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.JSON;
using Newtonsoft.Json;
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
			var unlock = GetInstanceNew<STUUnlock>(guid);
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
			return Name ?? GetFileName(GUID);
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
				return "PlayerIcon";
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
			    STUUnlock stu = GetInstanceNew<STUUnlock>(guid);
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
}
