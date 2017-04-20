using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct OWStringHeader {
        public ulong offset;
        public uint size;
        public uint references;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct OWRecord {
        public ulong padding;
        public ulong key;

        public static implicit operator long (OWRecord i) {
            return (long)i.key;
        }

        public static implicit operator ulong (OWRecord i) {
            return i.key;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct GuidT {
        public uint Data1;
        public ushort Data2;
        public ushort Data3;
        public ulong Data4;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ImageDefinitionHeader {
        public ulong offset1;
        public ulong offset2;
        public ulong textureOffset;
        public ulong offset3;
        public uint unk1;
        public ushort unk2;
        public ushort unk3;
        public byte textureCount;
        public byte offset3Count;
        public ushort unk4;
        public uint unk5;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ImageLayer {
        public ulong key;
        public uint unk;
        public uint layer;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vec4 {
        public float x;
        public float y;
        public float z;
        public float w;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vec4d {
        public double x;
        public double y;
        public double z;
        public double w;

        public Vec4d(double x, double y, double z, double w = 1.0) {
            this.x = x; this.y = y; this.z = z; this.w = w;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vec3 {
        public float x;
        public float y;
        public float z;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vec3d {
        public double x;
        public double y;
        public double z;

        public Vec3d(double x, double y, double z) {
            this.x = x; this.y = y; this.z = z;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct MD5Hash {
        public fixed byte Value[16];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct Matrix4B {
        public fixed float Value[16];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct Matrix3x4B {
        public fixed float Value[12];
    }

    public delegate Stream LookupContentByKeyDelegate(ulong key);
    public delegate Stream LookupContentByHashDelegate(MD5Hash hash);

    public enum HeroType : uint {
        OFFENSIVE = 1,
        DEFENSIVE = 2,
        TANK = 3,
        SUPPORT = 4
    }

    public enum MANAGER_ERROR {
        E_SUCCESS = 0x00,
        E_ALREADY_ADDED = 0x01,
        E_FAULT = 0x02,
        E_FAULT_AT_ID = 0x03,
        E_FAULT_AT_NAME = 0x04,
        E_UNKNOWN = 0x05,
        E_GENERIC = 0x06,
        E_DUPLICATE = 0x07
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ChunkedHeader {
        public uint magic;
        public uint identifier;
        public int size;
        public int unk;

        internal string GetIdentifier() {
            return System.Text.Encoding.ASCII.GetString(BitConverter.GetBytes(identifier));
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ChunkedEntry {
        public uint identifier;
        public int unk;
        public int size;
        public uint checksum;

        internal string GetIdentifier() {
            return System.Text.Encoding.ASCII.GetString(BitConverter.GetBytes(identifier));
        }
    }
}
