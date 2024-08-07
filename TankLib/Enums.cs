﻿using System;
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
        public enum teSHADER_TYPE : ushort {
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
            // luckily they don't re-use chunk ids...

            UNKNOWN = 0,
            MODEL_GROUP = 0x1,
            SINGLE_MODEL = 0x2,
            OCCLUDER = 0x3,
            REFLECTIONPOINT = 0x4,
            CLUTTER = 0x5,
            LABEL = 0x6,
            TEXT = 0x7,
            MODEL = 0x8,
            LIGHT = 0x9,
            AREA = 0xA,
            ENTITY = 0xB,
            SOUND = 0xC,
            EFFECT = 0xD,
            FOG = 0xE,
            INDOORBOX = 0xF,
            POSTPROCESSING = 0x10,
            PLANAR_REFLECTION_SURFACE = 0x11,
            // MAP_REGION = 0x12,
            // SOUNDAREA = 0x13
            // COLLISION = 0x14, ??
            // PATHING = 0x15,
            
            SEQUENCE = 0x18
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum teLIGHTTYPE : uint {
            POINT = 0,
            FRUSTUM = 1,
            NONE = 2
        }
    }
}