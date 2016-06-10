using System.IO;
using OWLib;
using OWLib.Types;
using System.Collections.Generic;
using System.Globalization;
using System;

namespace ModelTool {
  public class OBJWriter {
    public static void Write(Model model, Stream stream, List<byte> LODs) {
		  NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
      numberFormatInfo.NumberDecimalSeparator = ".";
      using(StreamWriter writer = new StreamWriter(stream)) {
        Dictionary<byte, List<int>> LODMap = new Dictionary<byte, List<int>>();
        for(int i = 0; i < model.Submeshes.Length; ++i) {
          ModelSubmesh submesh = model.Submeshes[i];
          if(LODs != null && !LODs.Contains(submesh.lod)) {
            continue;
          }
          if(!LODMap.ContainsKey(submesh.lod)) {
            LODMap.Add(submesh.lod, new List<int>());
          }
          LODMap[submesh.lod].Add(i);
        }

        uint faceOffset = 1;
        foreach(KeyValuePair<byte, List<int>> kv in LODMap) {
          Console.Out.WriteLine("Writing LOD {0}", kv.Key);
          writer.WriteLine("o Submesh_{0}", kv.Key);
          foreach(int i in kv.Value) {
            ModelSubmesh submesh = model.Submeshes[i];
            writer.WriteLine("g Material_{0}", submesh.material);
            
            ModelVertex[] vertex = model.Vertices[i];
            ModelVertex[] normal = model.Normals[i];
            ModelUV[][] uvs = model.UVs[i];
            ModelUV[] uv = uvs[0];
            ModelIndice[] index = model.Faces[i];
            for(int j = 0; j < vertex.Length; ++j) {
              writer.WriteLine("v {0} {1} {2}", vertex[j].x, vertex[j].y, vertex[j].z);
            }
            for(int j = 0; j < vertex.Length; ++j) {
              writer.WriteLine("vt {0} {1}", uv[j].u.ToString("0.######", numberFormatInfo), uv[j].v.ToString("0.######", numberFormatInfo));
            }
            if(uvs.Length > 1) {
              for(int j = 0; j < uvs.Length; ++j) {
                for(int k = 0; k < vertex.Length; ++k) {
                  writer.WriteLine("vt{0} {0} {1}", j, uvs[j][k].u.ToString("0.######", numberFormatInfo), uvs[j][k].v.ToString("0.######", numberFormatInfo));
                }
              }
            }
            for(int j = 0; j < vertex.Length; ++j) {
              writer.WriteLine("vn {0} {1} {2}", normal[j].x, normal[j].y, normal[j].z);
            }
            writer.WriteLine("");
            for(int j = 0; j < index.Length; ++j) {
              writer.WriteLine("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", index[j].v1 + faceOffset, index[j].v2 + faceOffset, index[j].v3 + faceOffset);
            }
            faceOffset += (uint)vertex.Length;
            writer.WriteLine("");
          }
        }
      }
    }
  }
}
