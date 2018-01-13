// using STULib.Types.posthash;
using System;
using System.Collections.Generic;
using System.IO;
using DataTool.Flag;
using OWLib;
using OWLib.Types;
using OWLib.Types.Map;
using OWLib.Writer;
using STULib.Types;
using STULib.Types.Statescript.Components;
using static DataTool.Program;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.Logger;

namespace DataTool.SaveLogic {
    public class Map {
        public class OWMap14Writer : IDataWriter {
            public string Format => ".owmap";
            public WriterSupport SupportLevel => WriterSupport.VERTEX | WriterSupport.MAP;
            public char[] Identifier => new [] { 'O' };
            public string Name => "OWM Map Format";
    
            public bool Write(OWLib.Animation anim, Stream output, object[] data) {
                return false;
            }

            public Dictionary<ulong, List<string>>[] Write(Stream output, OWLib.Map map, OWLib.Map detail1, OWLib.Map detail2, OWLib.Map props, OWLib.Map lights, string name,
                IDataWriter modelFormat) {
                throw new NotImplementedException();
            }

            public bool Write(Map10 physics, Stream output, object[] data) {
                return false;
            }
    
            public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data) {
                return false;
            }
            
            public void Write(Stream output, STULib.Types.Map.Map map, STULib.Types.Map.Map detail1, STULib.Types.Map.Map detail2, STULib.Types.Map.Map props, STULib.Types.Map.Map lights, string name, IDataWriter modelFormat, FindLogic.Combo.ComboInfo info) {
                if (modelFormat == null) {
                    modelFormat = new OWMDLWriter();
                }
                using (BinaryWriter writer = new BinaryWriter(output)) {
                    writer.Write((ushort)1); // version major
                    writer.Write((ushort)1); // version minor
    
                    if (name.Length == 0) {
                        writer.Write((byte)0);
                    } else {
                        writer.Write(name);
                    }
    
                    uint size = 0;
                    foreach (IMapFormat t in map.Records) {
                        if (t != null && t.GetType() != typeof(Map01)) {
                            continue;
                        }
                        size++;
                    }
                    writer.Write(size); // nr objects
    
                    size = 1;
                    foreach (IMapFormat t in detail1.Records) {
                        if (t != null && t.GetType() != typeof(Map02)) {
                            continue;
                        }
                        size++;
                    }
                    foreach (IMapFormat t in detail2.Records) {
                        if (t != null && t.GetType() != typeof(Map08)) {
                            continue;
                        }
                        size++;
                    }
                    foreach (IMapFormat t in props.Records) {
                        if (t != null && t.GetType() != typeof(Map0B)) {
                            continue;
                        }
                        if (((Map0B)t).Model == 0) {
                            continue;
                        }
                        size++;
                    }
                    writer.Write(size); // nr details
    
                    // Extension 1.1 - Lights
                    size = 0;
                    foreach (IMapFormat t in lights.Records) {
                        if (t != null && t.GetType() != typeof(Map09)) {
                            continue;
                        }
                        size++;
                    }
                    writer.Write(size); // nr Lights
    
                    foreach (IMapFormat t in map.Records) {
                        if (t != null && t.GetType() != typeof(Map01)) {
                            continue;
                        }
                        Map01 obj = (Map01)t;
                        FindLogic.Combo.Find(info, obj.Header.Model);
                        FindLogic.Combo.ModelInfoNew modelInfo = info.Models[obj.Header.Model];
                        string modelFn = $"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}{modelFormat.Format}";
                        writer.Write(modelFn);
                        writer.Write(obj.Header.groupCount);
                        for (int j = 0; j < obj.Header.groupCount; ++j) {
                            Map01.Map01Group group = obj.Groups[j];
                            FindLogic.Combo.Find(info, group.ModelLook, null, new FindLogic.Combo.ComboContext {Model = obj.Header.Model});
                            FindLogic.Combo.ModelLookInfo modelLookInfo = info.ModelLooks[group.ModelLook];
                            string materialFn = $"Models\\{modelInfo.GetName()}\\ModelLooks\\{modelLookInfo.GetNameIndex()}.owmat";
                            
                            writer.Write(materialFn);
                            writer.Write(group.recordCount);
                            for (int k = 0; k < group.recordCount; ++k) {
                                Map01.Map01GroupRecord record = obj.Records[j][k];
                                writer.Write(record.position.x);
                                writer.Write(record.position.y);
                                writer.Write(record.position.z);
                                writer.Write(record.scale.x);
                                writer.Write(record.scale.y);
                                writer.Write(record.scale.z);
                                writer.Write(record.rotation.x);
                                writer.Write(record.rotation.y);
                                writer.Write(record.rotation.z);
                                writer.Write(record.rotation.w);
                            }
                        }
                    }
                                      
                    // todo: broken?
                    writer.Write($"Models\\physics\\physics.{modelFormat.Format}");
                    writer.Write((byte)0);
                    writer.Write(0.0f);
                    writer.Write(0.0f);
                    writer.Write(0.0f);
                    writer.Write(1.0f);
                    writer.Write(1.0f);
                    writer.Write(1.0f);
                    writer.Write(0.0f);
                    writer.Write(0.0f);
                    writer.Write(0.0f);
                    writer.Write(1.0f);
    
                    foreach (IMapFormat t in detail1.Records) {
                        if (t != null && t.GetType() != typeof(Map02)) {
                            continue;
                        }
                        Map02 obj = (Map02)t;
                        
                        FindLogic.Combo.Find(info, obj.Header.Model);
                        FindLogic.Combo.Find(info, obj.Header.ModelLook, null, new FindLogic.Combo.ComboContext {Model = obj.Header.Model});

                        FindLogic.Combo.ModelInfoNew modelInfo = info.Models[obj.Header.Model];
                        FindLogic.Combo.ModelLookInfo modelLookInfo = info.ModelLooks[obj.Header.ModelLook]; 
                        string modelFn = $"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}{modelFormat.Format}";
                        string matFn = $"Models\\{modelInfo.GetName()}\\ModelLooks\\{modelLookInfo.GetNameIndex()}.owmat";
                        
                        writer.Write(modelFn);
                        writer.Write(matFn);
                        writer.Write(obj.Header.position.x);
                        writer.Write(obj.Header.position.y);
                        writer.Write(obj.Header.position.z);
                        writer.Write(obj.Header.scale.x);
                        writer.Write(obj.Header.scale.y);
                        writer.Write(obj.Header.scale.z);
                        writer.Write(obj.Header.rotation.x);
                        writer.Write(obj.Header.rotation.y);
                        writer.Write(obj.Header.rotation.z);
                        writer.Write(obj.Header.rotation.w);
                    }
    
                    foreach (IMapFormat t in detail2.Records) {
                        if (t != null && t.GetType() != typeof(Map08)) {
                            continue;
                        }
                        Map08 obj = (Map08)t;
                        
                        FindLogic.Combo.Find(info, obj.Header.Model);
                        FindLogic.Combo.Find(info, obj.Header.ModelLook, null, new FindLogic.Combo.ComboContext {Model = obj.Header.Model});

                        FindLogic.Combo.ModelInfoNew modelInfo = info.Models[obj.Header.Model];
                        FindLogic.Combo.ModelLookInfo modelLookInfo = info.ModelLooks[obj.Header.ModelLook]; 
                        string modelFn = $"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}{modelFormat.Format}";
                        string matFn = $"Models\\{modelInfo.GetName()}\\ModelLooks\\{modelLookInfo.GetNameIndex()}.owmat";
                        
                        writer.Write(modelFn);
                        writer.Write(matFn);
                        writer.Write(obj.Header.position.x);
                        writer.Write(obj.Header.position.y);
                        writer.Write(obj.Header.position.z);
                        writer.Write(obj.Header.scale.x);
                        writer.Write(obj.Header.scale.y);
                        writer.Write(obj.Header.scale.z);
                        writer.Write(obj.Header.rotation.x);
                        writer.Write(obj.Header.rotation.y);
                        writer.Write(obj.Header.rotation.z);
                        writer.Write(obj.Header.rotation.w);
                    }
    
                    foreach (IMapFormat t in props.Records) {
                        if (t != null && t.GetType() != typeof(Map0B)) {
                            continue;
                        }
                        Map0B obj = (Map0B)t;
                        if (obj.Model == 0) {
                            continue;
                        }
                        
                        FindLogic.Combo.Find(info, obj.Model);
                        FindLogic.Combo.Find(info, obj.ModelLook, null, new FindLogic.Combo.ComboContext {Model = obj.Model});

                        FindLogic.Combo.ModelInfoNew modelInfo = info.Models[obj.Model];
                        FindLogic.Combo.ModelLookInfo modelLookInfo = info.ModelLooks[obj.ModelLook]; 
                        string modelFn = $"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}{modelFormat.Format}";
                        string matFn = $"Models\\{modelInfo.GetName()}\\ModelLooks\\{modelLookInfo.GetNameIndex()}.owmat";
                        
                        writer.Write(modelFn);
                        writer.Write(matFn);
                        writer.Write(obj.Header.position.x);
                        writer.Write(obj.Header.position.y);
                        writer.Write(obj.Header.position.z);
                        writer.Write(obj.Header.scale.x);
                        writer.Write(obj.Header.scale.y);
                        writer.Write(obj.Header.scale.z);
                        writer.Write(obj.Header.rotation.x);
                        writer.Write(obj.Header.rotation.y);
                        writer.Write(obj.Header.rotation.z);
                        writer.Write(obj.Header.rotation.w);
                    }
    
                    // Extension 1.1 - Lights
                    foreach (IMapFormat t in lights.Records) {
                        if (t != null && t.GetType() != typeof(Map09)) {
                            continue;
                        }
                        Map09 obj = (Map09)t;
                        writer.Write(obj.Header.position.x);
                        writer.Write(obj.Header.position.y);
                        writer.Write(obj.Header.position.z);
                        writer.Write(obj.Header.rotation.x);
                        writer.Write(obj.Header.rotation.y);
                        writer.Write(obj.Header.rotation.z);
                        writer.Write(obj.Header.rotation.w);
                        writer.Write(obj.Header.LightType);
                        writer.Write(obj.Header.LightFOV);
                        writer.Write(obj.Header.Color.x);
                        writer.Write(obj.Header.Color.y);
                        writer.Write(obj.Header.Color.z);
                        writer.Write(obj.Header.unknown1A);
                        writer.Write(obj.Header.unknown1B);
                        writer.Write(obj.Header.unknown2A);
                        writer.Write(obj.Header.unknown2B);
                        writer.Write(obj.Header.unknown2C);
                        writer.Write(obj.Header.unknown2D);
                        writer.Write(obj.Header.unknown3A);
                        writer.Write(obj.Header.unknown3B);
    
                        writer.Write(obj.Header.unknownPos1.x);
                        writer.Write(obj.Header.unknownPos1.y);
                        writer.Write(obj.Header.unknownPos1.z);
                        writer.Write(obj.Header.unknownQuat1.x);
                        writer.Write(obj.Header.unknownQuat1.y);
                        writer.Write(obj.Header.unknownQuat1.z);
                        writer.Write(obj.Header.unknownQuat1.w);
                        writer.Write(obj.Header.unknownPos2.x);
                        writer.Write(obj.Header.unknownPos2.y);
                        writer.Write(obj.Header.unknownPos2.z);
                        writer.Write(obj.Header.unknownQuat2.x);
                        writer.Write(obj.Header.unknownQuat2.y);
                        writer.Write(obj.Header.unknownQuat2.z);
                        writer.Write(obj.Header.unknownQuat2.w);
                        writer.Write(obj.Header.unknownPos3.x);
                        writer.Write(obj.Header.unknownPos3.y);
                        writer.Write(obj.Header.unknownPos3.z);
                        writer.Write(obj.Header.unknownQuat3.x);
                        writer.Write(obj.Header.unknownQuat3.y);
                        writer.Write(obj.Header.unknownQuat3.z);
                        writer.Write(obj.Header.unknownQuat3.w);
    
                        writer.Write(obj.Header.unknown4A);
                        writer.Write(obj.Header.unknown4B);
                        writer.Write(obj.Header.unknown5);
                        writer.Write(obj.Header.unknown6A);
                        writer.Write(obj.Header.unknown6B);
                        writer.Write(obj.Header.unknown7A);
                        writer.Write(obj.Header.unknown7B);
                    }
                }
            }
        }

