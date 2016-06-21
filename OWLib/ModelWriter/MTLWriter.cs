using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWLib.Types;

namespace OWLib.ModelWriter {
  public class MTLWriter : IModelWriter {
    public string Name => "Wavefront MTL";
    public string Format => ".mtl";
    public char[] Identifier => new char[1] { 'O' };

    public ModelWriterSupport SupportLevel => ModelWriterSupport.MATERIAL;

    public Stream Write(Model model, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] flags) {
      throw new NotImplementedException();
    }

    public void Write(Model model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] flags) {
      throw new NotImplementedException();
    }
  }
}
