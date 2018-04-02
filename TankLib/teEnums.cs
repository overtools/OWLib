using System.Diagnostics.CodeAnalysis;

namespace TankLib {
    public class teEnums {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum SDAM : long {
            NONE,
            MUTABLE,
            IMMUTABLE
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum teSHADER_TYPE : ulong {
            VERTEX = 1,
            PIXEL = 2,
            GEOMETRY = 4,
            UnknownA = 16,
            UnknownB = 128,
            COMPUTE = 256
        }
    }
}