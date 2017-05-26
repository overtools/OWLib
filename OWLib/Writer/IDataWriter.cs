using System;
using System.Collections.Generic;
using System.IO;
using OWLib.Types;
using OWLib.Types.Map;

namespace OWLib.Writer {
    [Flags]
    public enum WriterSupport : ushort {
        VERTEX = 0x0001,
        UV = 0x0002,
        ATTACHMENT = 0x0004,
        BONE = 0x0008,
        POSE = 0x0010,
        MATERIAL = 0x0020,
        ANIM = 0x0040,
        MODEL = 0x0080,
        REFPOSE = 0x0100,
        MAP = 0x0200,
        MATERIAL_DEF = 0x0400
    };

    public interface IDataWriter {
        WriterSupport SupportLevel {
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
        bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, params object[] data);
        bool Write(Map10 physics, Stream output, params object[] data);
        bool Write(Animation anim, Stream output, params object[] data);
        Dictionary<ulong, List<string>>[] Write(Stream output, Map map, Map detail1, Map detail2, Map props, Map lights, string name, IDataWriter modelFormat);
    }
}
