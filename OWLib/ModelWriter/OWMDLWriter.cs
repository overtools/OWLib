using System;
using System.Collections.Generic;
using System.IO;
using OpenTK;
using OWLib.Types;
using OWLib.Types.Chunk;
using OWLib.Types.Map;

namespace OWLib.ModelWriter {
    public class OWMDLWriter : IModelWriter {
        public string Format => ".owmdl";

        public char[] Identifier => new char[1] { 'w' };
        public string Name => "OWM Model Format";

        public ModelWriterSupport SupportLevel => (ModelWriterSupport.VERTEX | ModelWriterSupport.UV | ModelWriterSupport.BONE | ModelWriterSupport.POSE | ModelWriterSupport.MATERIAL | ModelWriterSupport.ATTACHMENT);

        public bool Write(Map10 physics, Stream output, object[] data) {
            Console.Out.WriteLine("Writing OWMDL");
            using (BinaryWriter writer = new BinaryWriter(output)) {
                writer.Write((ushort)1);
                writer.Write((ushort)0);
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

            Console.Out.WriteLine("Writing OWMDL");
            using (BinaryWriter writer = new BinaryWriter(output)) {
                writer.Write((ushort)1); // version major
                writer.Write((ushort)1); // version minor

                if (data.Length > 1 && data[1] != null && data[1].GetType() == typeof(string) && ((string)data[1]).Length > 0) {
                    writer.Write((string)data[1]);
                } else {
                    writer.Write((byte)0);
                }

                if (data.Length > 2 && data[2] != null && data[2].GetType() == typeof(string) && ((string)data[2]).Length > 0) {
                    writer.Write((string)data[2]);
                } else {
                    writer.Write((byte)0);
                }

                if (skeleton == null) {
                    writer.Write((ushort)0); // number of bones
                } else {
                    writer.Write(skeleton.Data.bonesAbs);
                }

                Dictionary<byte, List<int>> LODMap = new Dictionary<byte, List<int>>();
                uint sz = 0;
                uint lookForLod = 0;
                bool lodOnly = false;
                if (data.Length > 3 && data[3] != null && data[3].GetType() == typeof(bool) && (bool)data[3] == true) {
                    lodOnly = true;
                }
                for (int i = 0; i < model.Submeshes.Length; ++i) {
                    SubmeshDescriptor submesh = model.Submeshes[i];
                    if (data.Length > 4 && data[4] != null && data[4].GetType() == typeof(bool) && (bool)data[4] == true) {
                        if ((SubmeshFlags)submesh.flags == SubmeshFlags.COLLISION_MESH) {
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

                writer.Write(sz);

                if (hardpoints != null) {
                    writer.Write(hardpoints.HardPoints.Length);
                } else {
                    writer.Write((int)0); // number of attachments
                }

                if (skeleton != null) {
                    for (int i = 0; i < skeleton.Data.bonesAbs; ++i) {
                        writer.Write($"bone_{skeleton.IDs[i]:X4}");
                        short parent = skeleton.Hierarchy[i];
                        if (parent == -1) {
                            parent = (short)i;
                        }
                        writer.Write(parent);
                        Vector3 pos = skeleton.Matrices[i].ExtractTranslation();
                        Quaternion rot = skeleton.Matrices[i].ExtractRotation();
                        Vector3 scl = skeleton.Matrices[i].ExtractScale();
                        writer.Write(pos.X);
                        writer.Write(pos.Y);
                        writer.Write(pos.Z);
                        writer.Write(scl.X);
                        writer.Write(scl.X);
                        writer.Write(scl.X);
                        writer.Write(rot.X);
                        writer.Write(rot.Y);
                        writer.Write(rot.Z);
                        writer.Write(rot.W);
                    }
                }

                foreach (KeyValuePair<byte, List<int>> kv in LODMap) {
                    Console.Out.WriteLine("Writing LOD {0}", kv.Key);
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
                            for (int k = 0; k < uv.Length; ++k) {
                                writer.Write((float)uv[k][j].u);
                                writer.Write((float)uv[k][j].v);
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
                        for (int j = 0; j < index.Length; ++j) {
                            writer.Write((byte)3);
                            writer.Write((int)index[j].v1);
                            writer.Write((int)index[j].v2);
                            writer.Write((int)index[j].v3);
                        }
                    }
                }
                if (hardpoints != null) {
                    // attachments
                    for (int i = 0; i < hardpoints.HardPoints.Length; ++i) {
                        PRHM.HardPoint hp = hardpoints.HardPoints[i];
                        writer.Write($"attachment_{hp.name:X}");
                        Matrix4 mat = hp.matrix.ToOpenTK();
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
                    for (int i = 0; i < hardpoints.HardPoints.Length; ++i) {
                        PRHM.HardPoint hp = hardpoints.HardPoints[i];
                        writer.Write($"bone_{hp.id:X}");
                    }
                }
            }
            return true;
        }
    }
}
