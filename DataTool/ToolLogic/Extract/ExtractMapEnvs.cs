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
using TankLib.STU;
using TankLib.STU.Types;

namespace DataTool.ToolLogic.Extract
{
    [Tool("extract-map-envs", Description = "Extract map environment data", CustomFlags = typeof(ExtractMapEnvFlags))]
    public class ExtractMapEnvs : QueryParser, ITool
    {
        public void Parse(ICLIFlags toolFlags)
        {
            SaveMaps(toolFlags);
        }

        private string OCIOChunk(MapHeader info, string fname)
        {
            return $@"  - !<Look>
    name: {GetValidFilename($"{info.GetName().ToUpperInvariant()}_{teResourceGUID.Index(info.MapGUID):X}")}
    process_space: linear
    transform: !<GroupTransform>
      children:
        - !<FileTransform> {{src: {fname}.spi3d, interpolation: linear}}";
        }

        public void SaveMaps(ICLIFlags toolFlags)
        {
            string basePath;
            if (toolFlags is ExtractMapEnvFlags flags)
            {
                basePath = flags.OutputPath;
            }
            else
            {
                throw new Exception("no output path");
            }

            basePath = Path.Combine(basePath, "Environments");

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            if (!flags.SkipMapEnvironmentLUT && File.Exists(Path.Combine(basePath, "SPILUT", "config.ocio")))
            {
                File.Delete(Path.Combine(basePath, "SPILUT", "config.ocio"));
            }

            HashSet<KeyValuePair<ulong, string>> done = new HashSet<KeyValuePair<ulong, string>>();
            foreach (ulong metaKey in TrackedFiles[0x9F])
            {
                STUMapHeader map = GetInstance<STUMapHeader>(metaKey);
                if (map == null)
                {
                    continue;
                }

                MapHeader mapInfo = GetMap(metaKey);

                ulong dataKey = map.m_map;

                //if (teResourceGUID.Index(dataKey) != 0x7A4) continue;

                var mapName = GetValidFilename($"{mapInfo.GetName()}_{teResourceGUID.Index(mapInfo.MapGUID):X}");
                string fname = $"ow_map_{mapName}";

                var reflectionData = Map.GetPlaceableData(map, Enums.teMAP_PLACEABLE_TYPE.REFLECTIONPOINT);
                if (reflectionData != null) {
                    foreach (var placeable in reflectionData.Placeables ?? Array.Empty<IMapPlaceable>()) {
                        if (!(placeable is teMapPlaceableReflectionPoint reflectionPoint)) continue;
                        if (done.Add(new KeyValuePair<ulong, string>(reflectionPoint.Header.Texture1, mapInfo.Name + "cube"))) {
                            SaveTex(flags, basePath, Path.Combine("Cubemap", fname), reflectionPoint.Header.Texture1.ToString(), reflectionPoint.Header.Texture1);
                        }

                        if (done.Add(new KeyValuePair<ulong, string>(reflectionPoint.Header.Texture2, mapInfo.Name + "cube"))) {
                            SaveTex(flags, basePath, Path.Combine("Cubemap", fname), reflectionPoint.Header.Texture2.ToString(), reflectionPoint.Header.Texture2);
                        }
                    }
                }

                using (Stream data = OpenFile(dataKey)) {
                    if (data != null) {
                        using (BinaryReader dataReader = new BinaryReader(data)) {
                            teMap env = dataReader.Read<teMap>();

                            // using (Stream lightingStream = OpenFile(env.BakedLighting)) {
                            //    teLightingManifest lightingManifest = new teLightingManifest(lightingStream);   
                            //}

                            if (!flags.SkipMapEnvironmentSound && done.Add(new KeyValuePair<ulong, string>(env.MapEnvironmentSound, mapInfo.Name)))
                                SaveSound(flags, basePath, Path.Combine("Sound", mapName), env.MapEnvironmentSound);
                            if (!flags.SkipMapEnvironmentLUT && done.Add(new KeyValuePair<ulong, string>(env.LUT, mapInfo.Name))) {
                                SaveTex(flags, basePath, "LUT", fname + env.LUT, env.LUT);
                                SaveLUT(flags, basePath, "SPILUT", fname + env.LUT, env.LUT, Path.Combine(basePath, "SPILUT", "config.ocio"), mapInfo);
                            }

                            if (!flags.SkipMapEnvironmentBlendCubemap && done.Add(new KeyValuePair<ulong, string>(env.BlendEnvironmentCubemap, mapInfo.Name)))
                                SaveTex(flags, basePath, "BlendCubemap", fname + env.BlendEnvironmentCubemap, env.BlendEnvironmentCubemap);
                            if (!flags.SkipMapEnvironmentGroundCubemap && done.Add(new KeyValuePair<ulong, string>(env.GroundEnvironmentCubemap, mapInfo.Name)))
                                SaveTex(flags, basePath, "GroundCubemap", fname + env.GroundEnvironmentCubemap, env.GroundEnvironmentCubemap);
                            if (!flags.SkipMapEnvironmentSkyCubemap && done.Add(new KeyValuePair<ulong, string>(env.SkyEnvironmentCubemap, mapInfo.Name)))
                                SaveTex(flags, basePath, "SkyCubemap", fname + env.SkyEnvironmentCubemap, env.SkyEnvironmentCubemap);
                            if (!flags.SkipMapEnvironmentSkybox && done.Add(new KeyValuePair<ulong, string>(env.SkyboxModel + env.SkyboxModelLook, mapInfo.Name)))
                                SaveMdl(flags, basePath, Path.Combine("Skybox", mapName), env.SkyboxModel, env.SkyboxModelLook);
                            if (!flags.SkipMapEnvironmentEntity && done.Add(new KeyValuePair<ulong, string>(env.EntityDefinition, mapInfo.Name)))
                                SaveEntity(flags, basePath, Path.Combine("Entity", mapName), env.EntityDefinition);
                        }
                    }
                }

                InfoLog("Saved Environment data for {0}", mapInfo.GetUniqueName());
            }
        }

