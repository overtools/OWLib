using System;
using System.Runtime.InteropServices;

namespace OWLib.Types {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ModelIndice {
        public ushort v1;
        public ushort v2;
        public ushort v3;
    }

    public class ModelIndiceModifiable {
        public int v1;
        public int v2;
        public int v3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ModelUV {
        public Half u;
        public Half v;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct ModelVertex {
        public float x;
        public float y;
        public float z;
    }
  
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ModelBoneData {
        public ushort[] boneIndex;
        public float[] boneWeight;
    }
}
