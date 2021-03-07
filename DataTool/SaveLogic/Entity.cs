using System.IO;
using System.Linq;
using TankLib;
using TankLib.ExportFormats;

namespace DataTool.SaveLogic {
    public static class Entity {
        public class OverwatchEntity : IExportFormat {
            public string Extension => "owentity";

            protected readonly FindLogic.Combo.ComboInfo Info;
            protected readonly FindLogic.Combo.EntityAsset Entity;

            public const ushort VersionMajor = 1;
            public const ushort VersionMinor = 1;

            public OverwatchEntity(FindLogic.Combo.EntityAsset entity, FindLogic.Combo.ComboInfo info) {
                Info = info;
                Entity = entity;
            }

            public void Write(Stream stream) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    writer.Write(Extension); // type identifier
                    writer.Write(VersionMajor);
                    writer.Write(VersionMinor);

                    writer.Write(Entity.GetNameIndex());
                    if (Entity.m_modelGUID != 0) {
                        FindLogic.Combo.ModelAsset modelInfo = Info.m_models[Entity.m_modelGUID];
                        writer.Write(modelInfo.GetName());
                    } else {
                        writer.Write("null");
                    }

                    if (Entity.m_effectGUID != 0) {
                        FindLogic.Combo.EffectInfoCombo effectInfo = Info.m_effects[Entity.m_effectGUID];
                        writer.Write(effectInfo.GetName());
                    } else {
                        writer.Write("null");
                    }

                    writer.Write(teResourceGUID.Index(Entity.m_GUID));
                    writer.Write(teResourceGUID.Index(Entity.m_modelGUID));
                    writer.Write(teResourceGUID.Index(Entity.m_effectGUID));

                    if (Entity.Children == null) {
                        writer.Write(0);
                        return;
                    }

                    writer.Write(Entity.Children.Count(x => x.m_defGUID != 0));
                    foreach (FindLogic.Combo.ChildEntityReference childEntityReference in Entity.Children.Where(x => x.m_defGUID != 0)) {
                        FindLogic.Combo.EntityAsset childEntityInfo = Info.m_entities[childEntityReference.m_defGUID];

                        writer.Write(childEntityInfo.GetName());
                        writer.Write(childEntityReference.m_hardpointGUID);
                        writer.Write(childEntityReference.m_identifier);
                        writer.Write(teResourceGUID.Index(childEntityReference.m_hardpointGUID));
                        writer.Write(teResourceGUID.Index(childEntityReference.m_identifier));
                        if (childEntityReference.m_hardpointGUID != 0) {
                            writer.Write(OverwatchModel.IdToString("hardpoint", teResourceGUID.Index(childEntityReference.m_hardpointGUID)));
                        } else {
                            writer.Write("null"); // erm, k
                        }
                    }
                }
            }
        }
    }
}
