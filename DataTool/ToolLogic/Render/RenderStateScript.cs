using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using HealingML;
using TankLib;
using Newtonsoft.Json;
using TankLib.Helpers;
using TankLib.STU;
using Logger = TankLib.Helpers.Logger;

namespace DataTool.ToolLogic.Render {
    [Tool("render-statescript", Description = "Dump statescript to HML", CustomFlags = typeof(RenderFlags), IsSensitive = true)]
    public class RenderStateScript : ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (RenderFlags) toolFlags;
            var output = Path.Combine(flags.OutputPath, "Statescript", "HML");
            var settings = new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                TypeNameHandling = TypeNameHandling.All
            };
            settings.Converters.Add(new teResourceGUID_Newtonsoft());
            settings.Converters.Add(new ulong_Newtonsoft());
            settings.Converters.Add(new long_Newtonsoft());
            if (!Directory.Exists(output)) {
                Directory.CreateDirectory(output);
            }

            var serializers = new Dictionary<Type, ISerializer> {
                {typeof(teStructuredDataAssetRef<>), new teResourceGUIDSerializer()}
            };

            foreach (var type in new ushort[] {0x1B}) {
                if (!Directory.Exists(Path.Combine(output, type.ToString("X3")))) {
                    Directory.CreateDirectory(Path.Combine(output, type.ToString("X3")));
                }
                
                foreach (var guid in Program.TrackedFiles[type]) {
                    Logger.Log24Bit(ConsoleSwatch.XTermColor.Purple5, true, Console.Out, null, $"Saving {teResourceGUID.AsString(guid)}");
                    
                    using (Stream f = File.Open(Path.Combine(output, type.ToString("X3"), teResourceGUID.AsString(guid)), FileMode.Create))
                    using (Stream d = IO.OpenFile(guid)) {
                        d.CopyTo(f);
                    }

                    var stu = STUHelper.OpenSTUSafe(guid);

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
