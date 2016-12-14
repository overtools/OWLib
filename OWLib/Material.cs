using System.IO;
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
