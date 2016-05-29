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
          writer.WriteLine("{0} {1} {2}", model.BoneData[i][0].ToString("0.000000", numberFormatInfo), model.BoneData[i][1].ToString("0.000000", numberFormatInfo), model.BoneData[i][2].ToString("0.000000", numberFormatInfo));
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
            writer.WriteLine("LOD_{0}_{1}", kv.Key, submesh.material);
            writer.WriteLine("1");
            writer.WriteLine("1");
            writer.WriteLine("Material_{0}", submesh.material);
            writer.WriteLine("0");

            ModelVertex[] vertex = model.Vertices[i];
            ModelUV[] uv = model.UVs[i];
            ModelIndice[] index = model.Faces[i];
            ModelBoneData[] bones = model.Bones[i];
            writer.WriteLine(vertex.Length);
            for(int j = 0; j < vertex.Length; ++j) {
              writer.WriteLine("{0} {1} {2}", vertex[j].x, vertex[j].y, vertex[j].z);
              writer.WriteLine("0.0 0.0 0.0");
              writer.WriteLine("255 255 255");
              writer.WriteLine("{0} {1}", uv[j].u.ToString("0.######", numberFormatInfo), uv[j].v.ToString("0.######", numberFormatInfo));
              unsafe {
                fixed (ModelBoneData* p = &bones[j]) {
                  writer.WriteLine("{0} {1} {2} {3}", model.BoneLookup[p->boneIndex[0]], model.BoneLookup[p->boneIndex[1]], model.BoneLookup[p->boneIndex[2]], model.BoneLookup[p->boneIndex[3]]);
                  writer.WriteLine("{0} {1} {2} {3}", ((float)p->boneWeight[0]/255).ToString("0.######", numberFormatInfo), ((float)p->boneWeight[1]/255).ToString("0.######", numberFormatInfo), ((float)p->boneWeight[2]/255).ToString("0.######", numberFormatInfo), ((float)p->boneWeight[3]/255).ToString("0.######", numberFormatInfo));
                }
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
