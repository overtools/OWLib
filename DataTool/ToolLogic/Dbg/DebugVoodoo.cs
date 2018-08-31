using System;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.Extract;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.ToolLogic.Dbg {
    [Tool("brrap", Description = "I hear da call", TrackTypes = new ushort[] {0x5E}, IsSensitive = true, CustomFlags = typeof(ExtractFlags))]
    class DebugVoodoo : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            foreach (var guid in Program.TrackedFiles[0x5E]) {
                using (Stream f = File.OpenWrite($@"C:\Overwatch\05E\{teResourceGUID.AsString(guid)}"))
                using (Stream d = IO.OpenFile(guid)) {
                    d.CopyTo(f);
                }

                var stu = STUHelper.OpenSTUSafe(guid);
                var texs = stu.GetInstances<STUUXTextureSource>();

                var info = new Combo.ComboInfo();
                
                foreach (var tex in texs) {
                    Combo.Find(info, tex.m_textureGUID);
                }
                SaveLogic.Combo.SaveLooseTextures(toolFlags, $@"C:\Overwatch\Tex\{teResourceGUID.AsString(guid)}\", info);
            }
        }
    }
}
