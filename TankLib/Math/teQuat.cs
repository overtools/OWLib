using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>Quaternion</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teQuat {
        /// <summary>X component</summary>
        public float X;
        
        /// <summary>Y component</summary>
        public float Y;
        
        /// <summary>Z component</summary>
        public float Z;
        
        /// <summary>W component</summary>
        public float W;
    }
}