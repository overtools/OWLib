using System;
using System.Collections.Generic;
using System.IO;
using CASCLib;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using OWLib.Types;
using OWLib.Types.Map;
using OWLib.Writer;
using STULib.Types.Generic;
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
            
            public void Write(Stream output, EntityInfo entity, Dictionary<Common.STUGUID, string> nameOverrides) {
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
                    
                    writer.Write(entity.Children.Count);
                    foreach (ChildEntityReference childEntityReference in entity.Children) {
                        string childFile = GetFileName(childEntityReference.GUID);
                        if (nameOverrides.ContainsKey(childEntityReference.GUID)) {
                            childFile = nameOverrides[childEntityReference.GUID];
                        }
                        writer.Write(childFile);
                        writer.Write((ulong)childEntityReference.Hardpoint);
                        writer.Write((ulong)childEntityReference.Variable);
                        if (childEntityReference.Hardpoint != null) {
                            writer.Write(OWMDLWriter.IdToString("attachment_", GUID.Index(childEntityReference.Hardpoint)));
                        } else {
                            writer.Write("null"); // erm
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
        
        public class OWAnimWriter : IDataWriter {
            public WriterSupport SupportLevel => WriterSupport.ANIM;
            public char[] Identifier => new[] { 'o', 'w', 'a', 'n', 'i', 'm'};
            public string Format => ".owanim";

            public const ushort VersionMajor = 1;
            public const ushort VersionMinor = 0;
            
            public string Name => "OWM Animation Format";

            
            // plan and stuff:
            //     dmce submodels
            //     how are weapons attached? TICK
            //     stretch goal:
            //         how do camera
            public void Write(Stream output, AnimationInfo animation) {
                using (BinaryWriter writer = new BinaryWriter(output)) {
                    writer.Write(new string(Identifier));
                    writer.Write(VersionMajor);
                    writer.Write(VersionMinor);
                    
                    writer.Write(animation.GUID);

                    writer.Write(animation.DMCEs.Count);
                    foreach (DMCEInfo dmceInfo in animation.DMCEs) {
                        writer.Write(dmceInfo.Model);
                        writer.Write(dmceInfo.Material);
                        writer.Write(dmceInfo.Animation);
                        writer.Write(dmceInfo.StartFrame);
                        writer.Write(dmceInfo.ParentBone);
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
        
        public static void Save(ICLIFlags flags, string path, IEnumerable<EntityInfo> entities, Dictionary<Common.STUGUID, string> entityNames=null) {
            OWEntityWriter owEntityWriter = new OWEntityWriter();
            OWAnimWriter owAnimWriter = new OWAnimWriter();
            if (entityNames == null) entityNames = new Dictionary<Common.STUGUID, string>();
            foreach (EntityInfo entity in entities) {
                using (Stream entityStream = OpenFile(entity.GUID)) {
                    if (entityStream == null) {
                        continue;
                    }           
                    string entityName = entity.GUID.ToString();
                    if (entityNames.ContainsKey(entity.GUID)) entityName = entityNames[entity.GUID];
                    string basePath = Path.Combine(path, entityName);
                    CreateDirectoryFromFile($"{basePath}\\GabeN");
                    using (Stream fileStream =
                        new FileStream(Path.Combine(basePath, $"{basePath}\\{entityName}{owEntityWriter.Format}"), FileMode.Create)) {
                        fileStream.SetLength(0);
                        owEntityWriter.Write(fileStream, entity, entityNames);
                    }
                }
            }
        }
    }
}