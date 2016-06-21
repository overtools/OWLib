using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fbx;
using OWLib.Types;

namespace OWLib.ModelWriter {
  public class FBXASCIIWriter : IModelWriter {
    public string Name => "Autodesk FBX ASCII";
    public string Format => ".fbx";
    public char[] Identifier => new char[1] { 'f' };
    public ModelWriterSupport SupportLevel => (ModelWriterSupport.VERTEX | ModelWriterSupport.UV | ModelWriterSupport.ATTACHMENT);

    
    public Stream Write(Model model, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] flags) {
      MemoryStream stream = new MemoryStream();
      Write(model, stream, LODs, layers, flags);
      return stream;
    }

    public void Write(Model model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] flags) {
      FbxDocument document = FBXCommon.Write(model, LODs, flags);
      FbxAsciiWriter writer = new FbxAsciiWriter(output);
      writer.Write(document);
    }
  }
}
