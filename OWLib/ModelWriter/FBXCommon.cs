using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fbx;
using System.Threading.Tasks;
using System.IO;
using OWLib.Types;

namespace OWLib.ModelWriter {
  public static class FBXExtensions {
    public static FbxNode GetNamedNode(this FbxNode parent, string name) {
      for(int i = 0; i < parent.Nodes.Count; ++i) {
        if(parent.Nodes[i].Name == name) {
          return parent.Nodes[i];
        }
      }
      return null;
    }
    public static FbxNode GetNamedProperty(this FbxNode parent, string name) {
      FbxNode property = parent.GetNamedNode("Properties60");
      if(property == null) {
        return null;
      }
      for(int i = 0; i < property.Nodes.Count; ++i) {
        string n = (string)property.Nodes[i].Properties[0];
        if(n == name) {
          return property.Nodes[i];
        }
      }
      return null;
    }
  }

  public class FBXCommon {
    public static void Increment(FbxNode node, int index = 0, int index2 = 0) {
      node.Nodes[index].Properties[index2] = (int)node.Nodes[index].Properties[index2] + 1;
    }

    public static FbxNode CreateNode(string name, params object[] props) {
      FbxNode node = new FbxNode();
      node.Name = name;
      if(props != null && props.Length > 0) {
        for(int i = 0; i < props.Length; ++i) {
          node.Properties.Add(props[i]);
        }
      }
      return node;
    }

    public static FbxNode CreateLayerElement(string name, int typedIndex) {
      FbxNode layerElement = CreateNode("LayerElement");
      layerElement.Nodes.Add(CreateNode("Type", name));
      layerElement.Nodes.Add(CreateNode("TypedIndex", typedIndex));
      return layerElement;
    }

    public static FbxNode CreateMatrix(OpenTK.Matrix4 matrix, string name = "Matrix") {
      return CreateNode(name, matrix.M11, matrix.M12, matrix.M13, matrix.M14, matrix.M21, matrix.M22, matrix.M23, matrix.M24, matrix.M31, matrix.M32, matrix.M33, matrix.M34, matrix.M41, matrix.M42, matrix.M43, matrix.M44);
    }

    public static FbxNode CreatePoseNode(string node, OpenTK.Matrix4 matrix) {
      FbxNode poseNode = CreateNode("PoseNode");
      poseNode.Nodes.Add(CreateNode("Node", node));
      poseNode.Nodes.Add(CreateMatrix(matrix));
      return poseNode;
    }

