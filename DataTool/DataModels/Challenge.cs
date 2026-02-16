#nullable enable
using System.Linq;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.DataModels;

public class Challenge {
    public teResourceGUID GUID { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }

    public GenericGUIDValue? Hero { get; set; }
    public teResourceGUID? IconGUID { get; set; }
    public teResourceGUID? SeasonGUID { get; set; }
    public teResourceGUID? CelebrationGUID { get; set; }
    public teResourceGUID? CategoryGUID { get; set; }
    public teResourceGUID? SubCategoryGUID { get; set; }

    public UnlockLite? RequiredUnlock { get; set; }
    public UnlockLite?[]? Rewards { get; set; }
    public ChallengePrerequisites? Prerequisites { get; set; }

    public Challenge(STU_F9392C2B stu, ulong key = default) {
        Init(stu, key);
    }

    public void Init(STU_F9392C2B stu, ulong key = default) {
        GUID = (teResourceGUID) key;
        Name = IO.GetString(stu.m_name);
        Description = IO.GetString(stu.m_description);
        IconGUID = stu.m_544A6A4F;
        CelebrationGUID = stu.m_B44A42A0;
        SeasonGUID = stu.m_29E273F8;
        CategoryGUID = stu.m_5E4619AF?.m_id;
        SubCategoryGUID = stu.m_CAF37A9E?.m_id;
        Hero = stu.m_hero != null ? new GenericGUIDValue(stu.m_hero, HeroVM.GetName(stu.m_hero)) : null;

        if (stu.m_41A13472 != null) {
            RequiredUnlock = Unlock.Load(stu.m_41A13472)?.ToLiteUnlock();
        }

        if (stu.m_DEB2CEFA != null) {
            Rewards = stu.m_DEB2CEFA.m_unlocks?.Select(x => Unlock.Load(x)?.ToLiteUnlock()).Where(x => x != null).ToArray();
        }

        if (stu.m_481F944B != null) {
            Prerequisites = new ChallengePrerequisites {
                ChallengeGUIDs = stu.m_481F944B.m_FB667EC7?.Select(x => x.m_A236519D?.GUID).Where(x => x != null).ToArray() ?? [],
                RequiredCount = (int) stu.m_481F944B.m_amount,
            };
        }
    }

    public static Challenge? Load(ulong guid) {
        var stu = STUHelper.GetInstance<STU_F9392C2B>(guid);
        if (stu == null) return null;
        return new Challenge(stu, guid);
    }

    public ChallengeLite ToLite() {
        return ChallengeLite.FromChallenge(this);
    }

    public class ChallengePrerequisites {
        public teResourceGUID?[]? ChallengeGUIDs { get; set; }
        public int RequiredCount { get; set; }
    }
}

public class ChallengeLite {
    public teResourceGUID? GUID { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }

    public static ChallengeLite FromChallenge(Challenge challenge) {
        return new ChallengeLite {
            GUID = challenge.GUID,
            Name = challenge.Name,
            Description = challenge.Description
        };
    }
}