using System.Runtime.Serialization;
using TankTonka.Formatters;
using Utf8Json;

namespace TankTonka.Models {
    public static class Common {
        public enum AssetRepoType : ushort {
            Texture = 0x4,
            Anim = 0x6,
            Material = 0x8,
            Model = 0xC
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