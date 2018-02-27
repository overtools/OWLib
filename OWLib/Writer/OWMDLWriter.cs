using System.Collections.Generic;
using System.IO;
using System.Linq;
using APPLIB;
using OpenTK;
using OWLib.Types;
using OWLib.Types.Chunk;
using OWLib.Types.Chunk.LDOM;
using OWLib.Types.Map;

namespace OWLib.Writer {
    public class ClothVertContainer {
        public int J; // index
        public ModelVertex Vertex;
        public ModelVertex Normal;
        public ModelBoneData Bones;
    }

    public class ClothMesh {
        public List<ClothVertContainer> Vertcies;
        public List<ModelIndiceModifiable> Indices;
        public ulong Material;
        public ModelUV[][] UV;
        public int Coth;
        public List<int> VertIDs;  // I'm lazy
        public int Submesh;
        public uint FileIndex;
    }

    // ReSharper disable once InconsistentNaming
    public class OWMDLWriter : IDataWriter {
        public string Format => ".owmdl";
        public char[] Identifier => new [] { 'w' };
        public string Name => "OWM Model Format";
        public WriterSupport SupportLevel => WriterSupport.VERTEX | WriterSupport.UV | WriterSupport.BONE | WriterSupport.POSE | WriterSupport.MATERIAL | WriterSupport.ATTACHMENT | WriterSupport.MODEL;

        public bool Write(Map10 physics, Stream output, object[] data) {
            using (BinaryWriter writer = new BinaryWriter(output)) {
                writer.Write((ushort)1);
                writer.Write((ushort)1);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)0);
                writer.Write(1);
                writer.Write(0);

                writer.Write("PhysicsModel");
                writer.Write(0L);
                writer.Write((byte)0);
                writer.Write(physics.Vertices.Length);
                writer.Write(physics.Indices.Length);

                for (int i = 0; i < physics.Vertices.Length; ++i) {
                    writer.Write(physics.Vertices[i].position.x);
                    writer.Write(physics.Vertices[i].position.y);
                    writer.Write(physics.Vertices[i].position.z);
                    writer.Write(0.0f);
                    writer.Write(0.0f);
                    writer.Write(0.0f);
                    writer.Write((byte)0);
                }

