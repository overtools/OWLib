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
using Logger = TankLib.Helpers.Logger;

namespace DataTool.ToolLogic.Render {
    public class teResourceGUIDSerializer : ISerializer {
        public SerializationTarget OverrideTarget => SerializationTarget.Object;
        
        private readonly Dictionary<Type, string> TargetMap = new Dictionary<Type, string>();
        
        public object Print(object instance, IReadOnlyDictionary<Type, ISerializer> custom, HashSet<object> visited, IndentHelperBase indent, string fieldName) {
            var hmlNameTag = fieldName == null ? "" : $" hml:name=\"{fieldName}\"";

            try {
                // ReSharper disable once InvertIf
                if (!TargetMap.TryGetValue(instance.GetType(), out var target)) {
                    target = instance.GetType().GenericTypeArguments.First().Name;
                    TargetMap[instance.GetType()] = target;
                }
                return $"{indent}<tank:ref hml:id=\"{instance.GetHashCode()}\"{hmlNameTag} GUID=\"{instance}\" Target=\"{target}\"/>\n";
            } catch {
                return null;
            }
        }
    }
    
    [Tool("render-ui-elements", Description = "Render UI elements", CustomFlags = typeof(RenderFlags), IsSensitive = true)]
    public class RenderUIElements : ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (RenderFlags) toolFlags;
            var output = Path.Combine(flags.OutputPath, "UI", "Render");
            if (!Directory.Exists(output)) {
                Directory.CreateDirectory(output);
            }

            var serializers = new Dictionary<Type, ISerializer> {
                {typeof(teStructuredDataAssetRef<>), new teResourceGUIDSerializer()}
            };

            foreach (var type in new ushort[] {0x5E, 0x5A, 0x45}) {
                if (!Directory.Exists(Path.Combine(output, type.ToString("X3")))) {
                    Directory.CreateDirectory(Path.Combine(output, type.ToString("X3")));
                }
                
                foreach (var guid in Program.TrackedFiles[type]) {
                    Logger.Log24Bit(ConsoleSwatch.XTermColor.Purple5, true, Console.Out, null, $"Saving {teResourceGUID.AsString(guid)}");
                    
                    using (Stream f = File.Open(Path.Combine(output, type.ToString("X3"), teResourceGUID.AsString(guid)), FileMode.Create))
                    using (Stream d = IO.OpenFile(guid)) {
                        d.CopyTo(f);
                    }
                    
                    using (var stu = STUHelper.OpenSTUSafe(guid))
                    using (Stream f = File.Open(Path.Combine(output, type.ToString("X3"), teResourceGUID.AsString(guid) + ".xml"), FileMode.Create))
                    using (TextWriter w = new StreamWriter(f)) {
                        w.WriteLine(Serializer.Print(stu?.Instances[0], serializers));
//                        w.WriteLine(JsonConvert.SerializeObject(stu?.Instances[0], Formatting.Indented, settings));
                    }
                }
            }
        }
    }
}
