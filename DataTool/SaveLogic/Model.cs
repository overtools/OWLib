using System.Collections.Generic;
using System.IO;
using System.Linq;
using TankLib;
using TankLib.ExportFormats;

namespace DataTool.SaveLogic {
    public static class Model {
        public class OverwatchModelLook : IExportFormat {
            public string Extension => "owmat";

            protected FindLogic.Combo.ComboInfo Info;
            protected FindLogic.Combo.ModelLookInfo ModelLookInfo;

            public OverwatchModelLook(FindLogic.Combo.ComboInfo info, FindLogic.Combo.ModelLookInfo modelLookInfo) {
                Info = info;
                ModelLookInfo = modelLookInfo;
            }

            public void Write(Stream stream) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    writer.Write(OverwatchMaterial.VersionMajor);
                    writer.Write(OverwatchMaterial.VersionMinor);
                    if (ModelLookInfo.Materials == null) {
                        writer.Write(0L);
                        writer.Write((uint)OverwatchMaterial.OWMatType.ModelLook);
                        return;
                    }
                    writer.Write(ModelLookInfo.Materials.LongCount());
                    writer.Write((uint)OverwatchMaterial.OWMatType.ModelLook);
                    
                    foreach (ulong modelLookMaterial in ModelLookInfo.Materials) {
                        FindLogic.Combo.MaterialInfo materialInfo = Info.Materials[modelLookMaterial];
                        writer.Write($"..\\..\\Materials\\{materialInfo.GetNameIndex()}.{Extension}");
                    }
                }
            }
        }

        public class OverwatchMaterial : IExportFormat {
            public string Extension => "owmat";
            
            public const ushort VersionMajor = 2;
            public const ushort VersionMinor = 0;
            
            public enum OWMatType : uint {
                Material = 0,
                ModelLook = 1
            }
            
            protected FindLogic.Combo.ComboInfo Info;
            protected FindLogic.Combo.MaterialInfo MaterialInfo;

            public OverwatchMaterial(FindLogic.Combo.ComboInfo info, FindLogic.Combo.MaterialInfo materialInfo) {
                Info = info;
                MaterialInfo = materialInfo;
            }

            public void Write(Stream stream) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    FindLogic.Combo.MaterialDataInfo materialDataInfo = Info.MaterialDatas[MaterialInfo.MaterialData];
                    writer.Write(VersionMajor);
                    writer.Write(VersionMinor);
                    if (materialDataInfo.Textures != null) {
                        writer.Write(materialDataInfo.Textures.LongCount());
                    } else {
                        writer.Write(0L);
                    }
                    writer.Write((uint)OWMatType.Material);
                    writer.Write(teResourceGUID.Index(MaterialInfo.ShaderSource));
                    writer.Write(MaterialInfo.MaterialIDs.Count);
                    foreach (ulong id in MaterialInfo.MaterialIDs) {
                        writer.Write(id);
                    }

                    if (materialDataInfo.Textures != null) {
                        foreach (KeyValuePair<ulong, uint> texture in materialDataInfo.Textures) {
                            FindLogic.Combo.TextureInfoNew textureInfo = Info.Textures[texture.Key];
                            if (stream is FileStream fs) {
                                writer.Write(Combo.GetScratchRelative(textureInfo.GUID, Path.GetDirectoryName(fs.Name), $@"..\Textures\{textureInfo.GetNameIndex()}.dds"));
                            } else {
                                writer.Write($@"..\Textures\{textureInfo.GetNameIndex()}.dds");
                            }
                            writer.Write(texture.Value);
                        }
                    }
                }
            }
        }
    }
}
