using System;
using System.Collections.Generic;
using DataTool.Flag;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.Logger;
using static DataTool.Helper.IO;
using static DataTool.ToolLogic.List.ListMaps;
using System.IO;
using DataTool.ConvertLogic;
using DataTool.DataModels;
using DataTool.SaveLogic;
using TankLib;
using TankLib.Math;
using TankLib.STU.Types;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-map-envs", Description = "Extract map environment data", CustomFlags = typeof(ExtractFlags))]
    public class ExtractMapEnvs : QueryParser, ITool {
        public void Parse(ICLIFlags toolFlags) {
            SaveMaps(toolFlags);
        }

        private string OCIOChunk(MapHeader info, string fname) {
            return $@"  - !<Look>
    name: {GetValidFilename($"{info.GetName().ToUpperInvariant()}_{teResourceGUID.Index(info.MapGUID):X}")}
    process_space: linear
    transform: !<GroupTransform>
      children:
        - !<FileTransform> {{src: {fname}.spi3d, interpolation: linear}}";
        }

        public void SaveMaps(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            basePath = Path.Combine(basePath, "Environments");

            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();

            foreach (ulong key in TrackedFiles[0x9F]) {
                STUMapHeader mapHeader = GetInstance<STUMapHeader>(key);
                if (mapHeader == null) continue;
                MapHeader mapInfo = GetMap(key);
                string mapName = mapInfo.GetName();
                string mapPath = Path.Combine(basePath, GetValidFilename(mapName));

                for (int i = 0; i < mapHeader.m_D97BC44F.Length; i++) {
                    var variantModeInfo = mapHeader.m_D97BC44F[i];
                    var variantResultingMap = mapHeader.m_78715D57[i];
                    var variantGUID = variantResultingMap.m_BF231F12;

                    Stream mapStream = OpenFile(variantGUID);
                    if (mapStream == null) continue;

                    string variantName = Map.GetVariantName(variantModeInfo, variantResultingMap);
                    string variantPath = Path.Combine(mapPath, variantName);

                    using (BinaryReader reader = new BinaryReader(mapStream)) {
                        const long lightingDataOffset = 160;
                        mapStream.Position = lightingDataOffset + 228;
                        ushort envScenarioCount = reader.ReadUInt16();
                        mapStream.Position = lightingDataOffset + 240;
                        uint envScenarioOffset = reader.ReadUInt32();
                        mapStream.Position = lightingDataOffset + envScenarioOffset;

                        for (int j = 0; j < envScenarioCount; j++) {
                            mapStream.Position += 40; // 5x u64
                            ulong envState = reader.ReadUInt64();
                            STU_CD1ED5FE envStateInst = GetInstance<STU_CD1ED5FE>(envState);

                            // Sky
                            if (envStateInst.m_B3F27D37.TryGetValue(7, out var sky)) {
                                var skyAspect = (STU_70BAB99C) sky;
                                ulong skyModel = skyAspect.m_EAE71612;
                                ulong skyLook = skyAspect.m_FF76B5BA;

                                SaveMdl(flags, variantPath, "Sky", skyModel, skyLook);
                            }

                            // Color Grading
                            if (envStateInst.m_B3F27D37.TryGetValue(3, out var grading)) {
                                var gradingAspect = (STU_40181BF1) grading;
                                ulong lutKey = gradingAspect.m_450286A4;
                                SaveTex(flags, variantPath, "Color Grading", teResourceGUID.AsIndexString(lutKey), lutKey);
                            }

                            // Sun
                            if (envStateInst.m_B3F27D37.TryGetValue(6, out var sun)) {
                                var sunAspect = (STU_DABD6A9B) sun;
                                teQuat rotation = sunAspect.m_rotation;
                                teQuat zUp = new teQuat(rotation.X, -rotation.Z, rotation.Y, rotation.W);
                                teVec3 euler = zUp.ToEulerAngles();

                                // :mentalcat:
                                euler.X *= 57.295779513f;
                                euler.Y *= 57.295779513f;
                                euler.Z *= 57.295779513f;

                                ulong lensFlare = sunAspect.m_F83BCB43;
                                teColorRGB color = sunAspect.m_color;
                                float intensity = sunAspect.m_A1C4B45C;

                                FindLogic.Combo.ComboInfo lensFlareInfo = new FindLogic.Combo.ComboInfo();
                                FindLogic.Combo.Find(lensFlareInfo, lensFlare);

                                var context = new Combo.SaveContext(lensFlareInfo);
                                SaveAllTextures(flags, variantPath, "Sun", context);

                                string infoSuffix = j > 0 ? j.ToString() : "";
                                string sunInfoFile = $"{Path.Combine(variantPath, "Sun")}/info{infoSuffix}.txt";
                                CreateDirectoryFromFile(sunInfoFile);

                                using (Stream f = File.OpenWrite(sunInfoFile))
                                using (TextWriter w = new StreamWriter(f)) {
                                    w.WriteLine($"Rotation (Blender): X:{euler.X - 90f}, Y:{euler.Y}, Z: {euler.Z}");
                                    w.WriteLine($"Rotation (Quat): X:{rotation.X}, Y:{rotation.Z}, Z: {rotation.Y}, W: {rotation.W}");
                                    w.WriteLine($"Color: R: {color.R}, G: {color.B}, B: {color.B}");
                                    w.WriteLine($"Intensity: {intensity}");
                                }
                            }
                        }
                    }
                }
                Log($"Saved Environment data for {mapName}");
            }
        }

        private void SaveEntity(ExtractFlags flags, string basePath, string part, ulong key) {
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, key);

            string soundDirectory = Path.Combine(basePath, part, "Sound");

            var context = new Combo.SaveContext(info);
            foreach (KeyValuePair<ulong, FindLogic.Combo.SoundFileAsset> soundFile in info.m_soundFiles) {
                SaveLogic.Combo.SaveSoundFile(flags, soundDirectory, context, soundFile.Key, false);
            }

            foreach (KeyValuePair<ulong, FindLogic.Combo.SoundFileAsset> soundFile in info.m_voiceSoundFiles) {
                SaveLogic.Combo.SaveSoundFile(flags, soundDirectory, context, soundFile.Key, true);
            }

            SaveLogic.Combo.Save(flags, Path.Combine(basePath, part), context);
        }

        private void SaveLUT(ExtractFlags flags, string basePath, string part, string fname, ulong key, string ocioPath, MapHeader map) {
            if (!Directory.Exists(Path.Combine(basePath, part))) {
                Directory.CreateDirectory(Path.Combine(basePath, part));
            }

            if (key == 0) {
                return;
            }

            using (Stream lutStream = OpenFile(key)) {
                if (lutStream == null) {
                    return;
                }

                lutStream.Position = 128;

                string lut = LUT.SPILUT1024x32(lutStream);
                using (Stream spilut = File.OpenWrite(Path.Combine(basePath, part, $"{fname}.spi3d")))
                using (TextWriter spilutWriter = new StreamWriter(spilut)) {
                    spilutWriter.WriteLine(lut);
                    using (TextWriter ocioWriter = File.AppendText(Path.Combine(ocioPath))) {
                        ocioWriter.WriteLine(OCIOChunk(map, fname));
                    }
                }
            }
        }

        private void SaveSound(ExtractFlags flags, string basePath, string part, ulong key) {
            STU_F3EB00D4 stu = GetInstance<STU_F3EB00D4>(key); // todo: should be named
            if (stu == null || stu.m_B3685B0D == 0) {
                return;
            }

            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            var context = new Combo.SaveContext(info);
            FindLogic.Combo.Find(info, stu.m_B3685B0D);
            SaveLogic.Combo.SaveSound(flags, Path.Combine(basePath, part), context, stu.m_B3685B0D);
        }

        private void SaveMdl(ExtractFlags flags, string basePath, string part, ulong model, ulong modelLook) {
            if (model == 0 || modelLook == 0) {
                return;
            }

            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, model);
            FindLogic.Combo.Find(info, modelLook, null, 
                                 new FindLogic.Combo.ComboContext { Model = model});

            var context = new Combo.SaveContext(info) {
                m_saveAnimationEffects = false
            };
            SaveLogic.Combo.Save(flags, Path.Combine(basePath, part), context);
        }

        private void SaveTex(ExtractFlags flags, string basePath, string part, string filename, ulong key) {
            if (key == 0) {
                return;
            }

            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, key);
            info.SetTextureName(key, filename);

            var context = new Combo.SaveContext(info);
            SaveLogic.Combo.SaveTexture(flags, Path.Combine(basePath, part), context, key);
        }

        private void SaveAllTextures(ExtractFlags flags, string basePath, string part, Combo.SaveContext context) {
            foreach (var tex in context.m_info.m_textures.Values) {
                Combo.SaveTexture(flags, Path.Combine(basePath, part), context, tex.m_GUID);
            }
        }
    }
}
