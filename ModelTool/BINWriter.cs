using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWLib;
using System.IO;
using System.Globalization;
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
        writer.Write((uint)0); // size pow 4
        writer.Write((uint)0); // op info; bone count
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
          writer.Write(model.BoneData[i][0]);
          writer.Write(model.BoneData[i][1]);
          writer.Write(model.BoneData[i][2]);
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
            ModelUV[] uv = model.UVs[i];
            ModelIndice[] index = model.Faces[i];
            ModelBoneData[] bones = model.Bones[i];
            writer.Write((uint)vertex.Length);
            for(int j = 0; j < vertex.Length; ++j) {
              writer.Write(vertex[j].x);
              writer.Write(vertex[j].y);
              writer.Write(vertex[j].z);
              writer.Write(0.0f);
              writer.Write(0.0f);
              writer.Write(0.0f);
              writer.Write((byte)255);
              writer.Write((byte)255);
              writer.Write((byte)255);
              writer.Write((byte)255);
              writer.Write((float)uv[j].u);
              writer.Write((float)uv[j].v);
              if(model.BoneData.Length > 0) {
                unsafe
                {
                  fixed (ModelBoneData* p = &bones[j])
                  {
                    writer.Write(model.BoneLookup[p->boneIndex[0]]);
                    writer.Write(model.BoneLookup[p->boneIndex[1]]);
                    writer.Write(model.BoneLookup[p->boneIndex[2]]);
                    writer.Write(model.BoneLookup[p->boneIndex[3]]);
                    writer.Write((float)p->boneWeight[0] / 255);
                    writer.Write((float)p->boneWeight[1] / 255);
                    writer.Write((float)p->boneWeight[2] / 255);
                    writer.Write((float)p->boneWeight[3] / 255);
                  }
                }
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
