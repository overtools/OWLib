using System;
using System.Collections.Generic;
using System.Data.HashFunction.CRC;
using System.IO;
using System.Text;

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

            ICRC crc32 = CRCFactory.Instance.Create(CRCConfig.CRC32);

            foreach (KeyValuePair<uint, string> instance in _info.KnownInstances) {
                //if (instance.Value.StartsWith("STUStatescript")) {
                if (instance.Value.StartsWith("M")) continue;
                string mirrorType = instance.Value.Replace("STU", "M");
                if (!mirrorType.StartsWith("M")) continue;
                uint hash = BitConverter.ToUInt32(crc32.ComputeHash(Encoding.ASCII.GetBytes(mirrorType.ToLowerInvariant())).Hash, 0);

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