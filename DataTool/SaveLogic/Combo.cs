using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataTool.ConvertLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using DataTool.ToolLogic.Extract;
using DataTool.ToolLogic.List;
using DirectXTexNet;
using TankLib;
using TankLib.Chunks;
using TankLib.ExportFormats;
using static DataTool.Helper.IO;
using Logger = TankLib.Helpers.Logger;

namespace DataTool.SaveLogic {
    public static class Combo {
        public static ScratchDB ScratchDBInstance = new ScratchDB();

        private static SemaphoreSlim s_texurePrepareSemaphore = new SemaphoreSlim(100, 100); // don't load too many texures into memory

        public class SaveContext {
            public FindLogic.Combo.ComboInfo m_info;
            public bool m_saveAnimationEffects = true;

            private readonly ConcurrentBag<Task> m_pendingTasks = new ConcurrentBag<Task>();

            public SaveContext(FindLogic.Combo.ComboInfo info) {
                m_info = info;
            }

            private int CountPending() {
                int count = 0;
                foreach (Task task in m_pendingTasks) {
                    if (task.IsCompleted) continue;
                    count++;
                }

                return count;
            }

            private void UpdateTitle() {
                Console.Title = $"Saving... {CountPending()} tasks pending";
            }

            public void Wait() {
                while (true) {
                    bool done = true;
                    foreach (Task task in m_pendingTasks) {
                        if (task.IsCompleted) continue;
                        UpdateTitle();
                        task.Wait(100);
                        done = false;
                    }

                    if (done) break;
                }

                Console.Title = "done save";
            }

            public void AddTask(Action action) {
                if (Program.Flags?.DisableAsyncSave == true) {
                    action();
                } else {
                    m_pendingTasks.Add(Task.Run(() => {
                        try {
                            action();
                        } catch (Exception e) {
                            Logger.Error("Combo", $"Async exception: {e}");
                        }
                    }));
                }
            }

            public void AddTask(Func<Task> action) {
                if (Program.Flags?.DisableAsyncSave == true) {
                    action().Wait();
                } else {
                    m_pendingTasks.Add(Task.Run(async () => {
                        try {
                            await action();
                        } catch (Exception e) {
                            Logger.Error("Combo", $"Async exception: {e}");
                        }
                    }));
                }
            }
        }

        public static void Save(ICLIFlags flags, string path, SaveContext context) {
            foreach (FindLogic.Combo.ModelAsset model in context.m_info.m_models.Values) {
                context.AddTask(() => SaveModel(flags, path, context, model.m_GUID));
            }

            foreach (FindLogic.Combo.EntityAsset entity in context.m_info.m_entities.Values) {
                context.AddTask(() => SaveEntity(flags, path, context, entity.m_GUID));
            }

            foreach (FindLogic.Combo.EffectInfoCombo effectInfo in context.m_info.m_effects.Values) {
                context.AddTask(() => SaveEffect(flags, path, context, effectInfo.m_GUID));
            }
        }

        public static void SaveVoiceStimulus(ICLIFlags flags, string path, SaveContext context, FindLogic.Combo.VoiceLineInstanceInfo voiceLineInstanceInfo) {
            var saveSubtitles = true;

            if (flags is ExtractFlags extractFlags) {
                saveSubtitles = extractFlags.SubtitlesWithSounds;
            }

            var realPath = path;
            var soundSet = new HashSet<ulong>(voiceLineInstanceInfo.SoundFiles.Where(x => x != 0));
            string overrideName = null;

            if (saveSubtitles) {
                IEnumerable<string> subtitle = new HashSet<string>();

                if (context.m_info.m_subtitles.TryGetValue(voiceLineInstanceInfo.Subtitle, out var subtitleInfo)) {
                    subtitle = subtitle.Concat(subtitleInfo.m_text);
                }

                if (context.m_info.m_subtitles.TryGetValue(voiceLineInstanceInfo.SubtitleRuntime, out var subtitleRuntimeInfo)) {
                    subtitle = subtitle.Concat(subtitleRuntimeInfo.m_text);
                }

                var subtitleSet = new HashSet<string>(subtitle);

                if (subtitleSet.Any()) {
                    if (soundSet.Count > 1) {
                        realPath = Path.Combine(realPath, GetValidFilename(subtitleSet.First().Trim().TrimEnd('.')));
                        WriteFile(string.Join("\n", subtitleSet), Path.Combine(realPath, $"{teResourceGUID.LongKey(voiceLineInstanceInfo.Subtitle):X8}-{teResourceGUID.LongKey(voiceLineInstanceInfo.SubtitleRuntime):X8}-subtitles.txt"));
                    } else if (soundSet.Count == 1) {
                        try {
                            overrideName = GetValidFilename($"{teResourceGUID.AsString(soundSet.First())}-{subtitleSet.First().TrimEnd('.')}");
                            if (overrideName.Length > 128) overrideName = overrideName.Substring(0, 100);
                            WriteFile(string.Join("\n", subtitleSet), Path.Combine(realPath, $"{overrideName}.txt"));
                        } catch {
                            overrideName = teResourceGUID.AsString(soundSet.First());
                            WriteFile(string.Join("\n", subtitleSet), Path.Combine(realPath, $"{overrideName}.txt"));
                        }
                    }
                }
            }

            foreach (ulong soundFile in soundSet) {
                SaveSoundFile(flags, realPath, context, soundFile, true, overrideName);
            }
        }

