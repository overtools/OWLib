using System.IO;
using OWLib.Types;

namespace OWLib {
    public class ImageDefinition {
        private ImageDefinitionHeader header;
        private ImageLayer[] layers;

        public ImageDefinitionHeader Header => header;
        public ImageLayer[] Layers => layers;

        public ImageDefinition(Stream input) {
            if (input == null) {
                layers = new ImageLayer[0];
                return;
            }
            using (BinaryReader reader = new BinaryReader(input)) {
                header = reader.Read<ImageDefinitionHeader>();
                layers = new ImageLayer[header.textureCount];
                input.Position = (long)header.textureOffset;
                for (int i = 0; i < header.textureCount; ++i) {
                    layers[i] = reader.Read<ImageLayer>();
                }
            }
        }
    }
}
