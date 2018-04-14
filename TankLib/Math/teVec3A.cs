using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>4 component XYZA vector</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DebuggerDisplay("X: {X}, Y: {Y}, Z: {Z}, A: {A}")]
    public struct teVec3A {
        /// <summary>X component</summary>
        public float X;
        
        /// <summary>Y component</summary>
        public float Y;
        
        /// <summary>Z component</summary>
        public float Z;
        
        /// <summary>A component</summary>
        public float A;

        public teVec3A(float x, float y, float z, float a) {
            X = x;
            Y = y;
            Z = z;
            A = a;
        }
    }
}