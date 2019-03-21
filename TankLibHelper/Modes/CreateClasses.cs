using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

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
            
            BuilderConfig instanceBuilderConfig = new BuilderConfig {
                Namespace = "TankLib.STU.Types"
            };
            BuilderConfig enumBuilderConfig = new BuilderConfig {
                Namespace = "TankLib.STU.Types.Enums"
            };

            string generatedDirectory = Path.Combine(outDirectory, "Generated");
            string generatedEnumsDirectory = Path.Combine(outDirectory, "Generated", "Enums");
            Directory.CreateDirectory(generatedDirectory);
            Directory.CreateDirectory(generatedEnumsDirectory);

            Dictionary<uint, STUFieldJSON> enumFields = new Dictionary<uint, STUFieldJSON>();

            foreach (KeyValuePair<uint, STUInstanceJSON> instance in _info.Instances) {
                if (_info.BrokenInstances.Contains(instance.Key)) {
                    continue;
                }
                //if (instance.Key == 0x440233A5) {  // for generating the mirror types with oldhash
                //    continue;
                //}

                InstanceBuilder instanceBuilder = new InstanceBuilder(instanceBuilderConfig, _info, instance.Value);
                
                BuildAndWriteCSharp(instanceBuilder, generatedDirectory);

                foreach (var field in instance.Value.Fields) {
                    if (field.SerializationType != 8 && field.SerializationType != 9) continue;

                    var enumType = uint.Parse(field.Type, NumberStyles.HexNumber);
                    if (!enumFields.ContainsKey(enumType)) {
                        enumFields[enumType] = field;
                        
                        EnumBuilder enumBuilder = new EnumBuilder(enumBuilderConfig, _info, field);
                        BuildAndWriteCSharp(enumBuilder, generatedEnumsDirectory);
                    }
                }
            }

            foreach (KeyValuePair<uint, STUEnumJSON> enumData in _info.Enums) {
                if (enumFields.ContainsKey(enumData.Key)) continue;
                EnumBuilder enumBuilder = new EnumBuilder(enumBuilderConfig, _info, new STUFieldJSON {
                    Type = enumData.Key.ToString("X8"),
                    Size = 4
                });
                BuildAndWriteCSharp(enumBuilder, generatedEnumsDirectory);
            }


            return ModeResult.Success;
        }

        public void BuildAndWriteCSharp(ClassBuilder builder, string directory) {
            string instanceCode = builder.BuildCSharp();
            if (instanceCode == null) return;

            using (StreamWriter file = new StreamWriter(Path.Combine(directory, builder.GetName()+".cs"))) {
                file.Write(instanceCode);
            }
        }
    }
}