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

namespace DataTool.ToolLogic.Extract
{
    [Tool("extract-lut", Description = "Extract map LUTs", TrackTypes = new ushort[] { 0x39 }, CustomFlags = typeof(ExtractFlags))]
    public class ExtractLUT : QueryParser, ITool, IQueryParser
    {
        public void IntegrateView(object sender)
        {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags)
        {
            SaveMaps(toolFlags);
        }

        protected override void QueryHelp(List<QueryType> types)
        {
            IndentHelper indent = new IndentHelper();
            Log("Please specify what you want to extract:");
            Log($"{indent + 1}Command format: \"{{map name}}\" ");
            Log($"{indent + 1}Each query should be surrounded by \", and individual queries should be seperated by spaces");


            Log($"{indent + 1}Maps can be listed using the \"list-maps\" mode");
            Log($"{indent + 1}All map names are in your selected locale");

            Log("\r\nExample commands: ");
            Log($"{indent + 1}\"Kings Row\"");
            Log($"{indent + 1}\"Ilios\" \"Oasis\"");
        }

        private string OCIOChunk(MapInfo info)
        {
            return $@"  - !<Look>
    name: {GetValidFilename(info.UniqueName.Replace(':', '-'))}
    process_space: linear
    transform: !<GroupTransform>
      children:
        - !<FileTransform> {{src: ow_map_{GetValidFilename(info.UniqueName.Replace(' ', '_'))}.spi3d, interpolation: linear}}";
        }

        public void SaveMaps(ICLIFlags toolFlags)
        {
            string basePath;
            if (toolFlags is ExtractFlags flags)
            {
                basePath = flags.OutputPath;
            }
            else
            {
                throw new Exception("no output path");
            }

            basePath = Path.Combine(basePath, "LUT");

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            using (Stream ocioStream = File.OpenWrite(Path.Combine(basePath, "config.ocio")))
            using (TextWriter ocioWriter = new StreamWriter(ocioStream))
            {

                Dictionary<string, Dictionary<string, ParsedArg>> parsedTypes = ParseQuery(flags, QueryTypes, QueryNameOverrides);
                HashSet<ulong> done = new HashSet<ulong>();
                foreach (ulong key in TrackedFiles[0x39])
                {
                    STUMapDataBinding binding = GetInstance<STUMapDataBinding>(key);
                    if (binding == null) continue;

                    for (int i = 0; i < binding.MapDatas.Length; ++i)
                    {
                        ulong dataKey = binding.MapDatas[i];
                        ulong metaKey = binding.MapMetadatas[i];

                        STUMap map = GetInstance<STUMap>(metaKey);
                        if (map == null)
                        {
                            continue;
                        }

                        MapInfo mapInfo = GetMap(metaKey);
                        mapInfo.Name = mapInfo.Name ?? "Title Screen";

                        if (parsedTypes != null)
                        {
                            Dictionary<string, ParsedArg> config = new Dictionary<string, ParsedArg>();
                            foreach (string name in new[] { mapInfo.Name, mapInfo.NameB, mapInfo.UniqueName, GUID.Index(map.MapDataResource1).ToString("X"), "*" })
                            {
                                if (name == null) continue;
                                string theName = name.ToLowerInvariant();
                                if (!parsedTypes.ContainsKey(theName)) continue;
                                foreach (KeyValuePair<string, ParsedArg> parsedArg in parsedTypes[theName])
                                {
                                    if (config.ContainsKey(parsedArg.Key))
                                    {
                                        config[parsedArg.Key] = config[parsedArg.Key].Combine(parsedArg.Value);
                                    }
                                    else
                                    {
                                        config[parsedArg.Key] = parsedArg.Value.Combine(null); // clone for safety
                                    }
                                }
                            }

                            if (config.Count == 0) continue;
                        }

                        if (!done.Add(binding.MapDatas[i]))
                        {
                            continue;
                        }

                        using (Stream data = OpenFile(dataKey))
                        {
                            if (data == null)
                            {
                                continue;
                            }

                            using (BinaryReader dataReader = new BinaryReader(data))
                            {
                                MapEnviornment env = dataReader.Read<MapEnviornment>();

                                using (Stream lutStream = OpenFile(env.LUT))
                                {
                                    if (lutStream == null)
                                    {
                                        continue;
                                    }

                                    FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
                                    info.SaveRuntimeData = new FindLogic.Combo.ComboSaveRuntimeData { Threads = false };
                                    info.Textures.Add(env.LUT, new FindLogic.Combo.TextureInfoNew(env.LUT)
                                    {
                                        Name = $"ow_map_{GetValidFilename(mapInfo.UniqueName.Replace(' ', '_'))}"
                                    });
                                    SaveLogic.Combo.SaveTexture(flags, basePath, info, env.LUT);

                                    lutStream.Position = 128;

                                    string lut = LUT.SPILUT1024x32(lutStream);
                                    using (Stream spilut = File.OpenWrite(Path.Combine(basePath, $"ow_map_{GetValidFilename(mapInfo.UniqueName.Replace(' ', '_'))}.spi3d")))
                                    using (TextWriter spilutWriter = new StreamWriter(spilut))
                                    {
                                        spilutWriter.WriteLine(lut);
                                        ocioWriter.WriteLine(OCIOChunk(mapInfo));
                                        InfoLog("Saved LUT for {0}", mapInfo.UniqueName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public List<QueryType> QueryTypes => new List<QueryType> { new QueryType { Name = "MapFakeType" } };
        public Dictionary<string, string> QueryNameOverrides => new Dictionary<string, string>();
    }
}
