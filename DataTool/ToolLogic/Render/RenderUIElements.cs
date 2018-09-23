using System.IO;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib;
using Newtonsoft.Json;

namespace DataTool.ToolLogic.Render {
    [Tool("render-ui-elements", Description = "Render UI elements", TrackTypes = new ushort[] {0x5E}, CustomFlags = typeof(RenderFlags), IsSensitive = true)]
    public class RenderUIElements : ITool {
        public void IntegrateView(object sender) {
            throw new System.NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            var flags = (RenderFlags) toolFlags;
            var output = Path.Combine(flags.OutputPath, "UI", "Render");
            var settings = new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.All,
            };
            settings.Converters.Add(new teResourceGUID_Newtonsoft());
            if (!Directory.Exists(output)) {
                Directory.CreateDirectory(output);
            }
            
            foreach (var guid in Program.TrackedFiles[0x5E]) {
                using (Stream f = File.Open(Path.Combine(output, teResourceGUID.AsString(guid)), FileMode.Create))
                using (Stream d = IO.OpenFile(guid)) {
                    d.CopyTo(f);
                }

                var stu = STUHelper.OpenSTUSafe(guid);

                using (Stream f = File.Open(Path.Combine(output, teResourceGUID.AsString(guid) + ".json"), FileMode.Create)) 
                using (TextWriter w = new StreamWriter(f)) {
                    w.WriteLine(JsonConvert.SerializeObject(stu?.Instances, Formatting.Indented, settings));
                }
            }
        }
    }
}
