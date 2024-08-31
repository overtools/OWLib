using System;
using System.IO;
using DataTool.Flag;
using static DataTool.Program;
using static DataTool.Helper.IO;
using DataTool.Helper;
using TankLib;
using DragonLib.XML;
using TankLib.STU;
using System.Collections.Generic;
using System.Linq;
using DragonLib.Indent;
using TankLib.Helpers;

namespace DataTool.ToolLogic.Extract.Debug {
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

    [Tool("extract-stu-type", Description = "Extract all STUs of a type. Type not provided - extract defaults. Can output in XML (--XML)", CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractSTUType : ITool {

        static Dictionary<Type, IDragonMLSerializer> serializers = new Dictionary<Type, IDragonMLSerializer> {
                {typeof(teStructuredDataAssetRef<>), new teResourceGUIDSerializer()}
        };

        public void Parse(ICLIFlags toolFlags) {
            ExtractType(toolFlags);
        }

        static ushort[] default_types = {
                0x3, 0x15, 0x18, 0x1A, 0x1B, 0x1F, 0x20, 0x21, 0x24, 0x2C, 0x2D,
                0x2E, 0x2F, 0x30, 0x31, 0x32, 0x39, 0x3A, 0x3B, 0x45, 0x49, 0x4C, 0x4E, 0x51, 0x53, 0x54, 0x55, 0x58,
                0x5A, 0x5B, 0x5E, 0x5F, 0x62, 0x63, 0x64, 0x65, 0x66, 0x68, 0x70, 0x71, 0x72, 0x75, 0x78, 0x79, 0x7A,
                0x7F, 0x81, 0x90, 0x91, 0x95, 0x96, 0x97, 0x98, 0x9C, 0x9D, 0x9E, 0x9F, 0xA0, 0xA2, 0xA3, 0xA5, 0xA6,
                0xA8, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xB5, 0xB7, 0xBF, 0xC0, 0xC2, 0xC5, 0xC6, 0xC7, 0xC9, 0xCA, 0xCC,
                0xCE, 0xCF, 0xD0, 0xD4, 0xD5, 0xD6, 0xD7, 0xD9, 0xDC, 0xDF, 0xEB, 0xEC, 0xEE, 0xF8, 0x10D, 0x114, 0x116,
                0x11A, 0x122
        };

        public void ExtractType(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            string path = (toolFlags as ExtractFlags).OutputPath;

            if (toolFlags.Positionals.Length > 3) {
                Logger.Info($"Extracting {toolFlags.Positionals[3]} ");
                WriteType(Convert.ToUInt16(toolFlags.Positionals[3], 16), path, flags.ConvertToXML);
            } else {
                Logger.Info("Extracting most of STUs!");
                foreach (var type in default_types) {
                    WriteType(type, path, flags.ConvertToXML);
                }
            }
        }

        public void WriteType(ushort type, string path, bool convertToXML) {
            string thisPath = Path.Combine(path, type.ToString("X3"));
            foreach (ulong @ulong in TrackedFiles[type]) {
                if (!Directory.Exists(thisPath)) {
                    Directory.CreateDirectory(thisPath);
                }
                if (convertToXML) {
                    using (var stu = STUHelper.OpenSTUSafe(@ulong))
                    using (Stream f = File.Open(Path.Combine(thisPath, teResourceGUID.AsString(@ulong)) + ".xml", FileMode.Create))
                    using (TextWriter w = new StreamWriter(f)) {
                        DragonMLSettings settings = new DragonMLSettings();
                        settings.TypeSerializers = serializers;
                        settings.Namespaces["tank"] = "https://yretenai.com/dragonml/v1";
                        settings.Namespaces["hml"] = "https://yretenai.com/dragonml/v1";
                        w.WriteLine(DragonML.Print(stu?.Instances[0], settings));
                    }
                } else {
                    WriteFile(@ulong, thisPath);
                }
            }
        }
    }
}

