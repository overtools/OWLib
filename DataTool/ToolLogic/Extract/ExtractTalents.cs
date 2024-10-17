using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.ToolLogic.List;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-talents", Description = "Extract talents", CustomFlags = typeof(ExtractFlags))]
    public class ExtractTalents : ITool {
        public void Parse(ICLIFlags toolFlags) {
            SaveTalents(toolFlags);
        }

        public static void SaveTalents(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            flags.EnsureOutputDirectory();

            const string folderName = "Talents";
            
            Combo.ComboInfo info = new Combo.ComboInfo();
            foreach (var pair in ListTalents.GetData()) {
                Combo.Find(info, pair.Value.TextureGUID);
                info.SetTextureName(pair.Value.TextureGUID, pair.Value.Name);
            }

            var context = new SaveLogic.Combo.SaveContext(info);
            SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(flags.OutputPath, folderName), context);
        }
    }
}
