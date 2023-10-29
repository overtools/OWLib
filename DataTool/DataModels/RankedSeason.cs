using System.Linq;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    public class RankedSeason {
        public teResourceGUID GUID { get; set; }
        public int SeasonNumber { get; set; }
        public string Title { get; set; }
        public string Title2 { get; set; }
        public string Description { get; set; }
        public teResourceGUID Image { get; set; }
        public SeasonRanks[] Ranks;
        public Unlock[] YouTriedUnlocks { get; set; }
        public Unlock[] Top500Unlocks { get; set; }

        public RankedSeason(ulong key) {
            var stu = GetInstance<STURankedSeason>(key);
            Init(stu, key);
        }

        public RankedSeason(STURankedSeason stu, ulong key = default) {
            Init(stu, key);
        }

        private void Init(STURankedSeason season, ulong key = default) {
            if (season == null) return;

            GUID = (teResourceGUID) key;
            SeasonNumber = season.m_rankedSeason;
            Image = season.m_21EB3E73;
            Title = GetString(season.m_1E4E5957);
            Title2 = GetString(season.m_B804C4DB);
            Ranks = season.m_5BB8DFF3.Select(x => new SeasonRanks(x)).ToArray();
            YouTriedUnlocks = season.m_58066D8F?.m_unlocks?.Select(x => new Unlock(x)).ToArray();
            Top500Unlocks = season.m_heroicUnlocks?.m_unlocks?.Select(x => new Unlock(x)).ToArray();
        }

        public class SeasonRanks {
            public uint Min { get; set; }
            public uint Max { get; set; }
            public Unlock[] Unlocks { get; set; }

            public SeasonRanks(STU_A92F620A rank) {
                Min = rank.m_4BF7CD58;
                Max = rank.m_D2F14FFA;
                Unlocks = rank.m_unlocks?.m_unlocks?.Select(x => new Unlock(x)).ToArray();
            }
        }
    }
}
