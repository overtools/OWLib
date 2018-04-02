using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>3 component XYZ vector</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DebuggerDisplay("X: {X}, Y: {Y}, Z: {Z}")]
    public struct teVec3A {
        /// <summary>X component</summary>
        public float X;
        
        /// <summary>Y component</summary>
        public float Y;
        
        /// <summary>Z component</summary>
        public float Z;

        public teVec3A(float x, float y, float z) {
            X = x;
            Y = y;
            Z = z;
        }
    }
}