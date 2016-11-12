using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OWLib.Types;
using OWLib.Types.Map;

namespace OWLib.ModelWriter {
  public class OBJWriter : IModelWriter {
    public string Name => "Wavefront OBJ";
    public string Format => ".obj";
    public char[] Identifier => new char[1] { 'o' };
    public ModelWriterSupport SupportLevel => (ModelWriterSupport.VERTEX | ModelWriterSupport.UV | ModelWriterSupport.MATERIAL);

    public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] opts) {
      // TODO
      return false;
    }

    public bool Write(Map10 physics, Stream output, object[] data) {
      Console.Out.WriteLine("Writing OBJ");
      using(StreamWriter writer = new StreamWriter(output)) {
        writer.WriteLine("o Physics");

        for(int i = 0; i < physics.Vertices.Length; ++i) {
          writer.WriteLine("v {0} {1} {2}", physics.Vertices[i].position.x, physics.Vertices[i].position.y, physics.Vertices[i].position.z);
        }

        for(int i = 0; i < physics.Indices.Length; ++i) {
          writer.WriteLine("f {0} {1} {2}", physics.Indices[i].index.v1, physics.Indices[i].index.v2, physics.Indices[i].index.v3);
        }
      }
      return false;
    }
  }
}
