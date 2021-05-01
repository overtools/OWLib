using System;
using System.Collections.Generic;
using System.IO;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.Extract;
using DataTool.ToolLogic.Render;
using DragonLib.XML;
using TankLib;
using TankLib.Helpers;
using TankLib.STU;
using Logger = TankLib.Helpers.Logger;

namespace DataTool.ToolLogic.Dbg {
    [Tool("dbg-dump-stu", Description = "I've fallen and I can't get up", IsSensitive = true, CustomFlags = typeof(ExtractFlags))]
    class DebugDumpSTU : ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            var output = Path.Combine(flags.OutputPath, "Dump", "STU");
            if (!Directory.Exists(output)) {
                Directory.CreateDirectory(output);
            }

            var serializers = new Dictionary<Type, IDragonMLSerializer> {
                {typeof(teStructuredDataAssetRef<>), new teResourceGUIDSerializer()}
            };

            foreach (var type in new ushort[] {
                0x3, 0x15, 0x18, 0x1A, 0x1B, 0x1F, 0x20, 0x21, 0x24, 0x2C, 0x2D,
                0x2E, 0x2F, 0x30, 0x31, 0x32, 0x39, 0x3A, 0x3B, 0x45, 0x49, 0x4C, 0x4E, 0x51, 0x53, 0x54, 0x55, 0x58,
                0x5A, 0x5B, 0x5E, 0x5F, 0x62, 0x63, 0x64, 0x65, 0x66, 0x68, 0x70, 0x71, 0x72, 0x75, 0x78, 0x79, 0x7A,
                0x7F, 0x81, 0x90, 0x91, 0x95, 0x96, 0x97, 0x98, 0x9C, 0x9D, 0x9E, 0x9F, 0xA0, 0xA2, 0xA3, 0xA5, 0xA6,
                0xA8, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xB5, 0xB7, 0xBF, 0xC0, 0xC2, 0xC5, 0xC6, 0xC7, 0xC9, 0xCA, 0xCC,
                0xCE, 0xCF, 0xD0, 0xD4, 0xD5, 0xD6, 0xD7, 0xD9, 0xDC, 0xDF, 0xEB, 0xEC, 0xEE, 0xF8, 0x10D, 0x114, 0x116,
                0x11A, 0x122
            }) {
                if (!Directory.Exists(Path.Combine(output, type.ToString("X3")))) {
                    Directory.CreateDirectory(Path.Combine(output, type.ToString("X3")));
                }

                foreach (var guid in Program.TrackedFiles[type]) {
                    try {
                        Logger.Log24Bit(ConsoleSwatch.XTermColor.Purple5, true, Console.Out, null, $"Saving {teResourceGUID.AsString(guid)}");

                        using (var stu = STUHelper.OpenSTUSafe(guid))
                        using (Stream f = File.Open(Path.Combine(output, type.ToString("X3"), teResourceGUID.AsString(guid) + ".xml"), FileMode.Create))
                        using (TextWriter w = new StreamWriter(f)) {
                            w.WriteLine(DragonML.Print(stu?.Instances[0], new DragonMLSettings {TypeSerializers = serializers}));
                        }
                    } catch (Exception e) {
                        Logger.Error("STU", e.ToString());
                    }
                }
            }
        }
    }
}
