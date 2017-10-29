using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.ToolLogic.Extract;
using OWLib;
using OWLib.Types;
using OWLib.Types.Chunk;
using OWLib.Types.Chunk.LDOM;
using OWLib.Types.Map;
using OWLib.Writer;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic {
        public class Model {
            public class OWMatWriter14 : IDataWriter {
            public string Format => ".owmat";
            public char[] Identifier => new[] { 'W' };
            public string Name => "OWM Material Format (1.14+)";
            public WriterSupport SupportLevel => WriterSupport.MATERIAL | WriterSupport.MATERIAL_DEF;
    
            public bool Write(Map10 physics, Stream output, object[] data) {
                return false;
            }
    
            public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers,
                object[] data) {
                return false;
            }
            
            public bool Write(Stream output, ModelInfo model, Dictionary<TextureInfo, TextureType> typeData) {
                const ushort versionMajor = 1;
                const ushort versionMinor = 1;
    
                using (BinaryWriter writer = new BinaryWriter(output)) {
                    writer.Write(versionMajor);
                    writer.Write(versionMinor);
                    
                    Dictionary<ulong, List<TextureInfo>> materials = new Dictionary<ulong, List<TextureInfo>>();
    
                    foreach (TextureInfo modelTexture in model.Textures) {
                        if (modelTexture.MaterialID == 0) continue;
                        if (!materials.ContainsKey(modelTexture.MaterialID)) materials[modelTexture.MaterialID] = new List<TextureInfo>();
                        materials[modelTexture.MaterialID].Add(modelTexture);
                    }
                    
                    writer.Write(materials.LongCount());
    
                    foreach (KeyValuePair<ulong,List<TextureInfo>> material in materials) {
                        writer.Write(material.Key);
                        writer.Write(material.Value.Count);
                        foreach (TextureInfo texture in material.Value) {
                            string name = $"Textures\\{GUID.LongKey(texture.GUID):X12}.dds";
                        
                            writer.Write(name);
                            if (typeData != null && typeData.ContainsKey(texture)) {
                                writer.Write((byte)DDSTypeDetect.Detect(typeData[texture]));
                            } else {
                                writer.Write((byte)0xFF);
                            }
                        }
                    }
    
                    
                }
                return true;
            }
    
            public bool Write(OWLib.Animation anim, Stream output, object[] data) {
                return false;
            }
    
            public Dictionary<ulong, List<string>>[] Write(Stream output, Map map, Map detail1, Map detail2, Map props, Map lights, string name, IDataWriter modelFormat) {
                return null;
            }
        }
        
        public static void Save(ICLIFlags flags, string path, ModelInfo model, string name, string fileNameOverride=null) {
            bool convertAnims = true;
            bool skipAnims = false;
            bool convertModels = true;
            if (flags is ExtractFlags extractFlags) {
                convertModels = extractFlags.ConvertModels && !extractFlags.Raw;
                convertAnims = extractFlags.ConvertAnimations && !extractFlags.Raw;
                skipAnims = extractFlags.SkipAnimations;
                if (extractFlags.SkipModels) return;
            }
            string basePath = Path.Combine(path, $"{model.GUID.ToString()}");
            if (fileNameOverride != null) basePath = Path.Combine(path, fileNameOverride);
            Dictionary<ulong, List<TextureInfo>> textures =
                new Dictionary<ulong, List<TextureInfo>> {[0] = model.Textures.ToList()};
            Dictionary<TextureInfo, TextureType> textureTypes = Texture.Save(flags,
                Path.Combine(basePath, "Textures"), textures);

            OWMatWriter14 writer14 = new OWMatWriter14();
            OWMDLWriter mdlWriter = new OWMDLWriter();
            RefPoseWriter refposeWriter = new RefPoseWriter();
            SEAnimWriter animWriter = new SEAnimWriter();
            
            CreateDirectoryFromFile($"{basePath}\\billy"); // lies

            if (convertModels) {
                using (Stream fileStream =
                    new FileStream(
                        Path.Combine(basePath, $"{GUID.LongKey(model.GUID):X12}{writer14.Format}"),
                        FileMode.Create)) {
                    fileStream.SetLength(0);
                    writer14.Write(fileStream, model, textureTypes);
                }
                List<byte> lods = new List<byte>(new byte[] {0, 1, 0xFF});
                Chunked mdl = new Chunked(OpenFile(model.GUID));
            
                HTLC cloth = mdl.FindNextChunk("HTLC").Value as HTLC;
           
                using (Stream fileStream =
                    new FileStream(
                        Path.Combine(basePath, $"{GUID.LongKey(model.GUID):X12}{mdlWriter.Format}"),
                        FileMode.Create)) {
                    fileStream.SetLength(0);
                    mdlWriter.Write(mdl, fileStream, lods, null,
                        new object[] {true, $"{GUID.LongKey(model.GUID):X12}{writer14.Format}", name, null, true});
                }
                if (mdl.HasChunk<lksm>()) {
                    using (Stream fileStream =
                        new FileStream(
                            Path.Combine(basePath, $"{GUID.LongKey(model.GUID):X12}{refposeWriter.Format}"),
                            FileMode.Create)) {
                        fileStream.SetLength(0);
                        refposeWriter.Write(mdl, fileStream, null, null, null);
                    }
                    if (cloth != null) {
                        for (uint i = 0; i < cloth.Descriptors.Length; i++) {
                            HTLC.ClothDesc desc = cloth.Descriptors[i];
                            CreateDirectoryFromFile(Path.Combine(basePath, "Cloth\\jeff"));
                            using (Stream fileStream =
                                new FileStream(
                                    Path.Combine(basePath, $"Cloth\\{desc.Name}{refposeWriter.Format}"), FileMode.Create)) {
                                fileStream.SetLength(0);
                                refposeWriter.WriteCloth(mdl, fileStream, cloth, i);
                            }
                        }
                    }
                } else {
                    if (model.Skeleton != null) Debugger.Log(0, "DataTool.SaveLogic.Model", "[DataTool.SaveLogic.Model]: lksm chunk doesn't exist but skeleton does");
                }
            } else {
                using (Stream fileStream =
                    new FileStream(
                        Path.Combine(basePath, $"{GUID.LongKey(model.GUID):X12}.{GUID.Type(model.GUID):X3}"),
                        FileMode.Create)) {
                    fileStream.SetLength(0);
                    using (Stream cascStream = OpenFile(model.GUID)) {
                        cascStream.CopyTo(fileStream);
                    }
                }
            }
            // todo: SaveLogic.Animation

            if (skipAnims) return;
            foreach (AnimationInfo modelAnimation in model.Animations) {
                using (Stream animStream = OpenFile(modelAnimation.GUID)) {
                    if (animStream == null) {
                        continue;
                    }
                    
                    OWLib.Animation animation = new OWLib.Animation(animStream);

                    if (convertAnims) {
                        string animOutput = Path.Combine(basePath,
                            $"Animations\\{animation.Header.priority}\\{GUID.LongKey(modelAnimation.GUID):X12}{animWriter.Format}");
                        CreateDirectoryFromFile(animOutput);
                        using (Stream fileStream = new FileStream(animOutput, FileMode.Create)) {
                            animWriter.Write(animation, fileStream, new object[] { });
                        }
                    } else {
                        string animOutput2 = Path.Combine(basePath, $"Animations\\{animation.Header.priority}\\{GUID.LongKey(modelAnimation.GUID):X12}.{GUID.Type(modelAnimation.GUID):X3}");
                        CreateDirectoryFromFile(animOutput2);
                        using (Stream fileStream = new FileStream(animOutput2, FileMode.Create)) {
                            animStream.CopyTo(fileStream);
                        }
                    }
                }
            }
        }
    }
}
