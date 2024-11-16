using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using DataTool.ConvertLogic;
using DataTool.ConvertLogic.WEM;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.Extract;
using DirectXTexNet;
using RevorbStd;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using TankLib;
using TankLib.Chunks;
using TankLib.ExportFormats;
using static DataTool.Helper.IO;
using Image = SixLabors.ImageSharp.Image;
using Logger = TankLib.Helpers.Logger;

namespace DataTool.SaveLogic {
    public static class Combo {
        public static ScratchDB ScratchDBInstance = new ScratchDB();

        public class SaveContext {
            public FindLogic.Combo.ComboInfo m_info;
            public bool m_saveAnimationEffects = true;
            public bool m_saveAnimationEffectsAsLoose;

            public SaveContext(FindLogic.Combo.ComboInfo info) {
                m_info = info;
            }
        }

        public static void Save(ICLIFlags flags, string path, SaveContext context) {
            foreach (FindLogic.Combo.ModelAsset model in context.m_info.m_models.Values) {
                SaveModel(flags, path, context, model.m_GUID);
            }

            foreach (FindLogic.Combo.EntityAsset entity in context.m_info.m_entities.Values) {
                SaveEntity(flags, path, context, entity.m_GUID);
            }

            foreach (FindLogic.Combo.EffectInfoCombo effectInfo in context.m_info.m_effects.Values) {
                SaveEffect(flags, path, context, effectInfo);
            }

            if (context.m_saveAnimationEffectsAsLoose) {
                foreach (FindLogic.Combo.EffectInfoCombo animationEffectInfo in context.m_info.m_animationEffects.Values) {
                    SaveEffect(flags, path, context, animationEffectInfo);
                }
            }
        }

        /// <summary>
        /// Saves a voiceline instance and saves it's subtitles along with it
        /// </summary>
        /// <remarks>
        /// Note this is only really designed for saving voiceline instances with a SINGLE sound file, if the voiceline instance contains multiple
        /// you should be using the onlyThisSoundFile param
        /// </remarks>
        /// <param name="flags"></param>
        /// <param name="path"></param>
        /// <param name="voiceLineInstanceInfo"></param>
        /// <param name="fileNameOverride">overrides the filename the sound will be saved as, note that subtitles will be appended to the filename</param>
        /// <param name="onlyThisSoundFile">if instance contains multiple voicelines, the guid of the line we're saving</param>
        public static void SaveVoiceLineInstance(
            ICLIFlags flags, string path, FindLogic.Combo.VoiceLineInstanceInfo voiceLineInstanceInfo, string fileNameOverride = null,
            ulong onlyThisSoundFile = default, string fileNamePrefix = null) {
            var subtitlesWithSounds = false; // stores the subtitle alongside the sound file
            var subtitleAsSound = true; // renames the sound file to include the subtitle

            if (flags is ExtractFlags extractFlags) {
                subtitlesWithSounds = extractFlags.SubtitlesWithSounds;
                subtitleAsSound = extractFlags.SubtitlesAsSound;
            }

            var realPath = path;
            var soundSet = new HashSet<ulong>(voiceLineInstanceInfo.SoundFiles.Where(x => x != 0 && (onlyThisSoundFile == default || x == onlyThisSoundFile)));
            if (!soundSet.Any()) return;

            var soundFileName = fileNameOverride ?? teResourceGUID.AsString(soundSet.First()); // file name override or the guid of the sound
            if (fileNamePrefix != null) {
                soundFileName = $"{fileNamePrefix}-{soundFileName}";
            }

            string overrideName = soundFileName; // set this as a fallback if it isn't set below due to subtitles not being saved potentially

            // this is pretty jank
            if (subtitlesWithSounds || subtitleAsSound) {
                var subtitle = GetSubtitleString(voiceLineInstanceInfo.Subtitle);

                if (subtitle != null) {
                    var subtitleStr = subtitle.Trim().TrimEnd('.');
                    if (soundSet.Count > 1) {
                        realPath = Path.Combine(realPath, GetValidFilename(subtitleStr));
                        WriteFile(string.Join("\n", subtitle), Path.Combine(realPath, $"{teResourceGUID.LongKey(voiceLineInstanceInfo.Subtitle):X8}-{teResourceGUID.LongKey(voiceLineInstanceInfo.SubtitleRuntime):X8}-subtitles.txt"));
                    } else if (soundSet.Count == 1) {
                        try {
                            if (subtitleAsSound) {
                                // if we're using the subtitle in the file name, generate the new name here, also trim it to make sure it isn't too long
                                overrideName = GetValidFilename($"{soundFileName}-{subtitleStr}");
                                if (overrideName.Length > 128) {
                                    overrideName = overrideName.Substring(0, 130).Trim().TrimEnd('.');
                                }
                            }

                            if (subtitlesWithSounds)
                                WriteFile(string.Join("\n", subtitle), Path.Combine(realPath, $"{overrideName}.txt"));
                        } catch {
                            if (subtitlesWithSounds) {
                                overrideName = soundFileName; // use default name if we can't save above
                                WriteFile(string.Join("\n", subtitle), Path.Combine(realPath, $"{overrideName}.txt"));
                            }
                        }
                    }
                }
            }

            foreach (ulong soundFile in soundSet) {
                SaveSoundFile(flags, realPath, soundFile, overrideName);
            }
        }

        public static void SaveEntity(ICLIFlags flags, string path, SaveContext context, ulong entityGuid, string pathsRelativeTo="../..") {
            FindLogic.Combo.EntityAsset entityInfo = context.m_info.m_entities[entityGuid];

            Entity.OverwatchEntity entity = new Entity.OverwatchEntity(entityInfo, context.m_info, pathsRelativeTo);

            string entityDir = Path.Combine(path, "Entities", entityInfo.GetName());
            string outputFile = Path.Combine(entityDir, entityInfo.GetName() + $".{entity.Extension}");
            CreateDirectoryFromFile(outputFile);

            using (Stream entityOutputStream = File.OpenWrite(outputFile)) {
                entityOutputStream.SetLength(0);
                entity.Write(entityOutputStream);
            }

            if (!context.m_saveAnimationEffects) return;
            if (entityInfo.m_modelGUID == 0) return;

            foreach (ulong effect in entityInfo.m_effects) {
                SaveEffect(flags, entityDir, context, effect);
            }

            foreach (ulong animation in entityInfo.m_animations) {
                SaveAnimationEffectReference(entityDir, context.m_info, animation, entityInfo.m_modelGUID);
            }
        }

