using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OWLib.Types;
using System.Threading.Tasks;

namespace OWLib {
  public class ImageDefinition {
    private ImageDefinitionHeader header;
    private ImageLayer[] layers;

    public ImageDefinitionHeader Header => header;
    public ImageLayer[] Layers => layers;

    public ImageDefinition(Stream input) {
      using(BinaryReader reader = new BinaryReader(input)) {
        header = reader.Read<ImageDefinitionHeader>();
        layers = new ImageLayer[header.textureCount];
        input.Position = (long)header.textureOffset;
        for(int i = 0; i < header.textureCount; ++i) {
          layers[i] = reader.Read<ImageLayer>();
        }
      }
    }
  }
}
