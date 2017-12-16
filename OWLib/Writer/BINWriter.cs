using System;
using System.Collections.Generic;
using System.IO;
using OWLib.Types;
using OWLib.Types.Chunk;
using OWLib.Types.Map;

namespace OWLib.Writer {
    public class BINWriter : IDataWriter {
        private static void WriteString(BinaryWriter stream, string str) {
            stream.Write(str);
        }

        public static readonly float Rad2Deg = 360.0f / (float)(Math.PI * 2f);

        public string Name => "XNALara XPS Binary";
        public string Format => ".mesh";
        public char[] Identifier => new char[2] { 'L', 'b' };
        public WriterSupport SupportLevel => (WriterSupport.VERTEX | WriterSupport.UV | WriterSupport.BONE | WriterSupport.POSE | WriterSupport.MATERIAL | WriterSupport.MODEL);

        public static OpenTK.Vector3 NormalizeAngles(OpenTK.Vector3 angles) {
            angles.X = NormalizeAngle(angles.X);
            angles.Y = NormalizeAngle(angles.Y);
            angles.Z = NormalizeAngle(angles.Z);
            return angles;
        }

        public static float NormalizeAngle(float angle) {
            while (angle > 360) {
                angle -= 360;
            }
            while (angle < 0) {
                angle += 360;
            }
            return angle;
        }

        public static OpenTK.Vector3 QuaternionToVector(OpenTK.Quaternion q1) {
            float sqw = q1.W * q1.W;
            float sqx = q1.X * q1.X;
            float sqy = q1.Y * q1.Y;
            float sqz = q1.Z * q1.Z;
            float unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            float test = q1.X * q1.W - q1.Y * q1.Z;
            OpenTK.Vector3 v = new OpenTK.Vector3();

            if (test > 0.4995f * unit) { // singularity at north pole
                v.Y = 2f * (float)Math.Atan2(q1.Y, q1.X);
                v.X = (float)Math.PI / 2;
                v.Z = 0;
                return NormalizeAngles(v * Rad2Deg);
            }
            if (test < -0.4995f * unit) { // singularity at south pole
                v.Y = -2f * (float)Math.Atan2(q1.Y, q1.X);
                v.X = (float)Math.PI / 2;
                v.Z = 0;
                return NormalizeAngles(v * Rad2Deg);
            }
            OpenTK.Quaternion q = new OpenTK.Quaternion(q1.W, q1.Z, q1.X, q1.Y);
            v.Y = (float)Math.Atan2(2f * q.X * q.W + 2f * q.Y * q.Z, 1 - 2f * (q.Z * q.Z + q.W * q.W));       // Yaw
            v.X = (float)Math.Asin(2f * (q.X * q.Z - q.W * q.Y));                                             // Pitch
            v.Z = (float)Math.Atan2(2f * q.X * q.Y + 2f * q.Z * q.W, 1 - 2f * (q.Y * q.Y + q.Z * q.Z));       // Roll
            return NormalizeAngles(v * Rad2Deg);
        }

