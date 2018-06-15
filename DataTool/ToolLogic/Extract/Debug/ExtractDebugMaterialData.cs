using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-materialdata", Description = "Extract material data hashes (debug)", TrackTypes = new ushort[] {0x86}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugMaterialData : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetSoundbanks(toolFlags);
        }

        public void GetSoundbanks(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            uint[] missing = {0x72955E70, 0x8CA81224, 0x62081FBD, 0xC1BE91CC, 0xEB5DC7CE, 0xBC78DB46};
            
            Dictionary<uint, int> count = new Dictionary<uint, int>();

            foreach (ulong guid in Program.TrackedFiles[0xB3]) {
                teMaterialData instance = new teMaterialData(IO.OpenFile(guid));
                
                if (instance.BufferParts == null) continue;
                int thisCount = 0;
                foreach (teMaterialDataBufferPart bufferPart in instance.BufferParts) {
                    if (missing.Contains(bufferPart.Header.Hash)) {
                        //Console.Out.WriteLine($"Found {bufferPart.Header.Hash} in {teResourceGUID.AsString(guid)}");
                        if (!count.ContainsKey(bufferPart.Header.Hash)) {
                            count[bufferPart.Header.Hash] = 0;
                        }

                        thisCount++;

                        count[bufferPart.Header.Hash]++;
                    }
                }

                if (thisCount == missing.Length-1) {
                    
                }
            }

            foreach (KeyValuePair<uint,int> pair in count) {
                Console.Out.WriteLine($"{pair.Key:X8}: {pair.Value} times");
            }
        }
    }
}