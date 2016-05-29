using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWLib;
using OWLib.Types;
using System.Xml;
using System.Xml.Linq;

namespace ModelTool {
  class DAEWriter {
    public static void Write(Model model, Stream stream, List<byte> LODs) {
      Console.Out.WriteLine("WARNING: Bones are unsupported for now.");
		  NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
      numberFormatInfo.NumberDecimalSeparator = ".";
      XmlWriterSettings settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true, NewLineHandling = NewLineHandling.Entitize };
      using(XmlWriter writer = XmlWriter.Create(stream, settings)) {
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
        XNamespace daeNS = "http://www.collada.org/2008/03/COLLADASchema";
        XElement root = new XElement(daeNS + "COLLADA", new XAttribute("xmlns", daeNS.NamespaceName), new XAttribute("version", "1.5.0"));
        XDocument doc = new XDocument(root);

        XElement asset = new XElement("asset");
        asset.Add(new XElement("created"));
        asset.Add(new XElement("modified"));
        root.Add(asset);

        XElement libGeometries = new XElement("library_geometries");
        XElement libScene = new XElement("library_visual_scenes");
        XElement libController = new XElement("library_controllers");
        XElement scene = new XElement("scene");
        root.Add(libGeometries);
        root.Add(libController);
        root.Add(libScene);
        root.Add(scene);

        foreach(KeyValuePair<byte, List<int>> kv in LODMap) {
          Console.Out.WriteLine("Writing LOD {0}", kv.Key);
          string geometryName = string.Format("LOD{0}", kv.Key);
          XElement geometry = new XElement("geometry", new XAttribute("id", geometryName), new XAttribute("name", geometryName));
          XElement mesh = new XElement("mesh");
          XElement vertexSource = new XElement("source", new XAttribute("id", geometryName + "-positions"));

          ulong vertexSz = 0;
          ulong inSz = 0;
          
          string va = "";
          string ua = "";
          string ia = "";
          string wa = "";
          string ba = "";
          string bv = "";

          ulong off = 0;
          ulong woff = 0;

          foreach(int i in kv.Value) {
            ModelSubmesh submesh = model.Submeshes[i];
            ModelVertex[] vertex = model.Vertices[i];
            ModelUV[] uv = model.UVs[i];
            ModelIndice[] index = model.Faces[i];
            ModelBoneData[] bones = model.Bones[i];

            for(int j = 0; j < vertex.Length; ++j) {
              va += string.Format("{0} {1} {2} ", vertex[j].x, vertex[j].y, vertex[j].z);
            }
            vertexSz += (ulong)vertex.Length;
            if(false) { // model.BoneLookup.Length > 0
              for(int j = 0; j < vertex.Length; ++j) {
                unsafe
                {
                  fixed (ModelBoneData* p = &bones[j])
                  {
                    ba += string.Format("{0} {1} {2} {3} {4} {5} {6} {7} ", p->boneIndex[0], woff, p->boneIndex[1], woff + 1, p->boneIndex[2], woff + 2, p->boneIndex[3], woff + 3);
                    wa += string.Format("{0} {1} {2} {3}", ((float)p->boneWeight[0] / 255).ToString("0.######", numberFormatInfo), ((float)p->boneWeight[1] / 255).ToString("0.######", numberFormatInfo), ((float)p->boneWeight[2] / 255).ToString("0.######", numberFormatInfo), ((float)p->boneWeight[3] / 255).ToString("0.######", numberFormatInfo));
                  }
                  bv += "4 ";
                }
                woff += 4;
              }
            }

            for(int j = 0; j < vertex.Length; ++j) {
              ua += string.Format("{0} {1} ", uv[j].u.ToString("0.######", numberFormatInfo), uv[j].v.ToString("0.######", numberFormatInfo));
            }

            for(int j = 0; j < index.Length; ++j) {
              ia += string.Format("{0} {0} {1} {1} {2} {2} ", index[j].v1 + off, index[j].v2 + off, index[j].v3 + off);
            }
            inSz += (ulong)index.Length;
            off += (ulong)vertex.Length;
          }
          XElement vertexArray = new XElement("float_array", new XAttribute("id", geometryName + "-positions-array"), new XAttribute("count", vertexSz * 3));
          vertexArray.Value = va;
          vertexSource.Add(vertexArray);
          {
            XElement technique = new XElement("technique_common");
            XElement accessor = new XElement("accessor", new XAttribute("source", "#" + geometryName + "-positions-array"), new XAttribute("count", vertexSz), new XAttribute("stride", "3"));
            accessor.Add(new XElement("param", new XAttribute("name", "X"), new XAttribute("type", "float")));
            accessor.Add(new XElement("param", new XAttribute("name", "Y"), new XAttribute("type", "float")));
            accessor.Add(new XElement("param", new XAttribute("name", "Z"), new XAttribute("type", "float")));
            technique.Add(accessor);
            vertexSource.Add(technique);
          }
          mesh.Add(vertexSource);
          XElement uvSource = new XElement("source", new XAttribute("id", geometryName + "-uv"));
          XElement uvArray = new XElement("float_array", new XAttribute("id", geometryName + "-uv-array"), new XAttribute("count", vertexSz * 2));
          uvArray.Value = ua;
          uvSource.Add(uvArray);
          {
            XElement technique = new XElement("technique_common");
            XElement accessor = new XElement("accessor", new XAttribute("source", "#" + geometryName + "-uv-array"), new XAttribute("count", vertexSz), new XAttribute("stride", "2"));
            accessor.Add(new XElement("param", new XAttribute("name", "S"), new XAttribute("type", "float")));
            accessor.Add(new XElement("param", new XAttribute("name", "T"), new XAttribute("type", "float")));
            technique.Add(accessor);
            uvSource.Add(technique);
          }
          mesh.Add(uvSource);
          XElement vertexEl = new XElement("vertices", new XAttribute("id", geometryName + "-vertices"));
          vertexEl.Add(new XElement("input", new XAttribute("semantic", "POSITION"), new XAttribute("source", "#" + geometryName + "-positions")));
          mesh.Add(vertexEl);

          XElement triEl = new XElement("triangles", new XAttribute("count", inSz));
          triEl.Add(new XElement("input", new XAttribute("semantic", "VERTEX"), new XAttribute("offset", 0), new XAttribute("source", "#" + geometryName + "-vertices")));
          triEl.Add(new XElement("input", new XAttribute("semantic", "TEXCOORD"), new XAttribute("offset", 1), new XAttribute("source", "#" + geometryName + "-uv")));
          
          XElement triP = new XElement("p");
          triP.Value = ia;
          triEl.Add(triP);
          mesh.Add(triEl);

          geometry.Add(mesh);
          XElement rootNode = new XElement("visual_scene", new XAttribute("id", geometryName + "-scene"));
          if(true) { // model.BoneHierarchy.Length == 0
            XElement cNode = new XElement("node", new XAttribute("name", geometryName));
            XElement iNode = new XElement("node", new XAttribute("id", geometryName + "-inst"));
            iNode.Add(new XElement("instance_geometry", new XAttribute("url", "#" + geometryName)));
            cNode.Add(iNode);
            rootNode.Add(cNode);
          } else {
            XElement controller = new XElement("controller", new XAttribute("id", geometryName + "-skin"));
            XElement skin = new XElement("skin", new XAttribute("source", "#" + geometryName));

            XElement jointsSource = new XElement("source", new XAttribute("id", geometryName + "-joints"));
            XElement jointArray = new XElement("Name_array", new XAttribute("id", geometryName + "-joints-array"), new XAttribute("count", model.BoneLookup.Length));

            for(int i = 0; i < model.BoneLookup.Length; ++i) {
              jointArray.Value += string.Format("joint{0} ", model.BoneLookup[i]);
            }
            
            jointsSource.Add(jointArray);

            {
              XElement technique = new XElement("technique_common");
              XElement accessor = new XElement("accessor", new XAttribute("source", "#" + geometryName + "-joints-array"), new XAttribute("count", model.BoneLookup.Length), new XAttribute("stride", "1"));
              accessor.Add(new XElement("param", new XAttribute("name", "JOINT"), new XAttribute("type", "Name")));
              technique.Add(accessor);
              jointsSource.Add(technique);
            }

            skin.Add(jointsSource);

            XElement weightsSource = new XElement("source", new XAttribute("id", geometryName + "-weights"));
            XElement weightsArray = new XElement("float_array", new XAttribute("id", geometryName + "-weights-array"), new XAttribute("count", vertexSz * 4));

            weightsArray.Value = wa;

            weightsSource.Add(weightsArray);

            {
              XElement technique = new XElement("technique_common");
              XElement accessor = new XElement("accessor", new XAttribute("source", "#" + geometryName + "-weights-array"), new XAttribute("count", vertexSz * 4), new XAttribute("stride", "1"));
              accessor.Add(new XElement("param", new XAttribute("name", "WEIGHT"), new XAttribute("type", "float")));
              technique.Add(accessor);
              weightsSource.Add(technique);
            }

            skin.Add(weightsSource);

            XElement joints = new XElement("joints");
            joints.Add(new XElement("input", new XAttribute("semantic", "JOINT"), new XAttribute("source", "#" + geometryName + "-joints")));
            skin.Add(joints);

            XElement vertexWeights = new XElement("vertex_weights", new XAttribute("count", vertexSz));
            vertexWeights.Add(new XElement("input", new XAttribute("semantic", "JOINT"), new XAttribute("offset", 0), new XAttribute("source", "#" + geometryName + "-joints")));
            vertexWeights.Add(new XElement("input", new XAttribute("semantic", "WEIGHT"), new XAttribute("offset", 1), new XAttribute("source", "#" + geometryName + "-weights")));
          
            XElement vertexV = new XElement("vcount");
            XElement vertexP = new XElement("v");
            vertexV.Value = bv;
            vertexP.Value = ba;
            vertexWeights.Add(vertexV);
            vertexWeights.Add(vertexP);
            skin.Add(vertexWeights);
            controller.Add(skin);
            libController.Add(controller);

            XElement[] jointMap = new XElement[model.BoneData.Length];
            for(int i = 0; i < model.BoneLookup.Length; ++i) {
              XElement jointNode = new XElement("node", new XAttribute("sid", model.BoneLookup[i]), new XAttribute("type", "JOINT"));
              XElement translate = new XElement("translate");
              translate.Value = string.Format("{0} {1} {2}", model.BoneData[i][0].ToString("0.000000", numberFormatInfo), model.BoneData[i][1].ToString("0.000000", numberFormatInfo), model.BoneData[i][2].ToString("0.000000", numberFormatInfo));
              jointNode.Add(translate);
              jointMap[i] = jointNode;
            }
            XElement skeleton = new XElement("instance_contorller", new XAttribute("url", "#" + geometryName + "-skin"));
            for(int i = 0; i < model.BoneLookup.Length; ++i) {
              short parent = model.BoneHierarchy[i];
              if(parent > -1) {
                XElement p = jointMap[parent];
                p.Add(jointMap[i]);
                jointMap[parent + 1] = p;
              }
            }
            
            for(int i = 0; i < model.BoneLookup.Length; ++i) {
              short parent = model.BoneHierarchy[i];
              if(parent == -1) {
                XElement t = jointMap[i];
                t.SetAttributeValue("id", geometryName + "-skeleton-"+i);
                rootNode.Add(t);
                XElement skeletonUrl = new XElement("skeleton");
                skeletonUrl.Value = geometryName + "-skeleton-" + i;
                skeleton.Add(skeletonUrl);
              }
            }

            rootNode.Add(skeleton);
          }
          libScene.Add(rootNode);
          libGeometries.Add(geometry);
          scene.Add(new XElement("instance_visual_scene", new XAttribute("url", "#" + geometryName + "-scene")));
        }
        foreach(XNode x in root.DescendantNodesAndSelf()) {
          if(x.GetType() != typeof(XElement)) {
            continue;
          }
          XElement e = (XElement)x;
          if(e.Name.Namespace == string.Empty) {
            e.Name = daeNS + e.Name.LocalName;
          }
        }
        doc.WriteTo(writer);
      }
    }
  }
}
