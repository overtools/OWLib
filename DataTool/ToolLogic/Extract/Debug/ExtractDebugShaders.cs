using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

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
            
            //TestModelLook(0x98000000000682F); // Chateau - Lake
            
            //SaveMaterial(path, 0xE00000000004F46, "Chateau - Tower - Body");
            //SaveMaterial(path, 0xE00000000004F45, "Chateau - Tower - Borders");
            //SaveMaterial(path, 0xE00000000004D4B, "Chateau - Tower - Windows");
            //SaveMaterial(path, 0xE00000000004F44, "Chateau - Tower - Tip");
            //SaveMaterial(path, 0xE000000000003A5, "Rein - Shield - Main");
            //SaveMaterial(path, 0xE00000000005840, "Orisa - League - NYXL - Main");
            //SaveMaterial(path, 0xE00000000005809, "Orisa - League - NYXL - Team Decals");
            //SaveMaterial(path, 0xE00000000005C70, "Moira - Blackwatch - Decals");
            //SaveMaterial(path, 0xE0000000000562D, "Moira - Blackwatch - Face");
            //SaveCompute(path);

            // string allPath = Path.Combine(path, "All");
            // foreach (ulong inst in TrackedFiles[0x86]) {
            //     SaveShaderInstance(allPath, inst, teResourceGUID.AsString(inst));
            // }
            // return;
            
            //SaveMaterial(path, 0xE00000000002381, "Chateau - Tall bush");
            SaveMaterial(path, 0xE00000000004D29, "Chateau - Lake");
            //SaveMaterial(path, 0xE00000000004F0B, "Chateau - Background - Road");
            //SaveMaterial(path, 0xE00000000004EFF, "Chateau - Background - House");
            //SaveMaterial(path, 0xE00000000004F46, "Chateau - Tower - Body");
            SaveMaterial(path, 0xE000000000040C0, "Orisa - Classic - Main");
            //SaveMaterial(path, 0xE00000000005BBB, "Brigitte - Classic - Hair");
            
            
            //SavePostFX(path);
            //SaveScreenQuad(path);
            Save088(path);
        }

        public void TestModelLook(ulong guid) {
            STUModelLook modelLook = GetInstance<STUModelLook>(guid);
        } 
        
        public void Save088(string basePath) {
            var path = Path.Combine(basePath, "088");

            //{
            //    var path2 = Path.Combine(basePath, "xxShader}");
            //    using (Stream stream = IO.OpenFile((teResourceGUID) 0xE100000000000xx)) {
            //        teShaderGroup shaderGroup = new teShaderGroup(stream);
            //        SaveShaderGroup(shaderGroup, path2);
            //    }
            //}
            
            // 0xE1000000000001E = dev object shaders / UBER_SHADER_DEBUG_SHAPE
            // 0xE1000000000001A = LightShaders
            // 0xE1000000000001B = Shadow Composite
            // 0xE1000000000003E = Light Process
            // 0xE1000000000008D = HBAO
            // 0xE100000000000AE = HBAO_CS
            // 0xE1000000000008E = Light Compute
            // 0xE100000000000A4 = Misc Compute
            
            // 0xE1000000000000D = StretchRenderTarget
            // 0xE1000000000001F = Override
            // 0xE100000000000A2 = ColorBlind
            // 0xE10000000000020 = Model Default
            // 0xE10000000000005 = DrawDefaultShaderDomino
            // 0xE10000000000006 = DrawShaderDisplayAlphaClipSpace
            // 0xE10000000000004 = DrawDefaultShaderClipSpaceNoAlpha
            // 0xE10000000000003 = DrawDefaultShaderClipSpaceAdditive
            // 0xE10000000000002 = DrawDefaultShaderClipSpace
            // 0xE10000000000001 = DrawDefaultShader
            // 0xE10000000000066 = canvas
            // 0xE100000000000C1 = ImGui
            
            // 0xE1000000000000A = Lighting Env

            foreach (ulong guid in TrackedFiles[0x88]) {
                using (Stream stream = IO.OpenFile(guid)) {
                    teShaderGroup shaderGroup = new teShaderGroup(stream);

                    string groupPath = Path.Combine(path, teResourceGUID.AsString(guid));
                
                    SaveShaderGroup(shaderGroup, groupPath);
                }
            }
        }

        public void SaveScreenQuad(string basePath) {
            var path = Path.Combine(basePath, "ScreenQuad");
            using (Stream stream = IO.OpenFile((teResourceGUID)0xE1000000000000C)) {
                teShaderGroup shaderGroup = new teShaderGroup(stream);
                
                SavePostShader(path, shaderGroup, 0xF1B0EC09, "s_pVShader");
                SavePostShader(path, shaderGroup, 0x2183B37B, "s_pGeometryVShader");
                SavePostShader(path, shaderGroup, 0xF203D3B5, "s_pDepthVShader");
                SavePostShader(path, shaderGroup, 0xBD9B9307, "s_pLayerVShader"); // VERTEX, UNKNOWN_C????
            }
        }

        public void SavePostFX(string basePath) {
            var path = Path.Combine(basePath, "PostFX");
            using (Stream stream = IO.OpenFile((teResourceGUID)0xE10000000000044)) {
                teShaderGroup shaderGroup = new teShaderGroup(stream);

                SavePostShader(path, shaderGroup, 0xF0A5D76B, "CopyPointSampledState");
                SavePostShader(path, shaderGroup, 0x64DADF24, "OutlineFinalizeState");
                SavePostShader(path, shaderGroup, 0xA2883CA8, "OutlineFinalizeApplyState");
                SavePostShader(path, shaderGroup, 0x24F981CF, "RefractedOutlineFinalizeState[0]");
                //SavePostShader(path, shaderGroup, 0x64DADF24, "RefractedOutlineFinalizeState[1]"); // same as m_outlineFinalizeState
                SavePostShader(path, shaderGroup, 0x6E8A86F6, "RefractedOutlineFinalizeApplyState[0]");
                //SavePostShader(path, shaderGroup, 0xA2883CA8, "RefractedOutlineFinalizeState[1]"); // same as m_outlineFinalizeApplyState
                SavePostShader(path, shaderGroup, 0x2314B25F, "SolidColorState");
                SavePostShader(path, shaderGroup, 0x23DEF50E, "HistogramState");
                SavePostShader(path, shaderGroup, 0xF4F31C1E, "BrightPassStateLo");
                SavePostShader(path, shaderGroup, 0x2418E502, "BrightPassStateHi");
                SavePostShader(path, shaderGroup, 0x660AA4D1, "DownsampleState");
                SavePostShader(path, shaderGroup, 0x7918E37C, "DownsampleStateCheckForNaN");
                SavePostShader(path, shaderGroup, 0xE30F741A, "DownsampleLuminanceState");
                SavePostShader(path, shaderGroup, 0xED42C442, "HorizontalBlurState");
                SavePostShader(path, shaderGroup, 0x5D84E4EE, "VerticalBlurState");
                SavePostShader(path, shaderGroup, 0x7BD41894, "OverlayAddState");
                SavePostShader(path, shaderGroup, 0x26817C6F, "InvertZState");
                SavePostShader(path, shaderGroup, 0x3C8225F1, "DOFTileIntermediateState");
                SavePostShader(path, shaderGroup, 0x48ED2C1F, "DOFTileState");
                SavePostShader(path, shaderGroup, 0xEC9BC9DB, "DOFTileFilterState");
                SavePostShader(path, shaderGroup, 0x51305AFC, "DOFPresortState");
                SavePostShader(path, shaderGroup, 0xF191CE9E, "DOFCircularFilterState");
                SavePostShader(path, shaderGroup, 0xEA0B974D, "DOFMedianFilterState");
                SavePostShader(path, shaderGroup, 0x36DF160E, "DOFUpsampleState");
                SavePostShader(path, shaderGroup, 0xB3FE8DDE, "DOFUpsampleIntoGbufferState");
                SavePostShader(path, shaderGroup, 0x79C90032, "TonemapState");
                SavePostShader(path, shaderGroup, 0x895D656C, "GammaCorrectionState");
                SavePostShader(path, shaderGroup, 0x9615383C, "ColorGradingState");
                SavePostShader(path, shaderGroup, 0x14B25063, "ColorizationState");
                SavePostShader(path, shaderGroup, 0x924CFDEA, "CombinedMainState");
                SavePostShader(path, shaderGroup, 0xEB0B080A, "CombinedAllEnabledMainState");
                SavePostShader(path, shaderGroup, 0x7540E081, "RadialBlurState");
                SavePostShader(path, shaderGroup, 0x3460D76B, "FXAAState");
                SavePostShader(path, shaderGroup, 0x739B7624, "HorizontalOutlineBlurState");
                SavePostShader(path, shaderGroup, 0xC35D5688, "VerticalOutlineBlurState");
                SavePostShader(path, shaderGroup, 0x2314B25F, "OutlineBlackState");
                //SavePostShader(path, shaderGroup, 0x2314B25F, "OutlineClearToBlackState"); // same as m_outlineBlackState
                
                SavePostShader(path, shaderGroup, 0xA5DBBF87, "PostUnkVertex1");
                SavePostShader(path, shaderGroup, 0xC335DDAE, "PostUnkVertex2");
                
                
            }
        }

        public void SavePostShader(string path, teShaderGroup shaderGroup, uint hash, string name) {
            teResourceGUID state = shaderGroup.GetShaderByHash(hash);
            if (state == 0) {
                Console.Out.WriteLine($"Couldn't find {hash} / {name}");
                return;
            }
            SaveShaderInstance(path, state, name);
        }

        public void SaveCompute(string path) {
            Dictionary<ulong, int> bufferOccr = new Dictionary<ulong, int>();
            
            foreach (ulong guid in TrackedFiles[0x86]) {
                teShaderInstance instance = new teShaderInstance(IO.OpenFile(guid));
                //teShaderCode shaderCode = new teShaderCode(IO.OpenFile(instance.Header.ShaderCode));

                if (instance.BufferHeaders == null) continue;
                foreach (teShaderInstance.BufferHeader bufferHeader in instance.BufferHeaders) {
                    if (!bufferOccr.ContainsKey(bufferHeader.Hash)) {
                        bufferOccr[bufferHeader.Hash] = 0;
                    }

                    bufferOccr[bufferHeader.Hash]++;
                }

                //if (shaderCode.Header.ShaderType == Enums.teSHADER_TYPE.COMPUTE) {
                //    SaveShaderInstance(path, guid, instance, shaderCode);
                //}
            }

            int i = 0;
            foreach (KeyValuePair<ulong, int> buffer in bufferOccr.OrderByDescending(x => x.Value)) {
                Console.Out.WriteLine($"{buffer.Key:X16}: {buffer.Value}");
                i++;

                //if (i == 10) {
                //    break;
                //}
            }
        }

        public void SaveMaterial(string basePath, ulong materialGUID, string name) {
            string path = Path.Combine(basePath, name, IO.GetFileName(materialGUID));
            string rawPath = Path.Combine(path, "raw");
            // IO.CreateDirectorySafe(path);
            IO.CreateDirectorySafe(rawPath);

            teMaterial material = new teMaterial(IO.OpenFile(materialGUID));
            
            IO.WriteFile(materialGUID, rawPath);
            IO.WriteFile(material.Header.ShaderSource, rawPath);
            IO.WriteFile(material.Header.ShaderGroup, rawPath);
            IO.WriteFile(material.Header.GUIDx03A, rawPath);
            IO.WriteFile(material.Header.MaterialData, rawPath);
            
            teShaderGroup shaderGroup = new teShaderGroup(IO.OpenFile(material.Header.ShaderGroup));
            SaveShaderGroup(shaderGroup, path);
        }

        public void SaveShaderGroup(teShaderGroup shaderGroup, string path) {
            int i = 0;
            foreach (ulong shaderGroupInstance in shaderGroup.Instances) {
                teShaderInstance instance = new teShaderInstance(IO.OpenFile(shaderGroupInstance));
                teShaderCode shaderCode = new teShaderCode(IO.OpenFile(instance.Header.ShaderCode));

                string name = null;
                if (shaderGroup.Hashes != null && shaderGroup.Hashes[i] != 0) {
                    name = shaderGroup.Hashes[i].ToString("X8");
                }

                SaveShaderInstance(path, shaderGroupInstance, name, instance, shaderCode);
                i++;
            }
        }

        public static void SaveShaderInstance(string path, ulong guid, string name) {
            using (Stream stream = IO.OpenFile(guid)) {
                if (stream == null) return;
                teShaderInstance instance = new teShaderInstance(stream);
                using (Stream stream2 = IO.OpenFile(instance.Header.ShaderCode)) {
                    if (stream2 == null) return;
                    teShaderCode shaderCode = new teShaderCode(stream2);
                    SaveShaderInstance(path, guid, name, instance, shaderCode);
                }
            }
        }

        public static void SaveShaderInstance(string path, ulong guid, string name, teShaderInstance instance, teShaderCode shaderCode) {
            if (name == null) {
                name = teResourceGUID.AsString(guid);
            }
            string instanceDirectory = Path.Combine(path, shaderCode.Header.ShaderType.ToString(), name);
            IO.WriteFile(guid, instanceDirectory);
            IO.WriteFile(instance.Header.ShaderCode, instanceDirectory);
                
            //using (Stream file = File.OpenWrite(Path.Combine(instanceDirectory, IO.GetFileName(instance.Header.ShaderCode)))) {
            //    file.SetLength(0);
            //    file.Write(shaderCode.Data, 0, shaderCode.Header.DataSize);
            //}

            using (StreamWriter writer =
                new StreamWriter(Path.Combine(instanceDirectory, IO.GetFileName(instance.Header.ShaderCode)) + ".meta")) {
                writer.WriteLine($"{shaderCode.Header.ShaderType}");
                writer.WriteLine("{texture \"hash\"} : {shader input index}");
                if (instance.ShaderResources == null) return;
                foreach (teShaderInstance.ShaderResourceDefinition textureInputDefinition in instance.ShaderResources) {
                    writer.WriteLine($"{textureInputDefinition.NameHash:X8} : {textureInputDefinition.Register}");
                }
            }
        }
    }
}