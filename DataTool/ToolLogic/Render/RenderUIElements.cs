using System;
using System.IO;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using Newtonsoft.Json;
using TankLib.Helpers;
using Logger = TankLib.Helpers.Logger;

namespace DataTool.ToolLogic.Render {
    [Tool("render-ui-elements", Description = "Render UI elements", CustomFlags = typeof(RenderFlags), IsSensitive = true)]
    public class RenderUIElements : ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (RenderFlags) toolFlags;
            var output = Path.Combine(flags.OutputPath, "UI", "Render");
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

                    var stu = STUHelper.OpenSTUSafe(guid);

                    using (Stream f = File.Open(Path.Combine(output, type.ToString("X3"), teResourceGUID.AsString(guid) + ".json"), FileMode.Create))
                    using (TextWriter w = new StreamWriter(f)) {
                        w.WriteLine(JsonConvert.SerializeObject(stu?.Instances[0], Formatting.Indented, settings));
                    }
                }
            }
        }
    }
}
