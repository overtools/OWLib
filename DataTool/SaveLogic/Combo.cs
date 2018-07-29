using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using BCFF;
using DataTool.ConvertLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.Extract;
using OWLib;
using TankLib;
using TankLib.ExportFormats;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic {
    public class Combo {
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
            
            // rules for threads:
            // CASC IO MUST BE DONE IN MAIN THREAD. NO EXCEPTIONS
        }

        public static void SaveVoiceStimulus(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            FindLogic.Combo.VoiceLineInstanceInfo voiceLineInstanceInfo) {

            foreach (ulong soundFile in voiceLineInstanceInfo.SoundFiles) {
                SaveSoundFile(flags, path, info, soundFile, true);
            }
        }

        public static void SaveEntity(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            ulong entity) {
            FindLogic.Combo.EntityInfoNew entityInfo = info.Entities[entity];
            Entity.OWEntityWriter entityWriter = new Entity.OWEntityWriter();
            string entityDir = Path.Combine(path, "Entities", entityInfo.GetName());
            string outputFile = Path.Combine(entityDir, entityInfo.GetName() + entityWriter.Format);
            CreateDirectoryFromFile(outputFile);

            using (Stream entityOutputStream = File.OpenWrite(outputFile)) {
                entityOutputStream.SetLength(0);
                entityWriter.Write(entityOutputStream, entityInfo, info);
            }

            if (!info.SaveConfig.SaveAnimationEffects) return;
            if (entityInfo.Model == 0) return; 
            foreach (ulong animation in entityInfo.Animations) {
                SaveAnimationEffectReference(flags, entityDir, info, animation, entityInfo.Model);
            }
        }

        public static void SaveAnimationEffectReference(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            ulong animation, ulong model) {
            FindLogic.Combo.AnimationInfoNew animationInfo = info.Animations[animation];
            Effect.OWAnimWriter animWriter = new Effect.OWAnimWriter();
            string file = Path.Combine(path, Model.AnimationEffectDir,
                animationInfo.GetNameIndex() + animWriter.Format);
            CreateDirectoryFromFile(file);
            using (Stream outputStream = File.OpenWrite(file)) {
                animWriter.WriteReference(outputStream, info, animationInfo, model);
            }
        }

        private static void ConvertAnimation(Stream animStream, string path, bool convertAnims, FindLogic.Combo.AnimationInfoNew animationInfo) {
            animStream.Position = 0;
            teAnimation parsedAnimation = new teAnimation(animStream);

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
                string rawAnimOutput = Path.Combine(animationDirectory,
                    $"{animationInfo.GetNameIndex()}.{GUID.Type(animationInfo.GUID):X3}");
                CreateDirectoryFromFile(rawAnimOutput);
                using (Stream fileStream = new FileStream(rawAnimOutput, FileMode.Create)) {
                    animStream.CopyTo(fileStream);
                }
            }
            animStream.Dispose();
        }

        private static void SaveOWAnimFile(string animationEffectFile, FindLogic.Combo.EffectInfoCombo animationEffect, 
            FindLogic.Combo.AnimationInfoNew animationInfo, FindLogic.Combo.ComboInfo info, 
            Effect.OWAnimWriter owAnimWriter, ulong model, Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLines) {
            CreateDirectoryFromFile(animationEffectFile);

            using (Stream fileStream = new FileStream(animationEffectFile, FileMode.Create)) {
                fileStream.SetLength(0);
                owAnimWriter.Write(fileStream, info, animationInfo, animationEffect, model, svceLines);
            }
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
                if (animStream == null) {
                    return;
                }
                MemoryStream animMemStream = new MemoryStream();
                animStream.CopyTo(animMemStream);
                ConvertAnimation(animMemStream, path, convertAnims, animationInfo);
                
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

            Effect.OWAnimWriter owAnimWriter = new Effect.OWAnimWriter();
            string animationEffectDir = Path.Combine(path, Model.AnimationEffectDir, animationInfo.GetNameIndex());
            string animationEffectFile =
                Path.Combine(animationEffectDir, $"{animationInfo.GetNameIndex()}{owAnimWriter.Format}");
            Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLines = new Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>>();
            if (animationEffect.GUID != 0) {
                SaveEffectExtras(flags, animationEffectDir, info, animationEffect.Effect, out svceLines);
            }
            
            SaveOWAnimFile(animationEffectFile, animationEffect, animationInfo, info, owAnimWriter, model, svceLines);
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
            CreateDirectoryFromFile(soundDir + "\\harrypotter.png");

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
            Effect.OWEffectWriter effectWriter = new Effect.OWEffectWriter();
            FindLogic.Combo.EffectInfoCombo effectInfo = info.Effects[effect];
            string effectDirectory = Path.Combine(path, "Effects", effectInfo.GetName());
            string effectFile = Path.Combine(effectDirectory, $"{effectInfo.GetNameIndex()}{effectWriter.Format}");
            CreateDirectoryFromFile(effectFile);

            SaveEffectExtras(flags, effectDirectory, info, effectInfo.Effect, out Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLines);

            using (Stream effectOutputStream = File.OpenWrite(effectFile)) {
                effectOutputStream.SetLength(0);
                effectWriter.Write(effectOutputStream, effectInfo, info, svceLines);
            }
        }

        public static void SaveModel(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong modelGUID) {
            bool convertModels = true;
            bool doRefpose = false;

            if (flags is ExtractFlags extractFlags) {
                convertModels = extractFlags.ConvertModels  && !extractFlags.Raw;
                doRefpose = extractFlags.ExtractRefpose;
                if (extractFlags.SkipModels) return;
            }
            
            FindLogic.Combo.ModelInfoNew modelInfo = info.Models[modelGUID];
            string modelDirectory = Path.Combine(path, "Models", modelInfo.GetName());

            if (convertModels) {
                string modelPath = Path.Combine(modelDirectory, $"{modelInfo.GetNameIndex()}.owmdl");
                CreateDirectoryFromFile(modelPath);

                using (Stream modelStream = OpenFile(modelInfo.GUID)) {
                    teChunkedData chunkedData = new teChunkedData(modelStream);
                    
                    OverwatchModel model = new OverwatchModel(chunkedData);
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
                        // todo
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

            foreach (ulong looseMaterial in modelInfo.LooseMaterials) {
                SaveMaterial(flags, modelDirectory, info, looseMaterial);
            }

            foreach (ulong modelAnimation in modelInfo.Animations) {
                SaveAnimation(flags, modelDirectory, info, modelAnimation, modelGUID);
            }
        }

        public static void SaveOWMaterialModelLookFile(string path, FindLogic.Combo.ModelLookInfo modelLookInfo, Model.OWMatWriter14 materialWriter, FindLogic.Combo.ComboInfo info) {
            string modelLookPath =
                Path.Combine(path, "ModelLooks", $"{modelLookInfo.GetNameIndex()}{materialWriter.Format}");
            CreateDirectoryFromFile(modelLookPath);
            using (Stream modelLookOutputStream = File.OpenWrite(modelLookPath)) {
                modelLookOutputStream.SetLength(0);
                materialWriter.Write(modelLookOutputStream, info, modelLookInfo);
            }
        }

        public static void SaveModelLook(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            ulong modelLook) {
            Model.OWMatWriter14 materialWriter = new Model.OWMatWriter14();
            FindLogic.Combo.ModelLookInfo modelLookInfo = info.ModelLooks[modelLook];
            
            SaveOWMaterialModelLookFile(path, modelLookInfo, materialWriter, info);

            if (modelLookInfo.Materials == null) return;
            foreach (ulong modelLookMaterial in modelLookInfo.Materials) {
                SaveMaterial(flags, path, info, modelLookMaterial);
            }
        }

        public static void SaveOWMaterialFile(string path, FindLogic.Combo.MaterialInfo materialInfo, Model.OWMatWriter14 materialWriter, FindLogic.Combo.ComboInfo info) {
            string materialPath =
                Path.Combine(path, "Materials", $"{materialInfo.GetNameIndex()}{materialWriter.Format}");
            CreateDirectoryFromFile(materialPath);
            using (Stream materialOutputStream = File.OpenWrite(materialPath)) {
                materialOutputStream.SetLength(0);
                materialWriter.Write(materialOutputStream, info, materialInfo);
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

            Model.OWMatWriter14 materialWriter = new Model.OWMatWriter14();

            string textureDirectory = Path.Combine(path, "Textures");
            
            SaveOWMaterialFile(path, materialInfo, materialWriter, info);

            foreach (KeyValuePair<ulong, uint> texture in materialDataInfo.Textures) {
                SaveTexture(flags, textureDirectory, info, texture.Key);
            }
        }
        

        // helpers (NOT FOR INTERNAL USE)
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
        
        #warning This method does not support animation effects
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

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static class TextureConfig {
            internal const int FOURCC_DX10 = 808540228;
            internal const int FOURCC_ATI1 = 826889281;
            internal const int FOURCC_ATI2 = 843666497;
            internal static readonly int[] DXGI_BC4 = { 79, 80, 91 };
            internal static readonly int[] DXGI_BC5 = { 82, 83, 84 };
        }

        public static void SaveTexture(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong textureGUID) {
            bool convertTextures = true;
            string convertType = "dds";

            if (flags is ExtractFlags extractFlags) {
                convertTextures = extractFlags.ConvertTextures  && !extractFlags.Raw;
                convertType = extractFlags.ConvertTexturesType.ToLowerInvariant();
                if (extractFlags.SkipTextures) return;
            }
            path += Path.DirectorySeparatorChar;

            FindLogic.Combo.TextureInfoNew textureInfo = info.Textures[textureGUID];
            string filePath = Path.Combine(path, $"{textureInfo.GetNameIndex()}");

            CreateDirectoryFromFile(path);
            if (!convertTextures) {
                using (Stream textureStream = OpenFile(textureInfo.GUID)) {
                    teTexture texture = new teTexture(textureStream, true);
                    textureStream.Position = 0;
                    WriteFile(textureStream, $"{filePath}.004");

                    if (!texture.PayloadRequired) return;
                    using (Stream texturePayloadStream = OpenFile(texture.GetPayloadGUID(textureGUID)))
                        WriteFile(texturePayloadStream, $"{filePath}.04D");
                }
            } else {
                using (Stream textureStream = OpenFile(textureGUID)) {
                    teTexture texture = new teTexture(textureStream);

                    if (texture.PayloadRequired) {
                        texture.LoadPayload(OpenFile(texture.GetPayloadGUID(textureGUID)));
                    }

                    using (Stream convertedStream = texture.SaveToDDS()) {
                        if (convertedStream.Length == 0) {
                            WriteFile(convertedStream, $"{filePath}.{convertType}");
                            return;
                        }
                        
                        uint fourCC = texture.Header.GetFormat().ToPixelFormat().FourCC;
                        bool isBcffValid = TextureConfig.DXGI_BC4.Contains((int) texture.Header.Format) ||
                                           TextureConfig.DXGI_BC5.Contains((int) texture.Header.Format) ||
                                           fourCC == TextureConfig.FOURCC_ATI1 || fourCC == TextureConfig.FOURCC_ATI2;

                        ImageFormat imageFormat = null;
                        if (convertType == "tif") imageFormat = ImageFormat.Tiff;
                        // if (convertType == "tga") imageFormat = Im.... oh
                        // so there is no TGA image format.
                        // guess the TGA users are stuck with the DirectXTex stuff for now.

                        convertedStream.Position = 0;
                        if (isBcffValid && imageFormat != null) {
                            BlockDecompressor decompressor = new BlockDecompressor(convertedStream);
                            decompressor.CreateImage();
                            decompressor.Image.Save($"{filePath}.{convertType}", imageFormat);
                            return;
                        }

                        convertedStream.Position = 0;
                        if (convertType == "tga" || convertType == "tif" || convertType == "dds") {
                            // we need the dds for tif conversion
                            WriteFile(convertedStream, $"{filePath}.dds");
                        }
                    }
                    if (convertType != "tif" && convertType != "tga") return;
                    using (Process texconvProcess = new Process {
                        StartInfo = {
                            FileName = "Third Party\\texconv.exe",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            Arguments =
                                $"\"{filePath}.dds\" -y -wicmulti -nologo -m 1 -ft {convertType} -f R8G8B8A8_UNORM -o \"{path}"
                        }
                    }) {
                        texconvProcess.Start();
                        // pProcess.WaitForExit(); // not using this is kinda dangerous but I don't care
                        // when texconv writes with to the console -nologo is has done/failed conversion
                        string line = texconvProcess.StandardOutput.ReadLine();
                        if (line?.Contains($"{filePath}.dds FAILED") == false) {
                            // fallback if convert fails
                            File.Delete($"{filePath}.dds");
                        }
                    }
                }
            }
        }

        private static void ConvertSoundFile(Stream stream, FindLogic.Combo.SoundFileInfo soundFileInfo, string directory) {
            string outputFile = Path.Combine(directory, $"{soundFileInfo.GetName()}.wem");
            string outputFileOgg = Path.ChangeExtension(outputFile, "ogg");
            CreateDirectoryFromFile(outputFile);
            try {
                using (Sound.WwiseRIFFVorbis vorbis =
                    new Sound.WwiseRIFFVorbis(stream,
                        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Third Party",
                            "packed_codebooks_aoTuV_603.bin")))) {
                    Stream vorbisStream = new MemoryStream();
                    vorbis.ConvertToOgg(vorbisStream);
                    vorbisStream.Position = 0;
                    using (Stream revorbStream = RevorbStd.Revorb.Jiggle(vorbisStream)) {
                        using (Stream outputStream = File.OpenWrite(outputFileOgg)) {
                            outputStream.SetLength(0);
                            revorbStream.Position = 0;
                            revorbStream.CopyTo(outputStream);
                        }
                    }
                }
            } catch (Exception e) {
                Console.Out.WriteLine(e);
            }
        }

        public static void SaveSoundFile(ICLIFlags flags, string directory, FindLogic.Combo.ComboInfo info, ulong soundFile, bool voice) {
            // info.SaveConfig.Tasks.Add(Task.Run(() => { SaveSoundFile(flags, directory, info, soundFile, voice); }));
            bool convertWem = true;
            if (flags is ExtractFlags extractFlags) {
                convertWem = extractFlags.ConvertSound && !extractFlags.Raw;
                if (extractFlags.SkipSound) return;
            }
            
            FindLogic.Combo.SoundFileInfo soundFileInfo = voice ? info.VoiceSoundFiles[soundFile] : info.SoundFiles[soundFile];

            Stream soundStream = OpenFile(soundFile);  // disposed by thread
            if (soundStream == null) return;

            if (!convertWem) {
                WriteFile(soundStream, Path.Combine(directory, $"{soundFileInfo.GetName()}.wem"));
                soundStream.Dispose();
            } else {
                ConvertSoundFile(soundStream, soundFileInfo, directory);
            }
        }
    }
}