        public static void SaveAnimationEffectReference(string path, FindLogic.Combo.ComboInfo info, ulong animation, ulong model) {
            FindLogic.Combo.AnimationAsset animationInfo = info.m_animations[animation];

            Effect.OverwatchAnimationEffectReference reference = new Effect.OverwatchAnimationEffectReference(info, animationInfo, model);

            string file = Path.Combine(path, Effect.OverwatchAnimationEffect.AnimationEffectDir,
                                       animationInfo.GetNameIndex() + $".{reference.Extension}");

            CreateDirectoryFromFile(file);
            using (Stream outputStream = File.OpenWrite(file)) {
                reference.Write(outputStream);
            }
        }

        private static void ConvertAnimation(Stream animStream, string path, bool convertAnims, FindLogic.Combo.AnimationAsset animationInfo, bool scaleAnims, string format) {
            var parsedAnimation = default(teAnimation);
            var priority = 16;
            var group = 16;
            try {
                parsedAnimation = new teAnimation(animStream, true);
                priority = parsedAnimation.Header.Priority;
                group = parsedAnimation.Header.Group;
            } catch (Exception) {
                Logger.Warn("Combo", $"Unable to parse animation {animationInfo.GetName()}");
            }

            string animationDirectory = Path.Combine(path, "Animations", priority.ToString(), group.ToString());

            if (convertAnims && parsedAnimation != null) {
                IExportFormat exportFormat = format switch {
                    "seanim" => new SEAnim(parsedAnimation, scaleAnims),
                    _ => new OverwatchAnimationClip(parsedAnimation)
                };

                string animOutput = Path.Combine(animationDirectory,
                                                 animationInfo.GetNameIndex() + "." + exportFormat.Extension);

                CreateDirectoryFromFile(animOutput);
                using Stream fileStream = new FileStream(animOutput, FileMode.Create);
                fileStream.SetLength(0);
                exportFormat.Write(fileStream);
            } else {
                animStream.Position = 0;
                string rawAnimOutput = Path.Combine(animationDirectory,
                                                    $"{animationInfo.GetNameIndex()}.{teResourceGUID.Type(animationInfo.m_GUID):X3}");

                CreateDirectoryFromFile(rawAnimOutput);
                using (Stream fileStream = new FileStream(rawAnimOutput, FileMode.Create)) {
                    animStream.CopyTo(fileStream);
                }
            }
        }

        public static string GetScratchRelative(ulong GUID, string cwd, string basePath) {
            if (!Program.Flags.Deduplicate) {
                return basePath;
            }

            if (ScratchDBInstance.HasRecord(GUID)) {
                return ScratchDBInstance[GUID]?.MakeRelative(cwd);
            }

            return basePath;
        }

        public static void SaveAnimation(ICLIFlags flags, string path, SaveContext context, ulong animation, ulong model) {
            bool convertAnims = false;
            bool scaleAnims = false;
            bool skip = false;
            bool skipEffect = false;
            string format = "seanim";
            if (flags is ExtractFlags extractFlags) {
                scaleAnims = extractFlags.ScaleAnims;
                convertAnims = !extractFlags.RawAnimations && !extractFlags.Raw;
                skip = extractFlags.SkipAnimations;
                skipEffect = extractFlags.SkipAnimationEffects;
                format = extractFlags.ConvertAnimationsType;
            }

            FindLogic.Combo.AnimationAsset animationInfo = context.m_info.m_animations[animation];
            if (!skip) {
                using Stream animStream = OpenFile(animation);
                if (animStream == null) return;

                ConvertAnimation(animStream, path, convertAnims, animationInfo, scaleAnims, format);
            }

            if (!context.m_saveAnimationEffects || skipEffect) return;
            FindLogic.Combo.EffectInfoCombo animationEffect;


            // just create a fake effect if it doesn't exist
            if (animationInfo.m_effect == 0) {
                animationEffect = new FindLogic.Combo.EffectInfoCombo(0) { Effect = new EffectParser.EffectInfo() };
                animationEffect.Effect.SetupEffect();
            } else if (context.m_info.m_effects.ContainsKey(animationInfo.m_effect)) {
                // wot, why
                animationEffect = context.m_info.m_effects[animationInfo.m_effect];
            } else if (context.m_info.m_animationEffects.ContainsKey(animationInfo.m_effect)) {
                animationEffect = context.m_info.m_animationEffects[animationInfo.m_effect];
            } else {
                return;
            }

            string animationEffectDir = Path.Combine(path, Effect.OverwatchAnimationEffect.AnimationEffectDir, animationInfo.GetNameIndex());

            Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLines = new Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>>();
            if (animationEffect.m_GUID != 0) {
                SaveEffectExtras(flags, animationEffectDir, context, animationEffect.Effect, out svceLines);
            }

            // todo: broken af with mythic
            return;

            Effect.OverwatchAnimationEffect output = new Effect.OverwatchAnimationEffect(context.m_info, animationEffect, svceLines, animationInfo, model);
            string animationEffectFile =
                Path.Combine(animationEffectDir, $"{animationInfo.GetNameIndex()}.{output.Extension}");

            CreateDirectoryFromFile(animationEffectFile);

            using (Stream fileStream = new FileStream(animationEffectFile, FileMode.Create)) {
                fileStream.SetLength(0);
                output.Write(fileStream);
            }
        }

