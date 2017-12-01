using System.IO;
using OWLib.Types;

namespace OWLib {
    public class ImageDefinition {
        private ImageDefinitionHeader header;
        private ImageLayer[] layers;

        public ImageDefinitionHeader Header => header;
        public ImageLayer[] Layers => layers;

        public enum ImageType : uint {
            Unknown = 0,  // todo: ok?
            DiffuseAO = 2903569922,  // Alpha channel is AO
            DiffuseOpacity = 1239794147,
            DiffuseBlack = 3989656707,  // bad name, use this as fac for own diffuse bdsf
            Normal = 378934698,
            HairNormal = 562391268, // why?
            CorneaNormal = 562391268, // maybe not
            Tertiary = 548341454,  // Metal (R) + Highlight (G) + Detail (B)
            Opacity = 1482859648,
            MaterialMask = 1557393490, // ?
            SubsurfaceScattering = 3004687613,
            Emission = 3166598269,
            HairAnisotropy = 2337956496,
            Specular = 1117188170, // maybe hairspec
            AO = 3761386704,  // maybe hairao
            SomeKindOfGradient = 1140682086
        }

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
