using DataTool.Flag;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-materialdata", Description = "Extract material data hashes (debug)", CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugMaterialData : ITool {
        public void Parse(ICLIFlags toolFlags) {
            GetSoundbanks(toolFlags);
        }

        public void GetSoundbanks(ICLIFlags toolFlags) {
            //string basePath;
            //if (toolFlags is ExtractFlags flags) {
            //    basePath = flags.OutputPath;
            //} else {
            //    throw new Exception("no output path");
            //}

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
            
            // global values
            uint[] missing = {
                0x55745484,
                0x2D4CB644,
                0x7F315CD9,
                0xC7CDBD40,
                0x406D766F,
                0x417F8822,
                0x1A5EA166,
                0x1734073C,
                0xE46C5ADB,
                0xB8C8210F,
                0x488361E4,
                0xFA05F9E5,
                0x6712C073,
                0x6D3825D7,
                0xC1C9B2C9,
                0x506AA0BD,
                0x3C95EDA4,
                0xFEB4C14B,
                0xD4AEE37A,
                0x526DE67A,
                0x7877C44B,
                0xE04FA033,
                0x86517620,
                0xC156B88E,
                0x8395A3E4,
                0x6A7749C5,
                0xDD4FF96B,
                0xB50639D8
            };
            
            /*HashSet<ulong> buffers = new HashSet<ulong>();
            foreach (var guid in Program.TrackedFiles[0x86]) {
                teShaderInstance shaderInstance = new teShaderInstance(IO.OpenFile(guid));
                if (shaderInstance.BufferHeaders == null) continue;
                foreach (teShaderInstance.BufferHeader bufferHeader in shaderInstance.BufferHeaders) {
                    buffers.Add(bufferHeader.Hash);
                    //if (bufferHeader.Hash == 0xC367945EB4F78189) {
                    //    if (bufferHeader.PartCount > 0) {
                    //        
                    //    }
                    //}
                }
            }*/

            /*foreach (ulong guid in Program.TrackedFiles[0xB3]) {
                teMaterialData instance = new teMaterialData(IO.OpenFile(guid));
                
                //if (instance.Textures != null) {
                //    foreach (teMaterialDataTexture texture in instance.Textures) {
                //        if (missing.Contains(texture.NameHash)) {
                //            Console.Out.WriteLine($"{texture.NameHash:X8} - {teResourceGUID.AsString(guid)} (tex)");
                //        }
                //    }
                //}

                if (instance.StaticInputs != null) {
                    foreach (teMaterialDataStaticInput bufferPart in instance.StaticInputs) {
                        if (missing.Contains(bufferPart.Header.Hash)) {
                            Console.Out.WriteLine($"{bufferPart.Header.Hash:X8} - {teResourceGUID.AsString(guid)} (buffer part)");
                        }
                    }
                }
            }*/
        }
    }
}