using System.Linq;
using System.Runtime.Serialization;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    [DataContract]
    public class RankedSeason {
        [DataMember]
        public teResourceGUID GUID;

        [DataMember]
        public int SeasonNumber;

        [DataMember]
        public string Title;

        [DataMember]
        public string Title2;

        [DataMember]
        public string Description;

        [DataMember]
        public teResourceGUID Image;

        public SeasonRanks[] Ranks;
        
        [DataMember]
        public Unlock[] YouTriedUnlocks;

        [DataMember]
        public Unlock[] Top500Unlocks;

        public RankedSeason(ulong key) {
            var stu = GetInstance<STURankedSeason>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public RankedSeason(STURankedSeason stu) {
            Init(stu);
        }

        private void Init(STURankedSeason season, ulong key = default) {
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
            [DataMember]
            public uint Min;
            
            [DataMember]
            public uint Max;
            
            [DataMember]
            public Unlock[] Unlocks;

            public SeasonRanks(STU_A92F620A rank) {
                Min = rank.m_4BF7CD58;
                Max = rank.m_D2F14FFA;
                Unlocks = rank.m_unlocks?.m_unlocks?.Select(x => new Unlock(x)).ToArray();
            }
        }
    }
}