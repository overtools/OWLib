using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.ToolLogic.Extract;
using OWLib;
using OWLib.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;

namespace DataTool.SaveLogic {
    public class Texture {

        public static Dictionary<TextureInfo, TextureType> MergeTypes(Dictionary<TextureInfo, TextureType> first, Dictionary<TextureInfo, TextureType> second) {
            return first.Concat(second).GroupBy(d => d.Key).ToDictionary (d => d.Key, d => d.First().Value);
        }
        public static Dictionary<TextureInfo, TextureType> Save(ICLIFlags flags, string path, Dictionary<ulong, List<TextureInfo>> textures) {
            Dictionary<TextureInfo, TextureType> output = new Dictionary<TextureInfo, TextureType>();
            bool convertTextures = true;

            if (flags is ExtractFlags extractFlags) {
                convertTextures = extractFlags.ConvertTextures  && !extractFlags.Raw;
                if (extractFlags.SkipTextures) return output;
            }

            bool zeroOnly = textures.ContainsKey(0) && textures.All(x => x.Key == 0);

            foreach (KeyValuePair<ulong, List<TextureInfo>> pair in textures) {
                string rootOutput = Path.Combine(path, GUID.LongKey(pair.Key).ToString("X12")) +
                                    Path.DirectorySeparatorChar;
                if (zeroOnly)
                    rootOutput = path + Path.DirectorySeparatorChar;

                foreach (TextureInfo textureInfo in pair.Value) {
                    string outputPath = zeroOnly ? rootOutput : $"{rootOutput}{GUID.LongKey(textureInfo.GUID):X12}";
                    string outputPathSecondary = zeroOnly ? rootOutput : $"{rootOutput}{GUID.LongKey(textureInfo.DataGUID):X12}";

                    if (textureInfo.Name != null) {
                        outputPath = Path.Combine(outputPath, textureInfo.Name);
                        outputPathSecondary = Path.Combine(outputPathSecondary, textureInfo.Name);
                    }
                    if (textureInfo.Name == null) {
                        outputPath = Path.Combine(outputPath, $"{GUID.LongKey(textureInfo.GUID):X12}");
                        outputPathSecondary = Path.Combine(outputPathSecondary, $"{GUID.LongKey(textureInfo.GUID):X12}");
                    }
                    
                    TextureType type = TextureType.Unknown;

                    if (!convertTextures) {
                        using (Stream soundStream = OpenFile(textureInfo.GUID))
                            WriteFile(soundStream, $"{outputPath}.004");

                        if (textureInfo.DataGUID != null) {
                            using (Stream soundStream = OpenFile(textureInfo.DataGUID))
                                WriteFile(soundStream, $"{outputPathSecondary}.04D");
                        }

                        LoudLog($"Wrote 004{(textureInfo.DataGUID != null ? " and 04D" : "")} file to {outputPath}");
                    } else {
                        Stream convertedStream;
                        if (textureInfo.DataGUID != null) {
                            OWLib.Texture textObj = new OWLib.Texture(OpenFile(textureInfo.GUID), OpenFile(textureInfo.DataGUID));
                            convertedStream = textObj.Save();
                            type = textObj.Format;
                        } else {
                            TextureLinear textObj = new TextureLinear(OpenFile(textureInfo.GUID));
                            convertedStream = textObj.Save();
                            type = textObj.Header.Format();
                        }

                        if (convertedStream == null) continue;
                        convertedStream.Position = 0;
                        WriteFile(convertedStream, $"{outputPath}.dds");
                        convertedStream.Close();
                        LoudLog($"Wrote file {outputPath}.dds");
                    }
                    output[textureInfo] = type;
                }
            }

            return output;
        }
    }
}