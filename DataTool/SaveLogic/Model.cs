using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using APPLIB;
using DataTool.Flag;
using DataTool.ToolLogic.Extract;
using OpenTK;
using OWLib;
using OWLib.Types;
using OWLib.Types.Chunk;
using OWLib.Types.Chunk.LDOM;
using OWLib.Types.Map;
using OWLib.Writer;
using TankLib;
using static DataTool.Helper.IO;
using Animation = OWLib.Animation;

namespace DataTool.SaveLogic {
    public class Model {
        public const string AnimationEffectDir = "AnimationEffects";
        
        // things with 14 are for 1.14+
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

            public enum OWMatType : uint {
                Material = 0,
                ModelLook = 1
            }

            public const ushort VersionMajor = 2;
            public const ushort VersionMinor = 0;
            // ver 2.0+ writes materials seperately from ModelLooks

            public void Write(Stream output, FindLogic.Combo.ComboInfo info, FindLogic.Combo.ModelLookInfo modelLookInfo) {
                using (BinaryWriter writer = new BinaryWriter(output)) {
                    writer.Write(VersionMajor);
                    writer.Write(VersionMinor);
                    if (modelLookInfo.Materials == null) {
                        writer.Write(0L);
                        writer.Write((uint)OWMatType.ModelLook);
                        return;
                    }
                    writer.Write(modelLookInfo.Materials.LongCount());
                    writer.Write((uint)OWMatType.ModelLook);
                    
                    foreach (ulong modelLookMaterial in modelLookInfo.Materials) {
                        FindLogic.Combo.MaterialInfo materialInfo = info.Materials[modelLookMaterial];
                        writer.Write($"..\\..\\Materials\\{materialInfo.GetNameIndex()}{Format}");
                    }
                }
            }
            
            public void Write(Stream output, FindLogic.Combo.ComboInfo info, FindLogic.Combo.MaterialInfo materialInfo) {
                using (BinaryWriter writer = new BinaryWriter(output)) {
                    FindLogic.Combo.MaterialDataInfo materialDataInfo = info.MaterialDatas[materialInfo.MaterialData];
                    writer.Write(VersionMajor);
                    writer.Write(VersionMinor);
                    writer.Write(materialDataInfo.Textures.LongCount());
                    writer.Write((uint)OWMatType.Material);
                    writer.Write(GUID.Index(materialInfo.ShaderSource));
                    writer.Write(materialInfo.IDs.Count);
                    foreach (ulong id in materialInfo.IDs) {
                        writer.Write(id);
                    }
                    
                    foreach (KeyValuePair<ulong, teShaderTextureType> texture in materialDataInfo.Textures) {
                        FindLogic.Combo.TextureInfoNew textureInfo = info.Textures[texture.Key];
                        writer.Write($"..\\Textures\\{textureInfo.GetNameIndex()}.dds");
                        writer.Write((uint)texture.Value);
                    }
                }
            }
    
            public bool Write(Animation anim, Stream output, object[] data) {
                return false;
            }

            public Dictionary<ulong, List<string>>[] Write(Stream output, OWLib.Map map, OWLib.Map detail1, OWLib.Map detail2, OWLib.Map props, OWLib.Map lights, string name,
                IDataWriter modelFormat) {
                throw new NotImplementedException();
            }
        }
        
        public class OWModelWriter14 : IDataWriter {
            public string Format => ".owmdl";
            public char[] Identifier => new[] {'w'};
            public string Name => "OWM Model Format";

            public WriterSupport SupportLevel => WriterSupport.VERTEX | WriterSupport.UV | WriterSupport.BONE |
                                                 WriterSupport.POSE | WriterSupport.MATERIAL |
                                                 WriterSupport.ATTACHMENT | WriterSupport.MODEL;


            public bool Write(Map10 physics, Stream output, object[] data) {
                throw new NotImplementedException();
            }
            
            public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, params object[] data) {
                throw new NotImplementedException();
            }

            public bool Write(Animation anim, Stream output, params object[] data) {
                throw new NotImplementedException();
            }

            public Dictionary<ulong, List<string>>[] Write(Stream output, OWLib.Map map, OWLib.Map detail1, OWLib.Map detail2, OWLib.Map props, OWLib.Map lights, string name,
                IDataWriter modelFormat) {
                throw new NotImplementedException();
            }