    public static FbxNode CreateModel(string name, string type = "Null") {
      FbxNode modelNode = CreateNode("Model", GenCLSID("Model", name), type);
      modelNode.Nodes.Add(CreateNode("Version", 232));
      FbxNode properties = CreateNode("Properties60");
      {
        properties.Nodes.Add(CreateNode("Property", "QuaternionInterpolate", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "Visibility", "Visibility", "A+", 1));
        properties.Nodes.Add(CreateNode("Property", "Lcl Translation", "Lcl Translation", "A+", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "Lcl Rotation", "Lcl Rotation", "A+", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "Lcl Scaling", "Lcl Scaling", "A+", 1, 1, 1));
        properties.Nodes.Add(CreateNode("Property", "RotationOffset", "Vector3D", "", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "RotationPivot", "Vector3D", "", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "ScalingOffset", "Vector3D", "", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "ScalingPivot", "Vector3D", "", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "TranslationActive", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "TranslationMin", "Vector3D", "", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "TranslationMax", "Vector3D", "", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "TranslationMinX", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "TranslationMinY", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "TranslationMinZ", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "TranslationMaxX", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "TranslationMaxY", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "TranslationMaxZ", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "RotationOrder", "enum", "", 0));
        properties.Nodes.Add(CreateNode("Property", "RotationSpaceForLimitOnly", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "AxisLen", "double", "", 10));
        properties.Nodes.Add(CreateNode("Property", "PreRotation", "Vector3D", "", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "PostRotation", "Vector3D", "", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "RotationActive", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "RotationMin", "Vector3D", "", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "RotationMax", "Vector3D", "", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "RotationMinX", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "RotationMinY", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "RotationMinZ", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "RotationMaxX", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "RotationMaxY", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "RotationMaxZ", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "RotationStiffnessX", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "RotationStiffnessY", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "RotationStiffnessZ", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "MinDampRangeX", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "MinDampRangeY", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "MinDampRangeZ", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "MaxDampRangeX", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "MaxDampRangeY", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "MaxDampRangeZ", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "MinDampStrengthX", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "MinDampStrengthY", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "MinDampStrengthZ", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "MaxDampStrengthX", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "MaxDampStrengthY", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "MaxDampStrengthZ", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "PreferedAngleX", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "PreferedAngleY", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "PreferedAngleZ", "double", "", 0));
        properties.Nodes.Add(CreateNode("Property", "InheritType", "enum", "", 0));
        properties.Nodes.Add(CreateNode("Property", "ScalingActive", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "ScalingMin", "Vector3D", "", 1, 1, 1));
        properties.Nodes.Add(CreateNode("Property", "ScalingMax", "Vector3D", "", 1, 1, 1));
        properties.Nodes.Add(CreateNode("Property", "ScalingMinX", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "ScalingMinY", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "ScalingMinZ", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "ScalingMaxX", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "ScalingMaxY", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "ScalingMaxZ", "bool", "", 0));
        properties.Nodes.Add(CreateNode("Property", "GeometricTranslation", "Vector3D", "", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "GeometricRotation", "Vector3D", "", 0, 0, 0));
        properties.Nodes.Add(CreateNode("Property", "GeometricScaling", "Vector3D", "", 1, 1, 1));
        properties.Nodes.Add(CreateNode("Property", "LookAtProperty", "object", ""));
        properties.Nodes.Add(CreateNode("Property", "UpVectorProperty", "object", ""));
        properties.Nodes.Add(CreateNode("Property", "Show", "bool", "", 1));
        properties.Nodes.Add(CreateNode("Property", "NegativePercentShapeSupport", "bool", "", 1));
        properties.Nodes.Add(CreateNode("Property", "DefaultAttributeIndex", "int", "", 0));
        properties.Nodes.Add(CreateNode("Property", "Color", "Color", "A", 0.8, 0.8, 0.8));
        properties.Nodes.Add(CreateNode("Property", "Size", "double", "", 100));
        properties.Nodes.Add(CreateNode("Property", "Look", "enum", "", 1));
      }
      modelNode.Nodes.Add(properties);
      modelNode.Nodes.Add(CreateNode("MultiLayer", 0));
      modelNode.Nodes.Add(CreateNode("MultiTake", 1));
      modelNode.Nodes.Add(CreateNode("Shading", 1));
      modelNode.Nodes.Add(CreateNode("Culling", "CullingOff"));
      modelNode.Nodes.Add(CreateNode("TypeFlags", "Null"));
      return modelNode;
    }

    public static string GenCLSID(string cls, string id) {
      return cls + "::" + id;
    }