        public static void SaveEffectExtras(
            ICLIFlags flags, string path, SaveContext info,
            EffectParser.EffectInfo effectInfo, out Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLines) {
            string soundDirectory = Path.Combine(path, "Sounds");
            svceLines = GetSVCELines(effectInfo, info.m_info);

            HashSet<ulong> done = new HashSet<ulong>();
            foreach (EffectParser.OSCEInfo osceInfo in effectInfo.OSCEs) {
                if (osceInfo.Sound == 0 || done.Contains(osceInfo.Sound)) continue;
                SaveSound(flags, soundDirectory, info, osceInfo.Sound);
                done.Add(osceInfo.Sound);
            }

            foreach (KeyValuePair<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLine in svceLines) {
                SaveVoiceStimuli(flags, soundDirectory, info, svceLine.Value, true);
            }
        }

        public static void SaveSound(ICLIFlags flags, string path, SaveContext context, ulong sound) {
            if (!context.m_info.m_sounds.ContainsKey(sound))
                return;

            FindLogic.Combo.SoundInfoNew soundInfo = context.m_info.m_sounds[sound];
            string soundDir = Path.Combine(path, soundInfo.GetName());
            CreateDirectorySafe(soundDir);

            HashSet<ulong> done = new HashSet<ulong>();
            if (soundInfo.SoundFiles != null) {
                foreach (KeyValuePair<uint, ulong> soundPair in soundInfo.SoundFiles) {
                    if (done.Contains(soundPair.Value)) continue;
                    SaveSoundFile(flags, soundDir, context, soundPair.Value, false);
                    done.Add(soundPair.Value);
                }
            }

            if (soundInfo.SoundStreams != null) {
                foreach (KeyValuePair<uint, ulong> soundStream in soundInfo.SoundStreams) {
                    if (done.Contains(soundStream.Value)) continue;
                    SaveSoundFile(flags, soundDir, context, soundStream.Value, false);
                    done.Add(soundStream.Value);
                }
            }
        }

        private static Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> GetSVCELines(EffectParser.EffectInfo effectInfo, FindLogic.Combo.ComboInfo info) {
            var output = new Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>>();
            if (effectInfo.SVCEs.Count == 0 || effectInfo.VoiceSet == 0) return output;

            if (!info.m_voiceSets.TryGetValue(effectInfo.VoiceSet, out var voiceSetInfo)) {
                // locale issues ig..
                return output;
            }

            var instances = voiceSetInfo.VoiceLineInstances;
            if (instances == null) return output;

            foreach (EffectParser.SVCEInfo svceInfo in effectInfo.SVCEs) {
                if (!instances.TryGetValue(svceInfo.VoiceStimulus, out var voiceLineInstance)) continue;
                output[svceInfo.VoiceStimulus] = voiceLineInstance;
            }

            return output;
        }

        public static void SaveEffect(ICLIFlags flags, string path, SaveContext context, ulong effect) {
            SaveEffect(flags, path, context, context.m_info.m_effects[effect]);
        }

        private static void SaveEffect(ICLIFlags flags, string path, SaveContext context, FindLogic.Combo.EffectInfoCombo effectInfo) {
            string effectDirectory = Path.Combine(path, "Effects", effectInfo.GetName());

            SaveEffectExtras(flags, effectDirectory, context, effectInfo.Effect, out Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLines);

            return; // this is still crashing mythics bitch. dont remove
            Effect.OverwatchEffect output = new Effect.OverwatchEffect(context.m_info, effectInfo, svceLines);
            string effectFile = Path.Combine(effectDirectory, $"{effectInfo.GetNameIndex()}.{output.Extension}");
            CreateDirectoryFromFile(effectFile);

            using (Stream effectOutputStream = File.OpenWrite(effectFile)) {
                effectOutputStream.SetLength(0);
                output.Write(effectOutputStream);
            }
        }

        public static void SaveModel(ICLIFlags flags, string path, SaveContext info, ulong modelGUID) {
            bool convertModels = true;
            bool doRefpose = false;
            bool allLods = false;

            if (flags is ExtractFlags extractFlags) {
                convertModels = !extractFlags.RawModels && !extractFlags.Raw;
                doRefpose = extractFlags.ExtractRefpose;
                allLods = extractFlags.AllLODs;
                if (extractFlags.SkipModels) return;
            }

            FindLogic.Combo.ModelAsset modelInfo = info.m_info.m_models[modelGUID];
            string modelDirectory = Path.Combine(path, "Models", modelInfo.GetName());

            //Console.Out.WriteLine($"save model. {modelGUID:X16}");

            if (convertModels) {
                string modelPath = Path.Combine(modelDirectory, $"{modelInfo.GetNameIndex()}.owmdl");

                using (Stream modelStream = OpenFile(modelInfo.m_GUID)) {
                    if (modelStream == null) return;
                    CreateDirectoryFromFile(modelPath);

                    teModelChunk_RenderMesh.LoadAssetFunc = static guid => OpenFile(guid);

                    teChunkedData chunkedData = new teChunkedData(modelStream);
                    var modelChunk = chunkedData.GetChunk<teModelChunk_Model>();

                    var hasStream1 = (modelChunk.Header.m_104 & 0x7000) != 0;
                    var hasStream2 = (modelChunk.Header.m_104 & 0x1C0) != 0;
                    var hasStream3 = (modelChunk.Header.m_104 & 7) != 0;

                    var streamingLods = new List<StreamingLodsInfo>();
                    StreamingLodsInfo LoadStreamingLods(ulong guid) {
                        using Stream streamingLodStream = OpenFile(guid);
                        teChunkedData streamingLodChunks = new teChunkedData(streamingLodStream);
                        return new StreamingLodsInfo(streamingLodChunks);
                    }

                    // do we need to load every streaming payload? i don't know (how many models have >1? might be 0)
                    // but, it is used for AllLODs support

                    if (hasStream1) {
                        streamingLods.Add(LoadStreamingLods(modelInfo.m_GUID & 0xFFFFFFF3FFFFFFFF | 0x200000000));
                    }
                    if (hasStream2) {
                        streamingLods.Add(LoadStreamingLods(modelInfo.m_GUID & 0xFFFFFFF5FFFFFFFF | 0x400000000));
                    }
                    if (hasStream3) {
                        streamingLods.Add(LoadStreamingLods(modelInfo.m_GUID & 0xFFFFFFF7FFFFFFFF | 0x600000000));
                    }

                    OverwatchModel model = new OverwatchModel(chunkedData, modelInfo.m_GUID, GetGUIDName(modelInfo.m_GUID), allLods, streamingLods);
                    if (modelInfo.m_modelLooks.Count > 0) {
                        FindLogic.Combo.ModelLookAsset modelLookInfo = info.m_info.m_modelLooks[modelInfo.m_modelLooks.First()];
                        model.ModelLookFileName = Path.Combine("ModelLooks",
                                                               modelLookInfo.GetNameIndex() + ".owmat");
                    }

                    using (Stream fileStream = File.OpenWrite(modelPath)) {
                        fileStream.SetLength(0);
                        model.Write(fileStream);
                    }

                    if (doRefpose) {
                        string refposePath = Path.Combine(modelDirectory, modelInfo.GetNameIndex() + ".smd");

                        using (Stream fileStream = File.OpenWrite(refposePath)) {
                            fileStream.SetLength(0);
                            var refpose = new RefPoseSkeleton(chunkedData);
                            refpose.Write(fileStream);
                        }
                    }
                }
            } else {
                using (Stream modelStream = OpenFile(modelInfo.m_GUID)) {
                    WriteFile(modelStream, Path.Combine(modelDirectory, modelInfo.GetNameIndex() + ".00C"));
                }
            }

            foreach (ulong modelModelLook in modelInfo.m_modelLooks) {
                SaveModelLook(flags, modelDirectory, info, modelModelLook);
            }

            foreach (ulong looseMaterial in modelInfo.m_looseMaterials) {
                SaveMaterial(flags, modelDirectory, info, looseMaterial);
            }

            foreach (ulong modelAnimation in modelInfo.m_animations) {
                SaveAnimation(flags, modelDirectory, info, modelAnimation, modelGUID);
            }
        }

