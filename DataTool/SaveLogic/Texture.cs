using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.ToolLogic.Extract;
using OWLib;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic {
    public class Texture {
        public static void Save(ICLIFlags flags, string path, Dictionary<ulong, List<TextureInfo>> textures) {
            bool convertTextures = true;
            if (flags is ExtractFlags extractFlags) {
                convertTextures = extractFlags.ConvertWem;
                if (extractFlags.SkipTextures) return;
            }

            bool zeroOnly = textures.ContainsKey(0) && textures.All(x => x.Key == 0);

            foreach (KeyValuePair<ulong, List<TextureInfo>> pair in textures) {
                string rootOutput = Path.Combine(path, GUID.LongKey(pair.Key).ToString("X12")) +
                                    Path.DirectorySeparatorChar;
                if (zeroOnly) {
                    rootOutput = path + Path.DirectorySeparatorChar;
                }
                foreach (TextureInfo textureInfo in pair.Value) {
                    string outputPath = $"{rootOutput}{GUID.LongKey(textureInfo.GUID):X12}";
                    string outputPathSecondary = $"{rootOutput}{GUID.LongKey(textureInfo.DataGUID):X12}";
                    if (textureInfo.Name != null) outputPath = $"{rootOutput}{textureInfo.Name}";
                    if (!convertTextures) {
                        using (Stream soundStream = OpenFile(textureInfo.GUID)) {
                            WriteFile(soundStream, $"{outputPath}.004");
                        }
                        if (textureInfo.DataGUID == null) continue;
                        using (Stream soundStream = OpenFile(textureInfo.DataGUID)) {
                            WriteFile(soundStream, $"{outputPathSecondary}.04D");
                        }
                    } else {
                        Stream convertedStream;
                        if (textureInfo.DataGUID != null) {
                            OWLib.Texture textObj = new OWLib.Texture(OpenFile(textureInfo.GUID),
                                OpenFile(textureInfo.DataGUID));
                            convertedStream = textObj.Save();
                        } else {
                            TextureLinear textObj = new TextureLinear(OpenFile(textureInfo.GUID));
                            convertedStream = textObj.Save();
                        }
                        if (convertedStream == null) continue;
                        convertedStream.Position = 0;
                        WriteFile(convertedStream, $"{outputPath}.dds");
                        convertedStream.Close();
                    }
                }
            }
        }
    }
}