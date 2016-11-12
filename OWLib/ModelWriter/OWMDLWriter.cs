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

    public bool Write(Map10 physics, Stream output, object[] data) {
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
      return true;
    }

    public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data) {
      return false;
      Console.Out.WriteLine("Writing OWMDL");
      using(BinaryWriter writer = new BinaryWriter(output)) {
        writer.Write((ushort)1); // version major
        writer.Write((ushort)1); // version minor

        if(data.Length > 1 && data[1] != null && data[1].GetType() == typeof(string) && ((string)data[1]).Length > 0) {
          writer.Write((string)data[1]);
        } else {
          writer.Write((byte)0);
        }

        if(data.Length > 2 && data[2] != null && data[2].GetType() == typeof(string) && ((string)data[2]).Length > 0) {
          writer.Write((string)data[2]);
        } else {
          writer.Write((byte)0);
        }

        // TODO
      }
    }
  }
}
