using System.Collections.Generic;
using System.IO;
using OWLib.Types;

namespace OWLib.ModelWriter {
  public enum ModelWriterSupport : ushort {
    VERTEX = 0x0001,
    UV = 0x0002,
    ATTACHMENT = 0x0004,
    BONE = 0x0008,
    POSE = 0x0010,
    MATERIAL = 0x0020
  };

  public interface IModelWriter {
    ModelWriterSupport SupportLevel
    {
      get;
    }

    char[] Identifier
    {
      get;
    }

    string Format
    {
      get;
    }

    string Name
    {
      get;
    }

    Stream Write(Model model, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data);
    void Write(Model model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data);
  }
}
