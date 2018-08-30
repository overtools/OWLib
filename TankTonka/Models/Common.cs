using System.Runtime.Serialization;
using TankTonka.Formatters;
using Utf8Json;

namespace TankTonka.Models {
    public static class Common {
        public enum AssetRepoType : ushort {
            Texture = 0x4,
            Anim = 0x6,
            Material = 0x8,
            Model = 0xC,
            Effect = 0xD,
            DataFlow = 0x13,
            AnimAlias = 0x14,
            AnimCategory = 0x15,
            AnimWeightBoneMask = 0x18,
            ModelLook = 0x1A,
            StatescriptGraph = 0x1B
        }

        public class StructuredDataInfo {
            [DataMember(Name = "instances")]
            [JsonFormatter(typeof(FourCCArrayFormatter))]
            public uint[] Instances;
        }

        public class ChunkedDataInfo {
            [DataMember(Name = "chunks")]
            [JsonFormatter(typeof(FourCCArrayFormatter))]
            public uint[] Chunks;
        }
    }
}