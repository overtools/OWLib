using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels.GameModes;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels {
    public class MapHeader {
        public teResourceGUID GUID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Description2 { get; set; }
        public string Subline { get; set; }
        public string StateA { get; set; }
        public string StateB { get; set; }
        public string VariantName { get; set; }
        public teResourceGUID MapGUID { get; set; }
        public IEnumerable<GameModeLite> GameModes { get; set; }
        public Enum_668FA6B6 State { get; set; }
        public STUMapType MapType { get; set; }
        public teResourceGUID Thumbnail { get; set; }
        public teResourceGUID Image { get; set; }
        public teResourceGUID FlagImage { get; set; }
        public MapCelebrationVariant[] CelebrationVariants { get; set; }
        public MapVariation[] Variations { get; set; }

        public MapHeader(ulong key) {
            STUMapHeader stu = GetInstance<STUMapHeader>(key);
            Init(stu, key);
        }

        public MapHeader(STUMapHeader stu, ulong key = default) {
            Init(stu, key);
        }

        public void Init(STUMapHeader mapHeader, ulong key = default) {
            if (mapHeader == null) return;

            GUID = (teResourceGUID) key;
            Name = GetCleanString(mapHeader.m_displayName);
            VariantName = GetCleanString(mapHeader.m_overrideName);
            Description = GetCleanString(mapHeader.m_389CB894);
            Description2 = GetCleanString(mapHeader.m_ACB95597);
            Subline = GetCleanString(mapHeader.m_EBCFAD22);
            StateA = GetCleanString(mapHeader.m_8EBADA44);
            StateB = GetCleanString(mapHeader.m_5AFE2F61);
            MapGUID = mapHeader.m_map;
            State = mapHeader.m_A125818B;
            MapType = mapHeader.m_mapType;
            Thumbnail = mapHeader.m_0342E00E?.m_smallMapIcon;
            Image = mapHeader.m_0342E00E?.m_loadingScreen;
            FlagImage = mapHeader.m_0342E00E?.m_loadingScreenFlag;
            GameModes = mapHeader.m_supportedGameModes?
                .Select(x => new GameMode(x).ToLite())
                .Where(x => x.GUID != 0);

            if (mapHeader.m_celebrationOverrides != null) {
                CelebrationVariants = mapHeader.m_celebrationOverrides.Select(info => {
                    var celebrationMapHeaderGuid = ((teResourceGUID) info.m_map).WithType(0x9F);
                    var celebrationMapInfo = new MapHeaderLite(celebrationMapHeaderGuid);
                    return new MapCelebrationVariant(info.m_celebrationType, GetGUIDName(info.m_celebrationType), celebrationMapInfo);
                }).ToArray();
            }

            var mapVariations = new List<MapVariation>();
            for (int i = 0; i < mapHeader.m_D97BC44F.Length; i++) {
                var variantModeInfo = mapHeader.m_D97BC44F[i];
                var variantResultingMap = mapHeader.m_78715D57[i];
                var variantGuid = variantModeInfo.m_A9253C68;
                var variantName = GetString(variantResultingMap.m_0342E00E?.m_D978BBDC);

                var variation = new MapVariation(variantGuid, variantName);
                mapVariations.Add(variation);
            }

            // Multiple variations can have the same GUID but different Map GUIDs, we only care about unique variation ids here
            Variations = mapVariations.DistinctBy(x => x.GUID).ToArray();

            // Attempt to get the menu map name if one exists
            var mapHeaderGuid = ((teResourceGUID) mapHeader.m_map).WithType(0x9F);
            Name = GetNullableGUIDName(mapHeaderGuid) ?? Name;
        }

        public MapHeaderLite ToLite() {
            return new MapHeaderLite(this);
        }

        public string GetName() {
            return VariantName ?? Name ?? "Title Screen";
        }

        public static string GetName(ulong key) {
            var stu = GetInstance<STUMapHeader>(key);
            if (stu == null) return null;
            return GetNullableGUIDName(key) ?? GetString(stu.m_overrideName) ?? GetString(stu.m_displayName) ?? "Unknown";
        }

        public string GetUniqueName() {
            string name = GetName();
            return $"{name}:{teResourceGUID.Index(MapGUID):X}";
        }
    }

    public record MapCelebrationVariant(teResourceGUID GUID, string Name, MapHeaderLite MapInfo);
    public record MapVariation(teResourceGUID GUID, string Name);

    // Lighter version of the MapHeader, just used to make JSON exports less thicc if you only need basic map info
    public class MapHeaderLite {
        public teResourceGUID GUID;
        public string Name;
        public string VariantName;

        public MapHeaderLite(ulong key) {
            STUMapHeader stu = GetInstance<STUMapHeader>(key);
            if (stu == null) return;
            Init(stu, key);
        }

        public MapHeaderLite(STUMapHeader stu, ulong key = default) {
            Init(stu, key);
        }

        public void Init(STUMapHeader mapHeader, ulong key = default) {
            GUID = (teResourceGUID) key;
            Name = GetCleanString(mapHeader.m_displayName);
            VariantName = GetCleanString(mapHeader.m_overrideName);
        }

        public MapHeaderLite(MapHeader mapHeader) {
            GUID = mapHeader.GUID;
            Name = mapHeader.Name;
            VariantName = mapHeader.VariantName;
        }
    }
}