        public static void SaveOWMaterialModelLookFile(string path, FindLogic.Combo.ModelLookAsset modelLookInfo, FindLogic.Combo.ComboInfo info) {
            Model.OverwatchModelLook modelLook = new Model.OverwatchModelLook(info, modelLookInfo);

            string modelLookPath =
                Path.Combine(path, "ModelLooks", $"{modelLookInfo.GetNameIndex()}.{modelLook.Extension}");

            CreateDirectoryFromFile(modelLookPath);
            using (Stream modelLookOutputStream = File.OpenWrite(modelLookPath)) {
                modelLookOutputStream.SetLength(0);
                modelLook.Write(modelLookOutputStream);
            }
        }

        public static void SaveModelLook(
            ICLIFlags flags, string path, SaveContext info,
            ulong modelLook) {
            FindLogic.Combo.ModelLookAsset modelLookInfo = info.m_info.m_modelLooks[modelLook];

            SaveOWMaterialModelLookFile(path, modelLookInfo, info.m_info);

            if (modelLookInfo.m_materials == null) return;
            foreach (var modelLookMaterial in modelLookInfo.m_materials) {
                SaveMaterial(flags, path, info, modelLookMaterial.m_guid);
            }
        }

        public static void SaveOWMaterialFile(string path, FindLogic.Combo.MaterialAsset materialInfo, FindLogic.Combo.ComboInfo info, ICLIFlags flags) {
            string format = "dds";
            if (flags is ExtractFlags extractFlags && !extractFlags.RawTextures && !extractFlags.Raw) {
                format = extractFlags.ConvertTexturesType.ToLower();
            }

            string materialDir = Path.Combine(path, "Materials");
            Model.OverwatchMaterial material = new Model.OverwatchMaterial(info, materialInfo, format, materialDir);
            string materialPath =
                Path.Combine(materialDir, $"{materialInfo.GetNameIndex()}.{material.Extension}");

            CreateDirectoryFromFile(materialPath);
            using (Stream materialOutputStream = File.OpenWrite(materialPath)) {
                materialOutputStream.SetLength(0);
                material.Write(materialOutputStream);
            }
        }

        private static void SaveVoiceSetInternal(
            ICLIFlags flags, string path, SaveContext context,
            ulong voiceSet) {
            string thisPath = Path.Combine(path, GetFileName(voiceSet));

            FindLogic.Combo.VoiceSetAsset voiceSetInfo = context.m_info.m_voiceSets[voiceSet];
            if (voiceSetInfo.VoiceLineInstances == null) return;
            foreach (KeyValuePair<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> stimuliSet in voiceSetInfo.VoiceLineInstances) {
                SaveVoiceStimuliInternal(flags, thisPath, context, stimuliSet.Value, true);
            }
        }

        public static void SaveMaterial(ICLIFlags flags, string path, SaveContext info, ulong material) {
            FindLogic.Combo.MaterialAsset materialInfo = info.m_info.m_materials[material];
            FindLogic.Combo.MaterialDataAsset materialDataInfo = info.m_info.m_materialData[materialInfo.m_materialDataGUID];

            string textureDirectory = Path.Combine(path, "Textures");

            if (materialDataInfo.m_textureMap != null) {
                foreach (var (_, guid) in materialDataInfo.m_textureMap) {
                    SaveTexture(flags, textureDirectory, info, guid);
                }
            }

            SaveOWMaterialFile(path, materialInfo, info.m_info, flags);

            if (Program.Flags.ExtractShaders && materialInfo.m_shaders != null) {
                SaveShader(path, materialInfo, info.m_info);
            }
        }

