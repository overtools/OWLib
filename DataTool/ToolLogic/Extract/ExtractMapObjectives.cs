using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using DataTool.SaveLogic;
using DataTool.ToolLogic.List;
using TankLib;
using TankLib.Math;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-map-objectives", Description = "Extract map objective bounds", CustomFlags = typeof(ExtractFlags))]
    public class ExtractMapObjectives : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            flags.EnsureOutputDirectory();
            var basePath = Path.Combine(flags.OutputPath, "MapObjectives");
            
            List<MapOutput> allMaps = new List<MapOutput>();
            
            foreach (ulong mapHeaderGUID in TrackedFiles[0x9F]) {
                STUMapHeader mapHeader = GetInstance<STUMapHeader>(mapHeaderGUID);
                if (mapHeader == null) continue;
                
                var mapInfo = ListMaps.GetMap(mapHeaderGUID);
                
                for (int variantIdx = 0; variantIdx < mapHeader.m_D97BC44F.Length; variantIdx++) {
                    var variantModeInfo = mapHeader.m_D97BC44F[variantIdx];
                    var variantResultingMap = mapHeader.m_78715D57[variantIdx];
                    var variantGUID = variantResultingMap.m_BF231F12;

                    using (Stream stream = IO.OpenFile(variantGUID)) {
                        if (stream == null) {
                            // not shipping
                            continue;
                        }
                    }
                    
                    var thisMap = ProcessMapVariant(mapHeader, variantGUID);
                    if (thisMap.Areas.Count == 0) {
                        // no objectives... so lets not bother
                        continue;
                    }
                    
                    thisMap.MapName = $"{mapInfo.GetName()} - {Map.GetVariantName(variantModeInfo, variantResultingMap)}";
                    allMaps.Add(thisMap);
                }
            }
            
            OutputJSON(allMaps, Path.Combine(basePath, "Areas.json"));
        }

        private static MapOutput ProcessMapVariant(STUMapHeader mapHeader, ulong variantGUID) {
            var relevantAreas = new HashSet<teUUID>();

            var placeableEntities = Map.GetPlaceableData(mapHeader, variantGUID, Enums.teMAP_PLACEABLE_TYPE.ENTITY);
            foreach (var placeableEntityRaw in placeableEntities.Placeables) {
                var placeableEntity = (teMapPlaceableEntity)placeableEntityRaw;

                var statescriptInstanceData = placeableEntity.InstanceData.OfType<STUStatescriptComponentInstanceData>().SingleOrDefault();
                if (statescriptInstanceData == null) continue;

                // todo: please remove null array "feature"...
                statescriptInstanceData.m_6D10093E ??= [];
                statescriptInstanceData.m_84D0983B ??= [];

                // detect common script which seems to be used by all objectives
                var isObjective = statescriptInstanceData.m_6D10093E.Concat(statescriptInstanceData.m_84D0983B)
                    .Any(x => x.m_graph.GUID.GUID == 0x0580000000000E8E);
                if (!isObjective) continue;

                var targetTagInstanceData = placeableEntity.InstanceData.OfType<STUTargetTagInstanceData>().SingleOrDefault();
                if (targetTagInstanceData == null) continue;
                if (targetTagInstanceData.m_2746D7E4.Value == Guid.Empty) continue;

                // an area referenced on the same entity = used for objective area
                relevantAreas.Add(targetTagInstanceData.m_2746D7E4);
            }
                    
            var thisMap = new MapOutput();
                    
            var placeableAreas = Map.GetPlaceableData(mapHeader, variantGUID, Enums.teMAP_PLACEABLE_TYPE.AREA);
            for (int j = 0; j < placeableAreas.Placeables.Length; j++) {
                var commonStructure = placeableAreas.CommonStructures[j];
                if (!relevantAreas.Contains(commonStructure.UUID)) continue;
                        
                var placeableArea = (teMapPlaceableArea)placeableAreas.Placeables[j];

                if (placeableArea.Header.SphereCount > 0) {
                    Console.Out.WriteLine($"{thisMap.MapName} - area {commonStructure.UUID} uses {placeableArea.Header.SphereCount} spheres. not supported");
                }
                if (placeableArea.Header.CapsuleCount > 0) {
                    Console.Out.WriteLine($"{thisMap.MapName} - area {commonStructure.UUID} uses {placeableArea.Header.CapsuleCount} capsules. not supported");
                }
                if (placeableArea.Header.Unknown1Count > 0) {
                    Console.Out.WriteLine($"{thisMap.MapName} - area {commonStructure.UUID} uses {placeableArea.Header.Unknown1Count} unk1s. not supported");
                }
                if (placeableArea.Header.Unknown2Count > 0) {
                    Console.Out.WriteLine($"{thisMap.MapName} - area {commonStructure.UUID} uses {placeableArea.Header.Unknown2Count} unk2s. not supported");
                }
                        
                thisMap.Areas.Add(new AreaInfo {
                    UUID = commonStructure.UUID.Value.ToString(),
                    Boxes = placeableArea.Boxes.Select(x => new BoxJson(x)).ToArray()
                });
            }

            return thisMap;
        }

        private static float[] ToFloatArray(Vector3 vec3) {
            return [vec3.X, vec3.Y, vec3.Z];
        }
        
        private static float[] ToFloatArray(Quaternion quat) {
            return [quat.X, quat.Y, quat.Z, quat.W];
        }

        private struct AreaInfo {
            public string UUID { get; set; }
            public BoxJson[] Boxes { get; set; }
        }

        private struct BoxJson {
            public float[] Orientation { get; set; }
            public float[] Translation { get; set; }
            public float[] Extents { get; set; }

            public BoxJson(teMapPlaceableArea.Box box) {
                Orientation = ToFloatArray(box.Orientation);
                Translation = ToFloatArray(box.Translation);
                Extents = ToFloatArray(box.Extents);
            }
        }
        
        private struct MapOutput {
            public string MapName { get; set; }
            public List<AreaInfo> Areas { get; set; }
            
            public MapOutput() {
                Areas = new List<AreaInfo>();
            }
        }
    }
}
