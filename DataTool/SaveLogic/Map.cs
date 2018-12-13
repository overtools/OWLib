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

            public OverwatchMap(string name, FindLogic.Combo.ComboInfo info, teMapPlaceableData singleModels,
                teMapPlaceableData modelGroups, teMapPlaceableData models, teMapPlaceableData entities,
                teMapPlaceableData lights, teMapPlaceableData sounds, teMapPlaceableData effects) {
                Name = name;
                Info = info;

                SingleModels = singleModels;
                ModelGroups = modelGroups;
                Models = models;
                Entities = entities;
                Lights = lights;
                Sounds = sounds;
                Effects = effects;
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
                        teMapPlaceableEntity entity = (teMapPlaceableEntity) Entities.Placeables[i];
                        var components = GetInstances<STUModelComponent>(entity.Header.EntityDefinition).Where(component => teResourceGUID.Index(component.m_model) > 1);
                        if(components.Count() == 0)
                        {
                            continue;
                        }
                        modelComponentSets[i] = new STUModelComponent[components.Count()];
                        entitiesWithModelCount += modelComponentSets[i].Length;
                        modelComponentSets[i] = components.ToArray();
                    }

                    writer.Write((uint)(SingleModels.Header.PlaceableCount + Models.Header.PlaceableCount +
                                 entitiesWithModelCount)); // nr details
                    writer.Write(Lights.Header.PlaceableCount); // nr Lights

                    foreach (IMapPlaceable mapPlaceable in ModelGroups.Placeables ?? Array.Empty<IMapPlaceable>()) {
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

                    foreach (IMapPlaceable mapPlaceable in SingleModels.Placeables ?? Array.Empty<IMapPlaceable>()) {
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

                    foreach (IMapPlaceable mapPlaceable in Models.Placeables ?? Array.Empty<IMapPlaceable>()) {
                        teMapPlaceableModel placeableModel = (teMapPlaceableModel) mapPlaceable;
                        
                        FindLogic.Combo.Find(Info, placeableModel.Header.Model);
                        FindLogic.Combo.Find(Info, placeableModel.Header.ModelLook, null,
                            new FindLogic.Combo.ComboContext {Model = placeableModel.Header.Model});

                        FindLogic.Combo.ModelInfoNew modelInfo = Info.Models[placeableModel.Header.Model];
                        FindLogic.Combo.ModelLookInfo modelLookInfo = Info.ModelLooks[placeableModel.Header.ModelLook];
                        string modelFn =
                            $"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}.owmdl";
                        string matFn =
                            $"Models\\{modelInfo.GetName()}\\ModelLooks\\{modelLookInfo.GetNameIndex()}.owmat";

                        writer.Write(modelFn);
                        writer.Write(matFn);
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

                            FindLogic.Combo.ModelInfoNew modelInfo = Info.Models[model];
                            string modelFn = $"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}.owmdl";
                            if (Info.Entities.ContainsKey(entity.Header.EntityDefinition))
                            {
                                modelFn = $"Entities\\{Info.Entities[entity.Header.EntityDefinition].GetName()}\\{Info.Entities[entity.Header.EntityDefinition].GetName()}.owentity";
                            }
                            string matFn = "null";
                            try {
                                FindLogic.Combo.ModelLookInfo modelLookInfo = Info.ModelLooks[modelLookSet.First(x => x > 0)];
                                matFn = $"Models\\{modelInfo.GetName()}\\ModelLooks\\{modelLookInfo.GetNameIndex()}.owmat";
                            }
                            catch { }

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
                        
                        writer.Write((uint)light.Header.Type);
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

                    writer.Write(Sounds.Header.PlaceableCount); // nr Sounds

                    // Extension 1.2 - Sounds
                    foreach (IMapPlaceable mapPlaceable in Sounds.Placeables ?? Array.Empty<IMapPlaceable>()) {
                        var sound = (teMapPlaceableSound)mapPlaceable;
                        FindLogic.Combo.Find(Info, sound.Header.Sound);
                        writer.Write(sound.Header.Translation);
                        if(!Info.Sounds.ContainsKey(sound.Header.Sound) || Info.Sounds[sound.Header.Sound].SoundFiles == null) {
                            writer.Write(0);
                            continue;
                        }
                        writer.Write(Info.Sounds[sound.Header.Sound].SoundFiles.Count);
                        foreach(var soundfile in Info.Sounds[sound.Header.Sound].SoundFiles?.Values)
                        {
                            writer.Write($@"Sounds\{Info.SoundFiles[soundfile].GetName()}.ogg");
                        }
                    }

                    // Extension 1.3 - Effects
                    foreach (IMapPlaceable mapPlaceable in Effects.Placeables ?? Array.Empty<IMapPlaceable>())
                    {
                        // todo: wtf
                    }
                }
            }
        }

        public static void Save(ICLIFlags flags, STUMapHeader mapHeader, ulong key, string basePath) {
            string name = GetString(mapHeader.m_displayName) ?? "Title Screen";
            //string name = map.m_506FA8D8;
            var variantName = GetString(mapHeader.m_1C706502);
            if (variantName != null) name = variantName;

            LoudLog($"Extracting map {name}\\{teResourceGUID.Index(key):X}");
            name = GetValidFilename(name);
            
            // TODO: MAP11 HAS CHANGED
            // TODO: MAP10 TOO?
            
            string mapPath = Path.Combine(basePath, "Maps", name, teResourceGUID.Index(key).ToString("X")) + Path.DirectorySeparatorChar;
            
            CreateDirectoryFromFile(mapPath);
            
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            LoudLog("\tFinding");
            FindLogic.Combo.Find(info, mapHeader.m_map);

            //for (ushort i = 0; i < 255; i++) {
            //    using (Stream mapChunkStream = OpenFile(map.GetDataKey(i))) {
            //        if (mapChunkStream == null) continue;
            //        WriteFile(mapChunkStream, Path.Combine(mapPath, $"{(Enums.teMAP_PLACEABLE_TYPE)i}.0BC"));
            //    }
            //}

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
                FindLogic.Combo.Find(info, mapHeader.m_86C1CFAB);
                FindLogic.Combo.Find(info, mapHeader.m_9386E669);
                FindLogic.Combo.Find(info, mapHeader.m_C6599DEB);

                if (mapHeader.m_D608E9F3 != null) {
                    foreach (teResourceGUID gamemodeGUID in mapHeader.m_D608E9F3) {
                        STUGameMode gameMode = GetInstance<STUGameMode>(gamemodeGUID);
                        if (gameMode == null) continue;

                        FindLogic.Combo.Find(info, gameMode.m_6EB38130);  // 004
                        FindLogic.Combo.Find(info, gameMode.m_CF63B633);  // 01B
                        FindLogic.Combo.Find(info, gameMode.m_7F5B54B2);  // game mode voice set

                        foreach (STUGameModeTeam team in gameMode.m_teams) {
                            FindLogic.Combo.Find(info, team.m_bodyScript);  // 01B
                            FindLogic.Combo.Find(info, team.m_controllerScript);  // 01B
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
            Combo.Save(flags, mapPath, info);
            Combo.SaveLooseTextures(flags, Path.Combine(mapPath, "Textures"), info);
            
            if (mapHeader.m_7F5B54B2 != 0) {  // map voice set. not announcer
                FindLogic.Combo.Find(info, mapHeader.m_7F5B54B2);
            }

            if (announcerVoiceSet != 0) {  // whole thing in env mode, not here
                info.VoiceSets.Remove(announcerVoiceSet);
            }
            Combo.SaveAllVoiceSets(flags, Path.Combine(mapPath, "VoiceSets"), info);
            Combo.SaveAllSoundFiles(flags, Path.Combine(mapPath, "Sound"), info);

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