            public void Write(ICLIFlags flags, Stream output, FindLogic.Combo.ComboInfo info,
                FindLogic.Combo.ModelInfoNew modelInfo, Stream modelStream, Stream refposeStream) {
                bool doRefpose = false;

                if (flags is ExtractFlags extractFlags) {
                    doRefpose = extractFlags.ExtractRefpose;
                }
                
                // erm, we need to wrap for now
                using (Chunked modelChunked = new Chunked(modelStream)) {
                    string materialPath = "";
                    OWMatWriter14 materialWriter = new OWMatWriter14();
                    if (modelInfo.ModelLooks.Count > 0) {
                        FindLogic.Combo.ModelLookInfo modelLookInfo = info.ModelLooks[modelInfo.ModelLooks.First()];
                        materialPath = Path.Combine("ModelLooks",
                            modelLookInfo.GetNameIndex() + materialWriter.Format);
                    }
                    // data is object[] { bool exportAttachments, string materialReference, string modelName, bool onlyOneLOD, bool skipCollision }
                    Write(flags, modelChunked, output, new List<byte>(new byte[] {0, 1, 0xFF}), 
                        new object[] {true, materialPath, $"Model {GetFileName(modelInfo.GUID)}", null, true},
                        modelInfo);

                    if (!doRefpose) return;
                    RefPoseWriter refPoseWriter = new RefPoseWriter();
                    refPoseWriter.Write(modelChunked, refposeStream, true);
                    refposeStream.Position = 0;
                }
            }