                for (int i = 0; i < physics.Indices.Length; ++i) {
                    writer.Write((byte)3);
                    writer.Write(physics.Indices[i].index.v1);
                    writer.Write(physics.Indices[i].index.v2);
                    writer.Write(physics.Indices[i].index.v3);
                }
            }
            return true;
        }

        // ReSharper disable once InconsistentNaming
        public bool Write(Chunked chunked, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data) {
            IChunk chunk = chunked.FindNextChunk("MNRM").Value;
            if (chunk == null) {
                return false;
            }
            MNRM model = (MNRM)chunk;
            chunk = chunked.FindNextChunk("CLDM").Value;
            CLDM materials = null;
            if (chunk != null) {
                materials = (CLDM)chunk;
            }
            chunk = chunked.FindNextChunk("lksm").Value;
            lksm skeleton = null;
            if (chunk != null) {
                skeleton = (lksm)chunk;
            }
            chunk = chunked.FindNextChunk("PRHM").Value;
            PRHM hardpoints = null;
            if (chunk != null) {
                hardpoints = (PRHM)chunk;
            }
            
            short[] hierarchy = (short[])skeleton?.Hierarchy.Clone();
            Dictionary<int, HTLC.ClothNode> nodeMap = new Dictionary<int, HTLC.ClothNode>();
            
            if (chunked.FindNextChunk("HTLC").Value is HTLC cloth) {
                uint clothIndex = 0;
                foreach (HTLC.ClothNode[] nodeCollection in cloth.Nodes) {
                    if (nodeCollection == null) continue;
                    int nodeIndex = 0;
                    foreach (HTLC.ClothNode node in nodeCollection) {
                        int parentRaw = node.VerticalParent;
                        if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex) &&
                            cloth.NodeBones[clothIndex].ContainsKey(parentRaw)) {
                            if (cloth.NodeBones[clothIndex][nodeIndex] != -1) {
                                hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = cloth.NodeBones[clothIndex][parentRaw];
                                if (cloth.NodeBones[clothIndex][parentRaw] == -1) {
                                    HTLC.ClothNodeWeight weightedBone = node.Bones.Aggregate((i1, i2) => i1.Weight > i2.Weight ? i1 : i2);
                                    hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = weightedBone.Bone;
                                }
                            }
                        } else {
                            if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex)) {  // if on main skele
                                if (cloth.NodeBones[clothIndex][nodeIndex] != -1) {
                                    hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = -1;
                                    HTLC.ClothNodeWeight weightedBone = node.Bones.Aggregate((i1, i2) => i1.Weight > i2.Weight ? i1 : i2);
                                    hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = weightedBone.Bone;
                                }
                            }
                        }
                        if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex)) {
                            // good code:
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
                writer.Write((ushort)1); // version major
                writer.Write((ushort)4); // version minor

                if (data.Length > 1 && data[1] is string && ((string)data[1]).Length > 0) {
                    writer.Write((string)data[1]);
                } else {
                    writer.Write((byte)0);
                }

                if (data.Length > 2 && data[2] is string && ((string)data[2]).Length > 0) {
                    writer.Write((string)data[2]);
                } else {
                    writer.Write((byte)0);
                }

                if (skeleton == null) {
                    writer.Write((ushort)0); // number of bones
                } else {
                    writer.Write(skeleton.Data.bonesAbs);
                }

                // ReSharper disable once InconsistentNaming
                Dictionary<byte, List<int>> LODMap = new Dictionary<byte, List<int>>();
                uint sz = 0;
                uint lookForLod = 0;
                bool lodOnly = data.Length > 3 && data[3] is bool && (bool)data[3];
                for (int i = 0; i < model.Submeshes.Length; ++i) {
                    SubmeshDescriptor submesh = model.Submeshes[i];
                    if (data.Length > 4 && data[4] is bool && (bool)data[4]) {
                        if (submesh.flags == SubmeshFlags.COLLISION_MESH) {
                            continue;
                        }
                    }
                    if (LODs != null && !LODs.Contains(submesh.lod)) {
                        continue;
                    }
                    if (lodOnly && lookForLod > 0 && submesh.lod != lookForLod) {
                        continue;
                    }
                    if (!LODMap.ContainsKey(submesh.lod)) {
                        LODMap.Add(submesh.lod, new List<int>());
                    }
                    lookForLod = submesh.lod;
                    sz++;
                    LODMap[submesh.lod].Add(i);
                }

                //long meshCountPos = writer.BaseStream.Position;
                writer.Write(sz);

                writer.Write(hardpoints?.HardPoints.Length ?? 0);

                if (skeleton != null) {
                    for (int i = 0; i < skeleton.Data.bonesAbs; ++i) {
                        writer.Write(IdToString("bone", skeleton.IDs[i]));
                        short parent = hierarchy[i];
                        if (parent == -1) {
                            parent = (short)i;
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
                        writer.Write((byte)uv.Length);
                        
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
                            if (skeleton != null && bones != null && bones[j].boneIndex != null && bones[j].boneWeight != null) {
                                writer.Write((byte)4);
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
                                writer.Write((byte)0);
                            }
                        }
                        List<ModelIndiceModifiable> indexNew = new List<ModelIndiceModifiable>();
                        foreach (ModelIndice indice in index) {
                            indexNew.Add(new ModelIndiceModifiable {v1 = indice.v1, v2 = indice.v2, v3 = indice.v3});
                        }
                        foreach (ModelIndiceModifiable indice in indexNew) {
                            writer.Write((byte)3);
                            writer.Write(indice.v1);
                            writer.Write(indice.v2);
                            writer.Write(indice.v3);
                        }
                    }
                }
                
                if (hardpoints != null) {
                    // attachments
                    foreach (PRHM.HardPoint hp in hardpoints.HardPoints) {
                        writer.Write(IdToString("attachment_", GUID.Index(hp.HardPointGUID)));
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
                        // if (parent == -1) {
                        //     parent = (short)i;
                        // }
                        writer.Write(parent);
                        
                        Matrix3x4 bone = skeleton.Matrices34Inverted[i];
                        
                        // Quaternion3D quat = new Quaternion3D(bone[0, 0], bone[0, 1], bone[0, 2], bone[0, 3]);
                        // why are they different
                        Quaternion3D quat = new Quaternion3D(bone[0, 3], bone[0, 0], bone[0, 1], bone[0, 2]);
                        
                        Vector3D rot = C3D.ToEulerAngles(quat);
                        if (rot.X == -3.14159274f && rot.Y == 0 && rot.Z == 0) {
                            rot = new Vector3D(0, 3.14159274f, 3.14159274f); // effectively the same but you know, eulers.
                        }
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
            }
            
            return true;
        }

        public static string IdToString(string prefix, uint id) {
            return $"{prefix}_{id:X4}";
        }

        public bool Write(Animation anim, Stream output, object[] data) {
            return false;
        }

        public Dictionary<ulong, List<string>>[] Write(Stream output, Map map, Map detail1, Map detail2, Map props, Map lights, string name, IDataWriter modelFormat) {
            return null;
        }
    }
}
