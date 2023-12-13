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
            // example:
            // createclasses "C:/Users/User/Desktop/New folder/tanklibclasses2_08" "path to stu dump" Data DataPreHashChange
            
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

            if (!Directory.Exists(outDirectory))
                Directory.CreateDirectory(outDirectory);

            string generatedDirectory = Path.Combine(outDirectory, "Generated");
            string generatedEnumsDirectory = Path.Combine(outDirectory, "Generated", "Enums");

            FileWriter[] genericTypeFiles = new FileWriter[10];
            for (int i = 0; i < genericTypeFiles.Length; i++) {
                genericTypeFiles[i] = new FileWriter(Path.Combine(generatedDirectory, $"Misc_{i}.cs"), stuTypeNamespace);
            }
            var genericEnumsFile = new FileWriter(Path.Combine(generatedEnumsDirectory, "Misc.cs"), stuEnumNamespace);
            List<FileWriter> extraFileWriters = new List<FileWriter>();

            Directory.CreateDirectory(generatedDirectory);
            Directory.CreateDirectory(generatedEnumsDirectory);

            void Build(ClassBuilder classBuilder, bool isEnum) {
                FileWriter fileWriter = isEnum ? genericEnumsFile : genericTypeFiles[classBuilder.Hash % genericTypeFiles.Length];
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
                    enumFields.TryAdd(enumType, field);
                }
            }

            foreach (KeyValuePair<uint, EnumNew> enumData in _info.Enums.OrderBy(x => x.Value.Hash2)) {
                if (!enumFields.TryGetValue(enumData.Key, out var field)) {
                    field = new FieldNew {
                        m_typeHash = enumData.Key.ToString("X8"),
                        m_size = 4
                    };
                    Logger.Warn("Enum", $"Enum {enumData.Value.Hash2:X8} is not referenced by a field");
                }
                EnumBuilder enumBuilder = new EnumBuilder(_info, field);
                Build(enumBuilder, true);
            }

            // Jank code for generating missing enums if we don't have any data for them
            /*var missingEnums = enumFields.Select(x => x.Key).Except(_info.Enums.Select(x => x.Key));
            foreach (var missingEnum in missingEnums) {
                Logger.Warn("Enum", $"generating Missing Enum {missingEnum:X8}!");
                EnumBuilder enumBuilder = new EnumBuilder(_info, new FieldNew {
                    m_typeHash = missingEnum.ToString("X8"),
                    m_size = 4
                });

                Build(enumBuilder, true);
            }*/

            foreach (var genericTypeFile in genericTypeFiles) {
                genericTypeFile.Finish();
            }
            genericEnumsFile.Finish();
            foreach (FileWriter writer in extraFileWriters) {
                writer.Finish();
            }

            return ModeResult.Success;
        }
    }
}