            // ReSharper disable once InconsistentNaming
            // data is object[] { bool exportAttachments, string materialReference, string modelName, bool onlyOneLOD, bool skipCollision }
            public void Write(ICLIFlags flags, Chunked chunked, Stream output, List<byte> LODs, object[] data, FindLogic.Combo.ModelInfoNew modelInfo) {
                byte? flagLOD = null;
                if (flags is ExtractFlags extractFlags) {
                    flagLOD = extractFlags.LOD;
                }
                
                IChunk chunk = chunked.FindNextChunk("MNRM").Value;
                if (chunk == null) {
                    return;
                }
                MNRM model = (MNRM) chunk;
                chunk = chunked.FindNextChunk("CLDM").Value;
                CLDM materials = null;
                if (chunk != null) {
                    materials = (CLDM) chunk;
                }
                chunk = chunked.FindNextChunk("lksm").Value;
                lksm skeleton = null;
                if (chunk != null) {
                    skeleton = (lksm) chunk;
                }
                chunk = chunked.FindNextChunk("PRHM").Value;
                PRHM hardpoints = null;
                if (chunk != null) {
                    hardpoints = (PRHM) chunk;
                }
                HTLC cloth = chunked.FindNextChunk("HTLC").Value as HTLC;

                short[] hierarchy = (short[]) skeleton?.Hierarchy.Clone();
                Dictionary<int, HTLC.ClothNode> nodeMap = new Dictionary<int, HTLC.ClothNode>();
                
                if (cloth != null) {
                    uint clothIndex = 0;
                    foreach (HTLC.ClothNode[] nodeCollection in cloth.Nodes) {
                        if (nodeCollection == null) continue;
                        int nodeIndex = 0;
                        foreach (HTLC.ClothNode node in nodeCollection) {
                            int parentRaw = node.VerticalParent;
                            if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex) &&
                                cloth.NodeBones[clothIndex].ContainsKey(parentRaw)) {
                                if (cloth.NodeBones[clothIndex][nodeIndex] != -1) {
                                    hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] =
                                        cloth.NodeBones[clothIndex][parentRaw];
                                    if (cloth.NodeBones[clothIndex][parentRaw] == -1) {
                                        HTLC.ClothNodeWeight weightedBone =
                                            node.Bones.Aggregate((i1, i2) => i1.Weight > i2.Weight ? i1 : i2);
                                        hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = weightedBone.Bone;
                                    }
                                }
                            } else {
                                if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex)) {
                                    if (cloth.NodeBones[clothIndex][nodeIndex] != -1) {
                                        hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = -1;
                                        HTLC.ClothNodeWeight weightedBone =
                                            node.Bones.Aggregate((i1, i2) => i1.Weight > i2.Weight ? i1 : i2);
                                        hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = weightedBone.Bone;
                                    }
                                }
                            }
                            if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex)) {
                                if (cloth.NodeBones[clothIndex][nodeIndex] != -1) {
                                    nodeMap[cloth.NodeBones[clothIndex][nodeIndex]] = node;
                                }
                            }

                            nodeIndex++;
                        }
                        clothIndex++;
                    }
                }

                using (BinaryWriter writer = new BinaryWriter(output)) {
                    writer.Write((ushort) 1); // version major
                    writer.Write((ushort) 5); // version minor

                    if (data.Length > 1 && data[1] is string && ((string) data[1]).Length > 0) {
                        writer.Write((string) data[1]);
                    } else {
                        writer.Write((byte) 0);
                    }

                    if (data.Length > 2 && data[2] is string && ((string) data[2]).Length > 0) {
                        writer.Write((string) data[2]);
                    } else {
                        writer.Write((byte) 0);
                    }

                    if (skeleton == null) {
                        writer.Write((ushort) 0); // number of bones
                    } else {
                        writer.Write(skeleton.Data.bonesAbs);
                    }

                    // ReSharper disable once InconsistentNaming
                    Dictionary<byte, List<int>> LODMap = new Dictionary<byte, List<int>>();
                    uint sz = 0;
                    uint lookForLod = 0;

                    if (model.Submeshes.Any(x => x.lod == flagLOD)) {
                        lookForLod = (byte) flagLOD;
                    } else if (flagLOD != null) {
                        SubmeshDescriptor nextLowest = model.Submeshes.Where(p => p.lod < flagLOD).OrderBy(x => x.lod).LastOrDefault();
                        if (nextLowest.verticesToDraw == 0 && nextLowest.indexCount == 0) { // not real mesh
                            SubmeshDescriptor nextHighest = model.Submeshes.Where(p => p.lod > flagLOD).OrderBy(x => x.lod).FirstOrDefault();
                            lookForLod = nextHighest.lod;
                        } else {
                            lookForLod = nextLowest.lod;
                        }
                    }
                    
                    for (int i = 0; i < model.Submeshes.Length; ++i) {
                        SubmeshDescriptor submesh = model.Submeshes[i];
                        if (data.Length > 4 && data[4] is bool && (bool) data[4]) {
                            if (submesh.flags == SubmeshFlags.COLLISION_MESH) {
                                continue;
                            }
                        }
                        if (lookForLod > 0 && submesh.lod != lookForLod && submesh.lod != 255) {
                            continue;
                        }
                        
                        if (!LODMap.ContainsKey(submesh.lod)) {
                            LODMap.Add(submesh.lod, new List<int>());
                        }
                        sz++;
                        LODMap[submesh.lod].Add(i);
                    }
                    
                    writer.Write(sz);
                    writer.Write(hardpoints?.HardPoints.Length ?? 0);

                    if (skeleton != null) {
                        for (int i = 0; i < skeleton.Data.bonesAbs; ++i) {
                            writer.Write(IdToString("bone", skeleton.IDs[i]));
                            short parent = hierarchy[i];
                            if (parent == -1) {
                                parent = (short) i;
                            }
                            writer.Write(parent);

                            Matrix3x4 bone = skeleton.Matrices34[i];
                            Quaternion rot = new Quaternion(bone[0, 0], bone[0, 1], bone[0, 2], bone[0, 3]);
                            Vector3 scl = new Vector3(bone[1, 0], bone[1, 1], bone[1, 2]);
                            Vector3 pos = new Vector3(bone[2, 0], bone[2, 1], bone[2, 2]);
                            if (nodeMap.ContainsKey(i)) {
                                HTLC.ClothNode thisNode = nodeMap[i];
                                pos.X = thisNode.X;
                                pos.Y = thisNode.Y;
                                pos.Z = thisNode.Z;
                            }
                            writer.Write(pos.X);
                            writer.Write(pos.Y);
                            writer.Write(pos.Z);
                            writer.Write(scl.X);
                            writer.Write(scl.Y);
                            writer.Write(scl.Z);
                            writer.Write(rot.X);
                            writer.Write(rot.Y);
                            writer.Write(rot.Z);
                            writer.Write(rot.W);
                        }
                    }

                    foreach (KeyValuePair<byte, List<int>> kv in LODMap) {
                        foreach (int i in kv.Value) {
                            SubmeshDescriptor submesh = model.Submeshes[i];
                            ModelVertex[] vertex = model.Vertices[i];
                            ModelVertex[] normal = model.Normals[i];
                            ModelUV[][] uv = model.TextureCoordinates[i];
                            ModelIndice[] index = model.Indices[i];

                            ModelBoneData[] bones = model.Bones[i];
                            writer.Write($"Submesh_{i}.{kv.Key}.{materials.Materials[submesh.material]:X16}");
                            writer.Write(materials.Materials[submesh.material]);
                            writer.Write((byte) uv.Length);

                            writer.Write(vertex.Length);
                            writer.Write(index.Length);
                            for (int j = 0; j < vertex.Length; ++j) {
                                writer.Write(vertex[j].x);
                                writer.Write(vertex[j].y);
                                writer.Write(vertex[j].z);
                                writer.Write(-normal[j].x);
                                writer.Write(-normal[j].y);
                                writer.Write(-normal[j].z);
                                foreach (ModelUV[] t in uv) {
                                    writer.Write(t[j].u);
                                    writer.Write(t[j].v);
                                }
                                if (skeleton != null && bones != null && bones[j].boneIndex != null &&
                                    bones[j].boneWeight != null) {
                                    writer.Write((byte) 4);
                                    writer.Write(skeleton.Lookup[bones[j].boneIndex[0]]);
                                    writer.Write(skeleton.Lookup[bones[j].boneIndex[1]]);
                                    writer.Write(skeleton.Lookup[bones[j].boneIndex[2]]);
                                    writer.Write(skeleton.Lookup[bones[j].boneIndex[3]]);
                                    writer.Write(bones[j].boneWeight[0]);
                                    writer.Write(bones[j].boneWeight[1]);
                                    writer.Write(bones[j].boneWeight[2]);
                                    writer.Write(bones[j].boneWeight[3]);
                                } else {
                                    // bone -> size + index + weight
                                    writer.Write((byte) 0);
                                }
                            }
                            List<ModelIndiceModifiable> indexNew = new List<ModelIndiceModifiable>();
                            foreach (ModelIndice indice in index) {
                                indexNew.Add(new ModelIndiceModifiable {
                                    v1 = indice.v1,
                                    v2 = indice.v2,
                                    v3 = indice.v3
                                });
                            }
                            foreach (ModelIndiceModifiable indice in indexNew) {
                                writer.Write((byte) 3);
                                writer.Write(indice.v1);
                                writer.Write(indice.v2);
                                writer.Write(indice.v3);
                            }
                        }
                    }

                    if (hardpoints != null) {
                        // attachments
                        foreach (PRHM.HardPoint hp in hardpoints.HardPoints) {
                            writer.Write(IdToString("hardpoint", GUID.Index(hp.HardPointGUID)));
                            Matrix4 mat = hp.Matrix.ToOpenTK();

                            Vector3 pos = mat.ExtractTranslation();
                            Quaternion rot = mat.ExtractRotation();
                            
                            writer.Write(pos.X);
                            writer.Write(pos.Y);
                            writer.Write(pos.Z);
                            writer.Write(rot.X);
                            writer.Write(rot.Y);
                            writer.Write(rot.Z);
                            writer.Write(rot.W);
                        }
                        // extension 1.1
                        foreach (PRHM.HardPoint hp in hardpoints.HardPoints) {
                            writer.Write(IdToString("bone", GUID.Index(hp.GUIDx012)));
                        }
                    }

                    // ext 1.3: cloth
                    writer.Write(0);

                    // ext 1.4: embedded refpose
                    if (skeleton != null) {
                        for (int i = 0; i < skeleton.Data.bonesAbs; ++i) {
                            writer.Write(IdToString("bone", skeleton.IDs[i]));
                            short parent = hierarchy[i];
                            writer.Write(parent);

                            Matrix3x4 bone = skeleton.Matrices34Inverted[i];
                            
                            Quaternion3D quat = new Quaternion3D(bone[0, 3], bone[0, 0], bone[0, 1], bone[0, 2]);

                            Vector3D rot = C3D.ToEulerAngles(quat);
                            // ReSharper disable CompareOfFloatsByEqualityOperator
                            if (rot.X == -3.14159274f && rot.Y == 0 && rot.Z == 0) {
                                rot = new Vector3D(0, 3.14159274f, 3.14159274f); 
                                // effectively the same but you know, eulers.
                            }
                            // ReSharper restore CompareOfFloatsByEqualityOperator
                            Vector3 scl = new Vector3(bone[1, 0], bone[1, 1], bone[1, 2]);
                            Vector3 pos = new Vector3(bone[2, 0], bone[2, 1], bone[2, 2]);
                            writer.Write(pos.X);
                            writer.Write(pos.Y);
                            writer.Write(pos.Z);
                            writer.Write(scl.X);
                            writer.Write(scl.Y);
                            writer.Write(scl.Z);
                            writer.Write(rot.X);
                            writer.Write(rot.Y);
                            writer.Write(rot.Z);
                        }
                    }
                    
                    // ext 1.5: guid
                    writer.Write(GUID.Index(modelInfo.GUID));
                    
                    // ext 1.6: cloth 2.0
                    if (cloth == null) {
                        writer.Write(0);
                    } else {
                        writer.Write(cloth.Descriptors.Length);

                        for (int i = 0; i < cloth.Descriptors.Length; i++) {
                            var desc = cloth.Descriptors[i];
                            
                            writer.Write(desc.Name);
                            writer.Write(cloth.Nodes[i].Length);
                            foreach (HTLC.ClothNode clothNode in cloth.Nodes[i]) {
                                writer.Write(clothNode.Bones.Length);

                                foreach (HTLC.ClothNodeWeight clothNodeWeight in clothNode.Bones) {
                                    writer.Write(clothNodeWeight.Bone);
                                    writer.Write(clothNodeWeight.Weight);
                                }
                            }
                        }
                    }
                }
            }
            
            public static string IdToString(string prefix, uint id) {
                return $"{prefix}_{id:X4}";
            }
        }
    }
}
