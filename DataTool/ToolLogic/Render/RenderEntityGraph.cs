using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.SaveLogic;
using DataTool.ToolLogic.List;
using HealingML;
using TankLib;
using TankLib.Helpers;
using TankLib.STU;
using TankLib.STU.Types;
using Logger = TankLib.Helpers.Logger;

namespace DataTool.ToolLogic.Render {
    [Tool("render-map-entity-graph", Description = "Dump entity 003 to HML", CustomFlags = typeof(RenderFlags), IsSensitive = true)]
    public class RenderEntityGraph : ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (RenderFlags) toolFlags;
            var output = Path.Combine(flags.OutputPath, "EntityGraph", "HML");
            if (!Directory.Exists(output)) {
                Directory.CreateDirectory(output);
            }

            var serializers = new Dictionary<Type, ISerializer> {
                {typeof(teStructuredDataAssetRef<>), new teResourceGUIDSerializer()},
                {typeof(teResourceGUID), new teResourceGUIDSerializer()}
            };


            foreach (var guid in Program.TrackedFiles[0x9F]) {
                STUMapHeader map = STUHelper.GetInstance<STUMapHeader>(guid);
                if (map == null) continue;
                MapHeader mapHeader = new MapHeader(map);
                var placeable = Map.GetPlaceableData(map, Enums.teMAP_PLACEABLE_TYPE.ENTITY);
                var path = Path.Combine(output, IO.GetValidFilename(mapHeader.GetName()));
                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }
                Logger.Log24Bit(ConsoleSwatch.XTermColor.Purple5, true, Console.Out, null, $"Saving {teResourceGUID.AsString(guid)}");
                using (Stream f = File.Open(Path.Combine(path, "entities.xml"), FileMode.Create))
                using (TextWriter w = new StreamWriter(f)) {
                    w.WriteLine(Serializer.Print(placeable.Placeables, serializers));
                }
                
                foreach (var entity in placeable.Placeables) {
                    if (!(entity is teMapPlaceableEntity entityPlaceable)) continue;
                    var stu = STUHelper.OpenSTUSafe(entityPlaceable.Header.EntityDefinition);
                    if (stu == null || stu.Instances.Length == 0) continue;
                    using (Stream f = File.Open(Path.Combine(path, $"{teResourceGUID.AsString(entityPlaceable.Header.EntityDefinition)}.xml"), FileMode.Create))
                    using (TextWriter w = new StreamWriter(f)) {
                        w.WriteLine(Serializer.Print(stu.Instances.FirstOrDefault(), serializers));
                    }
                }
            }
        }
    }
}