        private static void SaveShader(string path, FindLogic.Combo.MaterialAsset materialInfo, FindLogic.Combo.ComboInfo info) {
            string shaderDirectory = Path.Combine(path, "Shaders", $"{materialInfo.m_materialDataGUID:X16}");
            var fn = teResourceGUID.LongKey(materialInfo.m_shaderGroupGUID).ToString("X12");
            WriteFile(materialInfo.m_shaderGroupGUID, shaderDirectory, $"{fn}.shadergroup");
            WriteFile(materialInfo.m_shaderSourceGUID, shaderDirectory, $"{fn}.shadersource"); // note to others: this isn't source code, its another shader group
            foreach (var (instance, code, byteCode) in materialInfo.m_shaders) {
                //var instancePath = Path.Combine(shaderDirectory, $"{instance:X12}");
                var codefn = $"{fn}{Path.DirectorySeparatorChar}{teResourceGUID.LongKey(code):X12}";
                WriteFile(instance, shaderDirectory, $"{codefn}.fxi");
                WriteFile(code, shaderDirectory, $"{codefn}.owfx");
                WriteFile(byteCode, Path.Combine(shaderDirectory, $"{codefn}.fxc"));
            }
        }


        // helpers (NOT FOR INTERNAL USE)
        public static void SaveAllVoiceSets(ICLIFlags flags, string path, SaveContext context) {
            foreach (KeyValuePair<ulong, FindLogic.Combo.VoiceSetAsset> voiceSet in context.m_info.m_voiceSets) {
                SaveVoiceSet(flags, path, context, voiceSet.Value);
            }
        }

        public static void SaveLooseTextures(ICLIFlags flags, string path, SaveContext context, SaveTextureOptions options = null) {
            foreach (FindLogic.Combo.TextureAsset textureInfo in context.m_info.m_textures.Values) {
                if (!textureInfo.m_loose) continue;
                SaveTexture(flags, path, context, textureInfo.m_GUID, options);
            }
        }

        public static void SaveAllStrings(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info) {
            foreach (FindLogic.Combo.DisplayTextAsset stringInfo in info.m_displayText.Values) {
                if (stringInfo.m_text == null) continue;
                string file = Path.Combine(path, stringInfo.GetName()) + ".txt";
                CreateDirectoryFromFile(file);
                using (StreamWriter writer = new StreamWriter(file)) {
                    writer.Write(stringInfo.m_text);
                }
            }
        }

        public static void SaveAllSoundFiles(ICLIFlags flags, string path, SaveContext context) {
            foreach (FindLogic.Combo.SoundFileAsset soundInfo in context.m_info.m_soundFiles.Values) {
                SaveSoundFile(flags, path, context, soundInfo.m_GUID, false);
            }
        }

        public static void SaveAllVoiceSoundFiles(ICLIFlags flags, string path, SaveContext context) {
            foreach (FindLogic.Combo.SoundFileAsset soundInfo in context.m_info.m_voiceSoundFiles.Values) {
                SaveSoundFile(flags, path, context, soundInfo.m_GUID, true);
            }
        }

        public static void SaveAllMaterials(ICLIFlags flags, string path, SaveContext info) {
            foreach (ulong material in info.m_info.m_materials.Keys) {
                SaveMaterial(flags, path, info, material);
            }
        }

        public static void SaveAllModelLooks(ICLIFlags flags, string path, SaveContext context) {
            foreach (ulong material in context.m_info.m_modelLooks.Keys) {
                SaveModelLook(flags, path, context, material);
            }
        }

#warning TODO: This method does not support animation effects
        public static void SaveAllAnimations(ICLIFlags flags, string path, SaveContext context) {
            // TODO: THREADING ISSUE HERE

            bool beforeSaveAnimEffects = context.m_saveAnimationEffects;
            context.m_saveAnimationEffects = false;

            foreach (ulong material in context.m_info.m_animations.Keys) {
                SaveAnimation(flags, path, context, material, 0);
            }

            context.m_saveAnimationEffects = beforeSaveAnimEffects;
        }

        public static void SaveVoiceSet(
            ICLIFlags flags, string path, SaveContext context,
            ulong voiceSet) {
            SaveVoiceSetInternal(flags, path, context, voiceSet);
        }

        public static void SaveVoiceSet(
            ICLIFlags flags, string path, SaveContext context,
            FindLogic.Combo.VoiceSetAsset voiceSetInfo) {
            SaveVoiceSetInternal(flags, path, context, voiceSetInfo.m_GUID);
        }

        public static void SaveVoiceStimuli(
            ICLIFlags flags, string path, SaveContext context,
            IEnumerable<FindLogic.Combo.VoiceLineInstanceInfo> voiceLineInstances, bool split) {
            SaveVoiceStimuliInternal(flags, path, context, voiceLineInstances, split);
        }

        // internal stuff for helpers (for internal use)
        private static void SaveVoiceStimuliInternal(
            ICLIFlags flags, string path, SaveContext context,
            IEnumerable<FindLogic.Combo.VoiceLineInstanceInfo> voiceLineInstances, bool split) {
            foreach (FindLogic.Combo.VoiceLineInstanceInfo voiceLineInstance in voiceLineInstances) {
                string thisPath = path;

                if (flags is ExtractFlags extractFlags) {
                    if (extractFlags.FlattenDirectory) split = false;
                }

                if (split) {
                    thisPath = Path.Combine(path, GetFileName(voiceLineInstance.VoiceStimulus));
                }

                SaveVoiceLineInstance(flags, thisPath, voiceLineInstance);
            }
        }

