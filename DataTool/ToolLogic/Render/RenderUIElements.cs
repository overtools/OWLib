using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using DragonLib.Indent;
using DragonLib.XML;
using TankLib;
using TankLib.Helpers;
using TankLib.STU;
using Logger = TankLib.Helpers.Logger;

namespace DataTool.ToolLogic.Render {
    public class teResourceGUIDSerializer : IDragonMLSerializer {
        public DragonMLType OverrideTarget => DragonMLType.Object;

        private readonly Dictionary<Type, string> TargetMap = new Dictionary<Type, string>();

        public object Print(object instance, Dictionary<object, int> visited, IndentHelperBase indents, string fieldName, DragonMLSettings settings) {
            var hmlNameTag = fieldName == null ? "" : $" hml:name=\"{fieldName}\"";

            try {
                // ReSharper disable once InvertIf
                if (!TargetMap.TryGetValue(instance.GetType(), out var target)) {
                    target = instance.GetType().GenericTypeArguments.First().Name;
                    TargetMap[instance.GetType()] = target;
                }

                var hmlIdTag = string.Empty;
                if (!visited.ContainsKey(instance)) {
                    visited[instance] = visited.Count;
                }

                if (settings.UseRefId) {
                    hmlIdTag = $" hml:id=\"{visited[instance]}\"";
                }

                return $"{indents}<tank:ref{hmlIdTag}{hmlNameTag} GUID=\"{instance}\" Target=\"{target}\"/>\n";
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

            var serializers = new Dictionary<Type, IDragonMLSerializer> {
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
                        w.WriteLine(DragonML.Print(stu?.Instances[0], new DragonMLSettings {TypeSerializers = serializers}));
//                        w.WriteLine(JsonConvert.SerializeObject(stu?.Instances[0], Formatting.Indented, settings));
                    }
                }
            }
        }
    }
}
