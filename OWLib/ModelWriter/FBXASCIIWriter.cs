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

    public void Write(Model model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] opts) {
      FbxDocument document = FBXCommon.Write(model, LODs, opts);
      FbxAsciiWriter writer = new FbxAsciiWriter(output);
      writer.Write(document);
    }
  }
}
