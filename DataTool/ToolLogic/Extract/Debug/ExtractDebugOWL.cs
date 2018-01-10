using System;
using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using OWLib.Types;
using STULib.Types;
using STULib.Types.STUUnlock;
using static DataTool.Program;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-owl", Description = "Extract owl skins (debug)", TrackTypes = new ushort[] {0xB3, 0x8, 0xA6, 0x4, 0xA5}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugOWL : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ExtractOWL(toolFlags);
        }

        public void SaveTextureDef(ICLIFlags flags, string path, ulong texDef, ImageDefinition imageDefinition) {
            string thePath = Path.Combine(path, GetFileName(texDef));
            CreateDirectoryFromFile(thePath+"\\poo");

            Combo.ComboInfo info = new Combo.ComboInfo();
            foreach (ImageLayer layer in imageDefinition.Layers) {
                Combo.Find(info, layer.Key);
            }
            SaveLogic.Combo.SaveLooseTextures(flags, thePath, info);
        }

        public void ExtractOWL(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            const string container = "DebugOWL2";
            string path = Path.Combine(basePath, container);

            // foreach (ulong key in TrackedFiles[0xB3]) {
            //     string p = Path.Combine(path, "MatDatas");
            //     if (GUID.Index(key) != 0x8EE9) continue;
            //     ImageDefinition def = new ImageDefinition(OpenFile(key));
            //     SaveTextureDef(flags, p, key, def);
            //     // if (def.Layers != null) {
            //     //     foreach (ImageLayer layer in def.Layers) {
            //     //         if (GUID.Index(layer.Key) == 0x120E2) {  // sf shock normal
            //     //             // Debugger.Break();
            //     //             SaveTextureDef(flags, p, key, def);
            //     //             // found 346777171307564171
            //     //         }
            //     //         
            //     //         if (GUID.Index(layer.Key) == 0x11C3F) {  // sym normal
            //     //             // Debugger.Break();
            //     //             SaveTextureDef(flags, p, key, def);
            //     //             // found 346777171307564777
            //     //             // found 346777171307565869
            //     //         }
            //     //         
            //     //         if (GUID.Index(layer.Key) == 0x11D56) {  // widow sss
            //     //             // Debugger.Break();
            //     //             SaveTextureDef(flags, p, key, def);
            //     //             //346777171307565850 / 00000000931A.0B3
            //     //         }
            //     //     }
            //     // }
            // }
            
            List<string> added = ExtractDebugNewEntities.GetAddedFiles("D:\\ow\\OverwatchDataManager\\versions\\1.19.1.3.42563\\data.json");

            Combo.ComboInfo imgInfo = new Combo.ComboInfo();
            // Combo.Find(imgInfo, 864691265894168957ul);
            foreach (ulong key in TrackedFiles[0xA5]) {
                string name = GetFileName(key);
                if (!added.Contains(name)) continue;
                Skin skin = GetInstance<Skin>(key);
                if (skin == null) continue;
            }

            // foreach (ulong key in TrackedFiles[0x4]) {
            //     string name = GetFileName(key);
            //     if (!added.Contains(name)) continue;
            //     Combo.Find(imgInfo, key);
            // }
            // SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(path, "Tex"), imgInfo);

            return;
            

            foreach (ulong key in TrackedFiles[0xA6]) {
                STUSkinOverride @override = GetInstance<STUSkinOverride>(key);
                string name = GetFileName(key);
                if (!added.Contains(name)) continue;
                
                Combo.ComboInfo info = new Combo.ComboInfo();
                Combo.ComboInfo info2 = new Combo.ComboInfo();
                foreach (KeyValuePair<ulong, ulong> overrideReplacement in @override.ProperReplacements) {
                    Combo.Find(info, overrideReplacement.Key);
                    Combo.Find(info2, overrideReplacement.Value);
                }

                string p = Path.Combine(path, "SkinOverrides", GetFileName(key));
                
                SaveLogic.Combo.Save(flags, Path.Combine(p, "Before"), info);
                // SaveLogic.Combo.SaveAllMaterials(flags, Path.Combine(p, "Before"), info);
                SaveLogic.Combo.SaveAllModelLooks(flags, Path.Combine(p, "Before"), info);
                SaveLogic.Combo.Save(flags, Path.Combine(p, "After"), info2);
                // SaveLogic.Combo.SaveAllMaterials(flags, Path.Combine(p, "After"), info2);                
                SaveLogic.Combo.SaveAllModelLooks(flags, Path.Combine(p, "After"), info2);

            }

            // foreach (ulong key in TrackedFiles[0x8]) {
            //     Material material = new Material(OpenFile(key), 0);
            //     if (GUID.Index(material.Header.ImageDefinition) == 0x931A) {
            //         Debugger.Break();
            //     }
            // }
        }
    }
}