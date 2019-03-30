using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using HealingML;
using TankLib;
using TankLib.Helpers;
using TankLib.STU;
using TankLib.STU.Types;
using Logger = TankLib.Helpers.Logger;

namespace DataTool.ToolLogic.Render {
    [Tool("render-statescript", Description = "Dump statescript to HML", CustomFlags = typeof(RenderFlags), IsSensitive = true)]
    public class RenderStateScript : ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (RenderFlags) toolFlags;
            var output = Path.Combine(flags.OutputPath, "Statescript", "HML");
            if (!Directory.Exists(output)) {
                Directory.CreateDirectory(output);
            }

            var serializers = new Dictionary<Type, ISerializer> {
                {typeof(teStructuredDataAssetRef<>), new teResourceGUIDSerializer()}
            };

            foreach (var type in new ushort[] {0x75}) {
                if (!Directory.Exists(Path.Combine(output, type.ToString("X3")))) {
                    Directory.CreateDirectory(Path.Combine(output, type.ToString("X3")));
                }
                
                foreach (var guid in Program.TrackedFiles[type]) {
                    Logger.Log24Bit(ConsoleSwatch.XTermColor.Purple5, true, Console.Out, null, $"Saving {teResourceGUID.AsString(guid)}");
                    
                    var stu = STUHelper.OpenSTUSafe(guid);

                    var hero = stu.GetInstance<STUHero>();
                    var name = IO.GetString(hero.m_0EDCE350);

                    var gameplay = hero.m_gameplayEntity.GetSTU<STUEntityDefinition, ulong>();
                    if (gameplay == null) continue;
                    var dir = Path.Combine(output, type.ToString("X3"), IO.GetValidFilename(name ?? guid.ToString()));
                    if (!Directory.Exists(dir)) {
                        Directory.CreateDirectory(dir);
                    }
                    
                    var graphComponent = gameplay.m_componentMap.Values.OfType<STUStatescriptComponent>().FirstOrDefault();
                    var graphInstanceComponent = gameplay.m_componentMap.Values.OfType<STUStatescriptComponentInstanceData>().FirstOrDefault();

                    using (Stream f = File.Open(Path.Combine(dir, "entity.xml"), FileMode.Create))
                    using (TextWriter w = new StreamWriter(f)) {
                        w.WriteLine(Serializer.Print(gameplay, serializers));
                    }

                    if (graphComponent != null) {
                        using (Stream f = File.Open(Path.Combine(dir, "graphComponent.xml"), FileMode.Create))
                        using (TextWriter w = new StreamWriter(f)) {
                            w.WriteLine(Serializer.Print(graphComponent, serializers));
                        }

                        foreach (var graphGuid in graphComponent.m_B634821A) {
                            var graph = STUHelper.GetInstance<STUStatescriptGraph>(graphGuid.m_graph);
                            if (graph == null) continue;
                            
                            using (Stream f = File.Open(Path.Combine(dir, $"graph_{teResourceGUID.LongKey(graphGuid.m_graph):X}.xml"), FileMode.Create))
                            using (TextWriter w = new StreamWriter(f)) {
                                w.WriteLine(Serializer.Print(graph, serializers));
                            }
                        }
                    }

                    if (graphInstanceComponent != null) {
                        using (Stream f = File.Open(Path.Combine(dir, "graphInstanceComponent.xml"), FileMode.Create))
                        using (TextWriter w = new StreamWriter(f)) {
                            w.WriteLine(Serializer.Print(graphInstanceComponent, serializers));
                        }

                        foreach (var graphGuid in graphInstanceComponent.m_6D10093E) {
                            var graph = STUHelper.GetInstance<STUStatescriptGraph>(graphGuid.m_graph);
                            if (graph == null) continue;
                            
                            using (Stream f = File.Open(Path.Combine(dir, $"graphInstance_{teResourceGUID.LongKey(graphGuid.m_graph):X}.xml"), FileMode.Create))
                            using (TextWriter w = new StreamWriter(f)) {
                                w.WriteLine(Serializer.Print(graph, serializers));
                            }
                        }
                    }
                }
            }
        }
    }
}