    public static FbxDocument Write(Model model, List<byte> LODs, object[] opts) {
      FbxDocument document = new FbxDocument();
      DateTime now = DateTime.Now.ToUniversalTime();
      FbxNode header = CreateNode("FBXHeaderExtension");
      document.Version = FbxVersion.v6_1;
      {
        header.Nodes.Add(CreateNode("FBXHeaderVersion", 1003));

        header.Nodes.Add(CreateNode("FBXVersion", (int)document.Version));

        FbxNode CreationTimeStamp = CreateNode("CreationTimeStamp");
        {
          CreationTimeStamp.Nodes.Add(CreateNode("Version", 1000));
          CreationTimeStamp.Nodes.Add(CreateNode("Year", now.Year));
          CreationTimeStamp.Nodes.Add(CreateNode("Month", now.Month));
          CreationTimeStamp.Nodes.Add(CreateNode("Day", now.Day));
          CreationTimeStamp.Nodes.Add(CreateNode("Hour", now.Hour));
          CreationTimeStamp.Nodes.Add(CreateNode("Minute", now.Minute));
          CreationTimeStamp.Nodes.Add(CreateNode("Second", now.Second));
          CreationTimeStamp.Nodes.Add(CreateNode("Millisecond", now.Millisecond));
        }
        header.Nodes.Add(CreationTimeStamp);
      }
      document.Nodes.Add(header);
      document.Nodes.Add(CreateNode("CreationTime", now.ToLongDateString()));
      document.Nodes.Add(CreateNode("Creator", "Blizzard; Overwatch"));

      FbxNode definitions = CreateNode("Definitions");
      definitions.Nodes.Add(CreateNode("Version", 100));
      definitions.Nodes.Add(CreateNode("Count", 0));
      FbxNode def_model = CreateNode("ObjectType", "Model");
      def_model.Nodes.Add(CreateNode("Count", 0));
      FbxNode def_geometry = CreateNode("ObjectType", "Geometry");
      def_geometry.Nodes.Add(CreateNode("Count", 0));
      FbxNode def_material = CreateNode("ObjectType", "Material");
      def_material.Nodes.Add(CreateNode("Count", 0));
      FbxNode def_deformer = CreateNode("ObjectType", "Deformer");
      def_deformer.Nodes.Add(CreateNode("Count", 0));
      FbxNode def_pose = CreateNode("ObjectType", "Pose");
      def_pose.Nodes.Add(CreateNode("Count", 0));

      FbxNode objects = CreateNode("Objects");
      FbxNode relations = CreateNode("Relations");
      FbxNode conections = CreateNode("Connections");
      FbxNode poseNode = null;
      if((bool)opts[0] == true) {
        for(int i = 0; i < model.AttachmentPoints.Length; ++i) {
          ModelAttachmentPoint point = model.AttachmentPoints[i];
          FbxNode modelNode = CreateModel("Attachment" + point.id.ToString("X"));
          OpenTK.Matrix4 matrix = point.matrix.ToOpenTK();
          OpenTK.Vector3 translation = matrix.ExtractTranslation();
          OpenTK.Vector3 rotation = BINWriter.QuaternionToVector(matrix.ExtractRotation(false));
          FbxNode translationNode = modelNode.GetNamedProperty("Lcl Translation");
          if(translationNode != null) {
            translationNode.Properties[3] = translation.X;
            translationNode.Properties[4] = translation.Y;
            translationNode.Properties[5] = translation.Z;
          }
          FbxNode rotationNode = modelNode.GetNamedProperty("Lcl Rotation");
          if(rotationNode != null) {
            rotationNode.Properties[3] = rotation.X;
            rotationNode.Properties[4] = rotation.Y;
            rotationNode.Properties[5] = rotation.Z;
          }
          
          Increment(def_model);
          Increment(definitions, 1);
          objects.Nodes.Add(modelNode);
          FbxNode relationNode = CreateNode("Model", modelNode.Properties[0], modelNode.Properties[1]);
          relationNode.Nodes.Add(null);
          relations.Nodes.Add(relationNode);
          conections.Nodes.Add(CreateNode("Connect", "OO", modelNode.Properties[0], "Model::Scene"));
        }
      }
      if(model.BoneData.Length > 0 && false) {
        poseNode = CreateNode("Pose", "Pose::BIND_POSES", "BindPose");
        poseNode.Nodes.Add(CreateNode("Type", "BindPose"));
        poseNode.Nodes.Add(CreateNode("Version", 100));
        {
          FbxNode empty = CreateNode("Properties60");
          empty.Nodes.Add(null);
          poseNode.Nodes.Add(empty);
        }
        poseNode.Nodes.Add(CreateNode("NbPoseNodes", model.BoneData.Length + 1));
        
        for(int i = 0; i < model.BoneData.Length; ++i) {
          FbxNode bone = CreateModel(string.Format("bone{0:X}", model.BoneIDs[i]), "Limb");
          short parent = model.BoneHierarchy[i];
          string name = null;
          if(parent == -1) {
            name = "Scene";
          } else {
            name = string.Format("bone{0:X}", model.BoneIDs[parent]);
          }
          OpenTK.Vector3 translation = model.BoneData[i].ExtractTranslation();
          OpenTK.Vector3 scale = model.BoneData[i].ExtractScale();
          OpenTK.Vector3 rotation = BINWriter.QuaternionToVector(model.BoneData[i].ExtractRotation());
          FbxNode translationNode = bone.GetNamedProperty("Lcl Translation");
          if(translationNode != null) {
            translationNode.Properties[3] = translation.X;
            translationNode.Properties[4] = translation.Y;
            translationNode.Properties[5] = translation.Z;
          }
          FbxNode rotationNode = bone.GetNamedProperty("Lcl Rotation");
          if(rotationNode != null) {
            rotationNode.Properties[3] = rotation.X;
            rotationNode.Properties[4] = rotation.Y;
            rotationNode.Properties[5] = rotation.Z;
          }
          FbxNode scaleNode = bone.GetNamedProperty("Lcl Scaling");
          if(scaleNode != null) {
            scaleNode.Properties[3] = scale.X;
            scaleNode.Properties[4] = scale.Y;
            scaleNode.Properties[5] = scale.Z;
          }
          bone["TypeFlags"].Value = "Skeleton";
          objects.Nodes.Add(bone);
          FbxNode relationNode = CreateNode("Model", bone.Properties[0], bone.Properties[1]);
          relationNode.Nodes.Add(null);
          relations.Nodes.Add(relationNode);
          conections.Nodes.Add(CreateNode("Connect", "OO", bone.Properties[0], "Model::"+name));
          Increment(def_model);
          Increment(definitions, 1);
          poseNode.Nodes.Add(CreatePoseNode((string)bone.Properties[0], model.BoneData[i]));
        }
      }
      
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
      foreach(KeyValuePair<byte, List<int>> kv in LODMap) {
        Console.Out.WriteLine("Writing LOD {0}", kv.Key);
        foreach(int i in kv.Value) {
          ModelSubmesh submesh = model.Submeshes[i];
          ModelVertex[] vertex = model.Vertices[i];
          ModelVertex[] normal = model.Normals[i];
          ModelUV[][] uv = model.UVs[i];
          ModelIndice[] index = model.Faces[i];
          ModelBoneData[] bones = model.Bones[i];

          FbxNode mesh = CreateModel(string.Format("Submesh_{0}.{1}.{2:X16}", i, kv.Key, model.MaterialKeys[submesh.material]), "Mesh");
          mesh.GetNamedProperty("Size").Properties[3] = 100;
          FbxNode vertices = CreateNode("Vertices");
          vertices.Properties = new List<object>(vertex.Length * 3);
          for(int j = 0; j < vertex.Length; ++j) {
            vertices.Properties.Add(vertex[j].x);
            vertices.Properties.Add(vertex[j].y);
            vertices.Properties.Add(vertex[j].z);
          }
          mesh.Nodes.Add(vertices);

          FbxNode indices = CreateNode("PolygonVertexIndex");
          indices.Properties = new List<object>(normal.Length);
          for(int j = 0; j < index.Length; ++j) {
            indices.Properties.Add(index[j].v1);
            indices.Properties.Add(index[j].v2);
            indices.Properties.Add(index[j].v3 ^ -1);
          }
          mesh.Nodes.Add(indices);
          mesh.Nodes.Add(CreateNode("GeometryVersion", 124));
          
          FbxNode layerNormal = CreateNode("LayerElementNormal", 0);
          {
            layerNormal.Nodes.Add(CreateNode("Version", 101));
            layerNormal.Nodes.Add(CreateNode("Name", ""));
            layerNormal.Nodes.Add(CreateNode("MappingInformationType", "ByVertice"));
            layerNormal.Nodes.Add(CreateNode("ReferenceInformationType", "Direct"));
            FbxNode normalsT = CreateNode("Normals");
            normalsT.Properties = new List<object>(normal.Length * 3);
            for(int j = 0; j < normal.Length; ++j) {
              normalsT.Properties.Add(normal[j].x);
              normalsT.Properties.Add(normal[j].y);
              normalsT.Properties.Add(normal[j].z);
            }
            layerNormal.Nodes.Add(normalsT);
          }
          mesh.Nodes.Add(layerNormal);

          for(int j = 0; j < uv.Length; ++j) {
            FbxNode layerUV = CreateNode("LayerElementUV", j);
            FbxNode layerTexture = CreateNode("LayerElementTexture", j);
            {
              layerUV.Nodes.Add(CreateNode("Version", 101));
              layerUV.Nodes.Add(CreateNode("Name", "UV"+(j+1)));
              layerUV.Nodes.Add(CreateNode("MappingInformationType", "ByVertice"));
              layerUV.Nodes.Add(CreateNode("ReferenceInformationType", "Direct"));

              FbxNode uvT = CreateNode("UV");
              uvT.Properties = new List<object>(uv[j].Length * 3);
              for(int k = 0; k < normal.Length; ++k) {
                uvT.Properties.Add((float)uv[j][k].u);
                uvT.Properties.Add((float)uv[j][k].v);
              }
              layerUV.Nodes.Add(uvT);
            }
            {
              layerTexture.Nodes.Add(CreateNode("Version", 101));
              layerTexture.Nodes.Add(CreateNode("Name", "UV"+(j+1)));
              layerTexture.Nodes.Add(CreateNode("MappingInformationType", "ByPolygon"));
              layerTexture.Nodes.Add(CreateNode("ReferenceInformationType", "Direct"));
              layerTexture.Nodes.Add(CreateNode("BlendMode", "Translucent"));
              layerTexture.Nodes.Add(CreateNode("TextureAlpha", "TextureAlpha"));
              FbxNode texture = CreateNode("TextureId");
              texture.Properties = new List<object>(index.Length);
              for(int k = 0; k < index.Length; ++k) {
                texture.Properties.Add(j == 0 ? 1 : 0);
              }
              layerTexture.Nodes.Add(texture);
            }
            mesh.Nodes.Add(layerUV);
            mesh.Nodes.Add(layerTexture);
          }
          FbxNode layerMaterial = CreateNode("LayerElementMaterial", 0);
          {
            layerMaterial.Nodes.Add(CreateNode("Version", 101));
            layerMaterial.Nodes.Add(CreateNode("Name", ""));
            layerMaterial.Nodes.Add(CreateNode("MappingInformationType", "ByPolygon"));
            layerMaterial.Nodes.Add(CreateNode("ReferenceInformationType", "Direct"));
            FbxNode material = CreateNode("Materials");
            Increment(def_material);
            Increment(definitions, 1);
            material.Properties = new List<object>(index.Length);
            for(int j = 0; j < index.Length; ++j) {
              material.Properties.Add(1);
            }
            layerMaterial.Nodes.Add(material);
          }
          mesh.Nodes.Add(layerMaterial);
          for(int j = 0; j < uv.Length; ++j) {
            FbxNode layer = CreateNode("Layer", j);
            layer.Nodes.Add(CreateNode("Version", 100));
            if(j == 0) {
              layer.Nodes.Add(CreateLayerElement("LayerElementNormal", 0));
              layer.Nodes.Add(CreateLayerElement("LayerElementMaterial", 0));
            }
            layer.Nodes.Add(CreateLayerElement("LayerElementUV", j));
            layer.Nodes.Add(CreateLayerElement("LayerElementTexture", j));
            mesh.Nodes.Add(layer);
          }

          Increment(def_model);
          Increment(definitions, 1);
          Increment(def_geometry);
          Increment(definitions, 1);
          objects.Nodes.Add(mesh);
          {
            FbxNode relationNode = CreateNode("Model", mesh.Properties[0], mesh.Properties[1]);
            relationNode.Nodes.Add(null);
            relations.Nodes.Add(relationNode);
          }
          conections.Nodes.Add(CreateNode("Connect", "OO", mesh.Properties[0], "Model::Scene"));

          if(model.BoneData.Length > 0 && false) {
            FbxNode skinNode = CreateNode("Deformer", "Deformer::Skin " + (string)mesh.Properties[0], "Skin");
            skinNode.Nodes.Add(CreateNode("Version", 100));
            skinNode.Nodes.Add(CreateNode("MultiLayer", 0));
            skinNode.Nodes.Add(CreateNode("Type", "Skin"));
            {
              FbxNode empty = CreateNode("Properties60");
              empty.Nodes.Add(null);
              skinNode.Nodes.Add(empty);
            }
            skinNode.Nodes.Add(CreateNode("Link_DeformAcuracy", 50));
            {
              FbxNode relationNode = CreateNode("Model", skinNode.Properties[0], skinNode.Properties[1]);
              relationNode.Nodes.Add(null);
              relations.Nodes.Add(relationNode);
            }
            conections.Nodes.Add(CreateNode("Connect", "OO", skinNode.Properties[0], mesh.Properties[0]));
            Increment(def_deformer);
            Increment(definitions, 1);
            objects.Nodes.Add(skinNode);

            List<FbxNode> deformers = new List<FbxNode>(model.BoneData.Length);
            for(int j = 0; j < model.BoneData.Length; ++j) {
              FbxNode deformerNode = CreateNode("Deformer", "SubDeformer::Cluster " + (string)mesh.Properties[0] + " " + string.Format("bone{0:X}", model.BoneIDs[j]), "Cluster");
              deformerNode.Nodes.Add(CreateNode("Version", 100));
              deformerNode.Nodes.Add(CreateNode("MultiLayer", 0));
              deformerNode.Nodes.Add(CreateNode("Type", "Cluster"));
              {
                FbxNode empty = CreateNode("Properties60");
                empty.Nodes.Add(CreateNode("Property", "SrcModel", "object", ""));
                empty.Nodes.Add(CreateNode("SrcModelReference", "SrcModel", "object", ""));
                deformerNode.Nodes.Add(empty);
              }
              deformerNode.Nodes.Add(CreateNode("UserData", "", ""));
              deformerNode.Nodes.Add(CreateNode("Indexes"));
              deformerNode.Nodes.Add(CreateNode("Weights"));
              OpenTK.Matrix4 boneDataNoPos = model.BoneData[j];
              boneDataNoPos.ClearRotation();
              boneDataNoPos.ClearScale();
              boneDataNoPos.ClearTranslation();
              OpenTK.Matrix4 boneDataNoPos2 = model.BoneData2[j];
              boneDataNoPos2.ClearRotation();
              boneDataNoPos2.ClearScale();
              boneDataNoPos2.ClearTranslation();
              deformerNode.Nodes.Add(CreateMatrix(boneDataNoPos2, "Transform"));
              deformerNode.Nodes.Add(CreateMatrix(boneDataNoPos, "TransformLink"));
              {
                FbxNode relationNode = CreateNode("Model", deformerNode.Properties[0], deformerNode.Properties[1]);
                relationNode.Nodes.Add(null);
                relations.Nodes.Add(relationNode);
              }
              conections.Nodes.Add(CreateNode("Connect", "OO", deformerNode.Properties[0], skinNode.Properties[0]));
              conections.Nodes.Add(CreateNode("Connect", "OO", string.Format("Model::bone{0:X}", model.BoneIDs[j]), deformerNode.Properties[0]));
              Increment(def_deformer);
              Increment(definitions, 1);
              objects.Nodes.Add(deformerNode);
              deformers.Add(deformerNode);
            }
            for(int j = 0; j < bones.Length; ++j) {
              ModelBoneData data = bones[j];
              List<ushort> done = new List<ushort>(data.boneIndex.Length);
              for(int k = 0; k < data.boneIndex.Length; ++k) {
                if(done.Contains(data.boneIndex[k])) {
                  continue;
                }
                done.Add(data.boneIndex[k]);
                ushort bindex = model.BoneLookup[data.boneIndex[k]];
                float bweight = data.boneWeight[k];
                deformers[bindex]["Indexes"].Properties.Add(j);
                deformers[bindex]["Weights"].Properties.Add(bweight);
              }
            }
          }
        }
      }

      if(poseNode != null) {
        objects.Nodes.Add(poseNode);
        Increment(def_pose);
        Increment(definitions, 1);
      }

      definitions.Nodes.Add(def_model);
      definitions.Nodes.Add(def_geometry);
      definitions.Nodes.Add(def_material);
      definitions.Nodes.Add(def_deformer);
      definitions.Nodes.Add(def_pose);
      document.Nodes.Add(definitions);
      document.Nodes.Add(objects);
      document.Nodes.Add(relations);
      document.Nodes.Add(conections);

      return document;
    }
  }
}
