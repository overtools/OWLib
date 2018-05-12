using System.Collections.Generic;
using System.IO;

namespace TankLibHelper.Modes {
    public class DumpHashes : IMode {
        public string Mode => "dumphashes";

        public ModeResult Run(string[] args) {
            string output = args[1];

            Directory.CreateDirectory(output);
            
            string dataPath = StructuredDataInfo.GetDefaultDirectory();
            if (args.Length >= 3) {
                dataPath = args[2];
            }
            
            StructuredDataInfo info = new StructuredDataInfo(dataPath);
            
            WriteFile(info.Instances, Path.Combine(output, "hashes.txt"));
            
            return ModeResult.Success;
        }

        public static void WriteFile(Dictionary<uint, STUInstanceJSON> source, string output) {
            using (StreamWriter writer = new StreamWriter(output)) {
                foreach (KeyValuePair<uint, STUInstanceJSON> hashPair in source) {
                    writer.WriteLine($"{hashPair.Key:X8}");
                }
            }
        }
    }
}