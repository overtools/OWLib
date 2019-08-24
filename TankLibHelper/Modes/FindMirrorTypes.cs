using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TankLib.Helpers.Hash;

namespace TankLibHelper.Modes {
    public class FindMirrorTypes : IMode {
        public string Mode => "findmirrortypes";
        private StructuredDataInfo _info;

        public ModeResult Run(string[] args) {
            if (args.Length < 2) {
                Console.Out.WriteLine("Missing required arg: \"output\"");
                return ModeResult.Fail;
            }
            string dataDirectory;

            if (args.Length >= 2) {
                dataDirectory = args[1];
            } else {
                dataDirectory = StructuredDataInfo.GetDefaultDirectory();
            }

            _info = new StructuredDataInfo(dataDirectory);

            foreach (KeyValuePair<uint, string> instance in _info.KnownInstances) {
                //if (instance.Value.StartsWith("STUStatescript")) {
                if (instance.Value.StartsWith("M")) continue;
                string mirrorType = instance.Value.Replace("STU", "M");
                if (!mirrorType.StartsWith("M")) continue;

                var nameBytes = Encoding.ASCII.GetBytes(mirrorType.ToLowerInvariant());
                uint hash = CRC.CRC32(nameBytes);

                if (_info.Instances.ContainsKey(hash)) {
                    Console.Out.WriteLine($"{hash:X8}, {mirrorType}");
                }
                //}
            }

            return ModeResult.Success;
        }

        public void BuildAndWriteCSharp(ClassBuilder builder, string directory) {
            string instanceCode = builder.BuildCSharp();

            using (StreamWriter file = new StreamWriter(Path.Combine(directory, builder.GetName()+".cs"))) {
                file.Write(instanceCode);
            }
        }
    }
}