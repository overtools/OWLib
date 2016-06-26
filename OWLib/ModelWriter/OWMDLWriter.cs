using System;
using System.Collections.Generic;
using System.IO;
using OWLib.Types;
using OWLib.Types.Map;

namespace OWLib.ModelWriter {
  public class OWMDLWriter : IModelWriter {
    public string Format => ".owmdl";

    public char[] Identifier => new char[1] { 'w' };
    public string Name => "OWM Model Format";

    public ModelWriterSupport SupportLevel => (ModelWriterSupport.VERTEX | ModelWriterSupport.UV | ModelWriterSupport.BONE | ModelWriterSupport.POSE | ModelWriterSupport.MATERIAL | ModelWriterSupport.ATTACHMENT);

    public void Write(Map10 physics, Stream output, object[] data) {
      Console.Out.WriteLine("Writing OWMDL");
      using(BinaryWriter writer = new BinaryWriter(output)) {
        writer.Write((ushort)1);
        writer.Write((ushort)0);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)0);
        writer.Write(1);
        writer.Write(0);

        writer.Write("PhysicsModel");
        writer.Write(0L);
        writer.Write((byte)0);
        writer.Write(physics.Vertices.Length);
        writer.Write(physics.Indices.Length);
        
        for(int i = 0; i < physics.Vertices.Length; ++i) {
          writer.Write(physics.Vertices[i].position.x);
          writer.Write(physics.Vertices[i].position.y);
          writer.Write(physics.Vertices[i].position.z);
          writer.Write(0.0f);
          writer.Write(0.0f);
          writer.Write(0.0f);
          writer.Write((byte)0);
        }

        for(int i = 0; i < physics.Indices.Length; ++i) {
          writer.Write((byte)3);
          writer.Write(physics.Indices[i].index.v1);
          writer.Write(physics.Indices[i].index.v2);
          writer.Write(physics.Indices[i].index.v3);
        }
      }
    }

    public void Write(Model model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data) {
      Console.Out.WriteLine("Writing OWMDL");
      using(BinaryWriter writer = new BinaryWriter(output)) {
        writer.Write((ushort)1); // version major
        writer.Write((ushort)0); // version minor

        if(data.Length > 1 && data[1] != null && data[1].GetType() == typeof(string)) {
          writer.Write((string)data[1]);
        } else {
          writer.Write((byte)0);
        }

        if(data.Length > 2 && data[2] != null && data[2].GetType() == typeof(string)) {
          writer.Write((string)data[2]);
        } else {
          writer.Write((byte)0);
        }

        writer.Write((ushort)model.BoneData.Length); // nr bones
        
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

        writer.Write(sz); // nr meshes

        if(data.Length > 0 && data[0] != null && data[0].GetType() == typeof(bool) && (bool)data[0] == true) {
          writer.Write(model.AttachmentPoints.Length); // nr empties
        } else {
          writer.Write((int)0);
        }
        
        for(int i = 0; i < model.BoneData.Length; ++i) {
          writer.Write(string.Format("bone{0:X}", model.BoneIDs[i]));
          short parent = model.BoneHierarchy[i];
          if(parent == -1) {
            parent = (short)i;
          }
          writer.Write(parent);
          OpenTK.Vector3 bonePos = model.BoneData[i].ExtractTranslation();
          OpenTK.Quaternion boneRot = model.BoneData[i].ExtractRotation();
          OpenTK.Vector3 boneScl = model.BoneData[i].ExtractScale();
          writer.Write(bonePos.X);
          writer.Write(bonePos.Y);
          writer.Write(bonePos.Z);
          writer.Write(boneScl.X);
          writer.Write(boneScl.Y);
          writer.Write(boneScl.Z);
          writer.Write(boneRot.X);
          writer.Write(boneRot.Y);
          writer.Write(boneRot.Z);
          writer.Write(boneRot.W);
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
            writer.Write(string.Format("Submesh_{0}.{1}.{2:X16}", i, kv.Key, model.MaterialKeys[submesh.material]));
            writer.Write(model.MaterialKeys[submesh.material]);
            writer.Write((byte)uv.Length);
            writer.Write(vertex.Length);
            writer.Write(index.Length);
            for(int j = 0; j < vertex.Length; ++j) {
              writer.Write(vertex[j].x);
              writer.Write(vertex[j].y);
              writer.Write(vertex[j].z);
              writer.Write(-normal[j].x);
              writer.Write(-normal[j].y);
              writer.Write(-normal[j].z);
              for(int k = 0; k < uv.Length; ++k) {
                writer.Write((float)uv[k][j].u);
                writer.Write((float)uv[k][j].v);
              }
              if(model.BoneData.Length > 0 && bones != null) {
                writer.Write((byte)4);
                writer.Write(model.BoneLookup[bones[j].boneIndex[0]]);
                writer.Write(model.BoneLookup[bones[j].boneIndex[1]]);
                writer.Write(model.BoneLookup[bones[j].boneIndex[2]]);
                writer.Write(model.BoneLookup[bones[j].boneIndex[3]]);
                writer.Write(bones[j].boneWeight[0]);
                writer.Write(bones[j].boneWeight[1]);
                writer.Write(bones[j].boneWeight[2]);
                writer.Write(bones[j].boneWeight[3]);
              } else {
                writer.Write((byte)0);
              }
            }
            for(int j = 0; j < index.Length; ++j) {
              writer.Write((byte)3);
              writer.Write((int)index[j].v1);
              writer.Write((int)index[j].v2);
              writer.Write((int)index[j].v3);
            }
          }
        }

        if(data.Length > 0 && data[0] != null && data[0].GetType() == typeof(bool) && (bool)data[0] == true) {
          for(uint i = 0; i < model.AttachmentPoints.Length; ++i) {
            ModelAttachmentPoint attachment = model.AttachmentPoints[i];
            writer.Write(string.Format("Attachment{0:X}", attachment.id));
            OpenTK.Matrix4 mat = attachment.matrix.ToOpenTK();
            OpenTK.Vector3 pos = mat.ExtractTranslation();
            OpenTK.Quaternion quat = mat.ExtractRotation();
            writer.Write(pos.X);
            writer.Write(pos.Y);
            writer.Write(pos.Z);
            writer.Write(quat.X);
            writer.Write(quat.Y);
            writer.Write(quat.Z);
            writer.Write(quat.W);
          }
        }
      }
    }
  }
}
