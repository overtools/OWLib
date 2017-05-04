using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OWLib.Types;
using OWLib.Types.Chunk;
using OWLib.Types.Map;

namespace OWLib.Writer {
    public class OBJWriter : IDataWriter {
        public string Name => "Wavefront OBJ";
        public string Format => ".obj";
        public char[] Identifier => new char[1] { 'o' };
        public WriterSupport SupportLevel => (WriterSupport.VERTEX | WriterSupport.UV | WriterSupport.MATERIAL | WriterSupport.MODEL);

        public bool Write(Chunked chunked, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] opts) {
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

            NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
            numberFormatInfo.NumberDecimalSeparator = ".";
            using (StreamWriter writer = new StreamWriter(output)) {
                uint faceOffset = 1;
                if (opts.Length > 1 && opts[1] != null && opts[1].GetType() == typeof(string) && ((string)opts[1]).Length > 0) {
                    writer.WriteLine("mtllib {0}", (string)opts[1]);
                }

                Dictionary<byte, List<int>> LODMap = new Dictionary<byte, List<int>>();
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
                    if (!LODMap.ContainsKey(submesh.lod)) {
                        LODMap.Add(submesh.lod, new List<int>());
                    }
                    LODMap[submesh.lod].Add(i);
                }

                foreach (KeyValuePair<byte, List<int>> kv in LODMap) {
                    //Console.Out.WriteLine("Writing LOD {0}", kv.Key);
                    writer.WriteLine("o Submesh_{0}", kv.Key);
                    foreach (int i in kv.Value) {
                        SubmeshDescriptor submesh = model.Submeshes[i];
                        if (materials != null) {
                            writer.WriteLine("g Material_{0:X16}", materials.Materials[submesh.material]);
                            writer.WriteLine("usemtl {0:X16}", materials.Materials[submesh.material]);
                        } else {
                            writer.WriteLine("g Material_{0}", i);
                        }
                        ModelVertex[] vertex = model.Vertices[i];
                        ModelVertex[] normal = model.Normals[i];
                        ModelUV[][] uvs = model.TextureCoordinates[i];
                        ModelUV[] uv = uvs[0];
                        ModelIndice[] index = model.Indices[i];
                        for (int j = 0; j < vertex.Length; ++j) {
                            writer.WriteLine("v {0} {1} {2}", vertex[j].x, vertex[j].y, vertex[j].z);
                        }
                        for (int j = 0; j < vertex.Length; ++j) {
                            writer.WriteLine("vt {0} {1}", uv[j].u.ToString("0.######", numberFormatInfo), uv[j].v.ToString("0.######", numberFormatInfo));
                        }
                        if (uvs.Length > 1) {
                            for (int j = 0; j < uvs.Length; ++j) {
                                for (int k = 0; k < vertex.Length; ++k) {
                                    writer.WriteLine("vt{0} {0} {1}", j, uvs[j][k].u.ToString("0.######", numberFormatInfo), uvs[j][k].v.ToString("0.######", numberFormatInfo));
                                }
                            }
                        }
                        for (int j = 0; j < vertex.Length; ++j) {
                            writer.WriteLine("vn {0} {1} {2}", normal[j].x, normal[j].y, normal[j].z);
                        }
                        writer.WriteLine("");
                        for (int j = 0; j < index.Length; ++j) {
                            writer.WriteLine("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", index[j].v1 + faceOffset, index[j].v2 + faceOffset, index[j].v3 + faceOffset);
                        }
                        faceOffset += (uint)vertex.Length;
                        writer.WriteLine("");
                    }
                    if (opts.Length > 3 && opts[3] != null && opts[3].GetType() == typeof(bool) && (bool)opts[3] == true) {
                        break;
                    }
                }
            }
            return true;
        }

        public bool Write(Map10 physics, Stream output, object[] data) {
            //Console.Out.WriteLine("Writing OBJ");
            using (StreamWriter writer = new StreamWriter(output)) {
                writer.WriteLine("o Physics");

                for (int i = 0; i < physics.Vertices.Length; ++i) {
                    writer.WriteLine("v {0} {1} {2}", physics.Vertices[i].position.x, physics.Vertices[i].position.y, physics.Vertices[i].position.z);
                }

                for (int i = 0; i < physics.Indices.Length; ++i) {
                    writer.WriteLine("f {0} {1} {2}", physics.Indices[i].index.v1, physics.Indices[i].index.v2, physics.Indices[i].index.v3);
                }
            }
            return false;
        }

        public bool Write(Animation anim, Stream output, object[] data) {
            return false;
        }

        public Dictionary<ulong, List<string>>[] Write(Stream output, Map map, Map detail1, Map detail2, Map props, Map lights, string name = "") {
            return null;
        }
    }
}
