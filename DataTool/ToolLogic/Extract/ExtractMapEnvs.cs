using System;
using System.Collections.Generic;
using DataTool.Flag;
using DataTool.Helper;
using OWLib;
using STULib.Types;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.Logger;
using static DataTool.Helper.IO;
using static DataTool.ToolLogic.List.ListMaps;
using System.IO;
using OWLib.Types;
using STULib;
using STULib.Types.Dump;

namespace DataTool.ToolLogic.Extract
{
    [Tool("extract-map-envs", Description = "Extract map enviornment data", TrackTypes = new ushort[] { 0x9F }, CustomFlags = typeof(ExtractMapEnvFlags))]
    public class ExtractMapEnvs : QueryParser, ITool
    {
        public void IntegrateView(object sender)
        {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags)
        {
            SaveMaps(toolFlags);
        }

        private string OCIOChunk(MapInfo info, string fname)
        {
            return $@"  - !<Look>
    name: {GetValidFilename($"{info.NameB.ToUpperInvariant()}_{GUID.Index(info.MetadataGUID):X}")}
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

            basePath = Path.Combine(basePath, "Enviornments");

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            if (!flags.SkipMapEnviornmentLUT && File.Exists(Path.Combine(basePath, "SPILUT", "config.ocio")))
            {
                File.Delete(Path.Combine(basePath, "SPILUT", "config.ocio"));
            }

            HashSet<KeyValuePair<ulong, string>> done = new HashSet<KeyValuePair<ulong, string>>();
            foreach (ulong metaKey in TrackedFiles[0x9F])
            {
                STUMap map = GetInstance<STUMap>(metaKey);
                if (map == null)
                {
                    continue;
                }

                MapInfo mapInfo = GetMap(metaKey);
                mapInfo.Name = mapInfo.Name ?? "Title Screen";
                mapInfo.NameB = mapInfo.NameB ?? mapInfo.Name;

                ulong dataKey = map.MapDataResource1;

                using (Stream data = OpenFile(dataKey))
                {
                    if (data == null)
                    {
                        continue;
                    }

                    using (BinaryReader dataReader = new BinaryReader(data))
                    {
                        MapEnviornment env = dataReader.Read<MapEnviornment>();

                        string fname = $"ow_map_{GetValidFilename($"{mapInfo.NameB}_{GUID.Index(mapInfo.MetadataGUID):X}")}";

                        if (!flags.SkipMapEnviornmentSound && done.Add(new KeyValuePair<ulong, string>(env.MapEnviornmentSound, mapInfo.Name)))
                            SaveSound(flags, basePath, Path.Combine("Sound", GetValidFilename($"{mapInfo.NameB}_{GUID.Index(mapInfo.MetadataGUID):X}")), env.MapEnviornmentSound);
                        if (!flags.SkipMapEnviornmentLUT && done.Add(new KeyValuePair<ulong, string>(env.LUT, mapInfo.Name)))
                        {
                            SaveTex(flags, basePath, "LUT", fname, env.LUT);
                            SaveLUT(flags, basePath, "SPILUT", fname, env.LUT, Path.Combine(basePath, "SPILUT", "config.ocio"), mapInfo);
                        }
                        if (!flags.SkipMapEnviornmentBlendCubemap && done.Add(new KeyValuePair<ulong, string>(env.BlendEnviornmentCubemap, mapInfo.Name)))
                            SaveTex(flags, basePath, "BlendCubemap", fname, env.BlendEnviornmentCubemap);
                        if (!flags.SkipMapEnviornmentGroundCubemap && done.Add(new KeyValuePair<ulong, string>(env.GroundEnviornmentCubemap, mapInfo.Name)))
                            SaveTex(flags, basePath, "GroundCubemap", fname, env.GroundEnviornmentCubemap);
                        if (!flags.SkipMapEnviornmentSkyCubemap && done.Add(new KeyValuePair<ulong, string>(env.SkyEnviornmentCubemap, mapInfo.Name)))
                            SaveTex(flags, basePath, "SkyCubemap", fname, env.SkyEnviornmentCubemap);
                        if (!flags.SkipMapEnviornmentSkybox && done.Add(new KeyValuePair<ulong, string>(env.SkyboxModel ^ env.SkyboxModelLook, mapInfo.Name)))
                            SaveMdl(flags, basePath, Path.Combine("Skybox", GetValidFilename($"{mapInfo.NameB}_{GUID.Index(mapInfo.MetadataGUID):X}")), env.SkyboxModel, env.SkyboxModelLook);
                        if (!flags.SkipMapEnviornmentEntity && done.Add(new KeyValuePair<ulong, string>(env.StateScript, mapInfo.Name)))
                            SaveEntity(flags, basePath, Path.Combine("Entity", GetValidFilename($"{mapInfo.NameB}_{GUID.Index(mapInfo.MetadataGUID):X}")), env.StateScript);

                        InfoLog("Saved enviornment data for {0}", mapInfo.NameB);
                    }
                }
            }
        }

        private void SaveEntity(ExtractFlags flags, string basePath, string part, ulong key)
        {
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            info.SaveRuntimeData = new FindLogic.Combo.ComboSaveRuntimeData { Threads = false };
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

        private void SaveLUT(ExtractFlags flags, string basePath, string part, string fname, ulong key, string ocioPath, MapInfo mapInfo)
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
                        ocioWriter.WriteLine(OCIOChunk(mapInfo, fname));
                    }
                }
            }
        }

        private void SaveSound(ExtractFlags flags, string basePath, string part, ulong key)
        {
            STU_F3EB00D4 stu = GetInstance<STU_F3EB00D4>(key);
            if(stu == null || stu.SoundResource == 0)
            {
                return;
            }
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            info.SaveRuntimeData = new FindLogic.Combo.ComboSaveRuntimeData { Threads = false };
            FindLogic.Combo.Find(info, stu.SoundResource);
            SaveLogic.Combo.SaveSound(flags, Path.Combine(basePath, part), info, stu.SoundResource);
        }

        private void SaveMdl(ExtractFlags flags, string basePath, string part, ulong model, ulong modelLook)
        {
            if(model == 0 || modelLook == 0)
            {
                return;
            }

            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            info.SaveRuntimeData = new FindLogic.Combo.ComboSaveRuntimeData { Threads = false };
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
            info.SaveRuntimeData = new FindLogic.Combo.ComboSaveRuntimeData { Threads = false };
            FindLogic.Combo.Find(info, key);
            info.SetTextureName(key, filename);
            SaveLogic.Combo.SaveTexture(flags, Path.Combine(basePath, part), info, key);
        }
    }
}
