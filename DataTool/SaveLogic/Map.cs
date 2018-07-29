using System.IO;
using DataTool.Flag;
using TankLib;
using TankLib.ExportFormats;
using TankLib.STU;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.Logger;

namespace DataTool.SaveLogic {
    public static class Map {
        /// <summary>
        /// OWMAP format
        /// </summary>
        public class OverwatchMap : IExportFormat {
            public string Extension => "owmap";

            public string Name;

            public FindLogic.Combo.ComboInfo Info;

            public teMapPlaceableData SingleModels;
            public teMapPlaceableData ModelGroups;
            public teMapPlaceableData Eights;
            public teMapPlaceableData Entities;
            public teMapPlaceableData Lights;
            
            public OverwatchMap(string name, FindLogic.Combo.ComboInfo info, teMapPlaceableData singleModels,
                teMapPlaceableData modelGroups, teMapPlaceableData placeable8, teMapPlaceableData entities,
                teMapPlaceableData lights) {
                Name = name;
                Info = info;

                SingleModels = singleModels;
                ModelGroups = modelGroups;
                Eights = placeable8;
                Entities = entities;
                Lights = lights;
            }

            public void Write(Stream output) {
                using (BinaryWriter writer = new BinaryWriter(output)) {
                    writer.Write((ushort) 1); // version major
                    writer.Write((ushort) 1); // version minor

                    if (Name.Length == 0) {
                        writer.Write((byte) 0);
                    } else {
                        writer.Write(Name);
                    }

                    writer.Write(ModelGroups.Header.PlaceableCount); // nr objects

                    int entitiesWithModelCount = 0;
                    STUModelComponent[] modelComponents = new STUModelComponent[Entities.Header.PlaceableCount];

                    for (int i = 0; i < Entities.Header.PlaceableCount; i++) {
                        teMapPlaceableEntity entity = (teMapPlaceableEntity) Entities.Placeables[i];
                        STUModelComponent component = GetInstanceNew<STUModelComponent>(entity.Header.EntityDefinition);
                        if (component != null) {
                            entitiesWithModelCount++;
                            modelComponents[i] = component;
                        }
                    }

                    writer.Write((uint)(SingleModels.Header.PlaceableCount + Eights.Header.PlaceableCount +
                                 entitiesWithModelCount)); // nr details
                    writer.Write(Lights.Header.PlaceableCount); // nr Lights

                    foreach (IMapPlaceable mapPlaceable in ModelGroups.Placeables) {
                        teMapPlaceableModelGroup modelGroup = (teMapPlaceableModelGroup) mapPlaceable;

                        FindLogic.Combo.Find(Info, modelGroup.Header.Model);
                        FindLogic.Combo.ModelInfoNew modelInfo = Info.Models[modelGroup.Header.Model];
                        string modelFn = $"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}.owmdl";
                        writer.Write(modelFn);
                        writer.Write(modelGroup.Header.GroupCount);
                        for (int j = 0; j < modelGroup.Header.GroupCount; ++j) {
                            teMapPlaceableModelGroup.Group group = modelGroup.Groups[j];
                            FindLogic.Combo.Find(Info, group.ModelLook, null,
                                new FindLogic.Combo.ComboContext {Model = modelGroup.Header.Model});
                            FindLogic.Combo.ModelLookInfo modelLookInfo = Info.ModelLooks[group.ModelLook];
                            string materialFn =
                                $"Models\\{modelInfo.GetName()}\\ModelLooks\\{modelLookInfo.GetNameIndex()}.owmat";

                            writer.Write(materialFn);
                            writer.Write(group.EntryCount);
                            for (int k = 0; k < group.EntryCount; ++k) {
                                teMapPlaceableModelGroup.Entry record = modelGroup.Entries[j][k];

                                writer.Write(record.Translation);
                                writer.Write(record.Scale);
                                writer.Write(record.Rotation);
                            }
                        }
                    }

                    foreach (IMapPlaceable mapPlaceable in SingleModels.Placeables) {
                        teMapPlaceableSingleModel singleModel = (teMapPlaceableSingleModel) mapPlaceable;

                        FindLogic.Combo.Find(Info, singleModel.Header.Model);
                        FindLogic.Combo.Find(Info, singleModel.Header.ModelLook, null,
                            new FindLogic.Combo.ComboContext {Model = singleModel.Header.Model});

                        FindLogic.Combo.ModelInfoNew modelInfo = Info.Models[singleModel.Header.Model];
                        FindLogic.Combo.ModelLookInfo modelLookInfo = Info.ModelLooks[singleModel.Header.ModelLook];
                        string modelFn = $"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}.owmdl";
                        string matFn =
                            $"Models\\{modelInfo.GetName()}\\ModelLooks\\{modelLookInfo.GetNameIndex()}.owmat";

                        writer.Write(modelFn);
                        writer.Write(matFn);
                        writer.Write(singleModel.Header.Translation);
                        writer.Write(singleModel.Header.Scale);
                        writer.Write(singleModel.Header.Rotation);
                    }

                    foreach (IMapPlaceable mapPlaceable in Eights.Placeables) {
                        teMapPlaceable8 placeable8 = (teMapPlaceable8) mapPlaceable;

                        FindLogic.Combo.Find(Info, placeable8.Header.Model);
                        FindLogic.Combo.Find(Info, placeable8.Header.ModelLook, null,
                            new FindLogic.Combo.ComboContext {Model = placeable8.Header.Model});

                        FindLogic.Combo.ModelInfoNew modelInfo = Info.Models[placeable8.Header.Model];
                        FindLogic.Combo.ModelLookInfo modelLookInfo = Info.ModelLooks[placeable8.Header.ModelLook];
                        string modelFn =
                            $"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}.owmdl";
                        string matFn =
                            $"Models\\{modelInfo.GetName()}\\ModelLooks\\{modelLookInfo.GetNameIndex()}.owmat";

                        writer.Write(modelFn);
                        writer.Write(matFn);
                        writer.Write(placeable8.Header.Translation);
                        writer.Write(placeable8.Header.Scale);
                        writer.Write(placeable8.Header.Rotation);
                    }

                    for (int i = 0; i < Entities.Placeables.Length; i++) {
                        var entity = (teMapPlaceableEntity) Entities.Placeables[i];
                        
                        STUModelComponent modelComponent = modelComponents[i];
                        if (modelComponent == null) continue;
                        
                        ulong model = modelComponent.m_model;
                        ulong modelLook = modelComponent.m_look;

                        foreach (STUComponentInstanceData instanceData in entity.InstanceData) {
                            if (!(instanceData is STUModelComponentInstanceData modelComponentInstanceData)) continue;
                            if (modelComponentInstanceData.m_look != 0) {
                                modelLook = modelComponentInstanceData.m_look;
                            }
                        }

                        FindLogic.Combo.Find(Info, model);
                        FindLogic.Combo.Find(Info, modelLook, null, new FindLogic.Combo.ComboContext {Model = model});

                        FindLogic.Combo.ModelInfoNew modelInfo = Info.Models[model];
                        FindLogic.Combo.ModelLookInfo modelLookInfo = Info.ModelLooks[modelLook];
                        string modelFn = $"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}.owmdl";
                        string matFn = $"Models\\{modelInfo.GetName()}\\ModelLooks\\{modelLookInfo.GetNameIndex()}.owmat";

                        writer.Write(modelFn);
                        writer.Write(matFn);
                        writer.Write(entity.Header.Translation);
                        writer.Write(entity.Header.Scale);
                        writer.Write(entity.Header.Rotation);
                    }

                    // Extension 1.1 - Lights
                    foreach (IMapPlaceable mapPlaceable in Lights.Placeables) {
                        var light = (teMapPlaceableLight) mapPlaceable;

                        writer.Write(light.Header.Translation);
                        writer.Write(light.Header.Rotation);
                        
                        writer.Write(light.Header.Type);
                        writer.Write(light.Header.LightFOV);
                        writer.Write(light.Header.Color);
                        
                        writer.Write(light.Header.Unknown1A);
                        writer.Write(light.Header.Unknown1B);
                        writer.Write(light.Header.Unknown2A);
                        writer.Write(light.Header.Unknown2B);
                        writer.Write(light.Header.Unknown2C);
                        writer.Write(light.Header.Unknown2D);
                        writer.Write(light.Header.Unknown3A);
                        writer.Write(light.Header.Unknown3B);

                        writer.Write(light.Header.UnknownPos1);
                        writer.Write(light.Header.UnknownQuat1);
                        writer.Write(light.Header.UnknownPos2);
                        writer.Write(light.Header.UnknownQuat2);
                        writer.Write(light.Header.UnknownPos3);
                        writer.Write(light.Header.UnknownQuat3);

                        writer.Write(light.Header.Unknown4A);
                        writer.Write(light.Header.Unknown4B);
                        writer.Write(light.Header.Unknown5);
                        writer.Write(light.Header.Unknown6A);
                        writer.Write(light.Header.Unknown6B);
                        writer.Write(light.Header.Unknown7A);
                        writer.Write(light.Header.Unknown7B);
                    }
                }
            }
        }