        private void SaveEntity(ExtractFlags flags, string basePath, string part, ulong key)
        {
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, key);

            string soundDirectory = Path.Combine(basePath, part, "Sound");

            foreach (KeyValuePair<ulong, FindLogic.Combo.SoundFileInfo> soundFile in info.SoundFiles)
            {
                SaveLogic.Combo.SaveSoundFile(flags, soundDirectory, info, soundFile.Key, false);
            }

            foreach (KeyValuePair<ulong, FindLogic.Combo.SoundFileInfo> soundFile in info.VoiceSoundFiles)
            {
                SaveLogic.Combo.SaveSoundFile(flags, soundDirectory, info, soundFile.Key, true);
            }

            SaveLogic.Combo.Save(flags, Path.Combine(basePath, part), info);
        }

        private void SaveLUT(ExtractFlags flags, string basePath, string part, string fname, ulong key, string ocioPath, MapHeader map)
        {
            if (!Directory.Exists(Path.Combine(basePath, part)))
            {
                Directory.CreateDirectory(Path.Combine(basePath, part));
            }

            if (key == 0)
            {
                return;
            }

            using (Stream lutStream = OpenFile(key))
            {
                if (lutStream == null)
                {
                    return;
                }

                lutStream.Position = 128;

                string lut = LUT.SPILUT1024x32(lutStream);
                using (Stream spilut = File.OpenWrite(Path.Combine(basePath, part, $"{fname}.spi3d")))
                using (TextWriter spilutWriter = new StreamWriter(spilut))
                {
                    spilutWriter.WriteLine(lut);
                    using (TextWriter ocioWriter = File.AppendText(Path.Combine(ocioPath)))
                    {
                        ocioWriter.WriteLine(OCIOChunk(map, fname));
                    }
                }
            }
        }

        private void SaveSound(ExtractFlags flags, string basePath, string part, ulong key)
        {
            STU_F3EB00D4 stu = GetInstance<STU_F3EB00D4>(key); // todo: should be named
            if(stu == null || stu.m_B3685B0D == 0)
            {
                return;
            }
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, stu.m_B3685B0D);
            SaveLogic.Combo.SaveSound(flags, Path.Combine(basePath, part), info, stu.m_B3685B0D);
        }

        private void SaveMdl(ExtractFlags flags, string basePath, string part, ulong model, ulong modelLook)
        {
            if(model == 0 || modelLook == 0)
            {
                return;
            }

            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, model);
            FindLogic.Combo.Find(info, modelLook);
            SaveLogic.Combo.Save(flags, Path.Combine(basePath, part), info);
            SaveLogic.Combo.SaveAllModelLooks(flags, Path.Combine(basePath, part), info);
            SaveLogic.Combo.SaveAllMaterials(flags, Path.Combine(basePath, part), info);
        }

        private void SaveTex(ExtractFlags flags, string basePath, string part, string filename, ulong key)
        {
            if(key == 0)
            {
                return;
            }

            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, key);
            info.SetTextureName(key, filename);
            SaveLogic.Combo.SaveTexture(flags, Path.Combine(basePath, part), info, key);
        }
    }
    
    public class ExtractMapEnvFlags : ExtractFlags
    {
        [CLIFlag(Default = false, Flag = "skip-map-env-sound", Help = "Skip map Environment sound extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentSound;

        [CLIFlag(Default = false, Flag = "skip-map-env-lut", Help = "Skip map Environment lut extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentLUT;

        [CLIFlag(Default = false, Flag = "skip-map-env-blend", Help = "Skip map Environment blend cubemap extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentBlendCubemap;

        [CLIFlag(Default = false, Flag = "skip-map-env-ground", Help = "Skip map Environment ground cubemap extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentGroundCubemap;

        [CLIFlag(Default = false, Flag = "skip-map-env-sky", Help = "Skip map Environment sky cubemap extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentSkyCubemap;

        [CLIFlag(Default = false, Flag = "skip-map-env-skybox", Help = "Skip map Environment skybox extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentSkybox;

        [CLIFlag(Default = false, Flag = "skip-map-env-entity", Help = "Skip map Environment entity extraction", Parser = new[] { "DataTool.Flag.Converter", "CLIFlagBoolean" })]
        public bool SkipMapEnvironmentEntity;
    }
}