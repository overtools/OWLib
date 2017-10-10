using System.Collections.Generic;
using System.IO;
using static DataTool.Helper.IO;
using DataTool.Flag;
using DataTool.DataModels;
using DataTool.ToolLogic.Extract;
using static DataTool.Helper.STUHelper;
using STULib.Types;

namespace DataTool.SaveLogic {
    public class SprayAndImage {
        public static void SaveItems(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, IEnumerable<ulong> items) {
            var ext = "dds";
            if (flags is ExtractFlags extractFlags) {

            }

            foreach (var key in items) {
                ItemInfo item = GatherUnlock(key);

                if (item.Unlock.CosmeticTextureResource == null) continue;

                var name = GetValidFilename(item.Name);
                var texturePath = item.Unlock.CosmeticTextureResource;
                var filePath = Path.Combine(basePath, containerName, folderName, $"{name}.{ext}");

                STUDecal decal = GetInstance<STUDecal>(texturePath);

                CreateDirectoryFromFile(filePath);

                using (Stream fileStream = OpenFile(texturePath)) {
                    if (fileStream == null) continue;
                    using (Stream outputStream = File.OpenWrite(filePath)) {
                        fileStream.CopyTo(outputStream);
                    }
                }

            }
        }
    }
}