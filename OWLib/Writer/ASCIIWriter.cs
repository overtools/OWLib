using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OWLib.Types;
using OWLib.Types.Chunk;
using OWLib.Types.Map;

namespace OWLib.Writer {
    public class ASCIIWriter : IDataWriter {
        public string Name => "XNALara XPS ASCII";
        public string Format => ".mesh.ascii";
        public char[] Identifier => new char[2] { 'l', 'a' };
        private CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        public WriterSupport SupportLevel => (WriterSupport.VERTEX | WriterSupport.UV | WriterSupport.BONE | WriterSupport.MATERIAL | WriterSupport.MODEL);

        public bool Write(Chunked chunked, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] opts) {
            culture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;

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

            //Console.Out.WriteLine("Writing ASCII");
            using (StreamWriter writer = new StreamWriter(output)) {
                if (skeleton != null) {
                    writer.WriteLine(skeleton.Data.bonesAbs);
                    for (int i = 0; i < skeleton.Data.bonesAbs; ++i) {
                        writer.WriteLine("bone_{0:X4}", skeleton.IDs[i]);
                        writer.WriteLine(skeleton.Hierarchy[i]);
                        OpenTK.Vector3 bonePos = skeleton.Matrices[i].ExtractTranslation();
                        writer.WriteLine("{0:0.000000} {1:0.000000} {2:0.000000}", bonePos.X, bonePos.Y, bonePos.Z);
                    }
                } else {
                    writer.WriteLine("0");
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

                writer.WriteLine(sz);
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

                        writer.WriteLine("Submesh_{0}.{1}.{2:X16}", i, kv.Key, materialKey);
                        writer.WriteLine(uv.Length);
                        if (layers.ContainsKey(materialKey)) {
                            List<ImageLayer> materialLayers = layers[materialKey];
                            uint count = 0;
                            HashSet<ulong> done = new HashSet<ulong>();
                            for (int j = 0; j < materialLayers.Count; ++j) {
                                if (done.Add(materialLayers[j].Key)) {
                                    count += 1;
                                }
                            }
                            writer.WriteLine(count);
                            done.Clear();
                            for (int j = 0; j < materialLayers.Count; ++j) {
                                if (done.Add(materialLayers[j].Key)) {
                                    writer.WriteLine($"{GUID.LongKey(materialLayers[j].Key):X12}.dds");
                                    writer.WriteLine(0);
                                }
                            }
                        } else {
                            writer.WriteLine(uv.Length);
                            for (int j = 0; j < uv.Length; ++j) {
                                writer.WriteLine("{0:X16}_UV{1}.dds", materialKey, j);
                                writer.WriteLine(j);
                            }
                        }

                        writer.WriteLine(vertex.Length);
                        for (int j = 0; j < vertex.Length; ++j) {
                            writer.WriteLine("{0} {1} {2}", vertex[j].x, vertex[j].y, vertex[j].z);
                            writer.WriteLine("{0} {1} {2}", -normal[j].x, -normal[j].y, -normal[j].z);
                            writer.WriteLine("255 255 255 255");
                            for (int k = 0; k < uv.Length; ++k) {
                                writer.WriteLine("{0:0.######} {1:0.######}", uv[k][j].u, uv[k][j].v);
                            }
                            if (skeleton != null && skeleton.Data.bonesAbs > 0) {
                                if (bones != null && bones[j].boneIndex != null && bones[j].boneWeight != null) {
                                    writer.WriteLine("{0} {1} {2} {3}", skeleton.Lookup[bones[j].boneIndex[0]], skeleton.Lookup[bones[j].boneIndex[1]], skeleton.Lookup[bones[j].boneIndex[2]], skeleton.Lookup[bones[j].boneIndex[3]]);
                                    writer.WriteLine("{0:0.######} {1:0.######} {2:0.######} {3:0.######}", bones[j].boneWeight[0], bones[j].boneWeight[1], bones[j].boneWeight[2], bones[j].boneWeight[3]);
                                } else {
                                    writer.WriteLine("0 0 0 0");
                                    writer.WriteLine("0 0 0 0");
                                }
                            }
                        }
                        writer.WriteLine(index.Length);
                        for (int j = 0; j < index.Length; ++j) {
                            writer.WriteLine("{0} {1} {2}", index[j].v1, index[j].v2, index[j].v3);
                        }
                    }
                }
                writer.WriteLine("");
            }
            return true;
        }

        public bool Write(Map10 physics, Stream output, object[] data) {
            //Console.Out.WriteLine("Writing ASCII");
            using (StreamWriter writer = new StreamWriter(output)) {
                writer.WriteLine(0);
                writer.WriteLine(1);

                writer.WriteLine("Physics");
                writer.WriteLine(0);
                writer.WriteLine(0);
                writer.WriteLine(physics.Vertices.Length);
                for (int i = 0; i < physics.Vertices.Length; ++i) {
                    writer.WriteLine("{0} {1} {2}", physics.Vertices[i].position.x, physics.Vertices[i].position.y, physics.Vertices[i].position.z);
                    writer.WriteLine("0 0 0");
                    writer.WriteLine("255 255 255 255");
                }
                writer.WriteLine(physics.Indices.Length);
                for (int i = 0; i < physics.Indices.Length; ++i) {
                    writer.WriteLine("{0} {1} {2}", physics.Indices[i].index.v1, physics.Indices[i].index.v2, physics.Indices[i].index.v3);
                }
                return true;
            }
        }

        public bool Write(Animation anim, Stream output, object[] data) {
            return false;
        }

        public Dictionary<ulong, List<string>>[] Write(Stream output, Map map, Map detail1, Map detail2, Map props, Map lights, string name, IDataWriter modelFormat) {
            return null;
        }
  }
}
