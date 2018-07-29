using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OWLib;
using OWLib.Types;
using OWLib.Types.Map;
using OWLib.Writer;
using TankLib;
using TankLib.ExportFormats;
using Animation = OWLib.Animation;

namespace DataTool.SaveLogic {
    public class Entity {
        public class OWEntityWriter : IDataWriter {
            public WriterSupport SupportLevel => WriterSupport.ATTACHMENT;
            public char[] Identifier => new[] { 'o', 'w', 'e', 'n', 't', 'i', 't', 'y'};
            public string Format => ".owentity";

            public const ushort VersionMajor = 1;
            public const ushort VersionMinor = 0;
            
            public string Name => "OWM Entity Format";
            
            public void Write(Stream output, FindLogic.Combo.EntityInfoNew entity, FindLogic.Combo.ComboInfo info) {
                using (BinaryWriter writer = new BinaryWriter(output)) {
                    writer.Write(new string(Identifier));
                    writer.Write(VersionMajor);
                    writer.Write((ushort)1);  // todo
                    
                    writer.Write(entity.GetNameIndex());
                    if (entity.Model != 0) {
                        FindLogic.Combo.ModelInfoNew modelInfo = info.Models[entity.Model];
                        writer.Write(modelInfo.GetName());
                    } else {writer.Write("null");}
                    if (entity.Effect != 0) {
                        FindLogic.Combo.EffectInfoCombo effectInfo = info.Effects[entity.Effect];
                        writer.Write(effectInfo.GetName());
                    } else {writer.Write("null");}
                    writer.Write(teResourceGUID.Index(entity.GUID));
                    writer.Write(teResourceGUID.Index(entity.Model));
                    writer.Write(teResourceGUID.Index(entity.Effect));

                    if (entity.Children == null) {
                        writer.Write(0);
                        return;
                    }
                    writer.Write(entity.Children.Count(x => x.GUID != 0));
                    foreach (FindLogic.Combo.ChildEntityReferenceNew childEntityReference in entity.Children.Where(x => x.GUID != 0)) {
                        FindLogic.Combo.EntityInfoNew childEntityInfo = info.Entities[childEntityReference.GUID];
                        
                        writer.Write(childEntityInfo.GetName());
                        writer.Write(childEntityReference.Hardpoint);
                        writer.Write(childEntityReference.Variable);
                        writer.Write(teResourceGUID.Index(childEntityReference.Hardpoint));
                        writer.Write(teResourceGUID.Index(childEntityReference.Variable));
                        if (childEntityReference.Hardpoint != 0) {
                            writer.Write(OverwatchModel.IdToString("hardpoint", teResourceGUID.Index(childEntityReference.Hardpoint)));
                        } else {
                            writer.Write("null"); // erm, k
                        }
                    }
                }
            }

            public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, params object[] data) {
                throw new NotImplementedException();
            }

            public bool Write(Map10 physics, Stream output, params object[] data) {
                throw new NotImplementedException();
            }

            public bool Write(Animation anim, Stream output, params object[] data) {
                throw new NotImplementedException();
            }

            public Dictionary<ulong, List<string>>[] Write(Stream output, OWLib.Map map, OWLib.Map detail1, OWLib.Map detail2, OWLib.Map props, OWLib.Map lights, string name, IDataWriter modelFormat) {
                throw new NotImplementedException();
            }
        }
    }
}