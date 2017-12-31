using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BCFF;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.Extract;
using OWLib;
using OWLib.Types;
using OWLib.Writer;
using STULib.Types;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic {
    public class Combo {
        public static void Save(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info) {
            foreach (FindLogic.Combo.ModelInfoNew model in info.Models.Values) {
                SaveModel(flags, path, info, model.GUID);
            }

            foreach (FindLogic.Combo.EffectInfoCombo effectInfo in info.Effects.Values) {
                SaveEffect(flags, path, info, effectInfo.GUID);
            }

            foreach (FindLogic.Combo.EntityInfoNew entity in info.Entities.Values) {
                SaveEntity(flags, path, info, entity.GUID);
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

        public static void SaveAnimation(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong animation,
            ulong model) {
            bool convertAnims = false;
            if (flags is ExtractFlags extractFlags) {
                convertAnims = extractFlags.ConvertAnimations && !extractFlags.Raw;
                if (extractFlags.SkipAnimations) return;
            }

            SEAnimWriter animWriter = new SEAnimWriter();
            FindLogic.Combo.AnimationInfoNew animationInfo = info.Animations[animation];

            using (Stream animStream = OpenFile(animation)) {
                if (animStream == null) {
                    return;
                }

                OWLib.Animation parsedAnimation = new OWLib.Animation(animStream);
                animStream.Position = 0;

                string animationDirectory =
                    Path.Combine(path, "Animations", parsedAnimation.Header.priority.ToString());

                if (convertAnims) {
                    string animOutput = Path.Combine(animationDirectory,
                        animationInfo.GetNameIndex() + animWriter.Format);
                    CreateDirectoryFromFile(animOutput);
                    using (Stream fileStream = new FileStream(animOutput, FileMode.Create)) {
                        animWriter.Write(parsedAnimation, fileStream, new object[] { });
                    }
                } else {
                    string rawAnimOutput = Path.Combine(animationDirectory,
                        $"{animationInfo.GetNameIndex()}.{GUID.Type(animation):X3}");
                    CreateDirectoryFromFile(rawAnimOutput);
                    using (Stream fileStream = new FileStream(rawAnimOutput, FileMode.Create)) {
                        animStream.CopyTo(fileStream);
                    }
                }
            }

            if (!info.SaveConfig.SaveAnimationEffects) return;
            FindLogic.Combo.EffectInfoCombo animationEffect;

            // just create a fake effect if it doesn't exist
            if (animationInfo.Effect == 0) {
                animationEffect = new FindLogic.Combo.EffectInfoCombo(0) {Effect = new EffectParser.EffectInfo()};
                animationEffect.Effect.SetupEffect();
            } else {
                animationEffect = info.AnimationEffects[animationInfo.Effect];
            }

            Effect.OWAnimWriter owAnimWriter = new Effect.OWAnimWriter();
            string animationEffectDir = Path.Combine(path, Model.AnimationEffectDir, animationInfo.GetNameIndex());
            string animationEffectFile =
                Path.Combine(animationEffectDir, $"{animationInfo.GetNameIndex()}{owAnimWriter.Format}");
            CreateDirectoryFromFile(animationEffectFile);
            using (Stream fileStream = new FileStream(animationEffectFile, FileMode.Create)) {
                fileStream.SetLength(0);
                Dictionary<ulong, List<STUVoiceLineInstance>> svceLines = Effect.GetSVCELines(animationEffect.Effect);
                owAnimWriter.Write(fileStream, info, animationInfo, animationEffect, model, svceLines);
            }

            if (animationEffect.GUID != 0) {
                SaveEffectExtras(flags, animationEffectDir, info, animationEffect.Effect);
            }
        }

        public static void SaveEffectExtras(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            EffectParser.EffectInfo effectInfo) {
            string soundDirectory = Path.Combine(path, "Sounds");
            HashSet<ulong> done = new HashSet<ulong>();
            foreach (EffectParser.OSCEInfo osceInfo in effectInfo.OSCEs) {
                if (osceInfo.Sound == 0 || done.Contains(osceInfo.Sound)) continue;
                SaveSound(flags, soundDirectory, info, osceInfo.Sound);
                done.Add(osceInfo.Sound);
            }
        }

        public static void SaveSound(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong sound) {
            FindLogic.Combo.SoundInfoNew soundInfo = info.Sounds[sound];
            string soundDir = Path.Combine(path, soundInfo.GetName());
            CreateDirectoryFromFile(soundDir + "\\harrypotter.png");
            HashSet<ulong> done = new HashSet<ulong>();
            foreach (KeyValuePair<uint, ulong> soundPair in soundInfo.Sounds) {
                if (done.Contains(soundPair.Value)) continue;
                SaveSoundFile(flags, soundDir, info, soundPair.Value, false);
                done.Add(soundPair.Value);
            }
        }

        public static void SaveEffect(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong effect) {
            Effect.OWEffectWriter effectWriter = new Effect.OWEffectWriter();
            FindLogic.Combo.EffectInfoCombo effectInfo = info.Effects[effect];
            string effectDirectory = Path.Combine(path, "Effects", effectInfo.GetName());
            string effectFile = Path.Combine(effectDirectory, $"{GUID.LongKey(effect):X12}{effectWriter.Format}");
            CreateDirectoryFromFile(effectFile);

            Dictionary<ulong, List<STUVoiceLineInstance>> svceLines = Effect.GetSVCELines(effectInfo.Effect);

            // foreach (KeyValuePair<ulong,List<STUVoiceLineInstance>> svceLine in svceLines) {
            //     foreach (STUVoiceLineInstance voiceLineInstance in svceLine.Value) {
            //         foreach (STUSoundWrapper wrapper in new [] {voiceLineInstance.SoundContainer.Sound1, 
            //             voiceLineInstance.SoundContainer.Sound2, voiceLineInstance.SoundContainer.Sound3, 
            //             voiceLineInstance.SoundContainer.Sound4}) {
            //             if (wrapper != null) {
            //                 Sound.Save(flags, $"{effect}\\Sounds\\{svceLine.Key}", new Dictionary<ulong, List<SoundInfo>> {{0, new List<SoundInfo> {new SoundInfo {GUID = wrapper.SoundResource}}}}, false);
            //             }
            //         }
            //     }
            // }
            SaveEffectExtras(flags, effectDirectory, info, effectInfo.Effect);

            using (Stream effectOutputStream = File.OpenWrite(effectFile)) {
                effectOutputStream.SetLength(0);
                effectWriter.Write(effectOutputStream, effectInfo, info, svceLines);
            }
        }

        public static void SaveModel(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong model) {
            Model.OWModelWriter14 modelWriter = new Model.OWModelWriter14();
            FindLogic.Combo.ModelInfoNew modelInfo = info.Models[model];
            string modelDirectory = Path.Combine(path, "Models", modelInfo.GetName());
            string modelPath = Path.Combine(modelDirectory, $"{modelInfo.GetNameIndex()}{modelWriter.Format}");
            CreateDirectoryFromFile(modelPath);
            using (Stream modelOutputStream = File.OpenWrite(modelPath)) {
                modelOutputStream.SetLength(0);
                modelWriter.Write(modelOutputStream, info, modelInfo);
            }

            foreach (ulong modelModelLook in modelInfo.ModelLooks) {
                SaveModelLook(flags, modelDirectory, info, modelModelLook);
            }

            foreach (ulong looseMaterial in modelInfo.LooseMaterials) {
                SaveMaterial(flags, modelDirectory, info, looseMaterial);
            }

            foreach (ulong modelAnimation in modelInfo.Animations) {
                SaveAnimation(flags, modelDirectory, info, modelAnimation, model);
            }
        }

        public static void SaveModelLook(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info,
            ulong modelLook) {
            Model.OWMatWriter14 materialWriter = new Model.OWMatWriter14();
            FindLogic.Combo.ModelLookInfo modelLookInfo = info.ModelLooks[modelLook];
            string modelLookPath =
                Path.Combine(path, "ModelLooks", $"{modelLookInfo.GetNameIndex()}{materialWriter.Format}");
            CreateDirectoryFromFile(modelLookPath);
            using (Stream modelLookOutputStream = File.OpenWrite(modelLookPath)) {
                modelLookOutputStream.SetLength(0);
                materialWriter.Write(modelLookOutputStream, info, modelLookInfo);
            }

            foreach (ulong modelLookMaterial in modelLookInfo.Materials) {
                SaveMaterial(flags, path, info, modelLookMaterial);
            }
        }

        public static void SaveMaterial(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong material) {
            FindLogic.Combo.MaterialInfo materialInfo = info.Materials[material];
            FindLogic.Combo.MaterialDataInfo materialDataInfo = info.MaterialDatas[materialInfo.MaterialData];

            Model.OWMatWriter14 materialWriter = new Model.OWMatWriter14();

            string textureDirectory = Path.Combine(path, "Textures");
            string materialPath =
                Path.Combine(path, "Materials", $"{materialInfo.GetNameIndex()}{materialWriter.Format}");
            CreateDirectoryFromFile(materialPath);
            using (Stream materialOutputStream = File.OpenWrite(materialPath)) {
                materialOutputStream.SetLength(0);
                materialWriter.Write(materialOutputStream, info, materialInfo);
            }

            foreach (KeyValuePair<ulong, ImageDefinition.ImageType> texture in materialDataInfo.Textures) {
                SaveTexture(flags, textureDirectory, info, texture.Key);
            }
        }

        public static void SaveLooseTextures(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info) {
            foreach (FindLogic.Combo.TextureInfoNew textureInfo in info.Textures.Values) {
                if (!textureInfo.Loose) continue;
                SaveTexture(flags, path, info, textureInfo.GUID);
            }
        }

        public static void SaveAllMaterials(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info) {
            foreach (ulong material in info.Materials.Keys) {
                SaveMaterial(flags, path, info, material);
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

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static class TextureConfig {
            internal const int FOURCC_DX10 = 808540228;
            internal const int FOURCC_ATI1 = 826889281;
            internal const int FOURCC_ATI2 = 843666497;
            internal static readonly int[] DXGI_BC4 = { 79, 80, 91 };
            internal static readonly int[] DXGI_BC5 = { 82, 83, 84 };
        }

        public static void SaveTexture(ICLIFlags flags, string path, FindLogic.Combo.ComboInfo info, ulong texture) {
            bool convertTextures = true;
            string convertType = "dds";

            if (flags is ExtractFlags extractFlags) {
                convertTextures = extractFlags.ConvertTextures  && !extractFlags.Raw;
                convertType = extractFlags.ConvertTexturesType.ToLowerInvariant();
                if (extractFlags.SkipTextures) return;
            }
            path += Path.DirectorySeparatorChar;
            CreateDirectoryFromFile(path);

            FindLogic.Combo.TextureInfoNew textureInfo = info.Textures[texture];
            string filePath = Path.Combine(path, $"{textureInfo.GetNameIndex()}");

            if (!convertTextures) {
                using (Stream textureStream = OpenFile(textureInfo.GUID))
                    WriteFile(textureStream, $"{filePath}.004");

                if (!textureInfo.UseData) return;
                using (Stream textureStream = OpenFile(textureInfo.DataGUID))
                    WriteFile(textureStream, $"{filePath}.04D");
            } else {
                Stream convertedStream;
                TextureHeader header;
                if (textureInfo.UseData) {
                    OWLib.Texture textObj = new OWLib.Texture(OpenFile(textureInfo.GUID), OpenFile(textureInfo.DataGUID));
                    convertedStream = textObj.Save();
                    header = textObj.Header;
                } else {
                    TextureLinear textObj = new TextureLinear(OpenFile(textureInfo.GUID));
                    convertedStream = textObj.Save();
                    header = textObj.Header;
                }
                if (convertedStream == null) return;
                
                // conversion utils
                uint fourCC = header.Format().ToPixelFormat().fourCC;
                bool isBcffValid = TextureConfig.DXGI_BC4.Contains((int) header.format) || 
                                   TextureConfig.DXGI_BC5.Contains((int) header.format) ||
                                   fourCC == TextureConfig.FOURCC_ATI1 || fourCC == TextureConfig.FOURCC_ATI2;

                ImageFormat imageFormat = null;
                
                if (convertType == "tif") imageFormat = ImageFormat.Tiff;
                
                // if (convertType == "tga") imageFormat = Im.... oh
                // so there is no TGA image format.
                // guess the TGA users are stuck with the DirectXTex stuff for now.

                convertedStream.Position = 0;
                if (isBcffValid && imageFormat != null && convertedStream.Length != 0) {
                    BlockDecompressor decompressor = new BlockDecompressor(convertedStream);
                    decompressor.CreateImage();
                    decompressor.Image.Save($"{filePath}.{convertType}", imageFormat);
                    return;
                }

                convertedStream.Position = 0;
                if (convertType == "tga" || convertType == "tif" || convertType == "dds") {  // we need the dds for tif conversion
                    WriteFile(convertedStream, $"{filePath}.dds");
                }
                convertedStream.Close();

                if (convertType == "tif" || convertType == "tga") {
                    Process pProcess = new Process {
                        StartInfo = {
                            FileName = "Third Party\\texconv.exe",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            Arguments =
                                $"\"{filePath}.dds\" -y -wicmulti -nologo -m 1 -ft {convertType} -f R8G8B8A8_UNORM -o \"{path}"
                        }
                    };
                    // -wiclossless?
                    
                    // erm, so if you add an end quote to this then it breaks.
                    // but start one on it's own is fine (we need something for "Winged Victory")
                    
                    Task.Run( () => {  Console.WriteLine("Task {0} (asyncTask) executing on Thread {1}",
                            Task.CurrentId,
                            Thread.CurrentThread.ManagedThreadId);
                        pProcess.Start();
                        // pProcess.WaitForExit(); // not using this is kinda dangerous but I don't care
                        // when texconv writes with to the console -nologo is has done/failed conversion
                        string line = pProcess.StandardOutput.ReadLine();
                        if (line?.Contains($"{filePath}.dds FAILED") == false) {  // fallback if convert fails
                            File.Delete($"{filePath}.dds");
                        }
                    });
                    
                    long time_ago; //in a galaxy far away
                    
                    
                    
                    
                    
                    
                    // ... there was slow conversion.
                    // then I pooped out that Task system you can see above.
                    // yes it is basically cheesing past the problem.
                }
            }
        }

        public static void SaveSoundFile(ICLIFlags flags, string directory, FindLogic.Combo.ComboInfo info, ulong soundFile,
            bool voice) {
            bool convertWem = false;
            if (flags is ExtractFlags extractFlags) {
                convertWem = extractFlags.ConvertSound && !extractFlags.Raw;
                if (extractFlags.SkipSound) return;
            }
            
            FindLogic.Combo.SoundFileInfo soundFileInfo = voice ? info.VoiceSoundFiles[soundFile] : info.SoundFiles[soundFile];
            
            string outputFile = Path.Combine(directory, $"{soundFileInfo.GetName()}.wem");
            string outputFileOgg = Path.ChangeExtension(outputFile, "ogg");
            CreateDirectoryFromFile(outputFile);

            using (Stream soundStream = OpenFile(soundFile)) {
                if (soundStream == null) return;
                using (Stream outputStream = File.OpenWrite(outputFile)) {
                    soundStream.CopyTo(outputStream);
                }
                // ConvertLogic.Sound.WwiseRIFFVorbis vorbis =
                //     new ConvertLogic.Sound.WwiseRIFFVorbis(soundStream,
                //         "Third Party\\packed_codebooks_aoTuV_603.bin");
                // using (Stream outputStream = File.OpenWrite(outputFileOgg+"2")) {
                //     vorbis.ConvertToOgg(outputStream);
                // }
            }

            if (!convertWem) return;
            Process pProcess = new Process {
                StartInfo = {
                    FileName = "Third Party\\ww2ogg.exe",
                    Arguments =
                        $"\"{outputFile}\" --pcb \"Third Party\\packed_codebooks_aoTuV_603.bin\" -o \"{outputFileOgg}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                }
            };
            Process pProcess2 = new Process {
                StartInfo = {
                    FileName = "Third Party\\revorb.exe",
                    Arguments = $"\"{outputFileOgg}\"",
                    UseShellExecute = false
                }
            };
            
            // i'm sorry
            Task.Run( () => {  Console.WriteLine("Task {0} (asyncTask) executing on Thread {1}",
                    Task.CurrentId,
                    Thread.CurrentThread.ManagedThreadId);
                pProcess.Start();
                pProcess.WaitForExit();
                pProcess2.Start();
                File.Delete(outputFile);
            });
        }
    }
}