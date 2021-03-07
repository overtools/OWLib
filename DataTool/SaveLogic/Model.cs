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
            protected FindLogic.Combo.ModelLookAsset ModelLookInfo;

            public OverwatchModelLook(FindLogic.Combo.ComboInfo info, FindLogic.Combo.ModelLookAsset modelLookInfo) {
                Info = info;
                ModelLookInfo = modelLookInfo;
            }

            public void Write(Stream stream) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    writer.Write(OverwatchMaterial.VersionMajor);
                    writer.Write(OverwatchMaterial.VersionMinor);
                    if (ModelLookInfo.m_materialGUIDs == null) {
                        writer.Write(0L);
                        writer.Write((uint) OverwatchMaterial.OWMatType.ModelLook);
                        return;
                    }

                    writer.Write(ModelLookInfo.m_materialGUIDs.LongCount());
                    writer.Write((uint) OverwatchMaterial.OWMatType.ModelLook);

                    foreach (ulong modelLookMaterial in ModelLookInfo.m_materialGUIDs) {
                        FindLogic.Combo.MaterialAsset materialInfo = Info.m_materials[modelLookMaterial];
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
            protected FindLogic.Combo.MaterialAsset MaterialInfo;

            public OverwatchMaterial(FindLogic.Combo.ComboInfo info, FindLogic.Combo.MaterialAsset materialInfo) {
                Info = info;
                MaterialInfo = materialInfo;
            }

            public void Write(Stream stream) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    FindLogic.Combo.MaterialDataAsset materialDataInfo = Info.m_materialData[MaterialInfo.m_materialDataGUID];
                    writer.Write(VersionMajor);
                    writer.Write(VersionMinor);
                    if (materialDataInfo.m_textureMap != null) {
                        writer.Write(materialDataInfo.m_textureMap.LongCount());
                    } else {
                        writer.Write(0L);
                    }

                    writer.Write((uint) OWMatType.Material);
                    writer.Write(teResourceGUID.Index(MaterialInfo.m_shaderSourceGUID));
                    writer.Write(MaterialInfo.m_materialIDs.Count);
                    foreach (ulong id in MaterialInfo.m_materialIDs) {
                        writer.Write(id);
                    }

                    if (materialDataInfo.m_textureMap != null) {
                        foreach (KeyValuePair<ulong, uint> texture in materialDataInfo.m_textureMap) {
                            FindLogic.Combo.TextureAsset textureInfo = Info.m_textures[texture.Key];
                            if (stream is FileStream fs) {
                                writer.Write(Combo.GetScratchRelative(textureInfo.m_GUID, Path.GetDirectoryName(fs.Name), $@"..\Textures\{textureInfo.GetNameIndex()}.dds"));
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
