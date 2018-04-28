using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>4x4 matrix</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DebuggerDisplay("{" + nameof(DebugString) + "}")]
    public struct teMtx44 {
        public float M01;
        public float M02;
        public float M03;
        public float M04;
        
        public float M05;
        public float M06;
        public float M07;
        public float M08;
        
        public float M09;
        public float M10;
        public float M11;
        public float M12;
        
        public float M13;
        public float M14;
        public float M15;
        public float M16;

        public string DebugString => $"{M01:F3} {M02:F3} {M03:F3} {M04:F3}\r\n" +
                                     $"{M05:F3} {M06:F3} {M07:F3} {M08:F3}\r\n" +
                                     $"{M09:F3} {M10:F3} {M11:F3} {M12:F3}\r\n" +
                                     $"{M13:F3} {M14:F3} {M15:F3} {M16:F3}";
    }
}