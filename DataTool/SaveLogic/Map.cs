using System.IO;
using DataTool.Flag;
using TankLib;
using TankLib.ExportFormats;
using TankLib.STU;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.DataModels;
using TACTLib;

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
                    writer.Write((ushort) 1); // version major
                    writer.Write((ushort) 2); // version minor

                    if (Name.Length == 0) {
                        writer.Write((byte) 0);
                    } else {
                        writer.Write(Name);
                    }

                    writer.Write(ModelGroups.Header.PlaceableCount); // nr objects

                    int entitiesWithModelCount = 0;
                    STUModelComponent[][] modelComponentSets = new STUModelComponent[Entities.Header.PlaceableCount][];

                    for (int i = 0; i < Entities.Header.PlaceableCount; i++) {
                        // todo: wtf is this code

                        teMapPlaceableEntity entity = (teMapPlaceableEntity) Entities.Placeables[i];
                        var modelComponents = GetInstances<STUModelComponent>(entity.Header.EntityDefinition).Where(component => teResourceGUID.Index(component.m_model) > 1);
                        if (modelComponents.Count() == 0) {
                            foreach (STUComponentInstanceData instanceData in entity.InstanceData) {
                                if (instanceData is STUStatescriptComponentInstanceData statescriptComponentInstanceData) {
                                    if (statescriptComponentInstanceData.m_6D10093E != null) {
                                        foreach (STUStatescriptGraphWithOverrides graphWithOverrides in statescriptComponentInstanceData.m_6D10093E) {
                                            FindLogic.Combo.Find(Info, graphWithOverrides);
                                        }
                                    }

                                    if (statescriptComponentInstanceData.m_2D9815BA != null) {
                                        // todo: ??
                                    }
                                }
                            }

                            continue;
                        }

                        modelComponentSets[i] = new STUModelComponent[modelComponents.Count()];
                        entitiesWithModelCount += modelComponentSets[i].Length;
                        modelComponentSets[i] = modelComponents.ToArray();
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

                        STUModelComponent[] modelComponents = modelComponentSets[i];
                        if (modelComponents == null) continue;

                        FindLogic.Combo.Find(Info, entity.Header.EntityDefinition);

                        foreach (var modelComponent in modelComponents) {
                            ulong model = modelComponent.m_model;
                            var modelLookSet = new List<ulong> { modelComponent.m_look };

                            foreach (STUComponentInstanceData instanceData in entity.InstanceData) {
                                if (!(instanceData is STUModelComponentInstanceData modelComponentInstanceData)) continue;
                                if (modelComponentInstanceData.m_look != 0) {
                                    modelLookSet.Add(modelComponentInstanceData.m_look);
                                }
                            }

                            FindLogic.Combo.Find(Info, model);
                            foreach (var modelLook in modelLookSet) {
                                FindLogic.Combo.Find(Info, modelLook, null, new FindLogic.Combo.ComboContext { Model = model });
                            }

                            FindLogic.Combo.ModelAsset modelInfo = Info.m_models[model];
                            string modelFn = GetModelPath(modelInfo);
                            if (Info.m_entities.ContainsKey(entity.Header.EntityDefinition)) {
                                modelFn = Path.Combine("Entities", Info.m_entities[entity.Header.EntityDefinition].GetName(), Info.m_entities[entity.Header.EntityDefinition].GetName() + ".owentity");
                            }

                            string matFn = "null";
                            try {
                                FindLogic.Combo.ModelLookAsset modelLookInfo = Info.m_modelLooks[modelLookSet.First(x => x > 0)];
                                matFn = GetModelLookMatPath(modelInfo, modelLookInfo);
                            } catch { }

                            writer.Write(modelFn);
                            writer.Write(matFn);
                            writer.Write(entity.Header.Translation);
                            writer.Write(entity.Header.Scale);
                            writer.Write(entity.Header.Rotation);
                        }
                    }

                    // Extension 1.1 - Lights
                    foreach (IMapPlaceable mapPlaceable in Lights.Placeables ?? Array.Empty<IMapPlaceable>()) {
                        var light = (teMapPlaceableLight) mapPlaceable;

                        writer.Write(light.Header.Translation);
                        writer.Write(light.Header.Rotation);

                        writer.Write((uint) light.Header.Type);
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
                    }
                }
            }
        }

        public static void Save(ICLIFlags flags, MapHeader mapInfo, STUMapHeader mapHeader, ulong key, string basePath) {
            var name = mapInfo.GetName();
            LoudLog($"Extracting map {name}/{teResourceGUID.Index(key):X}");

            // TODO: MAP11 HAS CHANGED
            // TODO: MAP10 TOO?

            name = GetValidFilename(name);
            string mapPath = Path.Combine(basePath, "Maps", name, teResourceGUID.Index(key).ToString("X")) + Path.DirectorySeparatorChar;
            CreateDirectoryFromFile(mapPath);

            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            LoudLog("\tFinding");
            FindLogic.Combo.Find(info, mapHeader.m_map);

            //for (ushort i = 0; i < 255; i++) {
            //    using (Stream mapChunkStream = OpenFile(mapHeader.GetChunkKey((byte)i))) {
            //        if (mapChunkStream == null) continue;
            //        WriteFile(mapChunkStream, Path.Combine(mapPath, $"{(Enums.teMAP_PLACEABLE_TYPE)i}.0BC"));
            //    }
            //}
            //return;

            teMapPlaceableData placeableModelGroups = GetPlaceableData(mapHeader, Enums.teMAP_PLACEABLE_TYPE.MODEL_GROUP);
            teMapPlaceableData placeableSingleModels = GetPlaceableData(mapHeader, Enums.teMAP_PLACEABLE_TYPE.SINGLE_MODEL);
            teMapPlaceableData placeableModel = GetPlaceableData(mapHeader, Enums.teMAP_PLACEABLE_TYPE.MODEL);
            teMapPlaceableData placeableLights = GetPlaceableData(mapHeader, Enums.teMAP_PLACEABLE_TYPE.LIGHT);
            teMapPlaceableData placeableEntities = GetPlaceableData(mapHeader, Enums.teMAP_PLACEABLE_TYPE.ENTITY);
            teMapPlaceableData placeableSounds = GetPlaceableData(mapHeader, Enums.teMAP_PLACEABLE_TYPE.SOUND);
            teMapPlaceableData placeableEffects = GetPlaceableData(mapHeader, Enums.teMAP_PLACEABLE_TYPE.EFFECT);

            OverwatchMap exportMap = new OverwatchMap(name, info, placeableSingleModels, placeableModelGroups, placeableModel, placeableEntities, placeableLights, placeableSounds, placeableEffects);
            using (Stream outputStream = File.OpenWrite(Path.Combine(mapPath, $"{name}.{exportMap.Extension}"))) {
                exportMap.Write(outputStream);
            }

            {
                FindLogic.Combo.Find(info, mapHeader.m_loadingScreen);
                FindLogic.Combo.Find(info, mapHeader.m_smallMapIcon);
                FindLogic.Combo.Find(info, mapHeader.m_loadingScreenFlag);

                if (mapHeader.m_supportedGamemodes != null) {
                    foreach (teResourceGUID gamemodeGUID in mapHeader.m_supportedGamemodes) {
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

            FindLogic.Combo.Find(info, mapHeader.m_announcerWelcome);
            info.SetEffectName(mapHeader.m_announcerWelcome, "AnnouncerWelcome");
            FindLogic.Combo.Find(info, mapHeader.m_musicTease);
            info.SetEffectName(mapHeader.m_musicTease, "MusicTease");

            ulong announcerVoiceSet = 0;
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
            }

            LoudLog("\tSaving");
            var context = new Combo.SaveContext(info);
            Combo.Save(flags, mapPath, context);
            Combo.SaveLooseTextures(flags, Path.Combine(mapPath, "Textures"), context);

            if (mapHeader.m_7F5B54B2 != 0) { // map voice set. not announcer
                FindLogic.Combo.Find(info, mapHeader.m_7F5B54B2);
            }

            if (announcerVoiceSet != 0) { // whole thing in env mode, not here
                info.m_voiceSets.Remove(announcerVoiceSet);
            }

            Combo.SaveAllVoiceSets(flags, Path.Combine(mapPath, "VoiceSets"), context);
            Combo.SaveAllSoundFiles(flags, Path.Combine(mapPath, "Sound"), context);

            LoudLog("\tDone");
        }

        public static teMapPlaceableData GetPlaceableData(STUMapHeader map, Enums.teMAP_PLACEABLE_TYPE modelGroup) {
            using (Stream stream = OpenFile(map.GetChunkKey(modelGroup))) {
                return stream == null ? null : new teMapPlaceableData(stream, modelGroup);
            }
        }

        public static teMapPlaceableData GetPlaceableData(STUMapHeader map, byte type) {
            return GetPlaceableData(map, (Enums.teMAP_PLACEABLE_TYPE) type);
        }
    }
}