using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TankLib.Helpers;

namespace TankLibHelper.Modes {
    public class CreateClasses : IMode {
        public string Mode => "createclasses";
        private StructuredDataInfo _info;

        public ModeResult Run(string[] args) {
            if (args.Length < 2) {
                Console.Out.WriteLine("Missing required arg: \"output\"");
                return ModeResult.Fail;
            }
            string outDirectory = args[1];
            string dataDirectory;

            if (args.Length >= 3) {
                dataDirectory = args[2];
            } else {
                dataDirectory = StructuredDataInfo.GetDefaultDirectory();
            }

            string[] extraData = args.Skip(3).ToArray();

            _info = new StructuredDataInfo(dataDirectory);
            foreach (string extra in extraData) {
                _info.LoadExtra(extra);
            }

            const string stuTypeNamespace = "TankLib.STU.Types";
            const string stuEnumNamespace = "TankLib.STU.Types.Enums";
            
            string generatedDirectory = Path.Combine(outDirectory, "Generated");
            string generatedEnumsDirectory = Path.Combine(outDirectory, "Generated", "Enums");
            
            var genericTypeFile = new FileWriter(Path.Combine(generatedDirectory, "Misc.cs"), stuTypeNamespace);
            var genericEnumsFile = new FileWriter(Path.Combine(generatedEnumsDirectory, "Misc.cs"), stuEnumNamespace);
            List<FileWriter> extraFileWriters = new List<FileWriter>();
            
            Directory.CreateDirectory(generatedDirectory);
            Directory.CreateDirectory(generatedEnumsDirectory);

            void Build(ClassBuilder classBuilder, bool isEnum) {
                FileWriter fileWriter = isEnum ? genericEnumsFile : genericTypeFile;
                if (classBuilder.HasRealName) {
                    fileWriter = new FileWriter(Path.Combine(isEnum ? generatedEnumsDirectory : generatedDirectory, classBuilder.Name+".cs"), isEnum ? stuEnumNamespace : stuTypeNamespace);
                    extraFileWriters.Add(fileWriter);
                }
                classBuilder.Build(fileWriter);
            }

            Dictionary<uint, FieldNew> enumFields = new Dictionary<uint, FieldNew>();

            foreach (KeyValuePair<uint, InstanceNew> instance in _info.Instances.OrderBy(x => x.Value.Hash2)) {
                //if (_info.BrokenInstances.Contains(instance.Key)) {
                //    continue;
                //}
                
                //if (instance.Key == 0x440233A5) {  // for generating the mirror types with oldhash
                //    continue;
                //}
                
                if (_info.GetInstanceName(instance.Key) == "teStructuredData") continue;
                if (instance.Key == 0x2BB2C217) continue; // references mirror data. todo: handle better
                var tree = DumpHashes.GetParentTree(_info, instance.Value);
                if (tree.Contains(0x54D6A5F9u)) continue; // ignore MirrorData (thx tim)
                
                InstanceBuilder instanceBuilder = new InstanceBuilder(_info, instance.Value);
                Build(instanceBuilder, false);

                foreach (var field in instance.Value.m_fields) {
                    if (field.m_serializationType != 8 && field.m_serializationType != 9) continue;

                    var enumType = field.TypeHash2;
                    if (!enumFields.ContainsKey(enumType)) {
                        enumFields[enumType] = field;
                    }
                }
            }

            foreach (KeyValuePair<uint, EnumNew> enumData in _info.Enums.OrderBy(x => x.Value.Hash2)) {
                FieldNew field;
                if (!enumFields.TryGetValue(enumData.Key, out field)) {
                    field = new FieldNew {
                        m_typeHash = enumData.Key.ToString("X8"),
                        m_size = 4
                    };
                    Logger.Warn("Enum", $"Enum {enumData.Value.Hash2:X8} is not referenced by a field");
                }
                EnumBuilder enumBuilder = new EnumBuilder(_info, field);
                Build(enumBuilder, true);
            }

            genericTypeFile.Finish();
            genericEnumsFile.Finish();
            foreach (FileWriter writer in extraFileWriters) {
                writer.Finish();
            }

            return ModeResult.Success;
        }
    }
}