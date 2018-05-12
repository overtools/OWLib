using System;
using System.Diagnostics.CodeAnalysis;

namespace TankLib {
    public static class Enums {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum SDAM : long {
            NONE,
            MUTABLE,
            IMMUTABLE
        }

        [Flags]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum teSHADER_TYPE : ulong {
            VERTEX = 1,
            PIXEL = 2,
            GEOMETRY = 4,
            UNKNOWN_A = 8,
            UNKNOWN_B = 16,
            UNKNOWN_C = 128,
            COMPUTE = 256
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum teMAP_PLACEABLE_TYPE : byte {
            UNKNOWN = 0,
            MODEL_GROUP = 0x1,
            SINGLE_MODEL = 0x2,
            OCCLUDER = 0x3,
            REFLECTIONPOINT = 0x4,
            LABEL = 0x6,
            TEXT = 0x7,
            
            LIGHT = 0x9,  // not confirmed
            
            ENTITY = 0xB
        }
    }
}