        public static void SaveEntity(ICLIFlags flags, string path, SaveContext context, ulong entityGuid) {
            FindLogic.Combo.EntityAsset entityInfo = context.m_info.m_entities[entityGuid];

            Entity.OverwatchEntity entity = new Entity.OverwatchEntity(entityInfo, context.m_info);

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

        private static void ConvertAnimation(Stream animStream, string path, bool convertAnims, FindLogic.Combo.AnimationAsset animationInfo, bool scaleAnims) {
            var parsedAnimation = default(teAnimation);
            var priority = 100;
            try {
                parsedAnimation = new teAnimation(animStream, true);
                priority = parsedAnimation.Header.Priority;
            } catch (Exception) {
                Logger.Error("Combo", $"Unable to parse animation {animationInfo.GetName()}");
            }

            string animationDirectory =
                Path.Combine(path, "Animations", priority.ToString());

            if (convertAnims && parsedAnimation != null) {
                SEAnim seAnim = new SEAnim(parsedAnimation, scaleAnims);
                string animOutput = Path.Combine(animationDirectory,
                                                 animationInfo.GetNameIndex() + "." + seAnim.Extension);
                CreateDirectoryFromFile(animOutput);
                using (Stream fileStream = new FileStream(animOutput, FileMode.Create)) {
                    seAnim.Write(fileStream);
                }
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

        private static void SaveAnimationTask(ICLIFlags flags, string path, SaveContext context, ulong animation, ulong model) {
            bool convertAnims = false;
            bool scaleAnims = false;
            if (flags is ExtractFlags extractFlags) {
                scaleAnims = extractFlags.ScaleAnims;
                convertAnims = !extractFlags.RawAnimations && !extractFlags.Raw;
                if (extractFlags.SkipAnimations) return;
            }

            FindLogic.Combo.AnimationAsset animationInfo = context.m_info.m_animations[animation];
            using (Stream animStream = OpenFile(animation)) {
                if (animStream == null) return;
                ConvertAnimation(animStream, path, convertAnims, animationInfo, scaleAnims);
            }

            if (!context.m_saveAnimationEffects) return;
            FindLogic.Combo.EffectInfoCombo animationEffect;


            // just create a fake effect if it doesn't exist
            if (animationInfo.m_effect == 0) {
                animationEffect = new FindLogic.Combo.EffectInfoCombo(0) {Effect = new EffectParser.EffectInfo()};
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

            Effect.OverwatchAnimationEffect output = new Effect.OverwatchAnimationEffect(context.m_info, animationEffect, svceLines, animationInfo, model);
            string animationEffectFile =
                Path.Combine(animationEffectDir, $"{animationInfo.GetNameIndex()}.{output.Extension}");
            CreateDirectoryFromFile(animationEffectFile);

            using (Stream fileStream = new FileStream(animationEffectFile, FileMode.Create)) {
                fileStream.SetLength(0);
                output.Write(fileStream);
            }
        }

        public static void SaveAnimation(ICLIFlags flags, string path, SaveContext context, ulong animation, ulong model) {
            context.AddTask(() => SaveAnimationTask(flags, path, context, animation, model));
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
            Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> output = new Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>>();
            if (effectInfo.SVCEs.Count == 0 || effectInfo.VoiceSet == 0) return output;

            foreach (EffectParser.SVCEInfo svceInfo in effectInfo.SVCEs) {
                Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> instances = info.m_voiceSets[effectInfo.VoiceSet].VoiceLineInstances;
                if (instances?.ContainsKey(svceInfo.VoiceStimulus) == true) {
                    output[svceInfo.VoiceStimulus] = instances[svceInfo.VoiceStimulus];
                }
            }

            return output;
        }

        public static void SaveEffect(ICLIFlags flags, string path, SaveContext context, ulong effect) {
            FindLogic.Combo.EffectInfoCombo effectInfo = context.m_info.m_effects[effect];
            string effectDirectory = Path.Combine(path, "Effects", effectInfo.GetName());

            SaveEffectExtras(flags, effectDirectory, context, effectInfo.Effect, out Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLines);

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
            bool doStu = false;
            byte lod = 1;

            if (flags is ExtractFlags extractFlags) {
                convertModels = !extractFlags.RawModels && !extractFlags.Raw;
                doRefpose = extractFlags.ExtractRefpose;
                doStu = extractFlags.ExtractModelStu;
                lod = extractFlags.LOD;
                if (extractFlags.SkipModels) return;
            }

            FindLogic.Combo.ModelAsset modelInfo = info.m_info.m_models[modelGUID];
            string modelDirectory = Path.Combine(path, "Models", modelInfo.GetName());

            if (convertModels) {
                string modelPath = Path.Combine(modelDirectory, $"{modelInfo.GetNameIndex()}.owmdl");

                using (Stream modelStream = OpenFile(modelInfo.m_GUID)) {
                    if (modelStream == null) return;
                    CreateDirectoryFromFile(modelPath);

                    teChunkedData chunkedData = new teChunkedData(modelStream);

                    OverwatchModel model = new OverwatchModel(chunkedData, modelInfo.m_GUID, (sbyte) lod);
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

                    if (doStu) {
                        var stu = chunkedData.GetChunks<teModelChunk_STU>().Select(x => x.StructuredData).ToArray();
                        string stuPath = Path.Combine(modelDirectory, modelInfo.GetNameIndex() + ".json");
                        JSONTool.OutputJSONAlt(stu, new ListFlags {Output = stuPath}, false);
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

            foreach (ulong modelAnimation in modelInfo.n_animations) {
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

            if (modelLookInfo.m_materialGUIDs == null) return;
            foreach (ulong modelLookMaterial in modelLookInfo.m_materialGUIDs) {
                SaveMaterial(flags, path, info, modelLookMaterial);
            }
        }

        /*public static void SaveModelLookSet(ICLIFlags flags, string path, SaveContext info,
            IEnumerable<ulong> modelLookSet) {
            if(modelLookSet.Count() < 2) {
                if (modelLookSet.Count() < 1) {
                    return;
                }
                SaveModelLook(flags, path, info, modelLookSet.ElementAt(0));
                return;
            }

            FindLogic.Combo.ModelLookAsset modelLookInfo = new FindLogic.Combo.ModelLookAsset(0) {
                m_name = string.Join("_", modelLookSet.Select(x => info.m_info.m_modelLooks.ContainsKey(x) ? info.m_info.m_modelLooks[x].GetNameIndex() : $"{x & 0xFFFFFFFFFFFF:X12}")),
                m_materialGUIDs = new HashSet<ulong>()
            };

            var doneIDs = new HashSet<ulong>();
            
            foreach (ulong modelLookGuid in modelLookSet.Reverse()) {
                if (info.m_info.m_modelLooks.ContainsKey(modelLookGuid)) {
                    foreach(var materialGuid in info.m_info.m_modelLooks[modelLookGuid].m_materialGUIDs) {
                        var material = info.m_info.m_materials[materialGuid];
                        if (doneIDs.Any(x => material.m_materialIDs.Contains(x))) {
                            continue;
                        }
                        doneIDs.UnionWith(material.m_materialIDs);
                        modelLookInfo.m_materialGUIDs.Add(materialGuid);
                    }
                }
            }

            SaveOWMaterialModelLookFile(path, modelLookInfo, info.m_info);

            if (modelLookInfo.m_materialGUIDs == null) return;
            foreach (ulong modelLookMaterial in modelLookInfo.m_materialGUIDs) {
                SaveMaterial(flags, path, info, modelLookMaterial);
            }
        }*/

        public static void SaveOWMaterialFile(string path, FindLogic.Combo.MaterialAsset materialInfo, FindLogic.Combo.ComboInfo info) {
            Model.OverwatchMaterial material = new Model.OverwatchMaterial(info, materialInfo);
            string materialPath =
                Path.Combine(path, "Materials", $"{materialInfo.GetNameIndex()}.{material.Extension}");
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

            SaveOWMaterialFile(path, materialInfo, info.m_info);

            if (materialDataInfo.m_textureMap != null) {
                foreach (KeyValuePair<ulong, uint> texture in materialDataInfo.m_textureMap) {
                    SaveTexture(flags, textureDirectory, info, texture.Key);
                }
            }

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

        public static void SaveLooseTextures(ICLIFlags flags, string path, SaveContext context) {
            foreach (FindLogic.Combo.TextureAsset textureInfo in context.m_info.m_textures.Values) {
                if (!textureInfo.m_loose) continue;
                SaveTexture(flags, path, context, textureInfo.m_GUID);
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

                SaveVoiceStimulus(flags, thisPath, context, voiceLineInstance);
            }
        }

        private static async Task SaveTextureTask(ICLIFlags flags, string path, SaveContext info, ulong textureGUID, string name = null) {
            bool convertTextures = true;
            string convertType = "tif";
            string multiSurfaceConvertType = "tif";
            bool createMultiSurfaceSheet = false;
            bool lossless = false;
            int maxMips = 1;

            if (flags is ExtractFlags extractFlags) {
                if (extractFlags.SkipTextures) return;
                createMultiSurfaceSheet = extractFlags.SheetMultiSurface;
                convertTextures = !extractFlags.RawTextures && !extractFlags.Raw;
                convertType = extractFlags.ConvertTexturesType.ToLowerInvariant();
                lossless = extractFlags.ConvertTexturesLossless;

                multiSurfaceConvertType = convertType;
                if (extractFlags.ForceDDSMultiSurface) {
                    multiSurfaceConvertType = "dds";
                }

                if (convertType == "dds" && extractFlags.SaveMips) {
                    maxMips = 0xF;
                }
            }

            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                path += Path.DirectorySeparatorChar;


            FindLogic.Combo.TextureAsset textureInfo = info.m_info.m_textures[textureGUID];
            string filePath = Path.Combine(path, name ?? $"{textureInfo.GetNameIndex()}");
            if (teResourceGUID.Type(textureGUID) != 0x4) filePath += $".{teResourceGUID.Type(textureGUID):X3}";

            if (Program.Flags != null && Program.Flags.Deduplicate) {
                if (ScratchDBInstance.HasRecord(textureGUID)) {
                    return;
                }

                ScratchDBInstance[textureGUID] = new ScratchDB.ScratchPath($"{filePath}.{convertType}", true);
            }

            CreateDirectoryFromFile(path);

            await s_texurePrepareSemaphore.WaitAsync();
            try {
                if (!convertTextures) {
                    teTexture texture;
                    using (Stream textureStream = OpenFile(textureGUID)) {
                        texture = new teTexture(textureStream, true);
                        textureStream.Position = 0;
                        WriteFile(textureStream, $"{filePath}.004");
                    }

                    if (!texture.PayloadRequired) return;
                    for (int i = 0; i < texture.Payloads.Length; ++i) {
                        using (Stream texturePayloadStream = OpenFile(texture.GetPayloadGUID(textureGUID, i)))
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

                    //if (texture.Header.Flags.HasFlag(teTexture.Flags.CUBEMAP)) return;
                    // for diffing when they add/regen loads of cubemaps

                    if (texture.PayloadRequired) {
                        for (int i = 0; i < texture.Payloads.Length; ++i) {
                            using (var payloadStream = OpenFile(texture.GetPayloadGUID(textureGUID, i)))
                                texture.LoadPayload(payloadStream, i);
                            if (maxMips == 1) break;
                        }
                    }

                    uint? width = null;
                    uint? height = null;
                    uint? surfaces = null;
                    if (texture.Header.IsCubemap || texture.Header.IsArray || texture.HasMultipleSurfaces) {
                        if (createMultiSurfaceSheet) {
                            Logger.Debug("Combo", $"Saving {Path.GetFileName(filePath)} as a sheet because it has more than one surface");
                            height = (uint) (texture.Header.Height * texture.Header.Surfaces);
                            surfaces = 1;
                            texture.Header.Flags = 0;
                        } else if (convertType != "tif" && convertType != "dds") {
                            Logger.Debug("Combo", $"Saving {Path.GetFileName(filePath)} as {multiSurfaceConvertType} because it has more than one surface");
                            convertType = multiSurfaceConvertType;
                        }
                    }


                    WICCodecs? imageFormat = null;
                    switch (convertType) {
                        case "tif":
                            imageFormat = WICCodecs.TIFF;
                            break;
                        case "png":
                            imageFormat = WICCodecs.PNG;
                            break;
                        case "jpg":
                            imageFormat = WICCodecs.JPEG;
                            break;
                    }

                    // if (convertType == "tga") imageFormat = Im.... oh
                    // so there is no TGA image format.
                    // sucks to be them

                    if (convertType == "dds") {
                        using (Stream convertedStream = texture.SaveToDDS(maxMips == 1 ? 1 : texture.Header.MipCount, width, height, surfaces)) {
                            WriteFile(convertedStream, $"{filePath}.dds");
                        }

                        return;
                    }

                    Process pProcess;

                    using (Stream convertedStream = texture.SaveToDDS(maxMips == 1 ? 1 : texture.Header.MipCount, width, height, surfaces)) {
                        var data = DDSConverter.ConvertDDS(convertedStream, DXGI_FORMAT.R8G8B8A8_UNORM, imageFormat.Value, 0);
                        if (data != null) {
                            WriteFile(data, $"{filePath}.{convertType}");
                        } else {
                            convertedStream.Position = 0;
                            WriteFile(convertedStream, $"{filePath}.dds");
                            Logger.Error("Combo", $"Unable to save {Path.GetFileName(filePath)} as {convertType} because DirectXTex failed.");
                        }
                    }
                }
            } finally {
                s_texurePrepareSemaphore.Release();
            }
        }

        public static void SaveTexture(ICLIFlags flags, string path, SaveContext info, ulong textureGUID, string name = null) {
            info.AddTask(() => SaveTextureTask(flags, path, info, textureGUID, name));
        }

        private static void ConvertSoundFile(Stream stream, FindLogic.Combo.ComboAsset soundFileInfo, string directory, string name = null) {
            string outputFile = Path.Combine(directory, $"{name ?? soundFileInfo.GetName()}.ogg");
            CreateDirectoryFromFile(outputFile);
            try {
                using (Stream outputStream = File.OpenWrite(outputFile)) {
                    outputStream.SetLength(0);
                    ConvertSoundFile(stream, outputStream);
                }
            } catch (IOException) {
                if (File.Exists(outputFile)) return;
                throw;
            }
        }


        public static void ConvertSoundFile(Stream stream, Stream outputStream) {
            try {
                using (Sound.WwiseRIFFVorbis vorbis =
                    new Sound.WwiseRIFFVorbis(stream,
                                              Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Third Party",
                                                                            "packed_codebooks_aoTuV_603.bin")))) {
                    Stream vorbisStream = new MemoryStream();
                    vorbis.ConvertToOgg(vorbisStream);
                    vorbisStream.Position = 0;
                    using (Stream revorbStream = RevorbStd.Revorb.Jiggle(vorbisStream)) {
                        revorbStream.Position = 0;
                        revorbStream.CopyTo(outputStream);
                    }
                }
            } catch (Exception e) {
                Logger.Error("Combo", $"Error converting sound: {e}");
            }
        }

        private static void SaveSoundFileTask(ICLIFlags flags, string directory, FindLogic.Combo.SoundFileAsset soundFileInfo, string name = null) {
            bool convertWem = true;
            if (flags is ExtractFlags extractFlags) {
                convertWem = !extractFlags.RawSound && !extractFlags.Raw;
                if (extractFlags.SkipSound) return;
            }

            using (Stream soundStream = OpenFile(soundFileInfo.m_GUID)) {
                if (soundStream == null) return;

                if (!convertWem) {
                    WriteFile(soundStream, Path.Combine(directory, $"{name ?? soundFileInfo.GetName()}.wem"));
                } else {
                    ConvertSoundFile(soundStream, soundFileInfo, directory, name);
                }
            }
        }

        public static void SaveSoundFile(ICLIFlags flags, string directory, SaveContext context, ulong soundFile, bool voice, string name = null) {
            if (soundFile == 0) return;

            FindLogic.Combo.SoundFileAsset soundFileInfo = voice ? context.m_info.m_voiceSoundFiles[soundFile] : context.m_info.m_soundFiles[soundFile];
            context.AddTask(() => SaveSoundFileTask(flags, directory, soundFileInfo, name));
        }
    }
}