        private static void ProcessIconTexture(teTexture texture, string filePath, string convertType) {
            var converted = new TexDecoder(texture, false);

            using Image<Bgra32> alphaImage = converted.GetFrame(0);
            using Image<Bgra32> colorImage = converted.GetFrame(1);

            alphaImage.ProcessPixelRows(colorImage, (source, target) => {
                for (var y = 0; y < texture.Header.Height; ++y) {
                    var sourceRow = source.GetRowSpan(y);
                    var targetRow = target.GetRowSpan(y);
                    for (var x = 0; x < texture.Header.Width; ++x) {
                        var pixel = targetRow[x];
                        pixel.R = (byte) (Math.Pow(pixel.R / 255f, 1.1f) * 0xFF);
                        pixel.G = (byte) (Math.Pow(pixel.G / 255f, 1.1f) * 0xFF);
                        pixel.B = (byte) (Math.Pow(pixel.B / 255f, 1.1f) * 0xFF);

                        Rgba32 rgba32 = default;
                        sourceRow[x].ToRgba32(ref rgba32);

                        pixel.A = (byte) ((1 - ColorSpaceConverter.ToHsl(rgba32).L) * 0xFF);
                        targetRow[x] = pixel;
                    }
                }
            });

            SaveTexImageSharp(colorImage, filePath, convertType.ToLowerInvariant());
        }

        public class SaveTextureOptions {
            /// <summary>
            /// Overrides the filename.
            /// </summary>
            public string FileNameOverride { get; set; }

            /// <summary>
            /// Force split multi surface textures into multiple files.
            /// </summary>
            public bool? Split { get; set; }

            /// <summary>
            /// Forces texture to be processed as if it were an icon with multiple surfaces.
            /// </summary>
            public bool? ProcessIcon { get; set; }

            /// <summary>
            /// Overrides the file type the texture is saved as.
            /// </summary>
            public string FileTypeOverride { get; set; }
        }

        public static void SaveTexture(ICLIFlags flags, string path, SaveContext info, ulong textureGUID, SaveTextureOptions options = null) {
            options ??= new SaveTextureOptions();
            FindLogic.Combo.TextureAsset textureInfo = info.m_info.m_textures[textureGUID];

            var split = options?.Split ?? textureInfo.m_split ?? false;
            var processIcon = options?.ProcessIcon ?? textureInfo.m_processIcon ?? false;
            var fileType = options?.FileTypeOverride ?? textureInfo.m_fileType ?? null;

            bool convertTextures = true;
            string convertType = fileType;
            string multiSurfaceConvertType = "tif";

            var createMultiSurfaceSheet = split;
            var splitMultiSurface = false;
            var maxMips = 1;
            var useTextureDecoder = !OperatingSystem.IsWindows();
            var grayscale = false;

            if (flags is ExtractFlags extractFlags) {
                if (extractFlags.SkipTextures) return;
                createMultiSurfaceSheet = extractFlags.SheetMultiSurface;
                convertTextures = !extractFlags.RawTextures && !extractFlags.Raw;
                splitMultiSurface = (split || !extractFlags.CombineMultiSurface) && convertTextures && !createMultiSurfaceSheet;
                convertType = fileType ?? extractFlags.ConvertTexturesType.ToLowerInvariant();
                useTextureDecoder = extractFlags.UseTextureDecoder || useTextureDecoder;
                grayscale = extractFlags.Grayscale;

                if (extractFlags.ForceDDSMultiSurface) {
                    multiSurfaceConvertType = "dds";
                }

                if (convertType == "dds" && extractFlags.SaveMips) {
                    maxMips = 0xF;
                }
            }

            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                path += Path.DirectorySeparatorChar;

            string filePath = Path.Combine(path, options.FileNameOverride ?? $"{textureInfo.GetNameIndex()}");

            if (Program.Flags is { Deduplicate: true }) {
                if (ScratchDBInstance.HasRecord(textureGUID)) {
                    return;
                }

                ScratchDBInstance[textureGUID] = new ScratchDB.ScratchPath($"{filePath}.{convertType}", true);
            }

            CreateDirectoryFromFile(path);

            if (!convertTextures) {
                teTexture texture;
                using (Stream textureStream = OpenFile(textureGUID)) {
                    texture = new teTexture(textureStream, true);
                    textureStream.Position = 0;
                    WriteFile(textureStream, $"{filePath}.004");
                }

                if (!texture.PayloadRequired) return;
                for (uint i = 1; i < texture.Payloads.Length; i++) {
                    using Stream texturePayloadStream = OpenFile(texture.GetPayloadGUID(textureGUID, i));
                    WriteFile(texturePayloadStream, $"{filePath}_{i}.04D");
                }
            } else {
                teTexture texture;
                using (Stream textureStream = OpenFile(textureGUID)) {
                    if (textureStream == null) {
                        return;
                    }

                    texture = new teTexture(textureStream);
                }

                if (texture.Header.IsCubemap) return; // todo zingy - broken

                if (texture.PayloadRequired && texture.Payloads.Length > 1) {
                    for (uint i = (uint)texture.Payloads.Length-1u; i >= 1; i++) {
                        using (var payloadStream = OpenFile(texture.GetPayloadGUID(textureGUID, i)))
                            texture.LoadPayload(payloadStream, i);

                        if (maxMips == 1) break;
                    }
                }

                if ((texture.Header.IsCubemap || texture.Header.IsArray) && !processIcon && !useTextureDecoder) {
                    if (createMultiSurfaceSheet) {
                        Logger.Debug("Combo", $"Saving {Path.GetFileName(filePath)} as a sheet with TextureDecoder because it has more than one surface");
                        useTextureDecoder = true;
                    } else if (!splitMultiSurface && convertType != multiSurfaceConvertType) {
                        Logger.Debug("Combo", $"Saving {Path.GetFileName(filePath)} as {multiSurfaceConvertType} because it has more than one surface");
                        convertType = multiSurfaceConvertType;
                    }
                }

                WICCodecs? imageFormat = convertType[0] == 't' ? WICCodecs.TIFF : WICCodecs.PNG;

                try {
                    if (convertType == "dds") {
                        using Stream convertedStream = texture.SaveToDDS(maxMips == 1 ? 1 : texture.Header.MipCount);
                        WriteFile(convertedStream, $"{filePath}.dds");
                        return;
                    }

                    processIcon = processIcon && texture.Header.Surfaces == 2;

                    if (useTextureDecoder) {
                        try {
                            ConvertTexture(texture, splitMultiSurface, createMultiSurfaceSheet, grayscale, filePath, convertType);
                            return;
                        } catch(Exception e) {
                            Logger.Warn("Combo", $"Failed to convert {Path.GetFileName(filePath)} using the texture decoder: {e.Message}");
                        }
                    }

                    // DirectXTex is only available on Windows
                    if (!OperatingSystem.IsWindows()) {
                        return;
                    }

                    ConvertDDS(texture, maxMips, processIcon, filePath, convertType, splitMultiSurface, imageFormat.Value);
                } catch (Exception e) {
                    Logger.Error("Combo", $"Unable to save {textureGUID:X16} {Path.GetFileName(filePath)} {e}");
                }
            }
        }

