using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWLib.Types;

namespace OWLib {
  public class Material {
    private MaterialHeader header;

    public MaterialHeader Header => header;

    public Material(Stream input) {
      using(BinaryReader reader = new BinaryReader(input)) {
        header = reader.Read<MaterialHeader>();
      }
    }
  }
}
