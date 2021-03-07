using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DataTool.DataModels.GameModes;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels {
    [DataContract]
    public class MapHeader {
        [DataMember]
        public teResourceGUID GUID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Description2 { get; set; }

        [DataMember]
        public string Subline { get; set; }

        [DataMember]
        public string StateA { get; set; }

        [DataMember]
        public string StateB { get; set; }

        [DataMember]
        public string VariantName { get; set; }

        [DataMember]
        public teResourceGUID MapGUID { get; set; }

        [DataMember]
        public IEnumerable<GameModeLite> GameModes { get; set; }

        [DataMember]
        public Enum_668FA6B6 State { get; set; }

        [DataMember]
        public STUMapType MapType { get; set; }

        [DataMember]
        public teResourceGUID Thumbnail { get; set; }

        [DataMember]
        public teResourceGUID Image { get; set; }

        [DataMember]
        public teResourceGUID FlagImage { get; set; }

        public MapCelebrationVariant[] CelebrationVariants { get; set; }

        public MapHeader(ulong key) {
            STUMapHeader stu = GetInstance<STUMapHeader>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public MapHeader(STUMapHeader stu, ulong key = default) {
            Init(stu, key);
        }

        public void Init(STUMapHeader mapHeader, ulong key = default) {
            GUID = (teResourceGUID) key;
            Name = GetString(mapHeader.m_displayName);
            VariantName = GetString(mapHeader.m_1C706502);
            Description = GetString(mapHeader.m_389CB894);
            Description2 = GetString(mapHeader.m_ACB95597);
            Subline = GetString(mapHeader.m_EBCFAD22);
            StateA = GetString(mapHeader.m_8EBADA44);
            StateB = GetString(mapHeader.m_5AFE2F61);
            MapGUID = mapHeader.m_map;
            State = mapHeader.m_A125818B;
            MapType = mapHeader.m_mapType;
            Thumbnail = mapHeader.m_9386E669;
            Image = mapHeader.m_86C1CFAB;
            FlagImage = mapHeader.m_C6599DEB;
            GameModes = mapHeader.m_D608E9F3?.Select(x => new GameMode(x).ToLite()).Where(x => x.GUID != 0);

            if (mapHeader.m_celebrationOverrides != null) {
                CelebrationVariants = mapHeader.m_celebrationOverrides.Select(x => {
                    var hmm = (teResourceGUID) x.m_map;
                    hmm.SetType(0x9F);

                    return new MapCelebrationVariant {
                        Name = GetGUIDName(x.m_celebrationType),
                        Virtual01C = x.m_celebrationType,
                        MapInfo = new MapHeader(hmm).ToLite()
                    };
                }).ToArray();
            }

            // Attempt to get the menu map name if one exists
            var mapHeaderGuid = (teResourceGUID) mapHeader.m_map;
            mapHeaderGuid.SetType(0x9F);
            Name = GetNullableGUIDName(mapHeaderGuid) ?? Name;
        }

        public MapHeaderLite ToLite() {
            return new MapHeaderLite(this);
        }

        public string GetName() {
            return VariantName ?? Name ?? "Title Screen";
        }

        public string GetUniqueName() {
            string name = GetName();
            return $"{name}:{teResourceGUID.Index(MapGUID):X}";
        }
    }

    public class MapCelebrationVariant {
        [DataMember]
        public string Name;

        [DataMember]
        public teResourceGUID Virtual01C;

        [DataMember]
        public MapHeaderLite MapInfo;
    }

    // Lighter version of the MapHeader, just used to make JSON exports less thicc if you only need basic map info
    public class MapHeaderLite {
        [DataMember]
        public teResourceGUID GUID;

        [DataMember]
        public string Name;

        [DataMember]
        public string VariantName;

        public MapHeaderLite(MapHeader mapHeader) {
            GUID = mapHeader.GUID;
            Name = mapHeader.Name;
            VariantName = mapHeader.VariantName;
        }
    }
}