        private static void ConvertTexture(teTexture texture, bool splitMultiSurface, bool createMultiSurfaceSheet, bool grayscale, string filePath, string convertType) {
            var tex = new TexDecoder(texture, grayscale);

            if (splitMultiSurface) {
                for (var surfaceNr = 0; surfaceNr < tex.Surfaces; ++surfaceNr) {
                    using var surface = tex.GetFrame(surfaceNr);
                    var surfacePath = surfaceNr == 0 ? filePath : $"{filePath}_{surfaceNr}";
                    SaveTexImageSharp(surface, surfacePath, convertType);
                }
            } else if (createMultiSurfaceSheet) {
                using var sheetImg = tex.GetSheet();
                SaveTexImageSharp(sheetImg, filePath, convertType);
            } else {
                using var img = tex.GetFrames();
                SaveTexImageSharp(img, filePath, convertType);
            }
        }

        private static void SaveTexImageSharp(Image img, string path, string convertType) {
            var finalPath = $"{path}.{convertType}";
            CreateDirectoryFromFile(finalPath);
            if (convertType == "tif") {
                img.SaveAsTiff(finalPath);
            } else {
                img.SaveAsPng(finalPath);
            }
        }

        private static void ConvertDDS(teTexture texture, int maxMips, bool processIcon, string filePath, string convertType, bool splitMultiSurface, WICCodecs imageFormat) {
            using Stream convertedStream = texture.SaveToDDS(maxMips == 1 ? 1 : texture.Header.MipCount);
            if (processIcon) {
                try {
                    ProcessIconTexture(texture, filePath, convertType);
                    return;
                } catch {
                    Logger.Debug("Combo", $"Failed to process {Path.GetFileName(filePath)} as an icon, saving as normal");
                }
            }

            using var dds = new DDSConverter(convertedStream, DXGI_FORMAT.UNKNOWN, processIcon);
            var surfaceCount = splitMultiSurface ? texture.Header.Surfaces : 1;
            for (var surfaceNr = 0; surfaceNr < surfaceCount; ++surfaceNr) {
                var surfacePath = surfaceNr == 0 ? filePath : $"{filePath}_{surfaceNr}";
                try {
                    using var surface = dds.GetFrame(imageFormat, surfaceNr, splitMultiSurface ? 1 : dds.Info.ArraySize);
                    WriteFile(surface, $"{surfacePath}.{convertType}");
                } catch {
                    convertedStream.Position = 0;
                    WriteFile(convertedStream, $"{surfacePath}.dds");
                    Logger.Error("Combo", $"Unable to save {Path.GetFileName(filePath)} (surface {surfaceNr + 1}) as {convertType} because DirectXTex failed. {(DXGI_FORMAT)texture.Header.Format} {texture.Header.Format} {texture.Header.PayloadCount} {texture.Header.MipCount} {texture.Header.Surfaces}");
                    return;
                }
            }
        }

        private static void ConvertSoundFile(Stream stream, FindLogic.Combo.ComboAsset soundFileInfo, string directory, string name = null, bool useVgmStream = false, bool forceStereo = true) {
            string outputFile = Path.Combine(directory, $"{name ?? soundFileInfo.GetName()}.ogg");
            CreateDirectoryFromFile(outputFile);
            using Stream outputStream = File.OpenWrite(outputFile);
            outputStream.SetLength(0);
            try {
                ConvertSoundFile(stream, outputStream, outputFile, useVgmStream, forceStereo);
            } catch (Exception e) {
                Logger.Warn("Combo", $"Error converting sound: {e.Message}");
                using var errorStream = File.OpenWrite(Path.ChangeExtension(outputFile, ".wem"));
                errorStream.SetLength(0);
                stream.CopyTo(errorStream);
            }
        }

        public static void ConvertSoundFile(Stream stream, Stream outputStream, string outputFile, bool useVgmStream, bool forceStereo) {
            try {
                if (useVgmStream) {
                    ConvertSoundFileVgmStream(stream, outputStream, forceStereo);
                } else {
                    ConvertSoundFileWw2Ogg(stream, outputStream);
                }
            } catch (Exception e) {
                Logger.Warn("Combo", $"Error converting sound: {e.Message}, trying vgmstream directly");
                if (!VGMStreamSanity()) {
                    throw;
                }
                var tempFile = Path.Combine(Path.GetTempPath(), $"vgmstream{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.wem");
                stream.Position = 0;
                try {
                    using (Stream tempStream = File.OpenWrite(tempFile)) {
                        stream.CopyTo(tempStream);
                    }

                    ConvertSoundFileVgmStreamProcess(tempFile, outputFile);
                } finally {
                    if (File.Exists(tempFile)) {
                        File.Delete(tempFile);
                    }
                }
            }
        }

