using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.ToolLogic.Extract;
using OWLib;
using OWLib.Types;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic {
    public class Texture {

        public static Dictionary<TextureInfo, TextureType> MergeTypes(Dictionary<TextureInfo, TextureType> first, Dictionary<TextureInfo, TextureType> second) {
            return first.Concat(second).GroupBy(d => d.Key).ToDictionary (d => d.Key, d => d.First().Value);
        }
        public static Dictionary<TextureInfo, TextureType> Save(ICLIFlags flags, string path, Dictionary<ulong, List<TextureInfo>> textures) {
            Dictionary<TextureInfo, TextureType> output = new Dictionary<TextureInfo, TextureType>();
            bool convertTextures = true;
            
            bool convertDds = false;
            string convertType = "dds";

            if (flags is ExtractFlags extractFlags) {
                convertTextures = extractFlags.ConvertTextures  && !extractFlags.Raw;
                convertType = extractFlags.ConvertTexturesType.ToLowerInvariant();
                if (extractFlags.SkipTextures) return output;
            }

            bool zeroOnly = textures.ContainsKey(0) && textures.All(x => x.Key == 0);

            foreach (KeyValuePair<ulong, List<TextureInfo>> pair in textures) {
                string rootOutput = Path.Combine(path, GUID.LongKey(pair.Key).ToString("X12")) +
                                    Path.DirectorySeparatorChar;
                if (zeroOnly)
                    rootOutput = path + Path.DirectorySeparatorChar;

                foreach (TextureInfo textureInfo in pair.Value) {
                    string folderPath = zeroOnly ? rootOutput : $"{rootOutput}{GUID.LongKey(textureInfo.GUID):X12}";
                    string outputPathSecondary = zeroOnly ? rootOutput : $"{rootOutput}{GUID.LongKey(textureInfo.DataGUID):X12}";

                    string filePath = "";
                    if (textureInfo.Name != null) {
                        filePath = Path.Combine(folderPath, textureInfo.Name);
                        outputPathSecondary = Path.Combine(outputPathSecondary, textureInfo.Name);
                    }
                    if (textureInfo.Name == null) {
                        filePath = Path.Combine(folderPath, $"{GUID.LongKey(textureInfo.GUID):X12}");
                        outputPathSecondary = Path.Combine(outputPathSecondary, $"{GUID.LongKey(textureInfo.GUID):X12}");
                    }
                    
                    TextureType type = TextureType.Unknown;

                    if (!convertTextures) {
                        using (Stream textureStream = OpenFile(textureInfo.GUID))
                            WriteFile(textureStream, $"{filePath}.004");

                        if (textureInfo.DataGUID != null) {
                            using (Stream textureStream = OpenFile(textureInfo.DataGUID))
                                WriteFile(textureStream, $"{outputPathSecondary}.04D");
                        }

                        // LoudLog($"Wrote 004{(textureInfo.DataGUID != null ? " and 04D" : "")} file to {outputPath}");
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
                        if (convertType == "tga" || convertType == "tif" || convertType == "dds") {  // we need the dds for tif conversion
                            WriteFile(convertedStream, $"{filePath}.dds");
                        }
                        convertedStream.Close();

                        if (convertType == "tif" || convertType == "tga") { 
                            System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
                            pProcess.StartInfo.FileName = "Third Party\\texconv.exe";
                            pProcess.StartInfo.UseShellExecute = false;
                            pProcess.StartInfo.RedirectStandardOutput = true;
                            pProcess.StartInfo.Arguments = $"\"{filePath}.dds\" -y -wicmulti -nologo -m 1 -ft {convertType} -f R8G8B8A8_UNORM -o \"{folderPath}";
                            // -wiclossless?
                            
                            // erm, so if you add an end quote to this then it breaks.
                            // but start one on it's own is fine (we need something for "Winged Victory")
                            
                            pProcess.Start();
                            pProcess.WaitForExit();
                            string line = pProcess.StandardOutput.ReadLine();
                            if (line?.Contains($"{filePath}.dds FAILED") == false) {  // fallback if convert fails
                                File.Delete($"{filePath}.dds");

                            }
                        }
                    }
                    output[textureInfo] = type;
                }
            }

            return output;
        }
    }
}