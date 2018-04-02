using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.List;
using TankLib;
using static DataTool.Program;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-shaders", Description = "Extract shaders for a material (debug)", TrackTypes = new ushort[] {0x8}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugShaders : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetSoundbanks(toolFlags);
        }

        public void GetSoundbanks(ICLIFlags toolFlags) {
            const string container = "ShaderCode";
            const ulong materialGUID = 0xE00000000005860;  // 000000005860.008: Sombra - League - NYXL - Main
            
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            string path = Path.Combine(basePath, container, IO.GetFileName(materialGUID));
            string rawPath = Path.Combine(path, "raw");
            IO.CreateDirectoryFromFile(path+"\\david");
            IO.CreateDirectoryFromFile(rawPath+"\\phil");

            teMaterial material = new teMaterial(IO.OpenFile(materialGUID));
            
            IO.WriteFile(materialGUID, rawPath);
            IO.WriteFile((ulong)material.Header.ShaderSource, rawPath);
            IO.WriteFile((ulong)material.Header.ShaderGroup, rawPath);
            IO.WriteFile((ulong)material.Header.GUIDx03A, rawPath);
            
            teShaderGroup shaderGroup = new teShaderGroup(IO.OpenFile(material.Header.ShaderGroup));

            foreach (ulong shaderGroupInstance in shaderGroup.Instances) {
                teShaderInstance instance = new teShaderInstance(IO.OpenFile(shaderGroupInstance));
                
                teShaderCode shaderCode = new teShaderCode(IO.OpenFile(instance.Header.ShaderCode));

                //if (shaderCode.Header.ShaderType != teEnums.teSHADER_TYPE.PIXEL) continue;
                if (shaderCode.Header.ShaderType != teEnums.teSHADER_TYPE.VERTEX) continue;
                using (Stream file = File.OpenWrite(Path.Combine(path, IO.GetFileName(instance.Header.ShaderCode)))) {
                    file.SetLength(0);
                    file.Write(shaderCode.Data, 0, shaderCode.Header.DataSize);
                }

                using (StreamWriter writer =
                    new StreamWriter(Path.Combine(path, IO.GetFileName(instance.Header.ShaderCode)) + ".meta")) {
                    writer.WriteLine($"{shaderCode.Header.ShaderType}");
                    writer.WriteLine("{texture \"hash\"} : {shader input index}");
                    if (instance.TextureInputs != null) {
                        foreach (teShaderInstance.TextureInputDefinition textureInputDefinition in instance.TextureInputs) {
                            writer.WriteLine($"{textureInputDefinition.TextureType} : {textureInputDefinition.Index}");
                        }
                    }
                }
            }
        }
    }
}