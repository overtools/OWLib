using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWLib.Types;

namespace OWLib.ModelWriter {
  public class ASCIIWriter : IModelWriter {
    public string Name => "XNALara XPS ASCII";
    public string Format => ".mesh.ascii";
    public char[] Identifier => new char[2] { 'l', 'a' };
    public ModelWriterSupport SupportLevel => (ModelWriterSupport.VERTEX | ModelWriterSupport.UV | ModelWriterSupport.BONE | ModelWriterSupport.MATERIAL);

    public Stream Write(Model model, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, bool[] flags) {
      MemoryStream stream = new MemoryStream();
      Write(model, stream, LODs, layers, flags);
      return stream;
    }

    public void Write(Model model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, bool[] flags) {
			NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
			numberFormatInfo.NumberDecimalSeparator = ".";
      Console.Out.WriteLine("Writing ASCII");
      using(StreamWriter writer = new StreamWriter(output)) {
        writer.WriteLine(model.BoneData.Length);
        for(int i = 0; i < model.BoneData.Length; ++i) {
          writer.WriteLine("bone{0:X}", model.BoneIDs[i]);
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
            ModelVertex[] vertex = model.Vertices[i];
            ModelVertex[] normal = model.Normals[i];
            ModelUV[][] uv = model.UVs[i];
            ModelIndice[] index = model.Faces[i];
            ModelBoneData[] bones = model.Bones[i];

            writer.WriteLine("Submesh_{0}.{1}.{2:X16}", i, kv.Key, model.MaterialKeys[submesh.material]);
            writer.WriteLine(uv.Length);
            ulong materialKey = model.MaterialKeys[submesh.material];
            if(layers.ContainsKey(materialKey)) {
              List<ImageLayer> materialLayers = layers[materialKey];
              writer.WriteLine(materialLayers.Count);
              for(int j = 0; j < materialLayers.Count; ++j) {
                writer.WriteLine("{0:X16}_{1:X16}.dds", materialKey, materialLayers[j].unk);
                uint layer = layers[materialKey][j].layer;
                if(layer == 0) {
                  layer = 1;
                }
                layer = (uint)uv.Length - layers[materialKey][j].layer;
                layer = layer % (uint)uv.Length;
                writer.WriteLine(layer);
              }
            } else {
              writer.WriteLine(uv.Length);
              for(int j = 0; j < uv.Length; ++j) {
                writer.WriteLine("{0:X16}_UV{1}.dds", materialKey, j);
                writer.WriteLine(j);
              }
            }

            writer.WriteLine(vertex.Length);
            for(int j = 0; j < vertex.Length; ++j) {
              writer.WriteLine("{0} {1} {2}", vertex[j].x, vertex[j].y, vertex[j].z);
              writer.WriteLine("{0} {1} {2}", -normal[j].x, -normal[j].y, -normal[j].z);
              writer.WriteLine("255 255 255 255");
              for(int k = 0; k < uv.Length; ++k) {
                writer.WriteLine("{0} {1}", uv[k][j].u.ToString("0.######", numberFormatInfo), uv[k][j].v.ToString("0.######", numberFormatInfo));
              }
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
