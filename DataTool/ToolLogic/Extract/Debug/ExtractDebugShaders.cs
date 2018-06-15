using System;
using System.IO;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;

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
            //const ulong materialGUID = 0xE00000000005860;  // 000000005860.008: Sombra - League - NYXL - Main
            //const string matName = "Sombra - League - NYXL - Main";
            
            //const ulong materialGUID = 0xE00000000006086;
            //const string matName = "Orisa - Nature - Leaves";
            
            //const ulong materialGUID = 0xE00000000002381;
            //const string matName = "Maps - Tall bush";
            
            //const ulong materialGUID = 0xE000000000008B2;
            //const string matName = "Widow - Odette - Arm tassels";
            
            
            // orisa owl
            //const ulong materialGUID = 0xE00000000005809;  // 000000005809.008: Orisa - League - NYXL - Team Decals
            //const string matName = "Orisa - League - NYXL - Team Decals";
            
            //const ulong materialGUID = 0xE00000000005840;
            //const string matName = "Orisa - League - NYXL - Main";
            
            //const ulong materialGUID = 0xE000000000051CC;
            //const string matName = "Orisa - League - NYXL - 51CC";
            
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            string path = Path.Combine(basePath, container);
            
            SaveMaterial(path, 0xE00000000004F46, "Chateau - Tower - Body");
            SaveMaterial(path, 0xE00000000004F45, "Chateau - Tower - Borders");
            SaveMaterial(path, 0xE00000000004D4B, "Chateau - Tower - Windows");
            SaveMaterial(path, 0xE00000000004F44, "Chateau - Tower - Tip");
        }

        public void SaveMaterial(string basePath, ulong materialGUID, string name) {
            string path = Path.Combine(basePath, name, IO.GetFileName(materialGUID));
            string rawPath = Path.Combine(path, "raw");
            IO.CreateDirectoryFromFile(path+"\\david");
            IO.CreateDirectoryFromFile(rawPath+"\\will");

            teMaterial material = new teMaterial(IO.OpenFile(materialGUID));
            
            IO.WriteFile(materialGUID, rawPath);
            IO.WriteFile(material.Header.ShaderSource, rawPath);
            IO.WriteFile(material.Header.ShaderGroup, rawPath);
            IO.WriteFile(material.Header.GUIDx03A, rawPath);
            IO.WriteFile(material.Header.MaterialData, rawPath);
            
            teShaderGroup shaderGroup = new teShaderGroup(IO.OpenFile(material.Header.ShaderGroup));

            foreach (ulong shaderGroupInstance in shaderGroup.Instances) {

                teShaderInstance instance = new teShaderInstance(IO.OpenFile(shaderGroupInstance));
                teShaderCode shaderCode = new teShaderCode(IO.OpenFile(instance.Header.ShaderCode));
                
                string instanceDirectory = Path.Combine(path, shaderCode.Header.ShaderType.ToString(), teResourceGUID.AsString(shaderGroupInstance));
                IO.WriteFile(shaderGroupInstance, instanceDirectory);
                
                using (Stream file = File.OpenWrite(Path.Combine(instanceDirectory, IO.GetFileName(instance.Header.ShaderCode)))) {
                    file.SetLength(0);
                    file.Write(shaderCode.Data, 0, shaderCode.Header.DataSize);
                }

                using (StreamWriter writer =
                    new StreamWriter(Path.Combine(instanceDirectory, IO.GetFileName(instance.Header.ShaderCode)) + ".meta")) {
                    writer.WriteLine($"{shaderCode.Header.ShaderType}");
                    writer.WriteLine("{texture \"hash\"} : {shader input index}");
                    if (instance.TextureInputs == null) continue;
                    foreach (teShaderInstance.TextureInputDefinition textureInputDefinition in instance.TextureInputs) {
                        writer.WriteLine($"{textureInputDefinition.NameHash} : {textureInputDefinition.Index}");
                    }
                }
            }
        }
    }
}