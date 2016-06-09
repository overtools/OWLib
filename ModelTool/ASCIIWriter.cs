using System;
using System.Collections.Generic;
using OWLib;
using System.IO;
using System.Globalization;
using OWLib.Types;

namespace ModelTool {
  class ASCIIWriter {
    public static void Write(Model model, Stream stream, List<byte> LODs) {
			NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
			numberFormatInfo.NumberDecimalSeparator = ".";
      Console.Out.WriteLine("Writing ASCII");
      using(StreamWriter writer = new StreamWriter(stream)) {
        writer.WriteLine(model.BoneData.Length);
        for(int i = 0; i < model.BoneData.Length; ++i) {
          writer.WriteLine("bone{0}", i);
          writer.WriteLine(model.BoneHierarchy[i]);
          OpenTK.Vector3 bonePos = model.BoneData[i].ExtractTranslation();
          writer.WriteLine("{0} {1} {2}", bonePos.X.ToString("0.000000", numberFormatInfo), bonePos.Y.ToString("0.000000", numberFormatInfo), bonePos.Z.ToString("0.000000", numberFormatInfo));
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

        writer.WriteLine(sz);
        foreach(KeyValuePair<byte, List<int>> kv in LODMap) {
          Console.Out.WriteLine("Writing LOD {0}", kv.Key);
          foreach(int i in kv.Value) {
            ModelSubmesh submesh = model.Submeshes[i];
            writer.WriteLine("Submesh_{0}.{1}.{2}", i, kv.Key, submesh.material);
            writer.WriteLine("1");
            writer.WriteLine("1");
            writer.WriteLine("Material_{0}", submesh.material);
            writer.WriteLine("0");

            ModelVertex[] vertex = model.Vertices[i];
            ModelVertex[] normal = model.Normals[i];
            ModelUV[] uv = model.UVs[i];
            ModelIndice[] index = model.Faces[i];
            ModelBoneData[] bones = model.Bones[i];
            writer.WriteLine(vertex.Length);
            for(int j = 0; j < vertex.Length; ++j) {
              writer.WriteLine("{0} {1} {2}", vertex[j].x, vertex[j].y, vertex[j].z);
              writer.WriteLine("{0} {1} {2}", -normal[j].x, -normal[j].y, -normal[j].z);
              writer.WriteLine("255 255 255 255");
              writer.WriteLine("{0} {1}", uv[j].u.ToString("0.######", numberFormatInfo), uv[j].v.ToString("0.######", numberFormatInfo));
              if(model.BoneData.Length > 0) {
                writer.WriteLine("{0} {1} {2} {3}", model.BoneLookup[bones[j].boneIndex[0]], model.BoneLookup[bones[j].boneIndex[1]], model.BoneLookup[bones[j].boneIndex[2]], model.BoneLookup[bones[j].boneIndex[3]]);
                writer.WriteLine("{0} {1} {2} {3}", bones[j].boneWeight[0].ToString("0.######", numberFormatInfo), bones[j].boneWeight[1].ToString("0.######", numberFormatInfo), bones[j].boneWeight[2].ToString("0.######", numberFormatInfo), bones[j].boneWeight[3].ToString("0.######", numberFormatInfo));
              }
            }
            writer.WriteLine(index.Length);
            for(int j = 0; j < index.Length; ++j) {
              writer.WriteLine("{0} {1} {2}", index[j].v1, index[j].v2, index[j].v3);
            }
          }
        }
        writer.WriteLine("");
      }
    }
  }
}
