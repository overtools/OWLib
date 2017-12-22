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

    public class ClothInfo {
        public HTLC Cloth;
        public int ID;
        public List<List<ClothVertContainer>> ConnectionVerts; // from bone data, done later
        public List<int> ConnectionBones;
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
            List<short> clothBones = new List<short>();
            Dictionary<short, int> clothBoneMap = new Dictionary<short, int>(); // bone: cloth
            Dictionary<int, HTLC.ClothNode> nodeMap = new Dictionary<int, HTLC.ClothNode>();
            
            List<ClothInfo> clothes = new List<ClothInfo>();  // hmm, engligh is weird?
            if (chunked.FindNextChunk("HTLC").Value is HTLC cloth) {
                uint clothIndex = 0;
                foreach (HTLC.ClothNode[] nodeCollection in cloth.Nodes) {
                    if (nodeCollection == null) continue;
                    int nodeIndex = 0;
                    // ClothInfo newCloth = new ClothInfo {Cloth = cloth, ConnectionVerts = new List<List<ClothVertContainer>>(), ConnectionBones = new List<int>(), ID=(int)clothIndex};
                    // clothes.Add(newCloth);
                    foreach (HTLC.ClothNode node in nodeCollection) {
                        int parentRaw = node.VerticalParent;
                        if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex) &&
                            cloth.NodeBones[clothIndex].ContainsKey(parentRaw)) {
                            // good code:
                            if (cloth.NodeBones[clothIndex][nodeIndex] != -1) {
                                hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = cloth.NodeBones[clothIndex][parentRaw];
                                if (cloth.NodeBones[clothIndex][parentRaw] == -1) {
                                    HTLC.ClothNodeWeight weightedBone = node.Bones.Aggregate((i1, i2) => i1.Weight > i2.Weight ? i1 : i2);
                                    hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = weightedBone.Bone;
                                    // newCloth.ConnectionBones.Add(cloth.NodeBones[clothIndex][nodeIndex]);
                                }
                            }
                            // else: on subskele
                            // todo: add subskelebones
                        } else {
                            if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex)) {  // if on main skele
                                // good code:
                                if (cloth.NodeBones[clothIndex][nodeIndex] != -1) {
                                    hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = -1;
                                    HTLC.ClothNodeWeight weightedBone = node.Bones.Aggregate((i1, i2) => i1.Weight > i2.Weight ? i1 : i2);
                                    hierarchy[cloth.NodeBones[clothIndex][nodeIndex]] = weightedBone.Bone;
                                    // newCloth.ConnectionBones.Add(cloth.NodeBones[clothIndex][nodeIndex]);
                                }
                                // else: on subskele
                                // todo: add subskelebones
                            }
                        }
                        if (cloth.NodeBones[clothIndex].ContainsKey(nodeIndex)) {
                            // good code:
                            if (cloth.NodeBones[clothIndex][nodeIndex] != -1) {
                                clothBones.Add(cloth.NodeBones[clothIndex][nodeIndex]);
                                clothBoneMap[cloth.NodeBones[clothIndex][nodeIndex]] = (int)clothIndex;
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
                
                // todo: @zb: rewrite this please
                
                Dictionary<int, Dictionary<int, ClothMesh>> clothMeshes = new Dictionary<int, Dictionary<int, ClothMesh>>();

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
                        
                        // List<int> removedVerts = new List<int>();
                        //Dictionary<int, int> newVertIDs = new Dictionary<int, int>();  // todo: remove
                        
                        writer.Write(vertex.Length);
                        //long indexPos = writer.BaseStream.Position;
                        writer.Write(index.Length);
                        // int fakeVertCount = 0;
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
                                // if (clothBones.Contains((short) skeleton.Lookup[bones[j].boneIndex[0]]) ||
                                //     clothBones.Contains((short) skeleton.Lookup[bones[j].boneIndex[1]]) ||
                                //     clothBones.Contains((short) skeleton.Lookup[bones[j].boneIndex[2]]) ||
                                //     clothBones.Contains((short) skeleton.Lookup[bones[j].boneIndex[3]])) {
                                //     removedVerts.Add(j);
                                //     int parentCloth = -1;
                                //     foreach (ushort boneIdx in bones[j].boneIndex) {
                                //         if (clothBones.Contains((short) skeleton.Lookup[boneIdx])) {
                                //             parentCloth = clothBoneMap[(short) skeleton.Lookup[boneIdx]];
                                //         }
                                //     }
                                //     if (parentCloth == -1) {
                                //         Debugger.Break();
                                //         continue;
                                //     }
                                //     
                                //     if (!clothMeshes.ContainsKey(parentCloth)) {
                                //         clothMeshes[parentCloth] = new Dictionary<int, ClothMesh>();
                                //     }
                                //     if (!clothMeshes[parentCloth].ContainsKey(i)) {
                                //         clothMeshes[parentCloth][i] = new ClothMesh {
                                //             Indices = new List<ModelIndiceModifiable>(),
                                //             Material = materials.Materials[submesh.material],
                                //             Vertcies = new List<ClothVertContainer>(),
                                //             Coth = parentCloth,
                                //             VertIDs = new List<int>(), 
                                //             UV = model.TextureCoordinates[i],
                                //             Submesh = i
                                //         };
                                //     }
                                //     
                                //     clothMeshes[parentCloth][i].Vertcies.Add(new ClothVertContainer {Vertex = vertex[j],
                                //         Normal = normal[j], Bones = bones[j], J = j});
                                //     clothMeshes[parentCloth][i].VertIDs.Add(j);
                                // }
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
                            // newVertIDs[j] = fakeVertCount;
                            // fakeVertCount++;
                        }
                        List<ModelIndiceModifiable> indexNew = new List<ModelIndiceModifiable>();
                        foreach (ModelIndice indice in index) {
                            // if (removedVerts.Contains(indice.v1) && removedVerts.Contains(indice.v2) && removedVerts.Contains(indice.v3)) {
                            //     
                            //     foreach (KeyValuePair<int, Dictionary<int, ClothMesh>> clothMesh in clothMeshes) {
                            //         foreach (KeyValuePair<int, ClothMesh> clothSubmesh in clothMesh.Value) {
                            //             foreach (ushort indiceVert in new [] {indice.v1, indice.v2, indice.v3}) {
                            //                 if (!clothSubmesh.Value.VertIDs.Contains(indiceVert)) continue;
                            //                 int parentCloth = clothMesh.Key;
                            //                 int parentMesh = clothSubmesh.Value.Submesh;
                            //                 clothMeshes[parentCloth][parentMesh].Indices.Add(new ModelIndiceModifiable {v1 = indice.v1, 
                            //                     v2 = indice.v2, v3 = indice.v3
                            //                 });
                            //             }
                            //         }
                            //     }
                            //     continue;
                            // }

                            // if (newVertIDs.Count == 0 && fakeVertCount == 0) {
                            //     indexNew.Add(new ModelIndiceModifiable {v1 = indice.v1, v2 = indice.v2, v3 = indice.v3});
                            //     continue;
                            // }

                            // indexNew.Add(new ModelIndiceModifiable {v1 = newVertIDs[indice.v1], 
                            //     v2 = newVertIDs[indice.v2], v3 = newVertIDs[indice.v3]
                            // });
                            indexNew.Add(new ModelIndiceModifiable {v1 = indice.v1, v2 = indice.v2, v3 = indice.v3});
                        }
                        foreach (ModelIndiceModifiable indice in indexNew) {
                            writer.Write((byte)3);
                            writer.Write(indice.v1);
                            writer.Write(indice.v2);
                            writer.Write(indice.v3);
                        }
                        // long tempPos = writer.BaseStream.Position;
                        // writer.BaseStream.Position = indexPos;
                        // writer.Write(indexNew.Count);
                        // writer.BaseStream.Position = tempPos;
                    }
                }
                //foreach (KeyValuePair<int, Dictionary<int, ClothMesh>> clothMesh in clothMeshes) {
                //    HTLC.ClothDesc desc = cloth.Descriptors[clothMesh.Key];
                //    int submeshIndex = 0;
                //    foreach (KeyValuePair<int, ClothMesh> clothSubmesh in clothMesh.Value) {
                //        writer.Write($"Submesh_Cloth.{desc.Name}.{clothSubmesh.Value.Submesh}");
                //        clothSubmesh.Value.FileIndex = sz;
                //    
                //        List<ModelIndiceModifiable> indices = new List<ModelIndiceModifiable>();
                //        Dictionary<int, int> vertRemap = new Dictionary<int, int>();
                
                //        int vertIndex = 0;
                //        foreach (ClothVertContainer vertContainer in clothSubmesh.Value.Vertcies) {
                //            vertRemap[vertContainer.J] = vertIndex;
                //            vertIndex++;
                //        }
                
                //        foreach (ModelIndiceModifiable index in clothSubmesh.Value.Indices) {
                //            if (clothSubmesh.Value.VertIDs.Contains(index.v1) && clothSubmesh.Value.VertIDs.Contains(index.v2) &&
                //                clothSubmesh.Value.VertIDs.Contains(index.v3)) {
                //                indices.Add(new ModelIndiceModifiable {v1 = vertRemap[index.v1], v2 = vertRemap[index.v2], v3 = vertRemap[index.v3]});
                //            }
                //        }
                //        
                //        writer.Write(clothSubmesh.Value.Material);
                //        writer.Write((byte)clothSubmesh.Value.UV.Length);
                //        writer.Write(clothSubmesh.Value.Vertcies.Count);
                //        writer.Write(indices.Count);
                //        
                //        clothes[clothMesh.Key].ConnectionVerts.Add(new List<ClothVertContainer>());
                
                //        vertIndex = 0;
                //        foreach (ClothVertContainer vertContainer in clothSubmesh.Value.Vertcies) {
                //            writer.Write(vertContainer.Vertex.x);
                //            writer.Write(vertContainer.Vertex.y);
                //            writer.Write(vertContainer.Vertex.z);
                //            writer.Write(-vertContainer.Normal.x);
                //            writer.Write(-vertContainer.Normal.y);
                //            writer.Write(-vertContainer.Normal.z);
                //            foreach (ModelUV[] t in clothSubmesh.Value.UV) {
                //                writer.Write(t[vertContainer.J].u);
                //                writer.Write(t[vertContainer.J].v);
                //            }
                //            if (skeleton != null && vertContainer.Bones.boneIndex != null && vertContainer.Bones.boneWeight != null) {
                //                writer.Write((byte)4);
                //                writer.Write(skeleton.Lookup[vertContainer.Bones.boneIndex[0]]);
                //                writer.Write(skeleton.Lookup[vertContainer.Bones.boneIndex[1]]);
                //                writer.Write(skeleton.Lookup[vertContainer.Bones.boneIndex[2]]);
                //                writer.Write(skeleton.Lookup[vertContainer.Bones.boneIndex[3]]);
                //                writer.Write(vertContainer.Bones.boneWeight[0]);
                //                writer.Write(vertContainer.Bones.boneWeight[1]);
                //                writer.Write(vertContainer.Bones.boneWeight[2]);
                //                writer.Write(vertContainer.Bones.boneWeight[3]);
                
                //                if (clothes[clothMesh.Key].ConnectionBones
                //                        .Contains(skeleton.Lookup[vertContainer.Bones.boneIndex[0]]) ||
                //                    clothes[clothMesh.Key].ConnectionBones
                //                        .Contains(skeleton.Lookup[vertContainer.Bones.boneIndex[1]]) ||
                //                    clothes[clothMesh.Key].ConnectionBones
                //                        .Contains(skeleton.Lookup[vertContainer.Bones.boneIndex[2]]) ||
                //                    clothes[clothMesh.Key].ConnectionBones
                //                        .Contains(skeleton.Lookup[vertContainer.Bones.boneIndex[3]])) {
                //                    vertContainer.J = vertRemap[vertContainer.J]; // warning: invalid after this point
                //                    clothes[clothMesh.Key].ConnectionVerts[submeshIndex].Add(vertContainer);
                //                }
                //            } else {
                //                // bone -> size + index + weight
                //                writer.Write((byte)0);
                //            }
                //            vertIndex++;
                //        }
                //        foreach (ModelIndiceModifiable indice in indices) {
                //            writer.Write((byte)3);
                //            writer.Write(indice.v1);
                //            writer.Write(indice.v2);
                //            writer.Write(indice.v3);
                //        }
                //        sz++;
                //        submeshIndex++;
                //    }
                //    
                //}
                // long tempPos2 = writer.BaseStream.Position;
                // writer.BaseStream.Position = meshCountPos;
                // writer.Write(sz);
                // writer.BaseStream.Position = tempPos2;
                
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
                // writer.Write(clothes.Count); // count
                // int clothIndex = 0;
                // foreach (ClothInfo clothInfo in clothes) {
                //     HTLC.ClothDesc desc = clothInfo.Cloth.Descriptors[clothInfo.ID];
                //     writer.Write(desc.Name);
                //     if (!clothMeshes.ContainsKey(clothIndex)) {
                //         Debugger.Log(0, "OWMDLWriter", $"[OWMDLWriter]: Cloth does not exist {clothIndex}");
                //         writer.Write(0);
                //         clothIndex++;
                //         continue;
                //     }
                //     Dictionary<int, ClothMesh> meshes = clothMeshes[clothIndex];
                //     writer.Write(meshes.Count);
                //     int meshIndex = 0;
                //     foreach (KeyValuePair<int,ClothMesh> clothMesh in meshes) {
                //         writer.Write(clothMesh.Value.FileIndex);
                //         writer.Write(clothInfo.ConnectionVerts[meshIndex].Count);
                //         writer.Write($"Submesh_Cloth.{desc.Name}.{clothMesh.Value.Submesh}"); 
                //         foreach (ClothVertContainer vertContainer in clothInfo.ConnectionVerts[meshIndex]) {
                //             writer.Write(vertContainer.J);
                //         }
                //         meshIndex++;
                //     }
                //     clothIndex++;
                // }
                
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
                        // Vector3 pos2 = new Vector3();
                        // if (parent != i) {
                        //     pos2 = RefPoseWriter.GetGlobalPos(skeleton.)
                        // }
                        // if (nodeMap.ContainsKey(i)) {
                        //     HTLC.ClothNode thisNode = nodeMap[i];
                        //     pos.X = thisNode.X;
                        //     pos.Y = thisNode.Y;
                        //     pos.Z = thisNode.Z;
                        // }
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
