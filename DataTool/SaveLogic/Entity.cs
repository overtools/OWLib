using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using OWLib.Types;
using OWLib.Types.Map;
using OWLib.Writer;
using STULib.Types;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic {
    public class Entity {
        public class OWEntityWriter : IDataWriter {
            public WriterSupport SupportLevel => WriterSupport.ATTACHMENT;
            public char[] Identifier => new[] { 'o', 'w', 'e', 'n', 't', 'i', 't', 'y'};
            public string Format => ".owentity";

            public const ushort VersionMajor = 1;
            public const ushort VersionMinor = 0;
            
            public string Name => "OWM Entity Format";
            
            public void Write(Stream output, EntityInfo entity, Dictionary<ulong, string> nameOverrides) {
                using (BinaryWriter writer = new BinaryWriter(output)) {
                    writer.Write(new string(Identifier));
                    writer.Write(VersionMajor);
                    writer.Write(VersionMinor);

                    string thisFile = GUID.Index(entity.GUID).ToString("X");
                    if (nameOverrides.ContainsKey(entity.GUID)) {
                        thisFile = nameOverrides[entity.GUID];
                    }
                    
                    writer.Write(thisFile);
                    writer.Write(GetFileName(entity.Model));
                    writer.Write(GUID.Index(entity.GUID));
                    writer.Write(GUID.Index(entity.Model));
                    
                    writer.Write(entity.Children.Count(x => x.GUID != null && x.GUID != 0));
                    foreach (ChildEntityReference childEntityReference in entity.Children) {
                        string childFile = GetFileName(childEntityReference.GUID);
                        if (childEntityReference.GUID == null || childEntityReference.GUID == 0) {
                            continue;
                        }
                        if (nameOverrides.ContainsKey(childEntityReference.GUID)) {
                            childFile = GetValidFilename(nameOverrides[childEntityReference.GUID]);
                        }
                        writer.Write(childFile);
                        writer.Write((ulong)childEntityReference.Hardpoint);
                        writer.Write((ulong)childEntityReference.Variable);
                        writer.Write(GUID.Index(childEntityReference.Hardpoint));
                        writer.Write(GUID.Index(childEntityReference.Variable));
                        if (childEntityReference.Hardpoint != null) {
                            writer.Write(Model.OWModelWriter14.IdToString("hardpoint", GUID.Index(childEntityReference.Hardpoint)));
                        } else {
                            writer.Write("null"); // erm, k
                        }
                    }
                }
            }
            
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
                    writer.Write(GUID.Index(entity.GUID));
                    writer.Write(GUID.Index(entity.Model));
                    writer.Write(GUID.Index(entity.Effect));

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
                        writer.Write(GUID.Index(childEntityReference.Hardpoint));
                        writer.Write(GUID.Index(childEntityReference.Variable));
                        if (childEntityReference.Hardpoint != 0) {
                            writer.Write(Model.OWModelWriter14.IdToString("hardpoint", GUID.Index(childEntityReference.Hardpoint)));
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

            public bool Write(OWLib.Animation anim, Stream output, params object[] data) {
                throw new NotImplementedException();
            }

            public Dictionary<ulong, List<string>>[] Write(Stream output, OWLib.Map map, OWLib.Map detail1, OWLib.Map detail2, OWLib.Map props, OWLib.Map lights, string name, IDataWriter modelFormat) {
                throw new NotImplementedException();
            }
        }
        
        public static void Save(ICLIFlags flags, string path, IEnumerable<EntityInfo> entities, Dictionary<ulong, string> entityNames=null) {
            OWEntityWriter owEntityWriter = new OWEntityWriter();
            if (entityNames == null) entityNames = new Dictionary<ulong, string>();
            foreach (EntityInfo entity in entities) {
                //using (Stream entityStream = OpenFile(entity.GUID)) {
                //    if (entityStream == null) {
                //        continue;
                //    }           
                string entityName = entity.GUID.ToString();
                if (entityNames.ContainsKey(entity.GUID)) entityName = GetValidFilename(entityNames[entity.GUID]);
                string basePath = Path.Combine(path, entityName);
                CreateDirectoryFromFile($"{basePath}\\GabeN");
                using (Stream fileStream =
                    new FileStream($"{basePath}\\{entityName}{owEntityWriter.Format}", FileMode.Create)) {
                    fileStream.SetLength(0);
                    owEntityWriter.Write(fileStream, entity, entityNames);
                }
                
                SaveAnimations(flags, basePath, entity.Animations, entity.Model, "Animations", true, entityNames);
                //}
            }
        }

        public static void SaveAnimations(ICLIFlags flags, string path, HashSet<AnimationInfo> animations, 
            ulong model, string dirname="Animations", bool isReference=false, 
            Dictionary<ulong, string> entityNames=null) {
            Effect.OWAnimWriter owAnimWriter = new Effect.OWAnimWriter();
            if (entityNames == null && isReference) throw new Exception("Entity names were not given to SaveLogic.Entity.SaveAnimations(isReference=false)");
            foreach (AnimationInfo animation in animations) {
                string name = GetValidFilename(animation.Name);
                if (name == null) name = GUID.LongKey(animation.GUID).ToString("X12");
                CreateDirectoryFromFile($"{path}\\{dirname}\\{name}\\drfdfd");
                Dictionary<ulong, List<STUVoiceLineInstance>> svceLines = null;
                using (Stream fileStream =
                    new FileStream($"{path}\\{dirname}\\{name}\\{name}.owanim", FileMode.Create)) {
                    fileStream.SetLength(0);
                    if (isReference) {
                        owAnimWriter.WriteReference(fileStream, animation, model);
                    } else {
                        svceLines = Effect.GetSVCELines(animation);
                        owAnimWriter.Write(fileStream, animation, model, entityNames, svceLines);
                    }
                }
                if (!isReference) {
                    Effect.Save(flags, $"{path}\\{dirname}\\{name}", animation, true, entityNames, svceLines);
                }
            }
        }
    }
}