using System;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-gamemode-images", Description = "Extracts gamemode and arcade images", CustomFlags = typeof(ExtractFlags))]
    public class ExtractGamemodeImages : ITool {
        public void Parse(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            const string container = "GamemodeImages";
            string path = Path.Combine(basePath, container);

            foreach (ulong key in TrackedFiles[0xEE]) {
                var stuE3594B8E = Helper.STUHelper.GetInstance<STU_E3594B8E>(key);

                if (stuE3594B8E == null) {
                    continue;
                }

                string name = $"{teResourceGUID.Index(key):X3}_{GetString(stuE3594B8E.m_name)}";

                Combo.ComboInfo info = new Combo.ComboInfo();
                Combo.Find(info, (ulong) stuE3594B8E.m_21EB3E73);
                info.SetTextureName((ulong) stuE3594B8E.m_21EB3E73, name);

                var context = new SaveLogic.Combo.SaveContext(info);
                SaveLogic.Combo.SaveLooseTextures(flags, path, context);
            }
        }
    }
}