        public static void Save(ICLIFlags flags, STUMap map, ulong key, string basePath) {
            string name = GetValidFilename(GetString(map.DisplayName)) ?? $"Unknown{GUID.Index(key):X}";
            
            if (GetString(map.VariantName) != null) name = GetValidFilename(GetString(map.VariantName));

            LoudLog($"Extracting map {name}\\{GUID.Index(key):X}");

            // if (map.Gamemodes != null) {
            //     foreach (Common.STUGUID gamemodeGUID in map.Gamemodes) {
            //         STUGamemode gamemode = GetInstance<STUGamemode>(gamemodeGUID);
            //     }
            // }
            
            // TODO: MAP11 HAS CHANGED
            // TODO: MAP10 TOO?
            
            string mapPath = Path.Combine(basePath, "Maps", name, GUID.Index(key).ToString("X")) + Path.DirectorySeparatorChar;
            
            CreateDirectoryFromFile(mapPath);
            
            // if (map.UnknownArray != null) {
            //     Dictionary<ulong, List<TextureInfo>> textures = new Dictionary<ulong, List<TextureInfo>>();
            //     foreach (STUMap.STU_7D6D8405 stu_7D6D8405 in map?.UnknownArray) {
            //         ISTU overrideStu = OpenSTUSafe(stu_7D6D8405.Override);
            //         STUSkinOverride @override = GetInstance<STUSkinOverride>(stu_7D6D8405.Override);
            //         textures = FindLogic.Texture.FindTextures(textures, @override.SkinImage);
            //     }
            //     SaveLogic.Texture.Save(flags, Path.Combine(mapPath, "override"), textures);
            // }
            
            OWMDLWriter modelWriter = new OWMDLWriter();
            OWMap14Writer owmap = new OWMap14Writer();
            
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            LoudLog("\tFinding");
            FindLogic.Combo.Find(info, map.MapDataResource1);
            
            using (Stream mapStream = OpenFile(map.GetDataKey(1))) {
                STULib.Types.Map.Map mapData = new STULib.Types.Map.Map(mapStream, BuildVersion);
                using (Stream map2Stream = OpenFile(map.GetDataKey(2))) {
                    if (map2Stream == null) return;
                    STULib.Types.Map.Map map2Data = new STULib.Types.Map.Map(map2Stream, BuildVersion);
                    using (Stream map8Stream = OpenFile(map.GetDataKey(8))) {
                        STULib.Types.Map.Map map8Data = new STULib.Types.Map.Map(map8Stream, BuildVersion);
                        using (Stream mapBStream = OpenFile(map.GetDataKey(0xB))) {
                            STULib.Types.Map.Map mapBData =
                                new STULib.Types.Map.Map(mapBStream, BuildVersion, true);

                            mapBStream.Position =
                                (long) (Math.Ceiling(mapBStream.Position / 16.0f) * 16); // Future proofing (?)
                            
                            // type 0x75526BC2 sometimes kills the parser
                            // foreach (ISTU stu in mapBData.STUs) {
                            //     STUStatescriptComponentInstanceData componentInstanceData = stu.Instances.OfType<STUStatescriptComponentInstanceData>().FirstOrDefault();
                            //     if (componentInstanceData == null) continue;
                            // }
                            // int test = mapBData.STUs.Count(x => x.TypeHashes.Contains(0x75526BC2));

                            for (int i = 0; i < mapBData.Records.Length; ++i) {
                                if (mapBData.Records[i] != null && mapBData.Records[i].GetType() != typeof(Map0B)) {
                                    continue;
                                }
                                Map0B mapprop = (Map0B) mapBData.Records[i];

                                if (mapprop == null) continue;
                                FindLogic.Combo.Find(info, mapprop.Header.binding);
                                STUModelComponent component =
                                    GetInstance<STUModelComponent>(mapprop.Header.binding);

                                if (component == null) continue;
                                mapprop.ModelLook = component.Look;
                                mapprop.Model = component.Model;
                                mapBData.Records[i] = mapprop;
                            }

                            using (Stream mapLStream = OpenFile(map.GetDataKey(9))) {
                                STULib.Types.Map.Map mapLData = new STULib.Types.Map.Map(mapLStream, BuildVersion);
                                using (Stream outputStream = File.Open(Path.Combine(mapPath, $"{name}.owmap"),
                                    FileMode.Create, FileAccess.Write)) {
                                    owmap.Write(outputStream, mapData, map2Data, map8Data, mapBData, mapLData, name,
                                        modelWriter, info);
                                }
                            }
                        }
                    }
                }
            }

            FindLogic.Combo.Find(info, map.EffectAnnouncer);
            info.SetEffectName(map.EffectAnnouncer, "LoadAnnouncer");
            FindLogic.Combo.Find(info, map.EffectMusic);
            info.SetEffectName(map.EffectMusic, "LoadMusic");
            
            LoudLog("\tSaving");
            Combo.Save(flags, mapPath, info);
            
            // if (extractFlags.ConvertModels) {
            //     string physicsFile = Path.Combine(mapPath, "Models", "physics", "physics.owmdl");
            //     // CreateDirectoryFromFile(physicsFile);
            //     // using (Stream map10Stream = OpenFile(map.GetDataKey(0x10))) {
            //     //     Map10 physics = new Map10(map10Stream);
            //     //     using (Stream outputStream = File.Open(physicsFile, FileMode.Create, FileAccess.Write)) {
            //     //         modelWriter.Write(physics, outputStream, new object[0]);
            //     //     }
            //     // }
            // }

            if (map.SoundMasterResource != null) {
                FindLogic.Combo.ComboInfo soundInfo = new FindLogic.Combo.ComboInfo();
                FindLogic.Combo.Find(soundInfo, map.SoundMasterResource);

                if (soundInfo.VoiceMasters.ContainsKey(map.SoundMasterResource)) {
                    string soundPath = Path.Combine(mapPath, "Sound");
                    FindLogic.Combo.VoiceMasterInfo voiceMasterInfo = soundInfo.VoiceMasters[map.SoundMasterResource];
                    Combo.SaveVoiceMaster(flags, soundPath, soundInfo, voiceMasterInfo);
                }
            }
            
            LoudLog("\tDone");
        }
    }
}