// using STULib.Types.posthash;

using System;
using System.Collections.Generic;
using System.IO;
using DataTool.Flag;
using OWLib;
using OWLib.Types;
using OWLib.Types.Map;
using OWLib.Writer;
using STULib;
using STULib.Types;
using STULib.Types.Dump;
using STULib.Types.Generic;
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
    
            public bool Write(Animation anim, Stream output, object[] data) {
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
            
            public void Write(Stream output, STULib.Types.Map.Map map, STULib.Types.Map.Map detail1, STULib.Types.Map.Map detail2, STULib.Types.Map.Map entities, STULib.Types.Map.Map lights, string name, IDataWriter modelFormat, FindLogic.Combo.ComboInfo info) {
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
                    foreach (IMapFormat t in entities.Records) {
                        if (t != null && t.GetType() != typeof(MapEntity)) {
                            continue;
                        }
                        if (((MapEntity)t).Model == 0) {
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
                            string materialFn;
                            if (!info.ModelLooks.ContainsKey(group.ModelLook)) {
                                materialFn = ""; // encrypted
                            } else {
                                FindLogic.Combo.ModelLookInfo modelLookInfo = info.ModelLooks[group.ModelLook];
                                materialFn = $"Models\\{modelInfo.GetName()}\\ModelLooks\\{modelLookInfo.GetNameIndex()}.owmat";
                            }    
                            
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
    
                    foreach (IMapFormat t in entities.Records) {
                        if (t != null && t.GetType() != typeof(MapEntity)) {
                            continue;
                        }
                        MapEntity mapEntity = (MapEntity)t;
                        if (mapEntity.Model == 0) {
                            continue;
                        }

                        ulong modelLook = mapEntity.ModelLook;

                        foreach (object container in mapEntity.STUContainers) {
                            ISTU realContainer = (ISTU) container;

                            foreach (Common.STUInstance instance in realContainer.Instances) {
                                if (instance is STUModelComponentInstanceData modelComponentInstanceData) {
                                    if (modelComponentInstanceData.Look != 0) {
                                        modelLook = modelComponentInstanceData.Look;
                                    }
                                }
                            }
                        }
                        
                        FindLogic.Combo.Find(info, mapEntity.Model);
                        FindLogic.Combo.Find(info, modelLook, null, new FindLogic.Combo.ComboContext {Model = mapEntity.Model});

                        FindLogic.Combo.ModelInfoNew modelInfo = info.Models[mapEntity.Model];
                        string matFn = "";
                        if (info.ModelLooks.ContainsKey(modelLook)) {
                            FindLogic.Combo.ModelLookInfo modelLookInfo = info.ModelLooks[modelLook];
                            matFn = $"Models\\{modelInfo.GetName()}\\ModelLooks\\{modelLookInfo.GetNameIndex()}.owmat";
                        }
                        string modelFn = $"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}{modelFormat.Format}";
                        
                        writer.Write(modelFn);
                        writer.Write(matFn);
                        writer.Write(mapEntity.Header.Position.x);
                        writer.Write(mapEntity.Header.Position.y);
                        writer.Write(mapEntity.Header.Position.z);
                        writer.Write(mapEntity.Header.Scale.x);
                        writer.Write(mapEntity.Header.Scale.y);
                        writer.Write(mapEntity.Header.Scale.z);
                        writer.Write(mapEntity.Header.Rotation.x);
                        writer.Write(mapEntity.Header.Rotation.y);
                        writer.Write(mapEntity.Header.Rotation.z);
                        writer.Write(mapEntity.Header.Rotation.w);
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
            string name = GetValidFilename(GetString(map.DisplayName)) ?? "Title Screen";
            
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

            MapEnvironment? env = null;
            
            using (Stream mapStream = OpenFile(map.GetDataKey(1))) {
                STULib.Types.Map.Map mapData = new STULib.Types.Map.Map(mapStream, BuildVersion);
                
                ulong dataKey = map.MapDataResource1;

                using (Stream data = OpenFile(dataKey)) {
                    if (data != null) {
                        using (BinaryReader dataReader = new BinaryReader(data)) {
                            env = dataReader.Read<MapEnvironment>();
                        }
                    }
                }

                using (Stream map2Stream = OpenFile(map.GetDataKey(2))) {
                    if (map2Stream == null) return;
                    STULib.Types.Map.Map map2Data = new STULib.Types.Map.Map(map2Stream, BuildVersion);
                    using (Stream map8Stream = OpenFile(map.GetDataKey(8))) {
                        STULib.Types.Map.Map map8Data = new STULib.Types.Map.Map(map8Stream, BuildVersion);
                        using (Stream mapEntitiesStream = OpenFile(map.GetDataKey(0xB))) {
                            STULib.Types.Map.Map mapEntities =
                                new STULib.Types.Map.Map(mapEntitiesStream, BuildVersion, true);

                            mapEntitiesStream.Position = (long) (Math.Ceiling(mapEntitiesStream.Position / 16.0f) * 16); // Future proofing (?)

                            for (int i = 0; i < mapEntities.Records.Length; ++i) {
                                if (mapEntities.Records[i] != null && mapEntities.Records[i].GetType() != typeof(MapEntity)) {
                                    continue;
                                }
                                MapEntity mapEntity = (MapEntity) mapEntities.Records[i];

                                if (mapEntity == null) continue;
                                FindLogic.Combo.Find(info, mapEntity.Header.Entity);
                                STUModelComponent component =
                                    GetInstance<STUModelComponent>(mapEntity.Header.Entity);

                                if (component == null) continue;
                                mapEntity.ModelLook = component.Look;
                                mapEntity.Model = component.Model;
                                mapEntities.Records[i] = mapEntity;
                            }

                            using (Stream mapLStream = OpenFile(map.GetDataKey(9))) {
                                STULib.Types.Map.Map mapLData = new STULib.Types.Map.Map(mapLStream, BuildVersion);
                                using (Stream outputStream = File.Open(Path.Combine(mapPath, $"{name}.owmap"),
                                    FileMode.Create, FileAccess.Write)) {
                                    owmap.Write(outputStream, mapData, map2Data, map8Data, mapEntities, mapLData, name,
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

            ulong announcerVoiceSet = 0;
            if (env != null) {
                STUVoiceSetComponent voiceSetComponent = GetInstance<STUVoiceSetComponent>(env.Value.EntityDefinition);
                if (voiceSetComponent != null) {
                    FindLogic.Combo.Find(info, voiceSetComponent.VoiceSet);
                    info.SetEffectVoiceSet(map.EffectAnnouncer, voiceSetComponent.VoiceSet);
                    info.SetEffectVoiceSet(map.EffectMusic, voiceSetComponent.VoiceSet);
                }
            }
            
            LoudLog("\tSaving");
            Combo.Save(flags, mapPath, info);
            if (announcerVoiceSet != 0) {
                // we don't want this saved in full
                info.VoiceSets.Remove(announcerVoiceSet);
            }
            
            if (map.VoiceSet != null) {
                FindLogic.Combo.Find(info, map.VoiceSet);
            }
            
            string soundPath = Path.Combine(mapPath, "Sound");
            string voiceSetsPath = Path.Combine(soundPath, "VoiceSets");
            string otherSoundsPath = Path.Combine(soundPath, "SFX");

            Combo.SaveVoiceSets(flags, voiceSetsPath, info);
            foreach (KeyValuePair<ulong, FindLogic.Combo.SoundFileInfo> sound in info.SoundFiles) {
                Combo.SaveSoundFile(flags, otherSoundsPath, info, sound.Key, false);
            }
            
            LoudLog("\tDone");
        }
    }
}