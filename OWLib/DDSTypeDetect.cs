using OWLib.Types;

namespace OWLib {
    public class DDSTypeDetect {
        public enum DDSType : byte {
            ALBEDO = 0x00,
            NORMAL = 0x01,
            SHADER = 0x02
        }

        public static DDSType Detect(TextureType typ) {
            if (typ == TextureType.ATI1) { // Effect, highlights, etc.
                return DDSType.SHADER;
            } else if (typ == TextureType.ATI2) { // Normal maps 99% of the time.
                return DDSType.NORMAL;
            } else { // DXT1, DXT5
                return DDSType.ALBEDO;
            }
        }
    }
}
