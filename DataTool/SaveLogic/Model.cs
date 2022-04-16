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
                    if (ModelLookInfo.m_materials == null) {
                        writer.Write(0L);
                        writer.Write(0L);
                        writer.Write((uint) OverwatchMaterial.OWMatType.ModelLook);
                        return;
                    }

                    writer.Write(ModelLookInfo.m_materials.LongCount());
                    writer.Write(0L);
                    writer.Write((uint) OverwatchMaterial.OWMatType.ModelLook);

                    foreach (var modelLookMaterial in ModelLookInfo.m_materials) {
                        FindLogic.Combo.MaterialAsset materialInfo = Info.m_materials[modelLookMaterial.m_guid];
                        writer.Write(Path.Combine("..", "..", "Materials", materialInfo.GetNameIndex() + $".{Extension}"));
                    }

                    foreach (var modelLookMaterial in ModelLookInfo.m_materials) {
                        writer.Write(modelLookMaterial.m_key);
                    }
                }
            }
        }

        public class OverwatchMaterial : IExportFormat {
            public string Extension => "owmat";

            public const ushort VersionMajor = 2;
            public const ushort VersionMinor = 2;

            public enum OWMatType : uint {
                Material = 0,
                ModelLook = 1
            }

            protected FindLogic.Combo.ComboInfo Info;
            protected FindLogic.Combo.MaterialAsset MaterialInfo;
            public string Format;
            public string MaterialDir;

            public OverwatchMaterial(FindLogic.Combo.ComboInfo info, FindLogic.Combo.MaterialAsset materialInfo, string textureFormat, string materialDir) {
                Info = info;
                MaterialInfo = materialInfo;
                Format = textureFormat;
                MaterialDir = materialDir;
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

                    if (materialDataInfo.m_staticInputMap != null) {
                        writer.Write(materialDataInfo.m_staticInputMap.LongCount());
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
                        foreach (var (guid, hash) in materialDataInfo.m_textureMap) {
                            FindLogic.Combo.TextureAsset textureInfo = Info.m_textures[guid];
                            writer.Write(Combo.GetScratchRelative(textureInfo.m_GUID, MaterialDir, Path.Combine("..", "Textures", textureInfo.GetNameIndex() + $".{Format}")));
                            writer.Write(hash);
                        }
                    }

                    if (materialDataInfo.m_staticInputMap != null) {
                        foreach (var (hash, data) in materialDataInfo.m_staticInputMap) {
                            writer.Write(hash);
                            writer.Write(data.Length);
                            stream.Write(data);
                        }
                    }
                }
            }
        }
    }
}