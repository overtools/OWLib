using System.IO;
using DataTool.Flag;
using TankLib;
using TankLib.ExportFormats;
using TankLib.STU;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;
using TankLib.Helpers;
using System;
using DataTool.DataModels;

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
            public teMapPlaceableData Models;
            public teMapPlaceableData Entities;
            public teMapPlaceableData Lights;
            public teMapPlaceableData Sounds;
            public teMapPlaceableData Effects;

            public OverwatchMap(
                string name, FindLogic.Combo.ComboInfo info, teMapPlaceableData singleModels,
                teMapPlaceableData modelGroups, teMapPlaceableData models, teMapPlaceableData entities,
                teMapPlaceableData lights, teMapPlaceableData sounds, teMapPlaceableData effects) {
                Name = name;
                Info = info;

                SingleModels = singleModels ?? new teMapPlaceableData();
                ModelGroups = modelGroups ?? new teMapPlaceableData();
                Models = models ?? new teMapPlaceableData();
                Entities = entities ?? new teMapPlaceableData();
                Lights = lights ?? new teMapPlaceableData();
                Sounds = sounds ?? new teMapPlaceableData();
                Effects = effects ?? new teMapPlaceableData();
            }

            private string GetModelLookMatPath(FindLogic.Combo.ModelAsset modelInfo, FindLogic.Combo.ModelLookAsset modelLookAsset) {
                return Path.Combine("Models", modelInfo.GetName(), "ModelLooks", modelLookAsset.GetNameIndex() + ".owmat");
            }

            private string GetModelPath(FindLogic.Combo.ModelAsset modelInfo) {
                return Path.Combine("Models", modelInfo.GetName(), modelInfo.GetNameIndex() + ".owmdl");
            }

            public void Write(Stream output) {
                using (BinaryWriter writer = new BinaryWriter(output)) {
                    writer.Write((ushort) 2); // version major
                    writer.Write((ushort) 1); // version minor

                    if (Name.Length == 0) {
                        writer.Write((byte) 0);
                    } else {
                        writer.Write(Name);
                    }

                    writer.Write(ModelGroups.Header.PlaceableCount); // nr objects

                    int entitiesWithModelCount = 0;
                    for (int i = 0; i < Entities.Header.PlaceableCount; i++) {

                        teMapPlaceableEntity entity = (teMapPlaceableEntity) Entities.Placeables[i];
                        FindLogic.Combo.Find(Info, entity.Header.EntityDefinition);

                        var entityInfo = Info.m_entities[entity.Header.EntityDefinition];
                        if (entityInfo.m_modelGUID != 0) {
                            entitiesWithModelCount += 1;
                        }

                        foreach (STUComponentInstanceData instanceData in entity.InstanceData) {
                            if (instanceData is STUStatescriptComponentInstanceData statescriptComponentInstanceData) {
                                if (statescriptComponentInstanceData.m_6D10093E != null) {
                                    foreach (STUStatescriptGraphWithOverrides graphWithOverrides in statescriptComponentInstanceData.m_6D10093E) {
                                        FindLogic.Combo.Find(Info, graphWithOverrides);
                                    }
                                }

                                FindLogic.Combo.Find(Info, statescriptComponentInstanceData.m_2D9815BA);
                            } else if (instanceData is STUModelComponentInstanceData modelComponentInstanceData) {
                                // (anim)
                                FindLogic.Combo.Find(Info, modelComponentInstanceData.m_FD090EAD, null, new FindLogic.Combo.ComboContext {
                                    Entity = entityInfo.m_GUID,
                                    Model = entityInfo.m_modelGUID
                                });
                            }
                        }
                    }

                    writer.Write((uint) (SingleModels.Header.PlaceableCount + Models.Header.PlaceableCount +
                                         entitiesWithModelCount)); // nr details

                    writer.Write(Lights.Header.PlaceableCount); // nr Lights

                    foreach (IMapPlaceable mapPlaceable in ModelGroups.Placeables ?? Array.Empty<IMapPlaceable>()) {
                        teMapPlaceableModelGroup modelGroup = (teMapPlaceableModelGroup) mapPlaceable;

                        FindLogic.Combo.Find(Info, modelGroup.Header.Model);
                        FindLogic.Combo.ModelAsset modelInfo = Info.m_models[modelGroup.Header.Model];
                        writer.Write(GetModelPath(modelInfo));
                        writer.Write(modelGroup.Header.GroupCount);
                        for (int j = 0; j < modelGroup.Header.GroupCount; ++j) {
                            teMapPlaceableModelGroup.Group group = modelGroup.Groups[j];
                            FindLogic.Combo.Find(Info, group.ModelLook, null,
                                                 new FindLogic.Combo.ComboContext { Model = modelGroup.Header.Model });

                            FindLogic.Combo.ModelLookAsset modelLookInfo = Info.m_modelLooks[group.ModelLook];

                            writer.Write(GetModelLookMatPath(modelInfo, modelLookInfo));
                            writer.Write(group.EntryCount);
                            for (int k = 0; k < group.EntryCount; ++k) {
                                teMapPlaceableModelGroup.Entry record = modelGroup.Entries[j][k];

                                writer.Write(record.Translation);
                                writer.Write(record.Scale);
                                writer.Write(record.Rotation);
                            }
                        }
                    }

                    foreach (IMapPlaceable mapPlaceable in SingleModels.Placeables ?? Array.Empty<IMapPlaceable>()) {
                        teMapPlaceableSingleModel singleModel = (teMapPlaceableSingleModel) mapPlaceable;

                        FindLogic.Combo.Find(Info, singleModel.Header.Model);
                        FindLogic.Combo.Find(Info, singleModel.Header.ModelLook, null,
                                             new FindLogic.Combo.ComboContext { Model = singleModel.Header.Model });

                        FindLogic.Combo.ModelAsset modelInfo = Info.m_models[singleModel.Header.Model];
                        FindLogic.Combo.ModelLookAsset modelLookInfo = Info.m_modelLooks[singleModel.Header.ModelLook];

                        writer.Write(GetModelPath(modelInfo));
                        writer.Write(GetModelLookMatPath(modelInfo, modelLookInfo));
                        writer.Write(singleModel.Header.Translation);
                        writer.Write(singleModel.Header.Scale);
                        writer.Write(singleModel.Header.Rotation);
                    }

                    foreach (IMapPlaceable mapPlaceable in Models.Placeables ?? Array.Empty<IMapPlaceable>()) {
                        teMapPlaceableModel placeableModel = (teMapPlaceableModel) mapPlaceable;

                        FindLogic.Combo.Find(Info, placeableModel.Header.Model);
                        FindLogic.Combo.Find(Info, placeableModel.Header.ModelLook, null,
                                             new FindLogic.Combo.ComboContext { Model = placeableModel.Header.Model });

                        FindLogic.Combo.ModelAsset modelInfo = Info.m_models[placeableModel.Header.Model];
                        FindLogic.Combo.ModelLookAsset modelLookInfo = Info.m_modelLooks[placeableModel.Header.ModelLook];

                        writer.Write(GetModelPath(modelInfo));
                        writer.Write(GetModelLookMatPath(modelInfo, modelLookInfo));
                        writer.Write(placeableModel.Header.Translation);
                        writer.Write(placeableModel.Header.Scale);
                        writer.Write(placeableModel.Header.Rotation);
                    }

                    for (int i = 0; i < Entities.Placeables?.Length; i++) {
                        var entity = (teMapPlaceableEntity) Entities.Placeables[i];
                        var entityInfo = Info.m_entities[entity.Header.EntityDefinition];

                        var model = entityInfo.m_modelGUID;
                        var look = entityInfo.m_modelLookGUID;

                        if (model == 0) {
                            continue;
                        }

                        foreach (STUComponentInstanceData instanceData in entity.InstanceData) {
                            if (instanceData is not STUModelComponentInstanceData modelComponentInstanceData) continue;
                            if (modelComponentInstanceData.m_EE77FFF9 != 0) {
                                look = modelComponentInstanceData.m_EE77FFF9;
                            } else if (modelComponentInstanceData.m_look != 0) {
                                look = modelComponentInstanceData.m_look;
                            } else {
                                break;
                            }

                            var lookContext = new FindLogic.Combo.ComboContext { Model = model };
                            FindLogic.Combo.Find(Info, look, null, lookContext);
                        }


                        FindLogic.Combo.ModelAsset modelInfo = Info.m_models[model];
                        string modelFn = GetModelPath(modelInfo);
                        if (Info.m_entities.ContainsKey(entity.Header.EntityDefinition)) {
                            modelFn = Path.Combine("Entities", Info.m_entities[entity.Header.EntityDefinition].GetName(), Info.m_entities[entity.Header.EntityDefinition].GetName() + ".owentity");
                        }

                        // todo: stop throw pls. anyway
                        string matFn = "null";
                        try {
                            FindLogic.Combo.ModelLookAsset modelLookInfo = Info.m_modelLooks[look];
                            matFn = GetModelLookMatPath(modelInfo, modelLookInfo);
                        } catch { }

                        writer.Write(modelFn);
                        writer.Write(matFn);
                        writer.Write(entity.Header.Translation);
                        writer.Write(entity.Header.Scale);
                        writer.Write(entity.Header.Rotation);
                    }

                    // Extension 1.1 - Lights
                    foreach (IMapPlaceable mapPlaceable in Lights.Placeables ?? Array.Empty<IMapPlaceable>()) {
                        var light = (teMapPlaceableLight) mapPlaceable;

                        writer.Write(light.Header.Translation);
                        writer.Write(light.Header.Rotation);

                        writer.Write((uint) light.Header.Type);
                        writer.Write(light.Header.LightFOV);
                        writer.Write(light.Header.Color);
                        writer.Write(light.Header.IntensityGUESS);

                        writer.Write(light.Header.ProjectionTexture1);
                        writer.Write(light.Header.ProjectionTexture2);

                        FindLogic.Combo.Find(Info, light.Header.ProjectionTexture1);
                        FindLogic.Combo.Find(Info, light.Header.ProjectionTexture2);
                    }

                    writer.Write(Sounds.Header.PlaceableCount); // nr Sounds

                    // Extension 1.2 - Sounds
                    foreach (IMapPlaceable mapPlaceable in Sounds.Placeables ?? Array.Empty<IMapPlaceable>()) {
                        var sound = (teMapPlaceableSound) mapPlaceable;
                        FindLogic.Combo.Find(Info, sound.Header.Sound);
                        writer.Write(sound.Header.Translation);
                        if (!Info.m_sounds.ContainsKey(sound.Header.Sound) || Info.m_sounds[sound.Header.Sound].SoundFiles == null) {
                            writer.Write(0);
                            continue;
                        }

                        writer.Write(Info.m_sounds[sound.Header.Sound].SoundFiles.Count);
                        foreach (var soundfile in Info.m_sounds[sound.Header.Sound].SoundFiles?.Values) {
                            writer.Write($@"Sounds\{Info.m_soundFiles[soundfile].GetName()}.ogg");
                        }
                    }

                    // Extension 1.3 - Effects
                    foreach (IMapPlaceable mapPlaceable in Effects.Placeables ?? Array.Empty<IMapPlaceable>()) {
                        var effect = (teMapPlaceableEffect) mapPlaceable;
                        FindLogic.Combo.Find(Info, effect.Header.Effect);
                        // todo: wtf

                        // todo: who did this - zingy
                    }
                }
            }
        }

        public static void Save(ICLIFlags flags, MapHeader mapInfo, STUMapHeader mapHeader, ulong key, string basePath) {
            var name = mapInfo.GetName();
            Logger.Log($"Extracting map {name}/{teResourceGUID.Index(key):X}");

            // TODO: MAP11 HAS CHANGED
            // TODO: MAP10 TOO?

            name = GetValidFilename(name);
            string mapPath = Path.Combine(basePath, "Maps", name, teResourceGUID.Index(key).ToString("X")) + Path.DirectorySeparatorChar;
            CreateDirectoryFromFile(mapPath);

            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            Logger.Log("\tFinding");
            FindLogic.Combo.Find(info, mapHeader.m_map);

            for (int i = 0; i < mapHeader.m_D97BC44F.Length; i++) {
                var variantModeInfo = mapHeader.m_D97BC44F[i];
                var variantResultingMap = mapHeader.m_78715D57[i];
                var variantGUID = variantResultingMap.m_BF231F12;

                using (Stream stream = OpenFile(variantGUID)) {
                    if (stream == null) {
                        // not shipping
                        continue;
                    }
                }

                string variantName = GetVariantName(variantModeInfo, variantResultingMap);

                FindLogic.Combo.Find(info, variantResultingMap.m_0342E00E?.m_loadingScreen); // big
                FindLogic.Combo.Find(info, variantResultingMap.m_0342E00E?.m_smallMapIcon);
                FindLogic.Combo.Find(info, variantResultingMap.m_0342E00E?.m_loadingScreenFlag);

                FindLogic.Combo.Find(info, variantResultingMap.m_0342E00E?.m_7F5B54B2);
                FindLogic.Combo.Find(info, variantResultingMap.m_0342E00E?.m_announcerWelcome);
                FindLogic.Combo.Find(info, variantResultingMap.m_0342E00E?.m_musicTease);

                // todo: announcer save doesnt work properly
                // at least for wrath of the bride...
                // cant find the stim in this voice set...

                info.SetEffectVoiceSet(variantResultingMap.m_0342E00E?.m_announcerWelcome, variantResultingMap.m_0342E00E?.m_7F5B54B2);
                info.SetEffectName(variantResultingMap.m_0342E00E?.m_announcerWelcome, $"AnnouncerWelcome - {variantName}");
                info.SetEffectName(variantResultingMap.m_0342E00E?.m_musicTease, $"MusicTease - {variantName}");

                teMapPlaceableData placeableModelGroups = GetPlaceableData(mapHeader, variantGUID, Enums.teMAP_PLACEABLE_TYPE.MODEL_GROUP);
                teMapPlaceableData placeableSingleModels = GetPlaceableData(mapHeader, variantGUID, Enums.teMAP_PLACEABLE_TYPE.SINGLE_MODEL);
                teMapPlaceableData placeableModel = GetPlaceableData(mapHeader, variantGUID, Enums.teMAP_PLACEABLE_TYPE.MODEL);
                teMapPlaceableData placeableLights = GetPlaceableData(mapHeader, variantGUID, Enums.teMAP_PLACEABLE_TYPE.LIGHT);
                teMapPlaceableData placeableEntities = GetPlaceableData(mapHeader, variantGUID, Enums.teMAP_PLACEABLE_TYPE.ENTITY);
                teMapPlaceableData placeableSounds = GetPlaceableData(mapHeader, variantGUID, Enums.teMAP_PLACEABLE_TYPE.SOUND);
                teMapPlaceableData placeableEffects = GetPlaceableData(mapHeader, variantGUID, Enums.teMAP_PLACEABLE_TYPE.EFFECT);
                teMapPlaceableData placeableSequences = GetPlaceableData(mapHeader, variantGUID, Enums.teMAP_PLACEABLE_TYPE.SEQUENCE);

                foreach (IMapPlaceable mapPlaceable in placeableSequences.Placeables ?? Array.Empty<IMapPlaceable>()) {
                    teMapPlaceableSequence sequence = (teMapPlaceableSequence)mapPlaceable;
                    FindLogic.Combo.Find(info, sequence.Header.Effect);
                }

                OverwatchMap exportMap = new OverwatchMap(name, info, placeableSingleModels, placeableModelGroups, placeableModel, placeableEntities, placeableLights, placeableSounds, placeableEffects);
                using (Stream outputStream = File.OpenWrite(Path.Combine(mapPath, $"{variantName}.{exportMap.Extension}"))) {
                    outputStream.SetLength(0);
                    exportMap.Write(outputStream);
                }

                /*ulong announcerVoiceSet = 0;
                using (Stream stream = OpenFile(mapHeader.m_map)) {
                    if (stream != null) {
                        using (BinaryReader reader = new BinaryReader(stream)) {
                            teMap map = reader.Read<teMap>();

                            STUVoiceSetComponent voiceSetComponent =
                                GetInstance<STUVoiceSetComponent>(map.EntityDefinition);

                            announcerVoiceSet = voiceSetComponent?.m_voiceDefinition;
                            FindLogic.Combo.Find(info, announcerVoiceSet);

                            info.SetEffectVoiceSet(mapHeader.m_announcerWelcome, announcerVoiceSet);
                        }
                    }
                }*/
            }

            {
                FindLogic.Combo.Find(info, mapHeader.m_0342E00E?.m_loadingScreen);
                FindLogic.Combo.Find(info, mapHeader.m_0342E00E?.m_smallMapIcon);
                FindLogic.Combo.Find(info, mapHeader.m_0342E00E?.m_loadingScreenFlag);

                if (mapHeader.m_supportedGameModes != null) {
                    foreach (teResourceGUID gamemodeGUID in mapHeader.m_supportedGameModes) {
                        STUGameMode gameMode = GetInstance<STUGameMode>(gamemodeGUID);
                        if (gameMode == null) continue;

                        FindLogic.Combo.Find(info, gameMode.m_6EB38130); // 004
                        FindLogic.Combo.Find(info, gameMode.m_CF63B633); // 01B
                        FindLogic.Combo.Find(info, gameMode.m_7F5B54B2); // game mode voice set

                        foreach (STUGameModeTeam team in gameMode.m_teams) {
                            FindLogic.Combo.Find(info, team.m_bodyScript); // 01B
                            FindLogic.Combo.Find(info, team.m_controllerScript); // 01B
                        }
                    }
                }
            }

            FindLogic.Combo.Find(info, mapHeader.m_0342E00E?.m_announcerWelcome);
            info.SetEffectName(mapHeader.m_0342E00E?.m_announcerWelcome, "AnnouncerWelcome");
            FindLogic.Combo.Find(info, mapHeader.m_0342E00E?.m_musicTease);
            info.SetEffectName(mapHeader.m_0342E00E?.m_musicTease, "MusicTease");

            Logger.Log("\tSaving");
            var context = new Combo.SaveContext(info);
            Combo.Save(flags, mapPath, context);
            Combo.SaveLooseTextures(flags, Path.Combine(mapPath, "Textures"), context);

            if (mapHeader.m_0342E00E?.m_7F5B54B2 != 0) { // map voice set. not announcer
                FindLogic.Combo.Find(info, mapHeader.m_0342E00E?.m_7F5B54B2);
            }

            //if (announcerVoiceSet != 0) { // whole thing in env mode, not here
            //    info.m_voiceSets.Remove(announcerVoiceSet);
            //}

            Combo.SaveAllVoiceSets(flags, Path.Combine(mapPath, "VoiceSets"), context);
            Combo.SaveAllSoundFiles(flags, Path.Combine(mapPath, "Sound"), context);

            Logger.Log("\tDone");
        }

        public static string GetVariantName(STU_71B2D30A variantModeInfo, STU_7FB10A24 variantResultingMap) {
            var gameMode = GetInstance<STUGameMode>(variantModeInfo.m_gameMode);
            var gameModeName = GetCleanString(gameMode?.m_displayName) ?? "Unknown Mode";
            if (gameModeName == "Calypso HeroMode") gameModeName = "HeroMode";

            var envName = GetCleanString(variantResultingMap.m_0342E00E.m_D978BBDC);
            if (envName == "Castle - Eichenwalde (Halloween) Junkenstein 2") {
                envName = "Wrath of the Bride";
            } else if (string.IsNullOrEmpty(envName)) {
                envName = $"{teResourceGUID.Index(variantModeInfo.m_A9253C68):X}";
            }

            var variantName = $"{envName} - {gameModeName}";
            if (variantModeInfo.m_216EA6DA != 0) {
                var mission = GetInstance<STU_8B0E97DC>(variantModeInfo.m_216EA6DA);
                var missionName = GetCleanString(mission?.m_0EDCE350);
                if (missionName != null) {
                    variantName += $" - {missionName}";
                } else {
                    variantName += $" - {variantModeInfo.m_216EA6DA}";
                }
            }

            if (variantModeInfo.m_celebration != 0) {
                var celebrationName = variantModeInfo.m_celebration.GUID.GUID switch {
                    0x04300000000001E9 => "Winter",
                    0x0430000000000254 => "Halloween",
                    _ => variantModeInfo.m_celebration.ToString()
                };
                variantName += $" - {celebrationName}";
            }

            variantName = GetValidFilename(variantName);
            return variantName;
        }

        public static teMapPlaceableData GetPlaceableData(STUMapHeader map, ulong variantGUID, Enums.teMAP_PLACEABLE_TYPE type) {
            using (Stream stream = OpenFile(map.GetChunkKey(variantGUID, type))) {
                return stream == null ? null : new teMapPlaceableData(stream, type);
            }
        }

        public static teMapPlaceableData GetPlaceableData(STUMapHeader map, ulong variantGUID, byte type) {
            return GetPlaceableData(map, variantGUID, (Enums.teMAP_PLACEABLE_TYPE) type);
        }
    }
}
