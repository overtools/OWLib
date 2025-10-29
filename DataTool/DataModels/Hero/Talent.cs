#nullable enable
using System.Linq;
using DataTool.Helper;
using TankLib;

using STUTalentBase = TankLib.STU.Types.STU_DF0481B0;
using STUTalent = TankLib.STU.Types.STU_BDDF370E;
using STUPerk = TankLib.STU.Types.STU_42B75C40;

namespace DataTool.DataModels.Hero;

public class Talent {
    public teResourceGUID GUID { get; set; }
    public ETalentType TalentType = ETalentType.Unknown;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public teResourceGUID TextureGUID { get; set; }
    public HeroLoadout? Loadout;
    public int MaxCount;
    public int Cost;
    public GenericGUIDValue? Hero;
    public GenericGUIDValue? Rarity;
    public GenericGUIDValue? Category;

    public int Level;
    public bool Major;

    public Talent(STUTalentBase stu, ulong key = default) {
        Init(stu, key);
    }

    public void Init(STUTalentBase stu, ulong key = default) {
        if (stu is STUTalent talent) {
            TalentType = ETalentType.Talent;
            Name = IO.GetString(talent.m_name);
            Description = IO.GetString(talent.m_description);
            TextureGUID = talent.m_544A6A4F;
            MaxCount = talent.m_BBD71D14;
            Cost = talent.m_925E7392;

            if (talent.m_hero != null) {
                Hero = new GenericGUIDValue(talent.m_hero, HeroVM.GetName(talent.m_hero));
            }

            if (talent.m_672DA932 != null) {
                Rarity = new GenericGUIDValue(talent.m_672DA932, IO.GetNullableGUIDName(talent.m_672DA932));
            }

            if (talent.m_category?.m_id != null) {
                Category = new GenericGUIDValue(talent.m_category.m_id, IO.GetNullableGUIDName(talent.m_category.m_id));
            }

            if (talent.m_loadout != null) {
                var hero = FindHeroForLoadout(talent.m_loadout);

                Loadout = new HeroLoadout {
                    GUID = talent.m_loadout,
                    Name = Helpers.GetLoadoutById(talent.m_loadout)?.Name,
                    HeroGUID = hero?.GUID ?? null,
                    HeroName = hero?.Name,
                };
            }
        } else if (stu is STUPerk perk) {
            TalentType = ETalentType.Perk;
            if (perk.m_loadout != null) {
                var hero = FindHeroForLoadout(perk.m_loadout);

                Loadout = new HeroLoadout {
                    GUID = perk.m_loadout,
                    Name = Helpers.GetLoadoutById(perk.m_loadout)?.Name,
                    HeroGUID = hero?.GUID ?? null,
                    HeroName = hero?.Name,
                };
            }

            Name = Helpers.GetLoadoutById(perk.m_loadout)?.Name;
            Level = (int) perk.m_4DDE5023;
            Major = perk.m_D60C9EA2 != 0;
        }
    }

    public static Talent? Load(ulong guid) {
        var stu = STUHelper.GetInstance<STUTalentBase>(guid);
        if (stu == null) return null;
        return new Talent(stu, guid);
    }

    private static HeroVM? FindHeroForLoadout(teResourceGUID loadoutGuid) {
        foreach (var (heroGuid, hero) in Helpers.GetHeroes()) {
            if (!hero.IsHero || hero.Loadouts == null) continue;
            if (hero.Loadouts.Any(loadout => loadout.GUID == loadoutGuid)) {
                return hero;
            }
        }

        return null;
    }

    public record HeroLoadout {
        public teResourceGUID GUID;
        public string? Name;
        public teResourceGUID? HeroGUID;
        public string? HeroName;
    }

    public enum ETalentType {
        Unknown = 0,
        Talent = 1,
        Perk = 2,
    }
}

