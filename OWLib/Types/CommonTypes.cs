using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OWLib.Types {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct OWStringHeader {
        public ulong offset;
        public uint unk1;
        public uint references;
    }

    // Overwatch somewhy uses 128-bit integers for offsets.
    // I really should've done this when I started.
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct ulonglong {
        public ulong Upper;
        public ulong Lower;

        public static implicit operator ulong(ulonglong i) {
            return i.Upper;
        }

        public static implicit operator long(ulonglong i) {
            return (long)i.Upper;
        }

        public BigInteger ToBigInt() {
            BigInteger bi = new BigInteger(Lower);
            return (bi << 64) + Upper;
        }

        public new string ToString() {
            return ToBigInt().ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct UUID {
        public uint A;
        public ushort B;
        public ushort C;
        public ulong D;

        public new string ToString() {
            return $"{A:X8}-{B:X4}-{C:X4}-{D:X16}";
        }
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

        public new string ToString() {
            return $"{GUID.LongKey(key):X12}.{GUID.Type(key):X3}";
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
        public ulong Key;
        public ImageDefinition.ImageType Type;
        public uint Flags;
        
        public ulong DataKey => (Key & 0xFFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vec3 {
        public float x;
        public float y;
        public float z;

        public Vec3(float ix, float iy, float iz) {
            x = ix;
            y = iy;
            z = iz;
        }
        public Vec3(double ix, double iy, double iz) {
            x = (float)ix;
            y = (float)iy;
            z = (float)iz;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vec3d {
        public double x;
        public double y;
        public double z;

        public Vec3d(double x, double y, double z) {
            this.x = x; this.y = y; this.z = z;
        }
        public Vec3d(float ix, float iy, float iz) {
            x = ix;
            y = iy;
            z = iz;
        }
        public Vec3d toDegrees() {
            Vec3d r = new Vec3d();

            // To degrees
            r.x = (x / Math.PI) * 180.0;
            r.y = (y / Math.PI) * 180.0;
            r.z = (z / Math.PI) * 180.0;

            return r;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vec4 {
        public float x;
        public float y;
        public float z;
        public float w;

        public Vec4(float ix, float iy, float iz, float iw = 0.0f) {
            x = ix;
            y = iy;
            z = iz;
            w = iw;
        }
        public Vec4(double ix, double iy, double iz, double iw = 0.0) {
            x = (float)ix;
            y = (float)iy;
            z = (float)iz;
            w = (float)iw;
        }
        public Vec4(Vec3 iv) {
            x = iv.x;
            y = iv.y;
            z = iv.z;
            w = 1.0f;
        }
        public static implicit operator Vec4(Vec3 v) {
            return new Vec4(v);
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vec4d {
        public double x;
        public double y;
        public double z;
        public double w;

        public Vec4d(double x, double y, double z, double w = 0.0) {
            this.x = x; this.y = y; this.z = z; this.w = w;
        }
        public Vec4d(float ix, float iy, float iz, float iw = 0.0f) {
            x = ix;
            y = iy;
            z = iz;
            w = iw;
        }
        public Vec4d(Vec3d iv) {
            x = iv.x;
            y = iv.y;
            z = iv.z;
            w = 0.0;
        }
        public static implicit operator Vec4d(Vec3d v) {
            return new Vec4d(v);
        }

        public Vec4d fromEuler(double yaw, double pitch, double roll) {
            double num = roll * 0.5f;
            double num2 = Math.Sin((double)num);
            double num3 = Math.Cos((double)num);
            double num4 = pitch * 0.5f;
            double num5 = Math.Sin((double)num4);
            double num6 = Math.Cos((double)num4);
            double num7 = yaw * 0.5f;
            double num8 = Math.Sin((double)num7);
            double num9 = Math.Cos((double)num7);
            Vec4d result;
            result.x = num9 * num5 * num3 + num8 * num6 * num2;
            result.y = num8 * num6 * num3 - num9 * num5 * num2;
            result.z = num9 * num6 * num2 - num8 * num5 * num3;
            result.w = num9 * num6 * num3 + num8 * num5 * num2;
            return result;
        }
        public Vec4d fromEuler(Vec3d input) {
            return fromEuler(input.y, input.x, input.z);
        }

        public Vec3d toEuler() {
            double[,] matrix = new double[4, 4];
            {
                double num = x * x + y * y + z * z + w * w;
                double num2;
                if (num > 0.0) {
                    num2 = 2.0 / num;
                } else {
                    num2 = 0.0;
                }
                double num3 = x * num2;
                double num4 = y * num2;
                double num5 = z * num2;
                double num6 = w * num3;
                double num7 = w * num4;
                double num8 = w * num5;
                double num9 = x * num3;
                double num10 = x * num4;
                double num11 = x * num5;
                double num12 = y * num4;
                double num13 = y * num5;
                double num14 = z * num5;
                matrix[0, 0] = 1.0 - (num12 + num14);
                matrix[0, 1] = num10 - num8;
                matrix[0, 2] = num11 + num7;
                matrix[1, 0] = num10 + num8;
                matrix[1, 1] = 1.0 - (num9 + num14);
                matrix[1, 2] = num13 - num6;
                matrix[2, 0] = num11 - num7;
                matrix[2, 1] = num13 + num6;
                matrix[2, 2] = 1.0 - (num9 + num12);
                matrix[3, 3] = 1.0;
            }
            {
                int i = 0;
                int j = 1;
                int k = 2;
                double[,] M = matrix;
                Vec3d vec = new Vec3d();
                double num2 = Math.Sqrt(M[i, i] * M[i, i] + M[j, i] * M[j, i]);
                if (num2 > 0.00016) {
                    vec.x = (float)Math.Atan2(M[k, j], M[k, k]);
                    vec.y = (float)Math.Atan2(-M[k, i], num2);
                    vec.z = (float)Math.Atan2(M[j, i], M[i, i]);
                } else {
                    vec.x = (float)Math.Atan2(-M[j, k], M[j, j]);
                    vec.y = (float)Math.Atan2(-M[k, i], num2);
                    vec.z = 0f;
                }
                return vec;
            }
        }
        public static Vec4d operator +(Vec4d vector1, Vec4d vector2) {
            Vec4d val;
            val.x = vector1.x + vector2.x;
            val.y = vector1.y + vector2.y;
            val.z = vector1.z + vector2.z;
            val.w = vector1.w + vector2.w;
            return val;
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

        public string StringIdentifier => System.Text.Encoding.ASCII.GetString(BitConverter.GetBytes(identifier));

        public new string ToString() {
            return base.ToString() + $" ({StringIdentifier})";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ChunkedEntry {
        public uint identifier;
        public int unk;
        public int size;
        public uint checksum;

        public string StringIdentifier => System.Text.Encoding.ASCII.GetString(BitConverter.GetBytes(identifier));

        public new string ToString() {
            return base.ToString() + $" ({StringIdentifier})";
        }
    }
}
