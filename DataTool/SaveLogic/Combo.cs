using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using BCFF;
using DataTool.ConvertLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.Extract;
using TankLib;
using TankLib.ExportFormats;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic {
    public class Combo {
        public static ScratchDB ScratchDBInstance = new ScratchDB();

        public static void Save(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info) {
            foreach (FindLogic.Combo.EntityInfoNew entity in info.Entities.Values) {
                SaveEntity(flags, path, info, entity.GUID);
            }
            foreach (FindLogic.Combo.EffectInfoCombo effectInfo in info.Effects.Values) {
                SaveEffect(flags, path, info, effectInfo.GUID);
            }
            foreach (FindLogic.Combo.ModelInfoNew model in info.Models.Values) {
                SaveModel(flags, path, info, model.GUID);
            }
        }

        public static void SaveVoiceStimulus(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            FindLogic.Combo.VoiceLineInstanceInfo voiceLineInstanceInfo) {

            foreach (ulong soundFile in voiceLineInstanceInfo.SoundFiles) {
                SaveSoundFile(flags, path, info, soundFile, true);
            }
        }

        public static void SaveEntity(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            ulong entityGuid) {
            FindLogic.Combo.EntityInfoNew entityInfo = info.Entities[entityGuid];
            
            Entity.OverwatchEntity entity = new Entity.OverwatchEntity(entityInfo, info);
            
            string entityDir = Path.Combine(path, "Entities", entityInfo.GetName());
            string outputFile = Path.Combine(entityDir, entityInfo.GetName() + $".{entity.Extension}");
            CreateDirectoryFromFile(outputFile);

            using (Stream entityOutputStream = File.OpenWrite(outputFile)) {
                entityOutputStream.SetLength(0);
                entity.Write(entityOutputStream);
            }

            if (!info.SaveConfig.SaveAnimationEffects) return;
            if (entityInfo.Model == 0) return; 
            foreach (ulong animation in entityInfo.Animations) {
                SaveAnimationEffectReference(entityDir, info, animation, entityInfo.Model);
            }

            foreach (ulong effect in entityInfo.Effects) {
                SaveEffect(flags, entityDir, info, effect);
            }
        }

        public static void SaveAnimationEffectReference(string path, FindLogic.Combo.ComboInfo info,
            ulong animation, ulong model) {
            FindLogic.Combo.AnimationInfoNew animationInfo = info.Animations[animation];
            
            Effect.OverwatchAnimationEffectReference reference = new Effect.OverwatchAnimationEffectReference(info, animationInfo, model);
            
            string file = Path.Combine(path, Effect.OverwatchAnimationEffect.AnimationEffectDir,
                animationInfo.GetNameIndex() + $".{reference.Extension}");
            CreateDirectoryFromFile(file);
            using (Stream outputStream = File.OpenWrite(file)) {
                reference.Write(outputStream);
            }
        }

        private static void ConvertAnimation(Stream animStream, string path, bool convertAnims, FindLogic.Combo.AnimationInfoNew animationInfo) {
            teAnimation parsedAnimation = new teAnimation(animStream, true);

            string animationDirectory =
                Path.Combine(path, "Animations", parsedAnimation.Header.Priority.ToString());

            if (convertAnims) {
                SEAnim seAnim = new SEAnim(parsedAnimation);
                string animOutput = Path.Combine(animationDirectory,
                    animationInfo.GetNameIndex() + "." + seAnim.Extension);
                CreateDirectoryFromFile(animOutput);
                using (Stream fileStream = new FileStream(animOutput, FileMode.Create)) {
                    seAnim.Write(fileStream);
                }
            } else {
                animStream.Position = 0;
                string rawAnimOutput = Path.Combine(animationDirectory,
                    $"{animationInfo.GetNameIndex()}.{teResourceGUID.Type(animationInfo.GUID):X3}");
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
                return ScratchDBInstance[GUID].MakeRelative(cwd);
            }
            return basePath;
        }

        public static void SaveAnimation(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong animation,
            ulong model) {
            bool convertAnims = false;
            if (flags is ExtractFlags extractFlags) {
                convertAnims = extractFlags.ConvertAnimations && !extractFlags.Raw;
                if (extractFlags.SkipAnimations) return;
            }
            
            FindLogic.Combo.AnimationInfoNew animationInfo = info.Animations[animation];

            using (Stream animStream = OpenFile(animation)) {
                if (animStream == null) return;
                ConvertAnimation(animStream, path, convertAnims, animationInfo);
            }

            if (!info.SaveConfig.SaveAnimationEffects) return;
            FindLogic.Combo.EffectInfoCombo animationEffect;

            
            // just create a fake effect if it doesn't exist
            if (animationInfo.Effect == 0) {
                animationEffect = new FindLogic.Combo.EffectInfoCombo(0) {Effect = new EffectParser.EffectInfo()};
                animationEffect.Effect.SetupEffect();
            } else if (info.Effects.ContainsKey(animationInfo.Effect)) {
                // wot, why
                animationEffect = info.Effects[animationInfo.Effect];
            } else {
                animationEffect = info.AnimationEffects[animationInfo.Effect];
            }

            string animationEffectDir = Path.Combine(path, Effect.OverwatchAnimationEffect.AnimationEffectDir, animationInfo.GetNameIndex());
            
            Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLines = new Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>>();
            if (animationEffect.GUID != 0) {
                SaveEffectExtras(flags, animationEffectDir, info, animationEffect.Effect, out svceLines);
            }
            Effect.OverwatchAnimationEffect output = new Effect.OverwatchAnimationEffect(info, animationEffect, svceLines, animationInfo, model);
            string animationEffectFile =
                Path.Combine(animationEffectDir, $"{animationInfo.GetNameIndex()}.{output.Extension}");
            CreateDirectoryFromFile(animationEffectFile);

            using (Stream fileStream = new FileStream(animationEffectFile, FileMode.Create)) {
                fileStream.SetLength(0);
                output.Write(fileStream);
            }
        }

        public static void SaveEffectExtras(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            EffectParser.EffectInfo effectInfo, out Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLines) {
            string soundDirectory = Path.Combine(path, "Sounds");
            svceLines = GetSVCELines(effectInfo, info);
            
            HashSet<ulong> done = new HashSet<ulong>();
            foreach (EffectParser.OSCEInfo osceInfo in effectInfo.OSCEs) {
                if (osceInfo.Sound == 0 || done.Contains(osceInfo.Sound)) continue;
                SaveSound(flags, soundDirectory, info, osceInfo.Sound);
                done.Add(osceInfo.Sound);
            }
            
            foreach (KeyValuePair<ulong,HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLine in svceLines) {
                SaveVoiceStimuli(flags, soundDirectory, info, svceLine.Value, true);
            }
        }

        public static void SaveSound(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong sound) {
            FindLogic.Combo.SoundInfoNew soundInfo = info.Sounds[sound];
            string soundDir = Path.Combine(path, soundInfo.GetName());
            CreateDirectorySafe(soundDir);

            HashSet<ulong> done = new HashSet<ulong>();
            if (soundInfo.SoundFiles != null) {
                foreach (KeyValuePair<uint, ulong> soundPair in soundInfo.SoundFiles) {
                    if (done.Contains(soundPair.Value)) continue;
                    SaveSoundFile(flags, soundDir, info, soundPair.Value, false);
                    done.Add(soundPair.Value);
                }
            }

            if (soundInfo.SoundStreams != null) {
                foreach (KeyValuePair<uint, ulong> soundStream in soundInfo.SoundStreams) {
                    if (done.Contains(soundStream.Value)) continue;
                    SaveSoundFile(flags, soundDir, info, soundStream.Value, false);
                    done.Add(soundStream.Value);
                }
            }
        }

        private static Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> GetSVCELines(EffectParser.EffectInfo effectInfo, FindLogic.Combo.ComboInfo info) {
            Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> output = new Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>>();
            if (effectInfo.SVCEs.Count == 0 || effectInfo.VoiceSet == 0) return output;

            foreach (EffectParser.SVCEInfo svceInfo in effectInfo.SVCEs) {
                Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> instances = info.VoiceSets[effectInfo.VoiceSet].VoiceLineInstances;
                if (instances.ContainsKey(svceInfo.VoiceStimulus)) {
                    output[svceInfo.VoiceStimulus] = instances[svceInfo.VoiceStimulus];
                }
            }

            return output;
        }

        public static void SaveEffect(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong effect) {
            FindLogic.Combo.EffectInfoCombo effectInfo = info.Effects[effect];
            string effectDirectory = Path.Combine(path, "Effects", effectInfo.GetName());

            SaveEffectExtras(flags, effectDirectory, info, effectInfo.Effect, out Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLines);

            Effect.OverwatchEffect output = new Effect.OverwatchEffect(info, effectInfo, svceLines);
            string effectFile = Path.Combine(effectDirectory, $"{effectInfo.GetNameIndex()}.{output.Extension}");
            CreateDirectoryFromFile(effectFile);
            
            using (Stream effectOutputStream = File.OpenWrite(effectFile)) {
                effectOutputStream.SetLength(0);
                output.Write(effectOutputStream);
            }
        }

        public static void SaveModel(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong modelGUID) {
            bool convertModels = true;
            bool doRefpose = false;
            byte lod = 1;

            if (flags is ExtractFlags extractFlags) {
                convertModels = extractFlags.ConvertModels  && !extractFlags.Raw;
                doRefpose = extractFlags.ExtractRefpose;
                lod = extractFlags.LOD;
                if (extractFlags.SkipModels) return;
            }
            
            FindLogic.Combo.ModelInfoNew modelInfo = info.Models[modelGUID];
            string modelDirectory = Path.Combine(path, "Models", modelInfo.GetName());

            if (convertModels) {
                string modelPath = Path.Combine(modelDirectory, $"{modelInfo.GetNameIndex()}.owmdl");
                CreateDirectoryFromFile(modelPath);

                using (Stream modelStream = OpenFile(modelInfo.GUID)) {
                    teChunkedData chunkedData = new teChunkedData(modelStream);
                    
                    OverwatchModel model = new OverwatchModel(chunkedData, modelInfo.GUID, (sbyte)lod);
                    if (modelInfo.ModelLooks.Count > 0) {
                        FindLogic.Combo.ModelLookInfo modelLookInfo = info.ModelLooks[modelInfo.ModelLooks.First()];
                        model.ModelLookFileName = Path.Combine("ModelLooks",
                            modelLookInfo.GetNameIndex() + ".owmat");
                    }
                    using (Stream fileStream = File.OpenWrite(modelPath)) {
                        fileStream.SetLength(0);
                        model.Write(fileStream);
                    }

                    if (doRefpose) {
                        string refposePath = Path.Combine(modelDirectory, modelInfo.GetNameIndex()+".smd");
                        
                        using (Stream fileStream = File.OpenWrite(refposePath)) {
                            fileStream.SetLength(0);
                            var refpose = new RefPoseSkeleton(chunkedData);
                            refpose.Write(fileStream);
                        }
                    }
                }
            } else {
                using (Stream modelStream = OpenFile(modelInfo.GUID)) {
                    WriteFile(modelStream, Path.Combine(modelDirectory, modelInfo.GetNameIndex()+".00C"));
                }
            }

            foreach (ulong modelModelLook in modelInfo.ModelLooks) {
                SaveModelLook(flags, modelDirectory, info, modelModelLook);
            }

            //
            //foreach (IEnumerable<ulong> modelModelLookSet in modelInfo.ModelLookSets) {
            //    SaveModelLookSet(flags, modelDirectory, info, modelModelLookSet);
            //}

            foreach (ulong looseMaterial in modelInfo.LooseMaterials) {
                SaveMaterial(flags, modelDirectory, info, looseMaterial);
            }

            foreach (ulong modelAnimation in modelInfo.Animations) {
                SaveAnimation(flags, modelDirectory, info, modelAnimation, modelGUID);
            }
        }

        public static void SaveOWMaterialModelLookFile(string path, FindLogic.Combo.ModelLookInfo modelLookInfo, FindLogic.Combo.ComboInfo info) {
            Model.OverwatchModelLook modelLook = new Model.OverwatchModelLook(info, modelLookInfo);
            
            string modelLookPath =
                Path.Combine(path, "ModelLooks", $"{modelLookInfo.GetNameIndex()}.{modelLook.Extension}");
            CreateDirectoryFromFile(modelLookPath);
            using (Stream modelLookOutputStream = File.OpenWrite(modelLookPath)) {
                modelLookOutputStream.SetLength(0);
                modelLook.Write(modelLookOutputStream);
            }
        }

        public static void SaveModelLook(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            ulong modelLook) {
            FindLogic.Combo.ModelLookInfo modelLookInfo = info.ModelLooks[modelLook];

            SaveOWMaterialModelLookFile(path, modelLookInfo, info);

            if (modelLookInfo.Materials == null) return;
            foreach (ulong modelLookMaterial in modelLookInfo.Materials) {
                SaveMaterial(flags, path, info, modelLookMaterial);
            }
        }

        public static void SaveModelLookSet(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            IEnumerable<ulong> modelLookSet) {
            if(modelLookSet.Count() < 2) {
                if (modelLookSet.Count() < 1) {
                    return;
                }
                SaveModelLook(flags, path, info, modelLookSet.ElementAt(0));
                return;
            }

            FindLogic.Combo.ModelLookInfo modelLookInfo = new FindLogic.Combo.ModelLookInfo(0) {
                Name = string.Join("_", modelLookSet.Select(x => info.ModelLooks.ContainsKey(x) ? info.ModelLooks[x].GetNameIndex() : $"{x & 0xFFFFFFFFFFFF:X12}")),
                Materials = new HashSet<ulong>()
            };

            var doneIDs = new HashSet<ulong>();
            
            foreach (ulong modelLookGuid in modelLookSet.Reverse()) {
                if (info.ModelLooks.ContainsKey(modelLookGuid)) {
                    foreach(var materialGuid in info.ModelLooks[modelLookGuid].Materials) {
                        var material = info.Materials[materialGuid];
                        if (doneIDs.Any(x => material.MaterialIDs.Contains(x))) {
                            continue;
                        }
                        doneIDs.UnionWith(material.MaterialIDs);
                        modelLookInfo.Materials.Add(materialGuid);
                    }
                }
            }

            SaveOWMaterialModelLookFile(path, modelLookInfo, info);

            if (modelLookInfo.Materials == null) return;
            foreach (ulong modelLookMaterial in modelLookInfo.Materials) {
                SaveMaterial(flags, path, info, modelLookMaterial);
            }
        }

        public static void SaveOWMaterialFile(string path, FindLogic.Combo.MaterialInfo materialInfo, FindLogic.Combo.ComboInfo info) {
            Model.OverwatchMaterial material = new Model.OverwatchMaterial(info, materialInfo);
            string materialPath =
                Path.Combine(path, "Materials", $"{materialInfo.GetNameIndex()}.{material.Extension}");
            CreateDirectoryFromFile(materialPath);
            using (Stream materialOutputStream = File.OpenWrite(materialPath)) {
                materialOutputStream.SetLength(0);
                material.Write(materialOutputStream);
            }
        }

        private static void SaveVoiceSetInternal(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            ulong voiceSet) {
            string thisPath = Path.Combine(path, GetFileName(voiceSet));

            FindLogic.Combo.VoiceSetInfo voiceSetInfo = info.VoiceSets[voiceSet];
            foreach (KeyValuePair<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> stimuliSet in voiceSetInfo.VoiceLineInstances) {
                SaveVoiceStimuliInternal(flags, thisPath, info, stimuliSet.Value, true);
            }
        }

        public static void SaveMaterial(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong material) {
            FindLogic.Combo.MaterialInfo materialInfo = info.Materials[material];
            FindLogic.Combo.MaterialDataInfo materialDataInfo = info.MaterialDatas[materialInfo.MaterialData];

            string textureDirectory = Path.Combine(path, "Textures");
            
            SaveOWMaterialFile(path, materialInfo, info);

            if (materialDataInfo.Textures != null) {
                foreach (KeyValuePair<ulong, uint> texture in materialDataInfo.Textures) {
                    SaveTexture(flags, textureDirectory, info, texture.Key);
                }
            }
        }
        

        // helpers (NOT FOR INTERNAL USE)
        public static void SaveAllVoiceSets(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo soundInfo) {
            foreach (KeyValuePair<ulong,FindLogic.Combo.VoiceSetInfo> voiceSet in soundInfo.VoiceSets) {
                SaveVoiceSet(flags, path, soundInfo, voiceSet.Value);
            }
        }
        
        public static void SaveLooseTextures(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info) {
            foreach (FindLogic.Combo.TextureInfoNew textureInfo in info.Textures.Values) {
                if (!textureInfo.Loose) continue;
                SaveTexture(flags, path, info, textureInfo.GUID);
            }
        }
        
        public static void SaveAllStrings(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info) {
            foreach (FindLogic.Combo.StringInfo stringInfo in info.Strings.Values) {
                if (stringInfo.Value == null) continue;
                string file = Path.Combine(path, stringInfo.GetName()) + ".txt";
                CreateDirectoryFromFile(file);
                using (StreamWriter writer = new StreamWriter(file)) {
                    writer.Write(stringInfo.Value);
                }
            }
        }
        
        public static void SaveAllSoundFiles(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info) {
            foreach (FindLogic.Combo.SoundFileInfo soundInfo in info.SoundFiles.Values) {
                SaveSoundFile(flags, path, info, soundInfo.GUID, false);
            }
        }
        
        public static void SaveAllVoiceSoundFiles(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info) {
            foreach (FindLogic.Combo.SoundFileInfo soundInfo in info.VoiceSoundFiles.Values) {
                SaveSoundFile(flags, path, info, soundInfo.GUID, true);
            }
        }
        
        public static void SaveAllMaterials(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info) {
            foreach (ulong material in info.Materials.Keys) {
                SaveMaterial(flags, path, info, material);
            }
        }

        public static void SaveAllModelLooks(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info) {
            foreach (ulong material in info.ModelLooks.Keys) {
                SaveModelLook(flags, path, info, material);
            }
        }

#warning TODO: This method does not support animation effects
        public static void SaveAllAnimations(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info) {
            bool beforeSaveAnimEffects = info.SaveConfig.SaveAnimationEffects;
            info.SaveConfig.SaveAnimationEffects = false;
            
            foreach (ulong material in info.Animations.Keys) {
                SaveAnimation(flags, path, info, material, 0);
            }
            info.SaveConfig.SaveAnimationEffects = beforeSaveAnimEffects;
        }
        
        public static void SaveVoiceSet(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, 
            ulong voiceSet) {
            SaveVoiceSetInternal(flags, path, info, voiceSet);
        }

        public static void SaveVoiceSet(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            FindLogic.Combo.VoiceSetInfo voiceSetInfo) {
            SaveVoiceSetInternal(flags, path, info, voiceSetInfo.GUID);
        }

        public static void SaveVoiceStimuli(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            IEnumerable<FindLogic.Combo.VoiceLineInstanceInfo> voiceLineInstances, bool split) {
            SaveVoiceStimuliInternal(flags, path, info, voiceLineInstances, split);
        }
        
        // internal stuff for helpers (for internal use)
        private static void SaveVoiceStimuliInternal(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            IEnumerable<FindLogic.Combo.VoiceLineInstanceInfo> voiceLineInstances, bool split) {
            foreach (FindLogic.Combo.VoiceLineInstanceInfo voiceLineInstance in voiceLineInstances) {
                string thisPath = path;
                if (split) {
                    thisPath = Path.Combine(path, GetFileName(voiceLineInstance.VoiceStimulus));
                }
                SaveVoiceStimulus(flags, thisPath, info, voiceLineInstance);
            }
        }

        public static void SaveTexture(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong textureGUID) {
            bool convertTextures = true;
            string convertType = "tif";
            bool lossless = false;

            if (flags is ExtractFlags extractFlags) {
                convertTextures = extractFlags.ConvertTextures  && !extractFlags.Raw;
                convertType = extractFlags.ConvertTexturesType.ToLowerInvariant();
                lossless = extractFlags.ConvertTexturesLossless;
                if (extractFlags.SkipTextures) return;
            }
            path += Path.DirectorySeparatorChar;

            FindLogic.Combo.TextureInfoNew textureInfo = info.Textures[textureGUID];
            string filePath = Path.Combine(path, $"{textureInfo.GetNameIndex()}");

            if (Program.Flags.Deduplicate) {
                if(ScratchDBInstance.HasRecord(textureGUID)) {
                    return;
                }
                ScratchDBInstance[textureGUID] = new ScratchDB.ScratchPath($"{filePath}.{convertType}");
            }

            CreateDirectoryFromFile(path);
            if (!convertTextures) {
                using (Stream textureStream = OpenFile(textureGUID)) {
                    teTexture texture = new teTexture(textureStream, true);
                    textureStream.Position = 0;
                    WriteFile(textureStream, $"{filePath}.004");

                    if (!texture.PayloadRequired) return;
                    using (Stream texturePayloadStream = OpenFile(texture.GetPayloadGUID(textureGUID)))
                        WriteFile(texturePayloadStream, $"{filePath}.04D");
                }
            } else {
                using (Stream textureStream = OpenFile(textureGUID)) {
                    if (textureStream == null) return;
                    teTexture texture = new teTexture(textureStream);

                    if (texture.PayloadRequired) {
                        texture.LoadPayload(OpenFile(texture.GetPayloadGUID(textureGUID)));
                    }

                    using (Stream convertedStream = texture.SaveToDDS()) {
                        convertedStream.Position = 0;
                        if (convertType == "dds" || convertedStream.Length == 0) {
                            WriteFile(convertedStream, $"{filePath}.dds");
                            return;
                        }
                        
                        bool isBcffValid = teTexture.DXGI_BC4.Contains(texture.Header.Format) || 
                                           teTexture.DXGI_BC5.Contains(texture.Header.Format) ||
                                           new[] {TextureTypes.TextureType.ATI1, 
                                               TextureTypes.TextureType.ATI2}.Contains(texture.Header.GetTextureType());
                        
                        ImageFormat imageFormat = null;
                        if (convertType == "tif") imageFormat = ImageFormat.Tiff;
                        if (convertType == "png") imageFormat = ImageFormat.Png;
                        if (convertType == "jpg") imageFormat = ImageFormat.Jpeg;
                        // if (convertType == "tga") imageFormat = Im.... oh
                        // so there is no TGA image format.
                        // guess the TGA users are stuck with the DirectXTex stuff for now.

                        if (isBcffValid && imageFormat != null) {
                            BlockDecompressor decompressor = new BlockDecompressor(convertedStream);
                            decompressor.CreateImage();
                            decompressor.Image.Save($"{filePath}.{convertType}", imageFormat);
                            return;
                        }
                        
                        string losslessFlag = lossless ? "-wiclossless" : string.Empty;

                        Process pProcess = new Process {
                            StartInfo = {
                                FileName = "Third Party\\texconv.exe",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardInput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true,
                                Arguments =
                                    $"-- \"{Path.GetFileName(filePath)}.dds\" -y -wicmulti {losslessFlag} -nologo -m 1 -ft {convertType} -f R8G8B8A8_UNORM -o \"{path}"
                            },
                            EnableRaisingEvents = true
                        };

                        // erm, so if you add an end quote to this then it breaks.
                        // but start one on it's own is fine (we need something for "Winged Victory")
                        pProcess.Start();
                        convertedStream.Position = 0;
                        convertedStream.CopyTo(pProcess.StandardInput.BaseStream);
                        pProcess.StandardInput.BaseStream.Close();

                        // pProcess.WaitForExit(); // not using this is kinda dangerous but I don't care
                        // when texconv writes with to the console -nologo is has done/failed conversion
                        string line = pProcess.StandardOutput.ReadLine();
                        if (line?.Contains("FAILED") == true) {
                            convertedStream.Position = 0;
                            WriteFile(convertedStream, $"{filePath}.dds");
                        }
                    }
                }
            }
        }

        private static void ConvertSoundFile(Stream stream, FindLogic.Combo.SoundFileInfo soundFileInfo, string directory)
        {
            string outputFile = Path.Combine(directory, $"{soundFileInfo.GetName()}.ogg");
            CreateDirectoryFromFile(outputFile);
            using (Stream outputStream = File.OpenWrite(outputFile))
            {
                outputStream.SetLength(0);
                ConvertSoundFile(stream, outputStream);
            }
        }


        public static void ConvertSoundFile(Stream stream, Stream outputStream)
        {
            try
            {
                using (Sound.WwiseRIFFVorbis vorbis =
                    new Sound.WwiseRIFFVorbis(stream,
                        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Third Party",
                            "packed_codebooks_aoTuV_603.bin"))))
                {
                    Stream vorbisStream = new MemoryStream();
                    vorbis.ConvertToOgg(vorbisStream);
                    vorbisStream.Position = 0;
                    using (Stream revorbStream = RevorbStd.Revorb.Jiggle(vorbisStream))
                    {
                        revorbStream.Position = 0;
                        revorbStream.CopyTo(outputStream);
                    }
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
            }
        }

        public static void SaveSoundFile(ICLIFlags flags, string directory, FindLogic.Combo.ComboInfo info, ulong soundFile, bool voice) {
            bool convertWem = true;
            if (flags is ExtractFlags extractFlags) {
                convertWem = extractFlags.ConvertSound && !extractFlags.Raw;
                if (extractFlags.SkipSound) return;
            }
            
            FindLogic.Combo.SoundFileInfo soundFileInfo = voice ? info.VoiceSoundFiles[soundFile] : info.SoundFiles[soundFile];

            using (Stream soundStream = OpenFile(soundFile)) {
                if (soundStream == null) return;

                if (!convertWem) {
                    WriteFile(soundStream, Path.Combine(directory, $"{soundFileInfo.GetName()}.wem"));
                } else {
                    ConvertSoundFile(soundStream, soundFileInfo, directory);
                }
            }
        }
    }
}
