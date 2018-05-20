using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>4x4 matrix</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DebuggerDisplay("{" + nameof(DebugString) + "}")]
    public struct teMtx44 {
        public float M11;
        public float M12;
        public float M13;
        public float M14;
        
        public float M21;
        public float M22;
        public float M23;
        public float M24;
        
        public float M31;
        public float M32;
        public float M33;
        public float M34;
        
        public float M41;
        public float M42;
        public float M43;
        public float M44;

        public string DebugString => $"{M11:F3} {M12:F3} {M13:F3} {M14:F3}\r\n" +
                                     $"{M21:F3} {M22:F3} {M23:F3} {M24:F3}\r\n" +
                                     $"{M31:F3} {M32:F3} {M33:F3} {M34:F3}\r\n" +
                                     $"{M41:F3} {M42:F3} {M43:F3} {M44:F3}";

        public teMtx44 Transpose() {
            teMtx44 @out = new teMtx44 {
                M11 = M11,
                M12 = M21,
                M13 = M31,
                M14 = M41,
                M21 = M12,
                M22 = M22,
                M23 = M32,
                M24 = M42,
                M31 = M13,
                M32 = M23,
                M33 = M33,
                M34 = M43,
                M41 = M14,
                M42 = M24,
                M43 = M34,
                M44 = M44
            };

            return @out;
        }

        public static teMtx44 Identity() {
            return new teMtx44 {
                M11 = 1, M22 = 1, M33 = 1, M44 = 1
            };
        }
    }
}