        public static void Save(ICLIFlags flags, STUMapHeader map, ulong key, string basePath) {
            string name = GetValidFilename(GetString(map.m_displayName)) ?? "Title Screen";
            //string name = map.m_506FA8D8;

            var variantName = GetString(map.m_1C706502);
            if (variantName != null) name = GetValidFilename(variantName);

            Log($"Extracting map {name}\\{teResourceGUID.Index(key):X}");
            
            // TODO: MAP11 HAS CHANGED
            // TODO: MAP10 TOO?
            
            string mapPath = Path.Combine(basePath, "Maps", name, teResourceGUID.Index(key).ToString("X")) + Path.DirectorySeparatorChar;
            
            CreateDirectoryFromFile(mapPath);
            
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            LoudLog("\tFinding");
            FindLogic.Combo.Find(info, map.m_map);

            //for (ushort i = 0; i < 255; i++) {
            //    using (Stream mapChunkStream = OpenFile(map.GetDataKey(i))) {
            //        if (mapChunkStream == null) continue;
            //        WriteFile(mapChunkStream, Path.Combine(mapPath, $"{(Enums.teMAP_PLACEABLE_TYPE)i}.0BC"));
            //    }
            //}

            teMapPlaceableData placeableModelGroups = GetPlaceableData(map, Enums.teMAP_PLACEABLE_TYPE.MODEL_GROUP);
            teMapPlaceableData placeableSingleModels = GetPlaceableData(map, Enums.teMAP_PLACEABLE_TYPE.SINGLE_MODEL);
            teMapPlaceableData placeable8 = GetPlaceableData(map, 8);
            teMapPlaceableData placeableLights = GetPlaceableData(map, Enums.teMAP_PLACEABLE_TYPE.LIGHT);
            teMapPlaceableData placeableEntities = GetPlaceableData(map, Enums.teMAP_PLACEABLE_TYPE.ENTITY);
            
            OverwatchMap exportMap = new OverwatchMap(name, info, placeableSingleModels, placeableModelGroups, placeable8, placeableEntities, placeableLights);
            using (Stream outputStream = File.OpenWrite(Path.Combine(mapPath, $"{name}.{exportMap.Extension}"))) {
                exportMap.Write(outputStream);
            }

            {
                FindLogic.Combo.Find(info, map.m_86C1CFAB);
                FindLogic.Combo.Find(info, map.m_9386E669);
                FindLogic.Combo.Find(info, map.m_C6599DEB);

                if (map.m_D608E9F3 != null) {
                    foreach (teResourceGUID gamemodeGUID in map.m_D608E9F3) {
                        STUGameMode gameMode = GetInstanceNew<STUGameMode>(gamemodeGUID);
                        if (gameMode == null) continue;

                        FindLogic.Combo.Find(info, gameMode.m_6EB38130);  // 004
                        FindLogic.Combo.Find(info, gameMode.m_CF63B633);  // 01B

                        foreach (STUGameModeTeam team in gameMode.m_teams) {
                            FindLogic.Combo.Find(info, team.m_bodyScript);  // 01B
                            FindLogic.Combo.Find(info, team.m_controllerScript);  // 01B
                        }
                    }
                }
            }

            FindLogic.Combo.Find(info, map.m_announcerWelcome);
            info.SetEffectName(map.m_announcerWelcome, "AnnouncerWelcome");
            FindLogic.Combo.Find(info, map.m_musicTease);
            info.SetEffectName(map.m_musicTease, "MusicTease");
            
            LoudLog("\tSaving");
            Combo.Save(flags, mapPath, info);
            Combo.SaveLooseTextures(flags, Path.Combine(mapPath, "Textures"), info);
            
            // if (map.VoiceSet != null) {
            //     FindLogic.Combo.ComboInfo soundInfo = new FindLogic.Combo.ComboInfo();
            //     FindLogic.Combo.Find(soundInfo, map.VoiceSet);
            //
            //     if (soundInfo.VoiceSets.ContainsKey(map.VoiceSet)) {
            //         string soundPath = Path.Combine(mapPath, "Sound");
            //         FindLogic.Combo.VoiceSetInfo voiceSetInfo = soundInfo.VoiceSets[map.VoiceSet];
            //         Combo.SaveVoiceSet(flags, soundPath, soundInfo, voiceSetInfo);
            //     }
            // }
            
            LoudLog("\tDone");
        }

        public static teMapPlaceableData GetPlaceableData(STUMapHeader map, Enums.teMAP_PLACEABLE_TYPE modelGroup) {
            return GetPlaceableData(map, (byte) modelGroup);
        }

        public static teMapPlaceableData GetPlaceableData(STUMapHeader map, byte type) {
            using (Stream stream = OpenFile(map.GetChunkKey(type))) {
                if (stream == null) return null;
                return new teMapPlaceableData(stream);
            }
        }
    }
}