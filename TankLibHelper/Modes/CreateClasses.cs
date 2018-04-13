using System;
using System.Collections.Generic;
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
            
            _info = new StructuredDataInfo(dataDirectory);
            BuilderConfig builderConfig = new BuilderConfig {
                Namespace = "TankLib.STU.Types"
            };

            Directory.CreateDirectory(Path.Combine(outDirectory, "Generated"));

            foreach (KeyValuePair<uint,STUInstanceJSON> instance in _info.Instances) {
                if (_info.BrokenInstances.Contains(instance.Key)) {
                    continue;
                }
                
                InstanceBuilder instanceBuilder = new InstanceBuilder(builderConfig, instance.Value, _info);

                string instanceCode = instanceBuilder.BuildCSharp();

                using (StreamWriter file = new StreamWriter(Path.Combine(outDirectory, "Generated", instanceBuilder.GetName()+".cs"))) {
                    file.Write(instanceCode);
                }
            }

            return ModeResult.Success;
        }
    }
}