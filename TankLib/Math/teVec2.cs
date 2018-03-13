using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>2 component XY vector</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teVec2 {
        /// <summary>X component</summary>
        public float X;
        
        /// <summary>Y component</summary>
        public float Y;
    }
}