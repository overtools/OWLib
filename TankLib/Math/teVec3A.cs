using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>3 component XYZ vector</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teVec3A {
        /// <summary>X component</summary>
        public float X;
        
        /// <summary>Y component</summary>
        public float Y;
        
        /// <summary>Z component</summary>
        public float Z;
    }
}