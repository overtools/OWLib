using System;
using System.Collections.Generic;
using OWLib;
using System.IO;
using OWLib.Types;

namespace ModelTool {
  class BINWriter {
    private static void WriteString(BinaryWriter stream, string str) {
      stream.Write(str);
    }

    public static void Write(Model model, Stream stream, List<byte> LODs) {
      Console.Out.WriteLine("Writing BIN");
      using(BinaryWriter writer = new BinaryWriter(stream)) {
        writer.Write((uint)323232);
        writer.Write((ushort)2);
        writer.Write((ushort)15);
        WriteString(writer, "XNAaraL");
        writer.Write((uint)5);
        WriteString(writer, "OVERWATCH");
        WriteString(writer, "BLIZZARD");
        WriteString(writer, "NULL");
        writer.Write((uint)180); // hash
        writer.Write((uint)1); // items
        // item 1
        writer.Write((uint)1); // type; 1 = pose; 2 = flags; 255 = padding
        if(model.BoneData.Length == 0) {
          writer.Write((uint)0); // size pow 4
          writer.Write((uint)0); // op info; bone count
        } else {
          using(MemoryStream ms = new MemoryStream()) {
            using(StreamWriter poseWriter = new StreamWriter(ms, System.Text.Encoding.ASCII, 4096, true)) {
              for(int i = 0; i < model.BoneData.Length; ++i) {
                /*
                double sqw = q.W * q.W;
                double sqx = q.X * q.X;
                double sqy = q.Y * q.Y;
                double sqz = q.Z * q.Z;
                rot.Z = (float)Math.Atan2(2f * q.X * q.W + 2f * q.Y * q.Z, 1 - 2f * (sqz  + sqw));     // Yaw 
                rot.X = (float)Math.Asin(2f * ( q.X * q.Z - q.W * q.Y ) );                             // Pitch 
                rot.Y = (float)Math.Atan2(2f * q.X * q.Y + 2f * q.Z * q.W, 1 - 2f * (sqy + sqz));      // Roll 
                */
                OpenTK.Matrix3x4 data = model.PoseData[i];
                OpenTK.Quaternion q = new OpenTK.Quaternion(data.Row0.Xyz, data.Row0.W);
                OpenTK.Vector3 rot = new OpenTK.Vector3();

                float sqw = q.W * q.W;
                float sqx = q.X * q.X;
                float sqy = q.Y * q.Y;
                float sqz = q.Z * q.Z;
                float unit = sqx + sqy + sqz + sqw;
                float test = q.X * q.Y + q.Z * q.W;
                float yaw = 0;
                float pitch = 0;
                float roll = 0;

                if(test > 0.4999f * unit) {
                  yaw   = 2f * (float)Math.Atan2(q.X, q.W);
                  pitch = (float)Math.PI * 0.5f;
                  roll  = 0;
                } else if(test < -0.4999f * unit) {
                  yaw   = -2f * (float)Math.Atan2(q.X, q.W);
                  pitch = -(float)Math.PI * 0.5f;
                  roll  = 0;
                } else {
                  yaw  = (float)Math.Atan2(2f * q.X * q.W + 2f * q.Y * q.Z, 1 - 2f * (sqz  + sqw));
                  pitch = (float)Math.Asin(2f * ( q.X * q.Z - q.W * q.Y ) );
                  roll  = (float)Math.Atan2(2f * q.X * q.Y + 2f * q.Z * q.W, 1 - 2f * (sqy + sqz));
                }

                rot.Y = yaw;
                rot.X = pitch;
                rot.Z = roll;
                OpenTK.Vector3 scale = data.Row1.Xyz;
                poseWriter.Write(string.Format("bone{0}:{1} {2} {3} {4} {5} {6} {7} {8} {9}\n", i, rot.X, rot.Y, rot.Z, 0, 0, 0, scale.X, scale.Y, scale.Z));
              }
            }

            writer.Write((uint)ms.Length);
            writer.Write((uint)model.BoneData.Length);
            ms.Position = 0;
            byte[] bytes = new byte[ms.Length];
            ms.Read(bytes, 0, (int)ms.Length);
            writer.Write(bytes);
            long n = 4 - (ms.Length % 4);
            if(n < 4) {
              for(long i = 0; i < n; ++i) {
                writer.Write((byte)0);
              }
            }
          }
        }
        /*
        pose data is always ASCII.
        Each line is:
        for each bone:
          boneName:rotx roty rotz posx posy posz scalex scaley scalez
        */

        writer.Write((uint)model.BoneData.Length);
        
        for(int i = 0; i < model.BoneData.Length; ++i) {
          WriteString(writer, "bone" + i);
          short parent = model.BoneHierarchy[i];
          if(parent == -1) {
            parent = (short)i;
          }
          writer.Write(parent);
          OpenTK.Vector3 bonePos = model.BoneData[i].ExtractTranslation();
          writer.Write(bonePos.X);
          writer.Write(bonePos.Y);
          writer.Write(bonePos.Z);
        }
        
        Dictionary<byte, List<int>> LODMap = new Dictionary<byte, List<int>>();
        uint sz = 0;
        for(int i = 0; i < model.Submeshes.Length; ++i) {
          ModelSubmesh submesh = model.Submeshes[i];
          if(LODs != null && !LODs.Contains(submesh.lod)) {
            continue;
          }
          if(!LODMap.ContainsKey(submesh.lod)) {
            LODMap.Add(submesh.lod, new List<int>());
          }
          sz++;
          LODMap[submesh.lod].Add(i);
        }
        writer.Write(sz);
        foreach(KeyValuePair<byte, List<int>> kv in LODMap) {
          Console.Out.WriteLine("Writing LOD {0}", kv.Key);
          foreach(int i in kv.Value) {
            ModelSubmesh submesh = model.Submeshes[i];
            WriteString(writer, string.Format("Submesh_{0}.{1}.{2}", i, kv.Key, submesh.material));
            writer.Write((uint)1);
            writer.Write((uint)1);
            WriteString(writer, string.Format("Material_{0}",submesh.material));
            writer.Write((uint)0);
            
            ModelVertex[] vertex = model.Vertices[i];
            ModelVertex[] normal = model.Normals[i];
            ModelUV[] uv = model.UVs[i];
            ModelIndice[] index = model.Faces[i];
            ModelBoneData[] bones = model.Bones[i];
            writer.Write((uint)vertex.Length);
            for(int j = 0; j < vertex.Length; ++j) {
              writer.Write(vertex[j].x);
              writer.Write(vertex[j].y);
              writer.Write(vertex[j].z);
              writer.Write(normal[j].x);
              writer.Write(normal[j].y);
              writer.Write(normal[j].z);
              writer.Write((byte)255);
              writer.Write((byte)255);
              writer.Write((byte)255);
              writer.Write((byte)255);
              writer.Write((float)uv[j].u);
              writer.Write((float)uv[j].v);
              if(model.BoneData.Length > 0) {
                writer.Write(model.BoneLookup[bones[j].boneIndex[0]]);
                writer.Write(model.BoneLookup[bones[j].boneIndex[1]]);
                writer.Write(model.BoneLookup[bones[j].boneIndex[2]]);
                writer.Write(model.BoneLookup[bones[j].boneIndex[3]]);
                writer.Write(bones[j].boneWeight[0]);
                writer.Write(bones[j].boneWeight[1]);
                writer.Write(bones[j].boneWeight[2]);
                writer.Write(bones[j].boneWeight[3]);
              }
            }
            writer.Write((uint)index.Length);
            for(int j = 0; j < index.Length; ++j) {
              writer.Write((uint)index[j].v1);
              writer.Write((uint)index[j].v2);
              writer.Write((uint)index[j].v3);
            }
          }
        }
      }
    }
  }
}
