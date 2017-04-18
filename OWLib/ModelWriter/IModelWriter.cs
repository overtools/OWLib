using System.Collections.Generic;
using System.IO;
using OWLib.Types;
using OWLib.Types.Map;

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
        ModelWriterSupport SupportLevel {
            get;
        }

        char[] Identifier {
            get;
        }

        string Format {
            get;
        }

        string Name {
            get;
        }

        // data is object[] { bool exportAttachments, string materialReference, string modelName, bool onlyOneLOD, bool skipCollision }
        bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data);
        bool Write(Map10 physics, Stream output, object[] data);
    }
}
