using System.Runtime.InteropServices;

namespace OWLib.Types {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MapHeader {
        public uint recordCount;
        public uint sizes;
        public uint offset;
        public uint unk;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MapPhysicsHeader {
        public MapHeader core;
        public ulong unkOffset1;
        public ulong unkOffset2;
        public ulong unkOffset3;
        public ulong footerOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MapPhysicsFooter {
        public ulong bboxOffset;
        public ulong vertexOffset;
        public ulong indexOffset;
        public uint bboxCount;
        public uint vertexCount;
        public uint indexCount;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct MapVec3 {
        public float x;
        public float y;
        public float z;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct MapQuat {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MapPhysicsVertex {
        public MapVec3 position;
        public float unk;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MapPhysicsIndex {
        public MapPhysicsSlice3 index;
        public MapPhysicsSlice3 unk1;
        public MapPhysicsSlice2 unk2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MapPhysicsSlice3 {
        public int v1;
        public int v2;
        public int v3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MapPhysicsSlice2 {
        public int v1;
        public int v2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MapPhysicsBoundingBox {
        public MapVec3 minimum;
        public MapVec3 maximum;
        public uint unk1;
        public uint unk2;
    }
  
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MapCommonHeader {
        public GuidT guid;
        public ushort mask;
        public byte unk;
        public byte type;
        public uint size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MapEnvironment
    {
        public Matrix3x4B M0;
        public Matrix4B M1;
        public Matrix4B M2;
        public Matrix4B M3;

        public ulong EntityDefinition;
        public ulong SkyEnvironmentCubemap;
        public ulong BakedLighting;
        public ulong BakedShadow;
        public ulong LUT;
        public ulong SkyboxModel;
        public ulong SkyboxModelLook;
        public ulong MapEnvironmentSound; // 055 file.
        public ulong GroundEnvironmentCubemap;
        public ulong BlendEnvironmentCubemap;
        public ulong Text;
        public ulong UnknownGUID1;

        public Matrix3x4B M4;
        public Matrix4B M5;
        public Matrix4B M6;
        public Matrix3x4B M7;

        public ulong UnknownGUID2;
    }
}