        public static string VgmStreamPathWin = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Third Party",
                                                                              "vgmstream-win", "vgmstream-cli.exe"));
        public static string VgmStreamPathLx = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Third Party",
                                                                              "vgmstream-cli"));

        public static string VgmStreamPath => OperatingSystem.IsLinux() ? VgmStreamPathLx : VgmStreamPathWin;

        public static void ConvertSoundFileVgmStreamProcess(string input, string output) {
            try {
                var proc = new ProcessStartInfo {
                    FileName = VgmStreamPath,
                    Arguments = $"\"{input}\" -l 0 -f 0 -d 0 -L -o \"{output}\"",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };

                using var process = Process.Start(proc);
                process!.WaitForExit();
                if (process.ExitCode != 0) {
                    var error = process.StandardError.ReadToEnd();
                    Logger.Error("Combo", $"vgmstream failed with exit code {process.ExitCode} {error.Trim()}");
                    File.Copy(input, Path.ChangeExtension(output, ".wem"), true);
                }
            } catch (Exception e) {
                Logger.Error("Combo", $"Error converting sound using vgmstream: {e}");
            }
        }

        public static void ConvertSoundFileVgmStream(Stream stream, Stream outputStream, bool forceStereo) {
            using WwiseRIFFOpus opus = new WwiseRIFFOpus(stream, forceStereo);
            opus.ConvertToOgg(outputStream);
        }

        public static void ConvertSoundFileWw2Ogg(Stream stream, Stream outputStream) {
            using var wem = new WwiseRIFFVorbis(stream, Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Third Party", "packed_codebooks_aoTuV_603.bin")));

            var ogg = new MemoryStream();
            wem.ConvertToOgg(ogg);
            ogg.Position = 0;

            using var vorbis = Revorb.Jiggle(ogg);
            vorbis.Position = 0;
            vorbis.CopyTo(outputStream);
        }

        private static void SaveSoundFileTask(ICLIFlags flags, string directory, FindLogic.Combo.SoundFileAsset soundFileInfo, string name = null) {
            bool convertWem = true;
            bool useVgmStream = false;
            bool forceStereo = true;
            if (flags is ExtractFlags extractFlags) {
                convertWem = !extractFlags.RawSound && !extractFlags.Raw;
                if (extractFlags.SkipSound) return;
                forceStereo = !extractFlags.KeepSoundChannels;
            }

            if (Program.Flags is { Deduplicate: true }) {
                if (ScratchDBInstance.HasRecord(soundFileInfo.m_GUID)) {
                    return;
                }

                ScratchDBInstance[soundFileInfo.m_GUID] = new ScratchDB.ScratchPath(Path.Combine(directory, $"{name ?? soundFileInfo.GetName()}.ogg"), true);
            }

            using (Stream soundStream = OpenFile(soundFileInfo.m_GUID)) {
                if (soundStream == null) return;

                if (convertWem) {
                    var type = WEMType(soundStream);
                    if (type == 0) {
                        convertWem = false;
                    } else if (type == 2) {
                        useVgmStream = true;
                    }
                    soundStream.Position = 0;
                }

                if (!convertWem) {
                    WriteFile(soundStream, Path.Combine(directory, $"{name ?? soundFileInfo.GetName()}.wem"));
                } else {
                    ConvertSoundFile(soundStream, soundFileInfo, directory, name, useVgmStream, forceStereo);
                }
            }
        }

        public static int WEMType(Stream stream) {
            try {
                var reader = new BinaryReader(stream, Encoding.Default, true);
                stream.Position = 12;
                while (true) {
                    char[] chunkType = reader.ReadChars(4);
                    string chunkTypeString = new string(chunkType);
                    int chunkSize = reader.ReadInt32();

                    if (chunkTypeString == "fmt ") {
                        var codec = reader.ReadUInt16();
                        if (codec == 0xFFFF) {
                            return 1; // vorbis
                        } else if(codec == 0x3041) {
                            return 2; // opus
                        } else {
                            return 0;
                        }
                    }

                    reader.BaseStream.Position += chunkSize;
                }
            } catch {
                return 0;
            } finally {
                stream.Position = 0;
            }
        }

        public static void SaveSoundFile(ICLIFlags flags, string directory, SaveContext context, ulong soundFile, bool voice, string name = null) {
            if (soundFile == 0) return;

            FindLogic.Combo.SoundFileAsset soundFileInfo = voice ? context.m_info.m_voiceSoundFiles[soundFile] : context.m_info.m_soundFiles[soundFile];
            SaveSoundFileTask(flags, directory, soundFileInfo, name);
        }

        public static void SaveSoundFile(ICLIFlags flags, string directory, ulong soundFile, string name = null) {
            if (soundFile == 0) return;

            SaveSoundFileTask(flags, directory, new FindLogic.Combo.SoundFileAsset(soundFile), name);
        }

        private static bool WarnedAboutVGMStream;
        public static bool HasVGMStream { get; private set; }

        public static bool VGMStreamSanity() {
            if (WarnedAboutVGMStream) {
                return HasVGMStream;
            }

            WarnedAboutVGMStream = true;

            if (!File.Exists(VgmStreamPath)) {
                HasVGMStream = false;
                Logger.Warn("Combo", "vgmstream not found, downloading latest...");
                try {
                    if (OperatingSystem.IsLinux()) {
                        // modcheck TarArchive dotnet?
                        Logger.Warn("Combo", $"Failed to download vgmstream. Please download vgmstream from https://dl.vgmstream.org/ and extract it to the Third Party folder ({VgmStreamPath})");
                    } else {
                        using var web = new HttpClient();
                        var zip = web.GetStreamAsync("https://github.com/vgmstream/vgmstream-releases/releases/download/nightly/vgmstream-win64.zip").Result;
                        using var archive = new ZipArchive(zip);
                        if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Third Party", "vgmstream-win"))) {
                            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Third Party", "vgmstream-win"));
                        }

                        foreach (var e in archive.Entries) {
                            var targetPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Third Party", "vgmstream-win", e.FullName));
                            using var target = File.OpenWrite(targetPath);
                            target.SetLength(0);
                            e.Open().CopyTo(target);
                        }

                        HasVGMStream = true;
                    }
                } catch {
                    Logger.Warn("Combo", $"Failed to download vgmstream. Please download vgmstream from https://dl.vgmstream.org/ and extract it to the Third Party folder ({VgmStreamPath})");
                }
            } else {
                HasVGMStream = true;
            }

            return HasVGMStream;
        }
    }
}
