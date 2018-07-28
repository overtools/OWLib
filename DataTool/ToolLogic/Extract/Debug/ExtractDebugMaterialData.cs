using System;
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

            // buffer parts
            /*uint[] missing = {0x72955E70, 0x8CA81224, 0x62081FBD, 0xC1BE91CC, 0xEB5DC7CE, 0xBC78DB46};
            Dictionary<uint, int> count = new Dictionary<uint, int>();

            foreach (ulong guid in Program.TrackedFiles[0xB3]) {
                teMaterialData instance = new teMaterialData(IO.OpenFile(guid));
                
                if (instance.BufferParts == null) continue;
                foreach (teMaterialDataBufferPart bufferPart in instance.BufferParts) {
                    if (missing.Contains(bufferPart.Header.Hash)) {
                        //Console.Out.WriteLine($"Found {bufferPart.Header.Hash} in {teResourceGUID.AsString(guid)}");
                        if (!count.ContainsKey(bufferPart.Header.Hash)) {
                            count[bufferPart.Header.Hash] = 0;
                        }

                        count[bufferPart.Header.Hash]++;
                    }
                }
            }

            foreach (KeyValuePair<uint,int> pair in count) {
                Console.Out.WriteLine($"{pair.Key:X8}: {pair.Value} times");
            }*/
            
            // global value
            uint[] missing = {0x8395A3E4};
            
            foreach (ulong guid in Program.TrackedFiles[0xB3]) {
                teMaterialData instance = new teMaterialData(IO.OpenFile(guid));
                
                if (instance.Textures != null) {
                    foreach (teMaterialDataTexture texture in instance.Textures) {
                        if (missing.Contains(texture.NameHash)) {
                            Console.Out.WriteLine($"{texture.NameHash:X8} - {teResourceGUID.AsString(guid)} (tex)");
                        }
                    }
                }

                if (instance.StaticInputs != null) {
                    foreach (teMaterialDataStaticInput bufferPart in instance.StaticInputs) {
                        if (missing.Contains(bufferPart.Header.Hash)) {
                            Console.Out.WriteLine($"{bufferPart.Header.Hash:X8} - {teResourceGUID.AsString(guid)} (buffer part)");
                        }
                    }
                }
            }
        }
    }
}