        public bool Write(Chunked chunked, Stream stream, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] opts) {
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

            //Console.Out.WriteLine("Writing BIN");
            using (BinaryWriter writer = new BinaryWriter(stream)) {
                writer.Write((uint)323232);
                writer.Write((ushort)2);
                writer.Write((ushort)99);
                WriteString(writer, "XNAaraL");
                writer.Write((uint)5);
                WriteString(writer, "OVERWATCH");
                WriteString(writer, "BLIZZARD");
                WriteString(writer, "NULL");
                writer.Write((uint)180); // hash
                writer.Write((uint)1); // items
                                       // item 1
                writer.Write((uint)1); // type; 1 = pose; 2 = flags; 255 = padding
                                       // pose
                writer.Write((uint)0); // size pow 4
                writer.Write((uint)0); // op info; bone count

                /*
                pose data is always ASCII.
                Each line is:
                for each bone:
                boneName:rotx roty rotz posx posy posz scalex scaley scalez
                */

                if (skeleton != null) {
                    writer.Write((uint)skeleton.Data.bonesAbs);
                    for (int i = 0; i < skeleton.Data.bonesAbs; ++i) {
                        WriteString(writer, $"bone_{skeleton.IDs[i]:X4}");
                        short parent = skeleton.Hierarchy[i];
                        if (parent == -1) {
                            parent = (short)i;
                        }
                        writer.Write(parent);
                        OpenTK.Vector3 bonePos = skeleton.Matrices[i].ExtractTranslation();
                        writer.Write(bonePos.X);
                        writer.Write(bonePos.Y);
                        writer.Write(bonePos.Z);
                    }
                } else {
                    writer.Write((uint)0);
                }

                Dictionary<byte, List<int>> LODMap = new Dictionary<byte, List<int>>();
                uint sz = 0;
                uint lookForLod = 0;
                bool lodOnly = false;
                if (opts.Length > 3 && opts[3] != null && opts[3].GetType() == typeof(bool) && (bool)opts[3] == true) {
                    lodOnly = true;
                }
                for (int i = 0; i < model.Submeshes.Length; ++i) {
                    SubmeshDescriptor submesh = model.Submeshes[i];
                    if (opts.Length > 4 && opts[4] != null && opts[4].GetType() == typeof(bool) && (bool)opts[4] == true) {
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
                writer.Write(sz);
                foreach (KeyValuePair<byte, List<int>> kv in LODMap) {
                    //Console.Out.WriteLine("Writing LOD {0}", kv.Key);
                    foreach (int i in kv.Value) {
                        SubmeshDescriptor submesh = model.Submeshes[i];
                        ModelVertex[] vertex = model.Vertices[i];
                        ModelVertex[] normal = model.Normals[i];
                        ModelUV[][] uv = model.TextureCoordinates[i];
                        ModelIndice[] index = model.Indices[i];
                        ModelBoneData[] bones = model.Bones[i];
                        ulong materialKey = submesh.material;
                        if (materials != null) {
                            materialKey = materials.Materials[submesh.material];
                        }
                        WriteString(writer, $"Submesh_{i}.{kv.Key}.{materialKey:X16}");
                        writer.Write((uint)uv.Length);
                        if (layers.ContainsKey(materialKey)) {
                            List<ImageLayer> materialLayers = layers[materialKey];
                            uint count = 0;
                            HashSet<ulong> done = new HashSet<ulong>();
                            for (int j = 0; j < materialLayers.Count; ++j) {
                                if (done.Add(materialLayers[j].Key)) {
                                    count += 1;
                                }
                            }
                            writer.Write(count);
                            done.Clear();
                            for (int j = 0; j < materialLayers.Count; ++j) {
                                if (done.Add(materialLayers[j].Key)) {
                                    writer.Write($"{GUID.LongKey(materialLayers[j].Key):X12}.dds");
                                    writer.Write((uint)0);
                                }
                            }
                        } else {
                            writer.Write((uint)uv.Length);
                            for (int j = 0; j < uv.Length; ++j) {
                                writer.Write($"{materialKey:X16}_UV{j}.dds");
                                writer.Write((uint)j);
                            }
                        }

                        writer.Write((uint)vertex.Length);
                        for (int j = 0; j < vertex.Length; ++j) {
                            writer.Write(vertex[j].x);
                            writer.Write(vertex[j].y);
                            writer.Write(vertex[j].z);
                            writer.Write(-normal[j].x);
                            writer.Write(-normal[j].y);
                            writer.Write(-normal[j].z);
                            writer.Write((byte)255);
                            writer.Write((byte)255);
                            writer.Write((byte)255);
                            writer.Write((byte)255);
                            for (int k = 0; k < uv.Length; ++k) {
                                writer.Write((float)uv[k][j].u);
                                writer.Write((float)uv[k][j].v);
                            }
                            if (skeleton != null && skeleton.Data.bonesAbs > 0) {
                                if (bones != null && bones[j].boneIndex != null && bones[j].boneWeight != null) {
                                    writer.Write(skeleton.Lookup[bones[j].boneIndex[0]]);
                                    writer.Write(skeleton.Lookup[bones[j].boneIndex[1]]);
                                    writer.Write(skeleton.Lookup[bones[j].boneIndex[2]]);
                                    writer.Write(skeleton.Lookup[bones[j].boneIndex[3]]);
                                    writer.Write(bones[j].boneWeight[0]);
                                    writer.Write(bones[j].boneWeight[1]);
                                    writer.Write(bones[j].boneWeight[2]);
                                    writer.Write(bones[j].boneWeight[3]);
                                } else {
                                    writer.Write((ushort)0);
                                    writer.Write((ushort)0);
                                    writer.Write((ushort)0);
                                    writer.Write((ushort)0);
                                    writer.Write(0.0f);
                                    writer.Write(0.0f);
                                    writer.Write(0.0f);
                                    writer.Write(0.0f);
                                }
                            }
                        }
                        writer.Write((uint)index.Length);
                        for (int j = 0; j < index.Length; ++j) {
                            writer.Write((uint)index[j].v1);
                            writer.Write((uint)index[j].v2);
                            writer.Write((uint)index[j].v3);
                        }
                    }
                }
            }
            return true;
        }

        public bool Write(Map10 physics, Stream output, object[] data) {
            //Console.Out.WriteLine("Writing BIN");
            using (BinaryWriter writer = new BinaryWriter(output)) {
                writer.Write((uint)323232);
                writer.Write((ushort)2);
                writer.Write((ushort)99);
                WriteString(writer, "XNAaraL");
                writer.Write((uint)5);
                WriteString(writer, "OVERWATCH");
                WriteString(writer, "BLIZZARD");
                WriteString(writer, "NULL");
                writer.Write((uint)180);
                writer.Write((uint)1);
                writer.Write((uint)1);
                writer.Write((uint)0);
                writer.Write((uint)0);

                writer.Write(0);
                writer.Write(1);

                WriteString(writer, "Physics");
                writer.Write(0);
                writer.Write(0);
                writer.Write(physics.Vertices.Length);
                for (int i = 0; i < physics.Vertices.Length; ++i) {
                    writer.Write(physics.Vertices[i].position.x);
                    writer.Write(physics.Vertices[i].position.y);
                    writer.Write(physics.Vertices[i].position.z);
                    writer.Write(0.0f);
                    writer.Write(0.0f);
                    writer.Write(0.0f);
                    writer.Write((byte)255);
                    writer.Write((byte)255);
                    writer.Write((byte)255);
                    writer.Write((byte)255);
                }
                writer.Write(physics.Indices.Length);
                for (int i = 0; i < physics.Indices.Length; ++i) {
                    writer.Write(physics.Indices[i].index.v1);
                    writer.Write(physics.Indices[i].index.v2);
                    writer.Write(physics.Indices[i].index.v3);
                }
            }
            return true;
        }

        public bool Write(Animation anim, Stream output, object[] data) {
            return false;
        }

        public Dictionary<ulong, List<string>>[] Write(Stream output, Map map, Map detail1, Map detail2, Map props, Map lights, string name, IDataWriter modelFormat) {
            return null;
        }
    }
}
